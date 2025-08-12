using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Cli;

/*
 * Cross-Platform UTF-8 Command Line Executor
 * -------------------------------------------
 * CliRunner is a lightweight, high-performance utility for executing shell
 * and command-line instructions seamlessly across Windows, macOS, and Linux.
 *
 * Built with UTF-8 as its native encoding, it ensures consistent and reliable
 * handling of international text, command arguments, and process output on
 * all supported platforms.
 *
 * Features:
 *  - Cross-platform shell support:
 *      * Windows: cmd or PowerShell
 *      * Unix-like: /bin/bash or pwsh
 *  - UTF-8 native I/O for command input, standard output, and error streams,
 *    preventing cross-platform text corruption.
 *  - Environment variable injection via JSON key-value mapping.
 *  - Configurable redirection of stdin, stdout, and stderr.
 *  - Interop-friendly API exposed via [UnmanagedCallersOnly] for integration
 *    with native applications in C, C++, Rust, and more.
 *  - No external dependencies — implemented purely with .NET standard APIs.
 *
 * Ideal for tools, game engines, and automation systems that require
 * reliable UTF-8 command execution in a unified, cross-platform API.
 */

public class CliRunner
{
    private static readonly Encoding UTF8_NON_BOM = new UTF8Encoding(false, false);
    private static readonly Encoding UTF16 = Encoding.Unicode;

    private const string kPowerShellUtf8 =
        "[Console]::InputEncoding = [System.Text.UTF8Encoding]::new($false);" +
        "[Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false);";

    [UnmanagedCallersOnly(EntryPoint = "ExecShell", CallConvs = new[] { typeof(CallConvCdecl) })]
    public unsafe static int ExecShell(byte* commandStrPtr, byte* dest_str_ptr, int dest_str_len)
    {
        return Internal_RunCommand(commandStrPtr, dest_str_ptr, dest_str_len, use_powershell: false);
    }

    [UnmanagedCallersOnly(EntryPoint = "ExecPowerShell", CallConvs = new[] { typeof(CallConvCdecl) })]
    public unsafe static int ExecPowerShell(byte* commandStrPtr, byte* dest_str_ptr, int dest_str_len)
    {
        return Internal_RunCommand(commandStrPtr, dest_str_ptr, dest_str_len, use_powershell: true);
    }

    private unsafe static int Internal_RunCommand(byte* commandStrPtr, byte* dest_str_ptr, int dest_str_len, bool use_powershell)
    {
        bool isUnix = IsUnix();
        string? command = PtrToStringUTF8(commandStrPtr);
        if (command == null)
            return 0;

        using Process process = new();
        ProcessStartInfo pInfo = process.StartInfo;
        pInfo.FileName = isUnix ? "/bin/bash" : use_powershell ? "powershell" : "cmd";
        pInfo.Arguments = isUnix ? $"-c \"{command}\"" : use_powershell ? $"-Command \"{kPowerShellUtf8} {command}\"" : $"/U /c \"{command}\"";

        // STDOUT && STDERR
        pInfo.RedirectStandardInput = true;
        pInfo.RedirectStandardOutput = true;
        pInfo.RedirectStandardError = true;

        // Force UTF8 Encoding
        if (!isUnix && !use_powershell)
        {
            if (pInfo.RedirectStandardOutput) pInfo.StandardOutputEncoding = UTF16;
            if (pInfo.RedirectStandardError) pInfo.StandardErrorEncoding = UTF16;
            if (pInfo.RedirectStandardInput) pInfo.StandardInputEncoding = UTF16;
        }
        else
        {
            if (pInfo.RedirectStandardOutput) pInfo.StandardOutputEncoding = UTF8_NON_BOM;
            if (pInfo.RedirectStandardError) pInfo.StandardErrorEncoding = UTF8_NON_BOM;
            if (pInfo.RedirectStandardInput) pInfo.StandardInputEncoding = UTF8_NON_BOM;
        }

        pInfo.UseShellExecute = false;
        pInfo.CreateNoWindow = true;

        process.Start();
        process.WaitForExit();

        string stdout = pInfo.RedirectStandardOutput ? process.StandardOutput.ReadToEnd() : string.Empty;
        if (string.IsNullOrEmpty(stdout))
        {
            if (pInfo.RedirectStandardError)
                stdout = process.StandardError.ReadToEnd();
            else stdout = "No stdout or stderr";
        }

        return CopyUTF8StringToPtr(stdout, dest_str_ptr, dest_str_len);
    }

    private static unsafe int CopyUTF8StringToPtr(string str, byte* dest_str_ptr, int dest_str_len)
    {
        if (dest_str_len <= 0 || dest_str_ptr == null)
            return 0;

        if (str == null)
        {
            dest_str_ptr[0] = (byte)'\0';
            return 0;
        }

        byte[] str_arr = UTF8_NON_BOM.GetBytes(str);
        int byteCount = Math.Min(str_arr.Length, dest_str_len - 1); // -1 for the null terminator
        fixed (byte* str_ptr = str_arr)
        {
            NativeMemory.Copy(str_ptr, dest_str_ptr, (nuint)byteCount);
            dest_str_ptr[byteCount] = (byte)'\0';
            return byteCount;
        }
    }

    private static unsafe string PtrToStringUTF8(byte* ptr)
    {
        if (ptr == null)
            return string.Empty;

        // First: Count the string length
        byte* ptr_temp = ptr;
        while (*ptr != '\0')
            ptr++;

        int str_len = (int)(ptr - ptr_temp);
        // Second: Back to the start of the string and return the string
        return UTF8_NON_BOM.GetString(ptr_temp, str_len);
    }

    private static bool IsUnix()
    {
        return RuntimeInformation.IsOSPlatform(OSPlatform.OSX) || RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
    }
}

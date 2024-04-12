using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

#pragma warning disable IDE0063

namespace Cli;

public class CliRunner
{
    [UnmanagedCallersOnly(EntryPoint = "Run")]
    public unsafe static int Run(char* command_ptr, char* output_ptr, int output_buffer_size, bool wait_for_exit, bool redirect_stdin, bool redirect_stdout, bool redirect_stderr, bool use_shell, bool create_no_window)
    {
        using (Process process = new())
        {
            string           command         = new(command_ptr);
            ProcessStartInfo pInfo           = process.StartInfo;
                             pInfo.FileName  = IsUnix() ? "/bin/bash" : "cmd";
                             pInfo.Arguments = IsUnix() ? $"-c \"{command}\"" : $"/c \"{command}\"";

              // STDOUT && STDERR
            pInfo.RedirectStandardInput  = redirect_stdin;
            pInfo.RedirectStandardOutput = redirect_stdout;
            pInfo.RedirectStandardError  = redirect_stderr;

              // Command running in background.
            pInfo.UseShellExecute = use_shell;
            pInfo.CreateNoWindow  = create_no_window;

              // Start the command and wait for it to finish!
            process.Start();
            if (wait_for_exit) process.WaitForExit();

            string stdout = pInfo.RedirectStandardOutput ? process.StandardOutput.ReadToEnd() : string.Empty;
            if (string.IsNullOrEmpty(stdout))
            {
                if   (pInfo.RedirectStandardError) stdout = process.StandardError.ReadToEnd();
                else stdout                               = "No stdout or stderr";
            }

            fixed (char* stdout_ptr = stdout)
            {
                    output_buffer_size *= sizeof(char);
                int byte_count          = Encoding.UTF8.GetByteCount(stdout) * sizeof(char);
                int strlen              = Math.Min(output_buffer_size, byte_count);
                Buffer.MemoryCopy(stdout_ptr, output_ptr, output_buffer_size, strlen);
            }
            return stdout.Length;
        }
    }

    private static bool IsUnix()
    {
        return RuntimeInformation.IsOSPlatform(OSPlatform.OSX) || RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
    }
}
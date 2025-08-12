#pragma warning disable
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Unicode;

// Default encoding is UTF-8, null-terminated strings

public class CliRunner
{
    private static readonly Encoding UTF8 = Encoding.UTF8;
    private const string DllName = "CliRunner.dll";

    [DllImport(DllName, EntryPoint = "ExecShell", CallingConvention = CallingConvention.Cdecl)]
    private static unsafe extern int Internal_ExecShell(byte* commandStrPtr, byte* dest_str_ptr, int dest_str_len); // UTF8 + null-terminated strings

    [DllImport(DllName, EntryPoint = "ExecPowerShell", CallingConvention = CallingConvention.Cdecl)]
    private static unsafe extern int Internal_ExecPowerShell(byte* commandStrPtr, byte* dest_str_ptr, int dest_str_len); // UTF8 + null-terminated strings

    private static unsafe string ExecShell(string command, int buffer_len)
    {
        ReadOnlySpan<byte> command_utf8_arr = UTF8.GetBytes(command);
        Span<byte> command_utf8_arr_null_terminated = new byte[command_utf8_arr.Length + 1]; // +1 for null terminator

        command_utf8_arr.CopyTo(command_utf8_arr_null_terminated);
        command_utf8_arr_null_terminated[command_utf8_arr_null_terminated.Length - 1] = (byte)'\0';

        fixed (byte* command_utf8_arr_null_terminated_ptr = command_utf8_arr_null_terminated)
        {
            fixed (byte* dest_str_ptr = new byte[buffer_len])
            {
                int size = Internal_ExecPowerShell(command_utf8_arr_null_terminated_ptr, dest_str_ptr, buffer_len);
                return UTF8.GetString(dest_str_ptr, size);
            }
        }
    }

    public static unsafe void Main()
    {
        Console.WriteLine(ExecShell("echo Configuração", 1024));
    }
}
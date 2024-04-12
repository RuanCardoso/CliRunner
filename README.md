Exemplo de uso:


```
[DllImport("CliRunner.so", EntryPoint = "Run")]
public unsafe static extern int Run(char* command_ptr, char* output_ptr, int output_buffer_size, bool wait_for_exit, bool redirect_stdin, boolredirect_stdout, bool redirect_stderr, bool use_shell, bool create_no_window);
public unsafe static void Main()
{
    fixed (char* command = CliRunner.IsUnix() ? "ifconfig" : "ipconfig")
    {
        int    buffer_size = 8192;
        string stdout      = new('.', buffer_size);
        fixed (char* stdout_ptr = stdout)
        {
            int stdout_size = CliRunner.Run(command, stdout_ptr, buffer_size, true, false, true, true, false, true);
            Console.WriteLine(stdout[..stdout_size]);
            if (!CliRunner.IsUnix())
            {
                Console.ReadLine();
            }
        }
    }
}
```
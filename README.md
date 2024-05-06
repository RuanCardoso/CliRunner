Exemplo de uso:


```
// Linux '.dll' to windows.
[DllImport("CliRunner.so", EntryPoint = "Run")]
public unsafe static extern int Run(char* command_ptr, char* output_ptr, char* environment_vars, int output_buffer_size, bool wait_for_exit, bool redirect_stdin, bool redirect_stdout, bool redirect_stderr, bool use_shell, bool create_no_window, bool use_powershell, bool force_utf8);
public unsafe static void Main()
{
    fixed (char* command = CliRunner.IsUnix() ? "ifconfig" : "ipconfig")
    {
        int    buffer_size = 8192;
        string stdout      = new('.', buffer_size);
        fixed (char* stdout_ptr = stdout)
        {
            // arguments: command, output, environment_vars, buffer_size, wait_for_exit, redirect_stdin, redirect_stdout, redirect_stderr, use_shell, create_no_window use_powershell, force_utf8
            int stdout_size = CliRunner.Run(command, stdout_ptr, null, buffer_size, true, false, true, true, false, true, true, false);
            Console.WriteLine(stdout[..stdout_size]);
            if (!CliRunner.IsUnix())
            {
                Console.ReadLine();
            }
        }
    }
}
```

Como buildar?

```
// .NET 8(Native AOT)
dotnet publish -c Release -r linux-x64 -o "YOUR_PATH"
dotnet publish -c Release -r win-x64 -o "YOUR_PATH"
```
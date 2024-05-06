  // Developed by: Ruan Cardoso
  // MIT License

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

#pragma warning disable IDE0063

namespace Cli;

[JsonSerializable(typeof(Dictionary<string, string>))]
internal partial class DictContext: JsonSerializerContext { }

public class CliRunner
{
    [UnmanagedCallersOnly(EntryPoint = "Run")]
    public unsafe static int Run(char* command_ptr, char* output_ptr, char* environment_vars, int output_buffer_size, bool wait_for_exit, bool redirect_stdin, bool redirect_stdout, bool redirect_stderr, bool use_shell, bool create_no_window, bool use_powershell = false, bool force_utf8 = false)
    {
        using (Process process = new())
        {
            string           command         = new(command_ptr);
            ProcessStartInfo pInfo           = process.StartInfo;
                             pInfo.FileName  = IsUnix() ? "/bin/bash" : use_powershell ? "powershell" : "cmd";
                             pInfo.Arguments = IsUnix() ? $"-c \"{command}\"" : use_powershell ? $"-Command \"{command}\"" : $"/c \"{command}\"";

              // STDOUT && STDERR
            pInfo.RedirectStandardInput  = redirect_stdin;
            pInfo.RedirectStandardOutput = redirect_stdout;
            pInfo.RedirectStandardError  = redirect_stderr;

              // Force UTF8 Encoding
            if (force_utf8)
            {
                if (pInfo.RedirectStandardOutput)
                    pInfo.StandardOutputEncoding = Encoding.UTF8;
                if (pInfo.RedirectStandardError)
                    pInfo.StandardErrorEncoding  = Encoding.UTF8;
                if (pInfo.RedirectStandardInput)
                    pInfo.StandardInputEncoding  = Encoding.UTF8;
            }

              // Command running in background.
            pInfo.UseShellExecute = use_shell;
            pInfo.CreateNoWindow  = create_no_window;

              // Set environment variables
            if (environment_vars != null)
            {
                string            json_env      = new(environment_vars);
                Dictionary<string, string>? env = JsonSerializer.Deserialize(json_env, DictContext.Default.DictionaryStringString);
                if (env != null)
                {
                    foreach (var (key, value) in env)
                    {
                        pInfo.EnvironmentVariables.Add(key, value);
                    }
                }
            }

              //pInfo.EnvironmentVariables.Add();

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
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace Uno.UI.RemoteControl.DevServer.Tests.Helpers;

public static class ProcessUtil
{
    /// <summary>
    /// Runs a process asynchronously, captures stdout and stderr, and returns exit code and combined output.
    /// Throws if the process fails to start.
    /// </summary>
    public static async Task<(int ExitCode, string Output)> RunProcessAsync(ProcessStartInfo startInfo)
    {
        var output = new StringBuilder();
        using var process = new Process();
        process.StartInfo = startInfo;
        process.OutputDataReceived += (_, args) => output.AppendLine(args.Data);;
        process.ErrorDataReceived += (_, args) => output.AppendLine(args.Data);;

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        await process.WaitForExitAsync();
        return (process.ExitCode, output.ToString());
    }
}

using System.Diagnostics;
using System.Text;

namespace Uno.UI.DevServer.Cli.E2E.Tests.Mcp;

internal sealed record CliCommandSpec(string FileName, IReadOnlyList<string> Arguments)
{
	public ProcessStartInfo CreateStartInfo(string workingDirectory)
	{
		var startInfo = new ProcessStartInfo(FileName)
		{
			WorkingDirectory = workingDirectory,
			UseShellExecute = false,
			CreateNoWindow = true,
			RedirectStandardInput = true,
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			StandardOutputEncoding = Encoding.UTF8,
			StandardErrorEncoding = Encoding.UTF8,
		};

		foreach (var argument in Arguments)
		{
			startInfo.ArgumentList.Add(argument);
		}

		return startInfo;
	}

	public static CliCommandSpec ForBuiltCli(string cliDllPath, params IEnumerable<string>[] argumentGroups)
		=> new(
			"dotnet",
			[cliDllPath, .. argumentGroups.SelectMany(static group => group)]);
}

internal static class CliCommandRunner
{
	public static CommandExecutionResult Run(CliCommandSpec command, string workingDirectory)
	{
		var startInfo = command.CreateStartInfo(workingDirectory);
		using var process = Process.Start(startInfo)
			?? throw new InvalidOperationException($"Unable to start '{command.FileName}' in '{workingDirectory}'.");

		var stdout = process.StandardOutput.ReadToEnd();
		var stderr = process.StandardError.ReadToEnd();
		process.WaitForExit();

		return new CommandExecutionResult(process.ExitCode, stdout, stderr);
	}
}

internal sealed record CommandExecutionResult(int ExitCode, string StandardOutput, string StandardError)
{
	public void EnsureSuccess(string operation)
	{
		if (ExitCode != 0)
		{
			throw new InvalidOperationException(
				$"{operation} failed with exit code {ExitCode}.{Environment.NewLine}STDOUT:{Environment.NewLine}{StandardOutput}{Environment.NewLine}STDERR:{Environment.NewLine}{StandardError}");
		}
	}
}

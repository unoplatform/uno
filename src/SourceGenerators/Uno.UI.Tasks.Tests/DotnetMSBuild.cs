using System.Diagnostics;

namespace Uno.UI.Tasks.Tests;

/// <summary>
/// Runs <c>dotnet msbuild</c> out of process, so the targets under test are evaluated as an SDK user would see them.
/// </summary>
internal static class DotnetMSBuild
{
	private static readonly TimeSpan Timeout = TimeSpan.FromMinutes(2);

	public static Result Run(string workingDirectory, IEnumerable<string> arguments)
	{
		var startInfo = new ProcessStartInfo("dotnet")
		{
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			UseShellExecute = false,
			WorkingDirectory = workingDirectory,
		};
		foreach (var argument in (string[])["msbuild", "-nologo", "-nodeReuse:false", .. arguments])
		{
			startInfo.ArgumentList.Add(argument);
		}
		startInfo.Environment["MSBUILDDISABLENODEREUSE"] = "1";
		// Keeps the first-run banner of a fresh SDK out of stdout, which -getProperty/-getItem callers parse as JSON.
		startInfo.Environment["DOTNET_NOLOGO"] = "1";

		using var process = Process.Start(startInfo)!;
		var stdout = process.StandardOutput.ReadToEndAsync();
		var stderr = process.StandardError.ReadToEndAsync();
		if (!process.WaitForExit(Timeout))
		{
			process.Kill(entireProcessTree: true);
			process.WaitForExit();
			throw new TimeoutException($"dotnet msbuild did not exit within {Timeout}.{Environment.NewLine}{stdout.Result}{stderr.Result}");
		}
		process.WaitForExit();

		return new Result(process.ExitCode, stdout.Result, stderr.Result);
	}

	/// <summary>Deletes a test's scratch directory without letting a locked file hide the test's own failure.</summary>
	public static void TryDeleteDirectory(string directory)
	{
		try
		{
			Directory.Delete(directory, recursive: true);
		}
		catch (Exception e) when (e is IOException or UnauthorizedAccessException)
		{
			// Leaving a temp folder behind must never fail a test run.
		}
	}

	public record Result(int ExitCode, string StandardOutput, string StandardError)
	{
		public string Output => StandardOutput + StandardError;
	}
}

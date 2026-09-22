#nullable enable

using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SamplesApp.AppiumTests.Infrastructure;

namespace SamplesApp.AppiumTests.Tests;

[TestClass]
[TestCategory(TestCategories.HostIndependent)]
public sealed class AppiumProcessTests
{
	[TestMethod]
	public void RunProcess_Drains_Both_Streams_Without_Deadlocking()
	{
		var (fileName, arguments) = Shell(
			"for ($i = 0; $i -lt 2500; $i++) { [Console]::Out.WriteLine(\"out-$i\"); [Console]::Error.WriteLine(\"err-$i\") }",
			"i=0; while [ \"$i\" -lt 2500 ]; do echo \"out-$i\"; echo \"err-$i\" >&2; i=$((i+1)); done");

		var result = MacAdapter.RunProcess(fileName, arguments, TimeSpan.FromSeconds(20));

		result.ExitCode.Should().Be(0);
		result.StandardOutput.Split('\n').Should().HaveCount(2500);
		result.StandardError.Split('\n').Should().HaveCount(2500);
		result.StandardOutput.Should().EndWith("out-2499");
		result.StandardError.Should().EndWith("err-2499");
	}

	[TestMethod]
	public void RunProcess_NonZeroExit_Propagates_ExitCode_And_Diagnostics()
	{
		var (fileName, arguments) = Shell(
			"[Console]::Out.Write('output'); [Console]::Error.Write('failure'); exit 17",
			"printf output; printf failure >&2; exit 17");

		var run = () => MacAdapter.RunProcess(fileName, arguments, TimeSpan.FromSeconds(10));
		run.Should().Throw<InvalidOperationException>()
			.WithMessage("*code 17*stdout='output'*stderr='failure'*");
	}

	[TestMethod]
	public void RunProcess_Explicit_ExitCode_Query_Preserves_Failure_Diagnostics()
	{
		var (fileName, arguments) = Shell(
			"[Console]::Error.Write('not running'); exit 3",
			"printf 'not running' >&2; exit 3");

		var result = MacAdapter.RunProcess(fileName, arguments, TimeSpan.FromSeconds(10), throwOnNonZeroExit: false);

		result.ExitCode.Should().Be(3);
		result.StandardError.Should().Be("not running");
	}

	[TestMethod]
	public void RunProcess_Timeout_Terminates_The_Owned_Process_And_Fails()
	{
		var (fileName, arguments) = Shell("Start-Sleep -Seconds 30", "sleep 30");
		var elapsed = Stopwatch.StartNew();

		var run = () => MacAdapter.RunProcess(fileName, arguments, TimeSpan.FromSeconds(1));
		run.Should().Throw<TimeoutException>().WithMessage("*did not exit and finish redirecting output*");

		elapsed.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(8));
	}

	[TestMethod]
	public void RunProcess_Timeout_Also_Bounds_Streams_Inherited_By_A_Child()
	{
		var directory = Path.Combine(Environment.CurrentDirectory, "AppiumProcessTests", Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(directory);
		var pidFile = Path.Combine(directory, "child.pid");
		var (fileName, arguments) = Shell(
			$"""
			$info = New-Object System.Diagnostics.ProcessStartInfo
			$info.FileName = 'powershell.exe'
			$info.Arguments = '-NoProfile -NonInteractive -Command Start-Sleep -Seconds 30'
			$info.UseShellExecute = $false
			$info.CreateNoWindow = $true
			$child = [System.Diagnostics.Process]::Start($info)
			[System.IO.File]::WriteAllText('{pidFile.Replace("'", "''")}', $child.Id.ToString())
			""",
			$"sleep 30 & printf '%s' \"$!\" > '{pidFile.Replace("'", "'\\''")}'");
		var elapsed = Stopwatch.StartNew();
		try
		{
			var run = () => MacAdapter.RunProcess(fileName, arguments, TimeSpan.FromSeconds(2));
			run.Should().Throw<TimeoutException>().WithMessage("*finish redirecting output*");
			elapsed.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(8));
			File.Exists(pidFile).Should().BeTrue("the parent must have started its pipe-owning child");
		}
		finally
		{
			if (File.Exists(pidFile))
			{
				var childId = int.Parse(File.ReadAllText(pidFile), CultureInfo.InvariantCulture);
				try
				{
					using var child = Process.GetProcessById(childId);
					if (!child.HasExited)
					{
						child.Kill(entireProcessTree: true);
						child.WaitForExit(TimeSpan.FromSeconds(5)).Should().BeTrue();
					}
				}
				catch (ArgumentException)
				{
					// The child may already have exited.
				}
			}
			Directory.Delete(directory, recursive: true);
		}
	}

	private static (string FileName, string[] Arguments) Shell(string windows, string unix)
		=> OperatingSystem.IsWindows()
			? ("powershell.exe", new[] { "-NoProfile", "-NonInteractive", "-Command", windows })
			: ("/bin/sh", new[] { "-c", unix });
}

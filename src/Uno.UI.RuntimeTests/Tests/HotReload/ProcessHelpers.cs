#if HAS_UNO
#nullable enable

using System;
using System.Diagnostics;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

using Uno.Foundation.Logging;

namespace Uno.UI.RuntimeTests.Tests.HotReload;

internal class ProcessHelpers
{
	public static (int exitCode, string output, string error) RunProcess(string executable, string parameters, string? workingDirectory = null)
	{
		var p = new Process
		{
			StartInfo =
			{
				UseShellExecute = false,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				FileName = executable,
				Arguments = parameters,
				CreateNoWindow = true,
				WindowStyle = ProcessWindowStyle.Hidden,
			}
		};

		if (workingDirectory != null)
		{
			p.StartInfo.WorkingDirectory = workingDirectory;
		}

		var output = new StringBuilder();
		var error = new StringBuilder();
		var elapsed = Stopwatch.StartNew();
		p.OutputDataReceived += (s, e) => { if (e.Data != null) { output.Append(e.Data); } };
		p.ErrorDataReceived += (s, e) => { if (e.Data != null) { error.Append(e.Data); } };

		if (p.Start())
		{
			p.BeginOutputReadLine();
			p.BeginErrorReadLine();
			p.WaitForExit();
			var exitCore = p.ExitCode;
			p.Close();

			return (exitCore, output.ToString(), error.ToString());
		}
		else
		{
			throw new Exception($"Failed to start [{executable}]");
		}
	}

	/// <summary>
	/// Waits for <paramref name="process"/> to exit, killing it and everything it spawned when
	/// <paramref name="ct"/> fires first.
	/// </summary>
	/// <remarks>
	/// Merely abandoning the wait is not enough: the orphaned app keeps holding its dev-server
	/// connection and goes on competing with the attempt that follows.
	/// </remarks>
	public static async Task WaitForExitAsync(Process process, string logPrefix, CancellationToken ct)
	{
#if HAS_UNO
		typeof(ProcessHelpers).Log().Debug(logPrefix + $" waiting for process exit");
#endif

		try
		{
			await process.WaitForExitAsync(ct);
		}
		catch (OperationCanceledException)
		{
#if HAS_UNO
			typeof(ProcessHelpers).Log().Error(logPrefix + $" did not exit in time, killing it");
#endif
			KillProcessTree(process);

			throw;
		}
	}

	/// <summary>
	/// Kills <paramref name="process"/> and its children, tolerating a process that already exited.
	/// </summary>
	public static void KillProcessTree(Process? process)
	{
		if (process is null)
		{
			return;
		}

		try
		{
			if (!process.HasExited)
			{
				// The app is started through "dotnet run", so killing the launcher alone would
				// leave the app itself running.
				process.Kill(entireProcessTree: true);
				process.WaitForExit(10_000);
			}
		}
		catch (Exception e)
		{
			// The process may exit on its own between the check and the kill.
#if HAS_UNO
			typeof(ProcessHelpers).Log().Debug($"Failed to kill process ({e.Message})");
#endif
		}
	}

	public static Process StartProcess(
		string executable,
		List<string> parameters,
		string workingDirectory,
		string logPrefix,
		Dictionary<string, string>? environmentVariables = null,
		StringBuilder? output = null)
	{
		var pi = new ProcessStartInfo(executable)
		{
			UseShellExecute = false,
			CreateNoWindow = true,
			WindowStyle = ProcessWindowStyle.Hidden,
			WorkingDirectory = workingDirectory,
		};

		foreach (var parm in parameters)
		{
			pi.ArgumentList.Add(parm);
		}

		if (environmentVariables is not null)
		{
			foreach (var env in environmentVariables)
			{
				pi.EnvironmentVariables[env.Key] = env.Value;
			}
		}

		// redirect the output
		pi.RedirectStandardOutput = true;
		pi.RedirectStandardError = true;

		var process = new System.Diagnostics.Process();

		// hookup the event handlers to capture the data that is received
		process.OutputDataReceived += (sender, args) =>
		{
			var logMessage = $"[{DateTime.Now}] " + logPrefix + ": " + args.Data ?? "<Empty>";
			output?.AppendLine(logMessage);
			typeof(Given_HotReloadWorkspace).Log().Debug(logMessage);
		};
		process.ErrorDataReceived += (sender, args) =>
		{
			var logMessage = $"[{DateTime.Now}] " + logPrefix + ": " + args.Data ?? "<Empty>";
			output?.AppendLine(logMessage);
			typeof(Given_HotReloadWorkspace).Log().Error(logMessage);
		};

		process.StartInfo = pi;

#if HAS_UNO
		typeof(ProcessHelpers).Log().Debug($"Started process (wd:{pi.WorkingDirectory}): {pi.FileName} {string.Join(" ", pi.ArgumentList)})");
#endif

		process.Start();

		// start our event pumps
		process.BeginOutputReadLine();
		process.BeginErrorReadLine();

		return process;
	}
}
#endif

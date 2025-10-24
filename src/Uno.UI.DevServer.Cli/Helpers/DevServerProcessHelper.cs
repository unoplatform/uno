using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Uno.UI.DevServer.Cli.Helpers;

internal static class DevServerProcessHelper
{
	public static ProcessStartInfo CreateDotnetProcessStartInfo(
		string hostPath,
		IEnumerable<string> arguments,
		bool redirectOutput,
		bool redirectInput = false)
	{
		var useDotnet = !RuntimeInformation.IsOSPlatform(OSPlatform.Windows) || hostPath.EndsWith(".dll", StringComparison.OrdinalIgnoreCase);

		var psi = new ProcessStartInfo
		{
			FileName = useDotnet ? "dotnet" : hostPath,
			UseShellExecute = false,
			CreateNoWindow = true,
			RedirectStandardOutput = redirectOutput,
			RedirectStandardError = redirectOutput,
			RedirectStandardInput = redirectInput,
			WorkingDirectory = Directory.GetCurrentDirectory(),
		};

		var hostArgPath = hostPath;
		if (useDotnet)
		{
			if (hostArgPath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
			{
				hostArgPath = Path.ChangeExtension(hostArgPath, "dll");
			}
			psi.ArgumentList.Add(hostArgPath);
		}

		foreach (var a in arguments)
		{
			psi.ArgumentList.Add(a);
		}

		return psi;
	}

	public static async Task<(int? ExitCode, string StdOut, string StdErr)> RunGuiProcessAsync(
		ProcessStartInfo startInfo,
		ILogger logger,
		TimeSpan graceStartupDuration)
	{
		logger.LogDebug("Starting host process: {File} {Args}", startInfo.FileName, string.Join(" ", startInfo.ArgumentList));

		Process process = new()
		{
			StartInfo = startInfo,
			EnableRaisingEvents = true
		};

		var (outputSb, errorSb) = ObserveOutputs(startInfo, "studio", logger, process);

		var processExited = new TaskCompletionSource();
		process.Exited += (_, __) =>
		{
			logger.LogTrace("Host has exited (code: {ExitCode})", process.ExitCode);
			processExited.TrySetResult();
		};

		process.Start();

		logger.LogDebug("Started Host process: {Pid}", process.Id);

		// Begin async reads if redirected
		try
		{
			if (startInfo.RedirectStandardOutput)
			{
				process.BeginOutputReadLine();
			}
			if (startInfo.RedirectStandardError)
			{
				process.BeginErrorReadLine();
			}
		}
		catch (InvalidOperationException)
		{
			// Streams may not be available
		}

		var gracePeriodTask = Task.Delay(graceStartupDuration);

		// Wait for both process exit event and WaitForExitAsync, in
		// case some std is blocking the process exit.
		var resultTask = await Task.WhenAny(process.WaitForExitAsync(), processExited.Task, gracePeriodTask);

		var stdOut = outputSb.ToString();
		var stdErr = errorSb.ToString();

		return (gracePeriodTask == resultTask ? null : process.ExitCode, stdOut, stdErr);
	}

	private static (StringBuilder output, StringBuilder error) ObserveOutputs(ProcessStartInfo startInfo, string displayName, ILogger logger, Process process)
	{
		var outputSb = new StringBuilder();
		var errorSb = new StringBuilder();

		if (startInfo.RedirectStandardOutput)
		{
			process.OutputDataReceived += (s, e) =>
			{
				if (e.Data != null)
				{
					outputSb.AppendLine(e.Data);
					if (logger.IsEnabled(LogLevel.Debug))
					{
						logger.LogDebug("[{DisplayName}:stdout] {Data}", displayName, e.Data);
					}
				}
			};
		}

		if (startInfo.RedirectStandardError)
		{
			process.ErrorDataReceived += (s, e) =>
			{
				if (e.Data != null)
				{
					errorSb.AppendLine(e.Data);
					if (logger.IsEnabled(LogLevel.Debug))
					{
						logger.LogDebug("[{DisplayName}:stderr] {Data}", displayName, e.Data);
					}
				}
			};
		}

		return (outputSb, errorSb);
	}

	public static async Task<(int ExitCode, string StdOut, string StdErr)> RunConsoleProcessAsync(ProcessStartInfo startInfo, ILogger logger)
	{
		logger.LogDebug("Starting host process: {File} {Args}", startInfo.FileName, string.Join(" ", startInfo.ArgumentList));

		Process process = new()
		{
			StartInfo = startInfo,
			EnableRaisingEvents = true
		};

		var (outputSb, errorSb) = ObserveOutputs(startInfo, "devserver", logger, process);

		var processExited = new TaskCompletionSource();
		process.Exited += (_, __) =>
		{
			logger.LogTrace("Host has exited (code: {ExitCode})", process.ExitCode);
			processExited.TrySetResult();
		};

		process.Start();

		logger.LogDebug("Started Host process: {Pid}", process.Id);

		// Begin async reads if redirected
		try
		{
			if (startInfo.RedirectStandardOutput)
			{
				process.BeginOutputReadLine();
			}
			if (startInfo.RedirectStandardError)
			{
				process.BeginErrorReadLine();
			}
		}
		catch (InvalidOperationException)
		{
			// Streams may not be available
		}

		// Wait for both process exit event and WaitForExitAsync, in
		// case some std is blocking the process exit.
		await Task.WhenAny(process.WaitForExitAsync(), processExited.Task);

		var stdOut = outputSb.ToString();
		var stdErr = errorSb.ToString();

		if (process.ExitCode != 0)
		{
			logger.LogError("Host exited with code {ExitCode}", process.ExitCode);
			if (!string.IsNullOrWhiteSpace(stdOut))
			{
				logger.LogError("Host standard output for troubleshooting:\n{Output}", stdOut);
			}
			if (!string.IsNullOrWhiteSpace(stdErr))
			{
				logger.LogError("Host error output for troubleshooting:\n{ErrorOutput}", stdErr);
			}
		}
		else
		{
			logger.LogInformation("Command completed successfully.");
		}

		return (process.ExitCode, stdOut, stdErr);
	}
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Uno.UI.RemoteControl.Host;

namespace Uno.UI.DevServer.Cli.Helpers;

internal static class DevServerProcessHelper
{
	/// <summary>
	/// Environment variables related to DOTNET_ROOT that should be propagated to child processes.
	/// </summary>
	private static readonly string[] DotnetRootEnvironmentVariables =
	[
		"DOTNET_ROOT",
		"DOTNET_ROOT(x86)",
		"DOTNET_ROOT_X64",
		"DOTNET_ROOT_X86",
		"DOTNET_ROOT_ARM64",
		"DOTNET_ROOT_ARM"
	];

	/// <summary>
	/// Propagation list in order to get licensing working properly on Linux.
	/// </summary>
	private static readonly string[] XdgEnvironmentVariables =
	[
		"XDG_DATA_HOME",
	];

	public static ProcessStartInfo CreateDotnetProcessStartInfo(
		string hostPath,
		IEnumerable<string> arguments,
		string workingDirectory,
		bool redirectOutput,
		bool redirectInput = false,
		bool enableMajorRollForward = false)
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
			WorkingDirectory = workingDirectory,
		};

		var allArgs = new List<string>();
		var hostArgPath = hostPath;
		if (useDotnet)
		{
			if (hostArgPath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
			{
				hostArgPath = Path.ChangeExtension(hostArgPath, "dll");
			}
			allArgs.Add(QuoteArgument(hostArgPath));
		}

		foreach (var a in arguments)
		{
			allArgs.Add(QuoteArgument(a));
		}

		psi.Arguments = string.Join(" ", allArgs);

		PropagateDotnetRootVariables(psi);
		PropagateXdgVariables(psi);

		if (enableMajorRollForward)
		{
			// Override the host's runtimeconfig.json rollForward policy so a host compiled for
			// an older major (e.g. net9.0 because that's the most recent TFM shipped by an
			// older Uno.Sdk) can still start on a machine whose only installed runtime is a
			// newer major (e.g. net10.0). Safe for dev tools; a no-op when the exact TFM
			// runtime is installed. Used by the host spawn path alongside UnoToolsLocator's
			// TFM fallback resolver.
			psi.Environment["DOTNET_ROLL_FORWARD"] = "Major";
		}

		return psi;
	}

	private static string QuoteArgument(string arg)
		=> arg.Contains(' ') || arg.Contains('"') ? $"\"{arg.Replace("\"", "\\\"")}\"" : arg;

	private static void PropagateDotnetRootVariables(ProcessStartInfo startInfo)
	{
		foreach (var variableName in DotnetRootEnvironmentVariables)
		{
			var value = Environment.GetEnvironmentVariable(variableName);
			if (!string.IsNullOrWhiteSpace(value))
			{
				startInfo.Environment[variableName] = value;
			}
		}
	}

	private static void PropagateXdgVariables(ProcessStartInfo startInfo)
	{
		foreach (var variableName in XdgEnvironmentVariables)
		{
			var value = Environment.GetEnvironmentVariable(variableName);
			if (!string.IsNullOrWhiteSpace(value))
			{
				startInfo.Environment[variableName] = value;
			}
		}
	}

	public static async Task<(int? ExitCode, string StdOut, string StdErr)> RunGuiProcessAsync(
		ProcessStartInfo startInfo,
		ILogger logger,
		TimeSpan graceStartupDuration)
	{
		logger.LogDebug("Starting host process: {File} {Args}", startInfo.FileName, startInfo.Arguments);

		Process process = new()
		{
			StartInfo = startInfo,
			EnableRaisingEvents = true
		};

		var (outputSb, errorSb) = ObserveOutputs(startInfo, "studio", logger, process, forwardOutputToConsole: false);

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

		return (gracePeriodTask == resultTask || !process.HasExited ? null : process.ExitCode, stdOut, stdErr);
	}

	private static (StringBuilder output, StringBuilder error) ObserveOutputs(
		ProcessStartInfo startInfo,
		string displayName,
		ILogger logger,
		Process process,
		bool forwardOutputToConsole)
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
					if (forwardOutputToConsole)
					{
						Console.Out.WriteLine(e.Data);
					}
					else if (logger.IsEnabled(LogLevel.Debug))
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
					if (forwardOutputToConsole)
					{
						Console.Error.WriteLine(e.Data);
					}
					else if (logger.IsEnabled(LogLevel.Debug))
					{
						logger.LogDebug("[{DisplayName}:stderr] {Data}", displayName, e.Data);
					}
				}
			};
		}

		return (outputSb, errorSb);
	}

	public static async Task<(int ExitCode, string StdOut, string StdErr)> RunConsoleProcessAsync(
		ProcessStartInfo startInfo,
		ILogger logger,
		bool forwardOutputToConsole = false)
	{
		logger.LogDebug("Starting host process: {File} {Args}", startInfo.FileName, startInfo.Arguments);

		Process process = new()
		{
			StartInfo = startInfo,
			EnableRaisingEvents = true
		};

		var (outputSb, errorSb) = ObserveOutputs(startInfo, "devserver", logger, process, forwardOutputToConsole);

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
			if (!string.IsNullOrWhiteSpace(stdOut))
			{
				logger.LogDebug("Host stdout:\n{Output}", stdOut);
			}
			if (!string.IsNullOrWhiteSpace(stdErr))
			{
				logger.LogDebug("Host stderr:\n{ErrorOutput}", stdErr);
			}
		}

		return (process.ExitCode, stdOut, stdErr);
	}

	/// <summary>
	/// Spawns the host in direct mode (no <c>--command</c>) and waits for
	/// the HTTP port to become reachable.  Returns 0 on success, 1 on timeout
	/// or process failure.
	/// </summary>
	public static async Task<int> RunDirectProcessAsync(
		ProcessStartInfo startInfo,
		ILogger logger,
		int readinessTimeoutMs = 30_000)
	{
		logger.LogDebug("Starting host (direct mode): {File} {Args}", startInfo.FileName, startInfo.Arguments);

		Process process = new()
		{
			StartInfo = startInfo,
			EnableRaisingEvents = true
		};

		var (_, errorSb) = ObserveOutputs(startInfo, "devserver", logger, process, forwardOutputToConsole: false);

		process.Start();
		logger.LogDebug("Started host process: {Pid}", process.Id);

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

		// Backfill the ambient registry with the ideChannelId we passed via --ideChannel.
		// Older host versions (Uno.WinUI.DevServer < ~6.6) don't include IdeChannelId in
		// their own registration, leaving disco's `activeServers[].ideChannelId` null and
		// preventing uno.studio's DevServerLauncher from adopting the host without
		// re-spawning a duplicate. The sidecar is removed automatically when the host
		// process is no longer alive.
		var ideChannelId = ExtractIdeChannel(startInfo.Arguments);
		if (!string.IsNullOrWhiteSpace(ideChannelId))
		{
			var ambient = new AmbientRegistry(NullLogger.Instance);
			ambient.WriteAuxiliaryRegistration(targetProcessId: process.Id, ideChannelId: ideChannelId);
		}

		// Extract the port from the args to probe for readiness
		var port = ExtractPort(startInfo.Arguments);
		if (port <= 0)
		{
			logger.LogWarning("Unable to extract HTTP port from host arguments; skipping readiness wait.");
			return 0;
		}

		var ready = await WaitForTcpReadyAsync(port, readinessTimeoutMs);
		if (!ready)
		{
			if (process.HasExited)
			{
				logger.LogError("Host process died (exit code {ExitCode}) before becoming ready.", process.ExitCode);
			}
			else
			{
				logger.LogError("Host did not become ready within {TimeoutMs}ms. Terminating.", readinessTimeoutMs);
				try
				{
					process.Kill();
					await process.WaitForExitAsync();
				}
				catch { }
			}

			var stdErr = errorSb.ToString();
			if (!string.IsNullOrWhiteSpace(stdErr))
			{
				logger.LogError("Host stderr:\n{ErrorOutput}", stdErr);
			}

			return 1;
		}

		logger.LogInformation("DevServer is ready on port {Port}.", port);
		return 0;
	}

	private static int ExtractPort(string arguments)
	{
		var parts = arguments.Split(' ', StringSplitOptions.RemoveEmptyEntries);
		for (var i = 0; i < parts.Length - 1; i++)
		{
			if (string.Equals(parts[i], "--httpPort", StringComparison.OrdinalIgnoreCase)
				&& int.TryParse(parts[i + 1], out var port))
			{
				return port;
			}
		}

		return 0;
	}

	private static string? ExtractIdeChannel(string arguments)
	{
		var parts = arguments.Split(' ', StringSplitOptions.RemoveEmptyEntries);
		for (var i = 0; i < parts.Length - 1; i++)
		{
			if (string.Equals(parts[i], "--ideChannel", StringComparison.OrdinalIgnoreCase))
			{
				// Strip surrounding quotes that the args builder may have added.
				return parts[i + 1].Trim('"');
			}
		}

		return null;
	}

	private static async Task<bool> WaitForTcpReadyAsync(int port, int timeoutMs)
	{
		var sw = System.Diagnostics.Stopwatch.StartNew();
		var endpoint = new System.Net.IPEndPoint(System.Net.IPAddress.Loopback, port);

		while (sw.ElapsedMilliseconds < timeoutMs)
		{
			try
			{
				using var tcp = new System.Net.Sockets.TcpClient();
				var connectTask = tcp.ConnectAsync(endpoint.Address, endpoint.Port);
				var timeoutTask = Task.Delay(1000);
				var winner = await Task.WhenAny(connectTask, timeoutTask);
				if (winner == connectTask && !connectTask.IsFaulted)
				{
					return true;
				}
			}
			catch { }

			await Task.Delay(500);
		}

		return false;
	}
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Uno.UI.RemoteControl.Host;

namespace Uno.UI.DevServer.Cli.Helpers;

/// <summary>
/// How a direct-mode host spawn failed, when it did — lets callers distinguish a
/// startup crash (candidate for a safe-mode retry) from a readiness timeout.
/// </summary>
internal enum DirectSpawnFailure
{
	None,
	DiedBeforeReady,
	ReadinessTimeout,
}

/// <summary>
/// Outcome of <see cref="DevServerProcessHelper.RunDirectProcessAsync"/>:
/// the CLI exit code plus enough failure context (kind, host exit code) for the caller
/// to decide on a retry. Host output is relayed live through the logger, not captured here.
/// </summary>
internal sealed record DirectSpawnResult(int ExitCode, DirectSpawnFailure Failure, int? HostExitCode)
{
	public static DirectSpawnResult Ready { get; } = new(0, DirectSpawnFailure.None, null);
}

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

		var (outputSb, errorSb) = CaptureOutputs(process, startInfo);

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

	/// <summary>
	/// Default levels for relaying a child process's streams through the logger. Shared by
	/// <see cref="LogOutputs"/> and <see cref="RunConsoleProcessAsync"/> so their defaults
	/// can never drift apart.
	/// </summary>
	private const LogLevel _defaultStdLevel = LogLevel.Debug;
	private const LogLevel _defaultErrLevel = LogLevel.Error;

	/// <summary>
	/// Relays the process's stdout/stderr to <paramref name="logger"/> line by line as they
	/// arrive — stdout at <paramref name="stdLevel"/>, stderr at <paramref name="errLevel"/>.
	/// When <paramref name="displayName"/> is set, lines are tagged <c>[name]</c> to mark
	/// provenance; leave it <c>null</c> to relay verbatim (needed when the child's stdout is
	/// itself a result the caller passes through, e.g. JSON). Where those lines
	/// surface (console, file, both) is the logger configuration's concern, not this method's:
	/// attach a console sink to see them, and in MCP mode the logger already routes everything
	/// to stderr so the JSON-RPC stdout stays clean. The stream events are multicast, so a
	/// caller can also subscribe <see cref="CaptureOutputs"/> on the same process.
	/// </summary>
	private static void LogOutputs(
		Process process,
		ProcessStartInfo startInfo,
		ILogger logger,
		string? displayName = null,
		LogLevel stdLevel = _defaultStdLevel,
		LogLevel errLevel = _defaultErrLevel)
	{
		// e.Data is passed as a value, not folded into the template, so braces in the
		// payload are safe; displayName (when set) is a known-safe constant tag. The
		// stream is not tagged — the log level already distinguishes stdout from stderr.
		void Relay(LogLevel level, string? data)
		{
			if (data is null || !logger.IsEnabled(level))
			{
				return;
			}

			if (displayName is null)
			{
				logger.Log(level, "{Data}", data);
			}
			else
			{
				logger.Log(level, "[{Name}] {Data}", displayName, data);
			}
		}

		if (startInfo.RedirectStandardOutput)
		{
			process.OutputDataReceived += (_, e) => Relay(stdLevel, e.Data);
		}

		if (startInfo.RedirectStandardError)
		{
			process.ErrorDataReceived += (_, e) => Relay(errLevel, e.Data);
		}
	}

	/// <summary>
	/// Subscribes buffers to the process's stdout/stderr and returns them, for callers that
	/// need the captured text after exit (e.g. to log a failure summary). Multicast events,
	/// so this composes with <see cref="LogOutputs"/> on the same process.
	/// </summary>
	private static (StringBuilder output, StringBuilder error) CaptureOutputs(
		Process process,
		ProcessStartInfo startInfo)
	{
		var outputSb = new StringBuilder();
		var errorSb = new StringBuilder();

		if (startInfo.RedirectStandardOutput)
		{
			process.OutputDataReceived += (_, e) =>
			{
				if (e.Data is not null)
				{
					outputSb.AppendLine(e.Data);
				}
			};
		}

		if (startInfo.RedirectStandardError)
		{
			process.ErrorDataReceived += (_, e) =>
			{
				if (e.Data is not null)
				{
					errorSb.AppendLine(e.Data);
				}
			};
		}

		return (outputSb, errorSb);
	}

	public static async Task<int> RunConsoleProcessAsync(
		ProcessStartInfo startInfo,
		ILogger logger,
		LogLevel stdLevel = _defaultStdLevel,
		LogLevel errLevel = _defaultErrLevel)
	{
		logger.LogDebug("Starting host process: {File} {Args}", startInfo.FileName, startInfo.Arguments);

		Process process = new()
		{
			StartInfo = startInfo,
			EnableRaisingEvents = true
		};

		// stdout at the caller's level (Information for the passthrough commands whose output
		// the user asked for, Debug otherwise); stderr at Error. The logger owns where it lands.
		LogOutputs(process, startInfo, logger, stdLevel: stdLevel, errLevel: errLevel);

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

		if (process.ExitCode != 0)
		{
			logger.LogError("Host exited with code {ExitCode}", process.ExitCode);
		}
		else
		{
			logger.LogInformation("Command completed successfully.");
		}

		return process.ExitCode;
	}

	/// <summary>
	/// Spawns the host in direct mode (no <c>--command</c>) and waits for
	/// the HTTP port to become reachable.  The result's <c>ExitCode</c> is 0 on
	/// success and 1 on timeout or process failure; the failure kind, host exit
	/// code and captured stderr let the caller decide on a safe-mode retry.
	/// </summary>
	public static async Task<DirectSpawnResult> RunDirectProcessAsync(
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

		// Relay host output through the logger so IDE launchers surface the running host
		// logs (incl. hot reload), not just the startup banner: stdout at Information (kept
		// visible — that is the point of direct mode), stderr at Error. Nothing is buffered,
		// so the long-lived (ppid-guardian) session cannot accumulate memory. The logger owns
		// the destination — in MCP mode it already routes to stderr, keeping stdout clean for
		// JSON-RPC (and this path is start-only anyway; the MCP bridge spawns via DevServerMonitor).
		LogOutputs(process, startInfo, logger, displayName: "devserver", stdLevel: LogLevel.Information);

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
			return DirectSpawnResult.Ready;
		}

		var ready = await WaitForTcpReadyAsync(port, readinessTimeoutMs);
		if (!ready)
		{
			var died = process.HasExited;
			if (died)
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

			// Host stderr was already relayed live through the logger; the caller decides on a
			// safe-mode retry from the failure kind + exit code, not from captured error text.
			return new DirectSpawnResult(
				1,
				died ? DirectSpawnFailure.DiedBeforeReady : DirectSpawnFailure.ReadinessTimeout,
				died ? process.ExitCode : null);
		}

		logger.LogInformation("DevServer is ready on port {Port}.", port);

		// If we injected our own PID as --ppid (because no explicit ppid was provided
		// but an ideChannel was), we must stay alive so the host's ParentProcessObserver
		// can detect our death (which happens when the IDE kills its child processes on
		// exit). Without this, the CLI exits immediately and the host sees its parent
		// gone → shuts down within ~7.5s.
		if (ShouldAwaitHostExit(startInfo.Arguments))
		{
			logger.LogDebug(
				"CLI will remain alive as ppid guardian for host PID {HostPid}. " +
				"The host will self-terminate when this process dies.",
				process.Id);
			await process.WaitForExitAsync();
			logger.LogDebug("Host process exited with code {ExitCode}.", process.ExitCode);
			return new DirectSpawnResult(process.ExitCode, DirectSpawnFailure.None, process.ExitCode);
		}

		return DirectSpawnResult.Ready;
	}

	/// <summary>
	/// Returns <c>true</c> when the CLI injected its own PID as <c>--ppid</c>
	/// and must therefore remain alive as a guardian process.
	/// </summary>
	private static bool ShouldAwaitHostExit(string arguments)
	{
		var currentPid = Environment.ProcessId.ToString(CultureInfo.InvariantCulture);
		var parts = arguments.Split(' ', StringSplitOptions.RemoveEmptyEntries);
		for (var i = 0; i < parts.Length - 1; i++)
		{
			if (string.Equals(parts[i], "--ppid", StringComparison.OrdinalIgnoreCase)
				&& parts[i + 1] == currentPid)
			{
				return true;
			}
		}

		return false;
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

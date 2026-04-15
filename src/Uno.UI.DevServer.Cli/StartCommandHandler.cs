using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using Uno.UI.DevServer.Cli.Helpers;
using Uno.UI.RemoteControl.Host.Helpers;

namespace Uno.UI.DevServer.Cli;

/// <summary>
/// Parsed arguments for the <c>start</c> command.
/// </summary>
internal sealed record StartCommandArgs(
	int HttpPort,
	int ParentPid,
	string? Solution,
	string? IdeChannel,
	string? SolutionDir,
	IReadOnlyList<string> PassthroughArgs);

/// <summary>
/// Handles the <c>start</c> command by managing the full lifecycle
/// (existing server detection, process spawning in direct mode, readiness waiting)
/// instead of delegating to the host's two-process launcher.
///
/// This ensures all arguments (including <c>--ideChannel</c>) reach the host
/// process regardless of the host binary version.
/// </summary>
internal sealed class StartCommandHandler
{
	private readonly ILogger _logger;
	private readonly IDevServerLookup _lookup;
	private readonly Func<int, string, Task<bool>> _rebindIdeChannel;
	private readonly Func<ProcessStartInfo, Task<int>> _spawnProcess;

	public StartCommandHandler(
		ILogger logger,
		IDevServerLookup lookup,
		Func<int, string, Task<bool>> rebindIdeChannel,
		Func<ProcessStartInfo, Task<int>> spawnProcess)
	{
		_logger = logger;
		_lookup = lookup;
		_rebindIdeChannel = rebindIdeChannel;
		_spawnProcess = spawnProcess;
	}

	/// <summary>
	/// Parses the raw <c>start</c> command arguments into a structured record.
	/// The first element ("start") is skipped. Known flags are extracted;
	/// everything else is collected as pass-through args for the host.
	/// </summary>
	public static StartCommandArgs ParseStartArgs(string[] originalArgs)
	{
		int httpPort = 0;
		int parentPid = 0;
		string? solution = null;
		string? ideChannel = null;
		string? solutionDir = null;
		var passthrough = new List<string>();

		// Skip "start" verb at index 0
		for (var i = 1; i < originalArgs.Length; i++)
		{
			var arg = originalArgs[i];
			if (string.Equals(arg, "--httpPort", StringComparison.OrdinalIgnoreCase) && i + 1 < originalArgs.Length)
			{
				if (int.TryParse(originalArgs[++i], out var parsedPort))
				{
					httpPort = parsedPort;
				}
			}
			else if (string.Equals(arg, "--ppid", StringComparison.OrdinalIgnoreCase) && i + 1 < originalArgs.Length)
			{
				if (int.TryParse(originalArgs[++i], out var parsedPid))
				{
					parentPid = parsedPid;
				}
			}
			else if (string.Equals(arg, "--solution", StringComparison.OrdinalIgnoreCase) && i + 1 < originalArgs.Length)
			{
				solution = originalArgs[++i];
			}
			else if (string.Equals(arg, "--ideChannel", StringComparison.OrdinalIgnoreCase) && i + 1 < originalArgs.Length)
			{
				ideChannel = originalArgs[++i];
			}
			else if (string.Equals(arg, "--solution-dir", StringComparison.OrdinalIgnoreCase) && i + 1 < originalArgs.Length)
			{
				solutionDir = originalArgs[++i];
			}
			else
			{
				passthrough.Add(arg);
			}
		}

		return new StartCommandArgs(httpPort, parentPid, solution, ideChannel, solutionDir, passthrough);
	}

	/// <summary>
	/// Builds a <see cref="ProcessStartInfo"/> that launches the host in direct mode
	/// (no <c>--command</c>), passing all arguments through so that even older hosts
	/// receive <c>--ideChannel</c> and other flags via <c>IConfiguration</c>.
	/// </summary>
	public ProcessStartInfo BuildDirectLaunchArgs(
		string hostPath,
		StartCommandArgs parsed,
		string? addins,
		string workingDirectory)
	{
		var args = new List<string>();

		args.Add("--httpPort");
		args.Add(parsed.HttpPort.ToString(CultureInfo.InvariantCulture));

		if (parsed.ParentPid > 0)
		{
			args.Add("--ppid");
			args.Add(parsed.ParentPid.ToString(CultureInfo.InvariantCulture));
		}

		if (!string.IsNullOrWhiteSpace(parsed.Solution))
		{
			args.Add("--solution");
			args.Add(parsed.Solution);
		}

		if (!string.IsNullOrWhiteSpace(parsed.IdeChannel))
		{
			args.Add("--ideChannel");
			args.Add(parsed.IdeChannel);
		}

		if (addins is not null)
		{
			args.Add("--addins");
			args.Add(addins);
		}

		// Forward all pass-through args (e.g., --metadata-updates true)
		args.AddRange(parsed.PassthroughArgs);

		return DevServerProcessHelper.CreateDotnetProcessStartInfo(
			hostPath, args, workingDirectory, redirectOutput: true);
	}

	/// <summary>
	/// Builds host args for non-start commands (stop, list, cleanup) using the
	/// existing <c>--command=&lt;verb&gt;</c> controller mode path.
	/// </summary>
	public ProcessStartInfo BuildControllerModeArgs(
		string hostPath,
		string[] originalArgs,
		string workingDirectory,
		string? addins)
	{
		var command = originalArgs.Length > 0 ? originalArgs[0] : "start";
		var args = new List<string> { $"--command={command}" };
		for (int i = 1; i < originalArgs.Length; i++)
		{
			args.Add(originalArgs[i]);
		}

		if (addins is not null)
		{
			args.Add("--addins");
			args.Add(addins);
		}

		return DevServerProcessHelper.CreateDotnetProcessStartInfo(
			hostPath, args, workingDirectory, redirectOutput: true);
	}

	/// <summary>
	/// Orchestrates the full start lifecycle:
	/// 1. Check for existing servers (by solution, then by port)
	/// 2. Rebind IDE channel if reusing an existing server
	/// 3. Allocate port if needed
	/// 4. Spawn host in direct mode
	/// 5. Wait for readiness
	/// </summary>
	public async Task<int> RunAsync(
		string hostPath,
		string[] originalArgs,
		string workingDirectory,
		string? addins,
		string? resolvedSolutionPath = null)
	{
		var parsed = ParseStartArgs(originalArgs);

		// Fall back to auto-discovered solution when --solution not in CLI args
		if (string.IsNullOrWhiteSpace(parsed.Solution) && !string.IsNullOrWhiteSpace(resolvedSolutionPath))
		{
			parsed = parsed with { Solution = resolvedSolutionPath };
		}

		// Check for existing server by solution
		if (!string.IsNullOrWhiteSpace(parsed.Solution))
		{
			var existing = _lookup.FindBySolution(parsed.Solution);
			if (existing is not null)
			{
				if (!string.IsNullOrWhiteSpace(parsed.IdeChannel))
				{
					_logger.LogInformation(
						"DevServer already running for solution (PID {Pid}, Port {Port}). Rebinding IDE channel.",
						existing.Value.ProcessId, existing.Value.Port);

					var rebound = await _rebindIdeChannel(existing.Value.Port, parsed.IdeChannel);
					if (rebound)
					{
						await TryUpdateCsprojUserAsync(parsed.Solution, existing.Value.Port);
					}

					return rebound ? 0 : 1;
				}

				_logger.LogInformation(
					"DevServer already running for solution (PID {Pid}, Port {Port}). Reusing.",
					existing.Value.ProcessId, existing.Value.Port);
				return 0;
			}
		}

		// Check for existing server by port
		if (parsed.HttpPort > 0)
		{
			var existing = _lookup.FindByPort(parsed.HttpPort);
			if (existing is not null)
			{
				if (!string.IsNullOrWhiteSpace(parsed.IdeChannel))
				{
					_logger.LogInformation(
						"DevServer already running on port {Port} (PID {Pid}). Rebinding IDE channel.",
						parsed.HttpPort, existing.Value.ProcessId);

					var rebound = await _rebindIdeChannel(parsed.HttpPort, parsed.IdeChannel);
					if (rebound && !string.IsNullOrWhiteSpace(existing.Value.SolutionPath))
					{
						await TryUpdateCsprojUserAsync(existing.Value.SolutionPath, parsed.HttpPort);
					}

					return rebound ? 0 : 1;
				}

				_logger.LogInformation(
					"DevServer already running on port {Port} (PID {Pid}). Reusing.",
					parsed.HttpPort, existing.Value.ProcessId);
				return 0;
			}
		}

		// Allocate port if needed
		var effectivePort = parsed.HttpPort;
		if (effectivePort == 0)
		{
			effectivePort = AllocateTcpPort();
			_logger.LogDebug("Allocated TCP port {Port}", effectivePort);
		}

		var effectiveParsed = parsed with { HttpPort = effectivePort };

		// Update .csproj.user so the running app can discover the DevServer port
		if (!string.IsNullOrWhiteSpace(effectiveParsed.Solution))
		{
			await TryUpdateCsprojUserAsync(effectiveParsed.Solution, effectivePort);
		}

		// Build direct-mode args and spawn
		var startInfo = BuildDirectLaunchArgs(hostPath, effectiveParsed, addins, workingDirectory);

		_logger.LogInformation("Starting DevServer in direct mode: {Command} {Args}",
			startInfo.FileName, startInfo.Arguments);

		return await _spawnProcess(startInfo);
	}

	private async Task TryUpdateCsprojUserAsync(string? solution, int port)
	{
		if (string.IsNullOrWhiteSpace(solution))
		{
			return;
		}

		try
		{
			await CsprojUserGenerator.SetCsprojUserPort(solution, port);
		}
		catch (Exception ex)
		{
			_logger.LogWarning(ex, "Failed to update .csproj.user for solution {Solution}: {Message}", solution, ex.Message);
		}
	}

	private static int AllocateTcpPort()
	{
		var tcp = new TcpListener(IPAddress.Any, 0) { ExclusiveAddressUse = true };
		tcp.Start();
		var port = ((IPEndPoint)tcp.LocalEndpoint).Port;
		tcp.Stop();
		return port;
	}
}

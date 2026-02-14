using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Uno.UI.DevServer.Cli.Helpers;
using Uno.UI.RemoteControl.Host;
using Uno.UI.RemoteControl.Host.Helpers;

namespace Uno.UI.DevServer.Cli.Mcp;

public class DevServerMonitor(IServiceProvider services, ILogger<DevServerMonitor> logger)
{
	private readonly IServiceProvider _services = services;
	private readonly ILogger<DevServerMonitor> _logger = logger;
	private int _originalPort;
	private List<string> _forwardedArgs = [];
	private string _currentDirectory = "";
	private CancellationTokenSource? _cts;
	private Task? _monitor;
	private string? _unoSdkVersion;
	private long _discoveryDurationMs;
	private Process? _serverProcess;

	public event Action<string>? ServerStarted;
	public event Action? ServerFailed;
	public event Action? ServerCrashed;

	public string? UnoSdkVersion => _unoSdkVersion;
	public long DiscoveryDurationMs => _discoveryDurationMs;

	internal void StartMonitoring(string currentDirectory, int port, List<string> forwardedArgs)
	{
		if (_monitor is not null)
		{
			throw new InvalidOperationException("DevServerMonitor is already running");
		}

		_originalPort = port;
		_forwardedArgs = forwardedArgs;
		_currentDirectory = currentDirectory;

		var forwardedArgsDisplay = string.Join(" ", _forwardedArgs);
		_logger.LogTrace(
			"DevServerMonitor starting for directory {Directory} (port: {Port}, forwardedArgs: {Args})",
			_currentDirectory,
			_originalPort,
			forwardedArgsDisplay);

		_cts = new CancellationTokenSource();
		_monitor = Task.Run(() => RunMonitor(_cts.Token), _cts.Token);
	}

	internal async Task StopMonitoringAsync()
	{
		_cts?.Cancel();
		TerminateServerProcess();

		if (_monitor is not null)
		{
			try
			{
				await _monitor;
			}
			catch (OperationCanceledException)
			{
				// Expected when canceling
			}
		}
	}

	private async Task RunMonitor(CancellationToken ct)
	{
		int retryCount = 0;
		const int maxRetries = 3;

		while (!ct.IsCancellationRequested)
		{
			try
			{
				// If we don't have a solution, we can't start a DevServer yet.
				var solutionFiles = Directory
					.EnumerateFiles(_currentDirectory, "*.sln")
					.Concat(Directory.EnumerateFiles(_currentDirectory, "*.slnx")).ToArray();

				_logger.LogTrace(
					"DevServerMonitor scan found {Count} solution files in {Directory}",
					solutionFiles.Length,
					_currentDirectory);

				if (solutionFiles.Length != 0)
				{
					// If we don't have a dev server host, we can't start a DevServer yet.
					var discoveryStopwatch = System.Diagnostics.Stopwatch.StartNew();
					var hostPath = await _services
						.GetRequiredService<UnoToolsLocator>()
						.ResolveHostExecutableAsync(_currentDirectory);
					discoveryStopwatch.Stop();
					_discoveryDurationMs = discoveryStopwatch.ElapsedMilliseconds;

					if (hostPath is null)
					{
						_logger.LogTrace("DevServerMonitor could not resolve a host executable in {Directory}", _currentDirectory);
					}
					else
					{
						_logger.LogTrace("DevServerMonitor resolved host executable {HostPath}", hostPath);
					}

					if (hostPath is not null)
					{
						var port = _originalPort;

						if (port == 0)
						{
							port = EnsureTcpPort();
							_logger.LogDebug($"Automatically selected free port {port}");
						}
						else
						{
							_logger.LogDebug($"Using user-specified port {port}");
						}

						var solution = solutionFiles.FirstOrDefault();

						_logger.LogTrace(
							"DevServerMonitor launching server from {HostPath} in {WorkingDirectory} on port {Port}",
							hostPath,
							_currentDirectory,
							port);
						var (success, effectivePort) = await StartProcess(hostPath, port, _currentDirectory, solution, ct);
						if (!success)
						{
							retryCount++;

							if (retryCount >= maxRetries)
							{
								_logger.LogError("DevServer failed to start after {MaxRetries} attempts", maxRetries);
								ServerFailed?.Invoke();
								break;
							}

							_logger.LogWarning(
								"DevServer failed to start (attempt {Attempt}/{MaxRetries}), retrying in 5 seconds...",
								retryCount,
								maxRetries);

							await Task.Delay(TimeSpan.FromSeconds(5), ct);
							continue;
						}

						// Success - reset retry count
						retryCount = 0;

						_logger.LogInformation("DevServer started on port {Port}", effectivePort);

						var remoteEndpoint = $"http://localhost:{effectivePort}/mcp";
						_logger.LogInformation("Starting MCP stdio proxy to {Endpoint}", remoteEndpoint);

						if (!await WaitForServerReadyAsync(effectivePort, ct))
						{
							ServerFailed?.Invoke();
							return;
						}

						_logger.LogTrace("DevServerMonitor detected ready server at {Endpoint}; raising ServerStarted", remoteEndpoint);
						ServerStarted?.Invoke(remoteEndpoint);

						// Wait for the process to exit before re-entering the discovery loop
						await WaitForProcessExitAsync(ct);

						_logger.LogWarning("Server process exited, initiating recovery");
						_serverProcess = null;
						ServerCrashed?.Invoke();

						// Reset retry count — the server was running successfully
						retryCount = 0;
						continue;
					}
				}

				await Task.Delay(TimeSpan.FromSeconds(10), ct);
			}
			catch (Exception ex) when (!ct.IsCancellationRequested)
			{
				_logger.LogError(ex, "Error in DevServer monitor loop");
				await Task.Delay(TimeSpan.FromSeconds(10), ct);
			}
		}
	}

	private async Task<bool> WaitForServerReadyAsync(int port, CancellationToken ct)
	{
		var endpoints = new[]
		{
			$"http://localhost:{port}/mcp",
			$"http://127.0.0.1:{port}/mcp",
			$"http://[::1]:{port}/mcp"
		};

		var maxAttempts = 30; // 30 seconds

		using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(1) };

		for (int i = 0; i < maxAttempts; i++)
		{
			// Test all endpoints simultaneously
			var tasks = endpoints.Select(async endpoint =>
			{
				try
				{
					var response = await httpClient.GetAsync(endpoint, ct);
					if (response.StatusCode != HttpStatusCode.NotFound
						&& response.StatusCode != HttpStatusCode.BadRequest)
					{
						return (success: true, endpoint);
					}
				}
				catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
				{
					// Server not ready yet on this endpoint
				}

				return (success: false, endpoint: endpoint);
			}).ToArray();

			var results = await Task.WhenAll(tasks);

			// Return success if any endpoint succeeded
			var successfulEndpoint = results.FirstOrDefault(r => r.success);
			if (successfulEndpoint.success)
			{
				_logger.LogDebug("DevServer is ready at {Endpoint}", successfulEndpoint.endpoint);
				return true;
			}

			await Task.Delay(1000, ct);
		}

		_logger.LogError("DevServer did not become ready within timeout period on any of: {Endpoints}", string.Join(", ", endpoints));
		return false;
	}

	internal async Task<(bool success, int effectivePort)> StartProcess(string hostPath, int port, string workingDirectory, string? solution, CancellationToken ct)
	{
		// Check for existing DevServer instance via AmbientRegistry.
		// This is safe for the MCP stdio bridge because it connects via HTTP (/mcp endpoint),
		// not via the IDEChannel named pipe. The Host's IDEChannel only supports a
		// single IDE connection (maxNumberOfServerInstances: 1), so reuse is limited
		// to MCP-alongside-IDE scenarios. Two full IDEs sharing the same Host would
		// conflict on the IDEChannel, Hot Reload notifications, and launch tracking.
		var ambient = new AmbientRegistry(_logger);

		if (!string.IsNullOrWhiteSpace(solution))
		{
			var existing = ambient.GetActiveDevServerForPath(solution);
			if (existing is not null)
			{
				_logger.LogInformation(
					"Reusing existing DevServer (PID {Pid}, port {Port}) for {Solution}",
					existing.ProcessId, existing.Port, solution);
				return (true, existing.Port);
			}
		}

		// Write .csproj.user files for Hot Reload port sync
		if (!string.IsNullOrWhiteSpace(solution))
		{
			try
			{
				await CsprojUserGenerator.SetCsprojUserPort(solution, port);
				_logger.LogDebug("Updated .csproj.user files for solution {Solution} with port {Port}", solution, port);
			}
			catch (Exception ex)
			{
				_logger.LogWarning(ex, "Failed to update .csproj.user files");
				// Non-fatal: continue with launch
			}
		}

		// Build args for direct server launch (no controller)
		var args = new List<string>
		{
			"--httpPort", port.ToString(System.Globalization.CultureInfo.InvariantCulture),
			"--ppid", Environment.ProcessId.ToString(System.Globalization.CultureInfo.InvariantCulture)
		};

		if (!string.IsNullOrWhiteSpace(solution))
		{
			args.Add("--solution");
			args.Add(solution);
		}

		// Resolve add-ins via convention-based discovery
		try
		{
			var locator = _services.GetRequiredService<UnoToolsLocator>();
			var discovery = await locator.DiscoverAsync(workingDirectory);
			_unoSdkVersion = discovery.UnoSdkVersion;
			if (discovery.PackagesJsonPath is not null)
			{
				var resolver = _services.GetRequiredService<TargetsAddInResolver>();
				var addIns = resolver.ResolveAddIns(discovery.PackagesJsonPath);
				var addInsValue = string.Join(";", addIns.Select(a => a.EntryPointDll).Distinct(StringComparer.OrdinalIgnoreCase));
				args.Add("--addins");
				args.Add(addInsValue);
				_logger.LogDebug("Resolved {Count} add-in(s) for server process", addIns.Count);
			}
		}
		catch (Exception ex)
		{
			_logger.LogWarning(ex, "Convention-based add-in discovery failed, server will use MSBuild fallback");
		}

		args.AddRange(_forwardedArgs);

		var startInfo = DevServerProcessHelper.CreateDotnetProcessStartInfo(hostPath, args, workingDirectory, redirectOutput: true);

		_logger.LogDebug("Starting server process: {File} {Args}", startInfo.FileName, string.Join(" ", startInfo.ArgumentList));

		var process = new Process
		{
			StartInfo = startInfo,
			EnableRaisingEvents = true
		};

		process.OutputDataReceived += (_, e) =>
		{
			if (e.Data is not null && _logger.IsEnabled(LogLevel.Debug))
			{
				_logger.LogDebug("[server:stdout] {Data}", e.Data);
			}
		};

		process.ErrorDataReceived += (_, e) =>
		{
			if (e.Data is not null && _logger.IsEnabled(LogLevel.Debug))
			{
				_logger.LogDebug("[server:stderr] {Data}", e.Data);
			}
		};

		if (!process.Start())
		{
			_logger.LogError("Failed to start server process");
			return (false, port);
		}

		_serverProcess = process;
		_logger.LogDebug("Started server process (PID {Pid})", process.Id);

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

		// Don't wait for exit — the server is long-running
		return (true, port);
	}

	private async Task WaitForProcessExitAsync(CancellationToken ct)
	{
		if (_serverProcess is not { HasExited: false } proc)
		{
			return;
		}

		var exitTcs = new TaskCompletionSource();
		proc.Exited += (_, _) => exitTcs.TrySetResult();

		// Process may have exited between check and event registration
		if (proc.HasExited)
		{
			return;
		}

		await exitTcs.Task.WaitAsync(ct);
	}

	private void TerminateServerProcess()
	{
		try
		{
			if (_serverProcess is { HasExited: false } proc)
			{
				proc.Kill(entireProcessTree: true);
				_logger.LogDebug("Terminated server process (PID {Pid})", proc.Id);
			}
		}
		catch (Exception ex)
		{
			_logger.LogWarning(ex, "Failed to terminate server process");
		}
	}

	private static int EnsureTcpPort()
	{
		var tcp = new TcpListener(IPAddress.Any, 0) { ExclusiveAddressUse = true };
		tcp.Start();
		var port = ((IPEndPoint)tcp.LocalEndpoint).Port;
		tcp.Stop();
		return port;
	}
}

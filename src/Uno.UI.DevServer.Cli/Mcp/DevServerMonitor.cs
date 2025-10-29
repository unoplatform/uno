using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Uno.UI.DevServer.Cli.Helpers;

namespace Uno.UI.DevServer.Cli.Mcp;

public class DevServerMonitor(IServiceProvider services, ILogger<DevServerMonitor> logger)
{
	private readonly IServiceProvider _services = services;
	private readonly ILogger<DevServerMonitor> _logger = logger;
	private int _originalPort;
	private List<string> _forwardedArgs = [];
	private string _currentDirectory = "";
	private CancellationTokenSource? _cts;

	public event Action<string>? ServerStarted;
	public event Action? ServerFailed;

	internal void StartMonitoring(string currentDirectory, int port, List<string> forwardedArgs)
	{
		_originalPort = port;
		_forwardedArgs = forwardedArgs;
		_currentDirectory = currentDirectory;

		_cts = new CancellationTokenSource();
		_ = Task.Run(() => RunMonitor(_cts.Token), _cts.Token);
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

				if (solutionFiles.Length != 0)
				{
					// If we don't have a dev server host, we can't start a DevServer yet.
					var hostPath = await _services
						.GetRequiredService<UnoToolsLocator>()
						.ResolveHostExecutableAsync();

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

						var (success, exitCode) = await StartProcess(hostPath, port, ct);
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

						_logger.LogInformation("DevServer started on port {Port}", port);

						var remoteEndpoint = $"http://localhost:{port}/mcp";
						_logger.LogInformation("Starting MCP stdio proxy to {Endpoint}", remoteEndpoint);

						if (!await WaitForServerReadyAsync(port, ct))
						{
							ServerFailed?.Invoke();
							return;
						}

						ServerStarted?.Invoke(remoteEndpoint);

						break;
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
		var endpoint = $"http://localhost:{port}/mcp";
		var maxAttempts = 30; // 30 seconds

		using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(1) };

		for (int i = 0; i < maxAttempts; i++)
		{
			try
			{
				var response = await httpClient.GetAsync(endpoint, ct);
				if (response.StatusCode != HttpStatusCode.NotFound
					&& response.StatusCode != HttpStatusCode.BadRequest)
				{
					_logger.LogDebug("DevServer is ready at {Endpoint}", endpoint);
					return true;
				}
			}
			catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
			{
				// Server not ready yet
			}

			await Task.Delay(1000, ct);
		}

		_logger.LogError("DevServer did not become ready within timeout period");
		return false;
	}

	private async Task<(bool success, int? exitCode)> StartProcess(string hostPath, int port, CancellationToken ct)
	{
		var args = new List<string>
		{
			"--command", "start",
			"--httpPort", port.ToString(System.Globalization.CultureInfo.InvariantCulture),
			"--ppid", Environment.ProcessId.ToString(System.Globalization.CultureInfo.InvariantCulture)
		};
		args.AddRange(_forwardedArgs);

		var startInfo = DevServerProcessHelper.CreateDotnetProcessStartInfo(hostPath, args, redirectOutput: true, redirectInput: true);

		var (exitCode, stdout, stderr) = await DevServerProcessHelper.RunConsoleProcessAsync(startInfo, _logger);
		if (exitCode != 0)
		{
			// Already logged by helper
			if (!string.IsNullOrWhiteSpace(stdout))
			{
				_logger.LogDebug("Controller stdout:\n{Stdout}", stdout);
			}
			if (!string.IsNullOrWhiteSpace(stderr))
			{
				_logger.LogError("Controller stderr:\n{Stderr}", stderr);
			}
			return (false, exitCode);
		}

		return (true, null);
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

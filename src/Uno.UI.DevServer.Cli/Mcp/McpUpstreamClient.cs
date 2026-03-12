using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ModelContextProtocol;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace Uno.UI.DevServer.Cli.Mcp;

/// <summary>
/// Manages the upstream HTTP connection to the DevServer MCP host. Handles connection
/// lifecycle, reconnection via TCS reset, and tool list change notification forwarding.
/// </summary>
/// <seealso href="../../../specs/001-fast-devserver-startup/spec-appendix-d-mcp-improvements.md"/>
internal class McpUpstreamClient
{
	private readonly ILogger<McpUpstreamClient> _logger;
	private readonly DevServerMonitor _monitor;
	private Func<Task>? _toolListChanged;
	private readonly CancellationTokenSource _disposeCts = new();
	private TaskCompletionSource<McpClient> _clientCompletionSource = new();

	public Task<McpClient> UpstreamClient
		=> Volatile.Read(ref _clientCompletionSource).Task;

	public McpUpstreamClient(ILogger<McpUpstreamClient> logger, DevServerMonitor monitor)
	{
		_logger = logger;
		_monitor = monitor;

		_monitor.ServerStarted += OnServerStarted;
	}

	private void OnServerStarted(string url)
	{
		// Capture the TCS snapshot before launching the async connect so that
		// if ResetConnectionAsync swaps in a new TCS, the error goes to the
		// correct (old) generation, not the fresh one.
		var snapshotTcs = Volatile.Read(ref _clientCompletionSource);
		_ = Task.Run(async () =>
		{
			try
			{
				await ConnectOrDieAsync(url, _logger, _disposeCts.Token);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to connect to upstream MCP server at {Url}", url);
				snapshotTcs.TrySetException(ex);
			}
		}, _disposeCts.Token);
	}

	async Task ConnectOrDieAsync(string url, Microsoft.Extensions.Logging.ILogger log, CancellationToken ct)
	{
		// Capture a local snapshot of the TCS so that a concurrent ResetConnectionAsync
		// cannot swap it out between our read and write.
		var localTcs = Volatile.Read(ref _clientCompletionSource);

		try
		{
			var clientTransport = new HttpClientTransport(new()
			{
				Name = "Devserver",
				Endpoint = new Uri(url),
				TransportMode = HttpTransportMode.StreamableHttp,
			});

			var notificationGuard = new MonitorDecisions.StartOnceGuard();
			var options = new McpClientOptions
			{
				ClientInfo = new Implementation
				{
					Name = "stdio-http-proxy",
					Version = "1.0.0",
				},
				Handlers = new()
				{
					NotificationHandlers =
					[
						new(NotificationMethods.ToolListChangedNotification, async (notification, cancellationToken) =>
						{
							log.LogTrace("Upstream MCP notified tool list changed");

							// ToolListChanged has no meaningful params — no deserialization needed
							if (_toolListChanged is { } callback && notificationGuard.TryStart())
							{
								try
								{
									await callback();
								}
								catch (Exception ex)
								{
									log.LogError(ex, "Error while handling tool list changed notification");
									notificationGuard.Reset();
								}
							}
						})
					],
				},
			};

			log.LogInformation("Connecting to upstream MCP at {Url}", url);
			var client = await McpClient.CreateAsync(clientTransport, options, cancellationToken: ct);
			log.LogInformation("Connected to upstream: {Name} {Version}", client.ServerInfo.Name, client.ServerInfo.Version);

			if (!localTcs.TrySetResult(client))
			{
				// A concurrent ResetConnectionAsync already canceled this TCS.
				// This connection is stale — dispose it and bail out.
				log.LogWarning("Upstream connection completed but TCS was already canceled (stale connect after reset); disposing client");
				await client.DisposeAsync();
				return;
			}

			var tools = await client.ListToolsAsync(cancellationToken: ct);

			log.LogTrace("Upstream MCP responded with {Count} tools", tools?.Count);

			// Always notify — 0 tools is a valid response and must unblock waiters.
			// Skip if the notification handler already fired to avoid duplicate downstream events.
			if (_toolListChanged is { } toolsCallback && notificationGuard.TryStart())
			{
				try
				{
					await toolsCallback();
				}
				catch (Exception ex)
				{
					log.LogError(ex, "Error while handling tool list changed callback after ListTools");
					notificationGuard.Reset();
				}
			}
		}
		catch (Exception ex)
		{
			log.LogWarning(ex, "Upstream connect failed");
			throw;
		}
	}

	internal void RegisterToolListChangedCallback(Func<Task> value)
		=> _toolListChanged = value;

	/// <summary>
	/// Resets the upstream connection, allowing a new connection to be established.
	/// Disposes the previous client if one was connected.
	/// </summary>
	internal async Task ResetConnectionAsync()
	{
		var oldTcs = Interlocked.Exchange(ref _clientCompletionSource, new TaskCompletionSource<McpClient>());

		if (oldTcs.Task.IsCompletedSuccessfully)
		{
			try
			{
				await oldTcs.Task.Result.DisposeAsync();
			}
			catch (Exception ex)
			{
				_logger.LogWarning(ex, "Error disposing old upstream client");
			}
		}
		else
		{
			oldTcs.TrySetCanceled();
		}
	}

	/// <summary>
	/// Handles the tool list changed notification from the upstream MCP server.
	/// Extracted for testability — called by the notification handler lambda.
	/// </summary>
	internal async ValueTask HandleToolListChangedNotificationAsync()
	{
		if (_toolListChanged is { } callback)
		{
			await callback();
		}
	}

	internal async Task DisposeAsync()
	{
		_disposeCts.Cancel();

		var tcs = Volatile.Read(ref _clientCompletionSource);
		tcs.TrySetCanceled();

		try
		{
			var clientTask = tcs.Task;
			if (clientTask.IsCompletedSuccessfully)
			{
				_logger.LogInformation("Disconnecting from upstream MCP");
				await clientTask.Result.DisposeAsync();
			}
		}
		catch (Exception ex) when (ex is OperationCanceledException or ObjectDisposedException)
		{
			// Expected during shutdown
		}
	}
}

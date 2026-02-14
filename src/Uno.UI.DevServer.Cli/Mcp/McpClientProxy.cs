using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ModelContextProtocol;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace Uno.UI.DevServer.Cli.Mcp;

internal class McpClientProxy
{
	private readonly ILogger<McpClientProxy> _logger;
	private readonly DevServerMonitor _monitor;
	private Func<Task>? _toolListChanged;
	private readonly CancellationTokenSource _disposeCts = new();
	private TaskCompletionSource<McpClient> _clientCompletionSource = new();

	public Task<McpClient> UpstreamClient
		=> _clientCompletionSource.Task;

	public McpClientProxy(ILogger<McpClientProxy> logger, DevServerMonitor monitor)
	{
		_logger = logger;
		_monitor = monitor;

		_monitor.ServerStarted += OnServerStarted;
	}

	private void OnServerStarted(string url)
	{
		_ = Task.Run(async () =>
		{
			try
			{
				await ConnectOrDieAsync(url, _logger, _disposeCts.Token);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to connect to upstream MCP server at {Url}", url);
				_clientCompletionSource.TrySetException(ex);
			}
		}, _disposeCts.Token);
	}

	async Task ConnectOrDieAsync(string url, Microsoft.Extensions.Logging.ILogger log, CancellationToken ct)
	{
		try
		{
			var clientTransport = new HttpClientTransport(new()
			{
				Name = "Devserver",
				Endpoint = new Uri(url),
				TransportMode = HttpTransportMode.StreamableHttp,
			});

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
							if (_toolListChanged is { } callback)
							{
								await callback();
							}
						})
					],
				},
			};

			log.LogInformation("Connecting to upstream MCP at {Url}", url);
			var client = await McpClient.CreateAsync(clientTransport, options, cancellationToken: ct);
			log.LogInformation("Connected to upstream: {Name} {Version}", client.ServerInfo.Name, client.ServerInfo.Version);

			_clientCompletionSource.TrySetResult(client);

			var tools = await client.ListToolsAsync(cancellationToken: ct);

			log.LogTrace("Upstream MCP responded with {Count} tools", tools?.Count);

			// Always notify — 0 tools is a valid response and must unblock waiters
			if (_toolListChanged is { } toolsCallback)
			{
				await toolsCallback();
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
		var oldTcs = _clientCompletionSource;
		_clientCompletionSource = new TaskCompletionSource<McpClient>();

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
		_clientCompletionSource.TrySetCanceled();

		try
		{
			var clientTask = _clientCompletionSource.Task;
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

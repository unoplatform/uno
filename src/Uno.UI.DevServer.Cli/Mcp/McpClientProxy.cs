using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
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
						new(NotificationMethods.ToolListChangedNotification, (notification, cancellationToken) =>
						{
							log.LogTrace("Upstream MCP notified tool list changed");

							var notificationParams = JsonSerializer.Deserialize<ResourceUpdatedNotificationParams>(notification.Params, McpJsonUtilities.DefaultOptions);

							_toolListChanged?.Invoke();

							return default;
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

			if (tools?.Count > 0)
			{
				// We already have a list, raise now.
				_toolListChanged?.Invoke();
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

	internal async Task DisposeAsync()
	{
		_disposeCts.Cancel();

		if (UpstreamClient is not null)
		{
			_logger.LogInformation("Disconnecting from upstream MCP");
			await (await UpstreamClient).DisposeAsync();
		}
	}
}

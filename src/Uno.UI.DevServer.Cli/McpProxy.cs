using System;
using System.Collections.Generic;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using System.Threading;
using Uno.UI.DevServer.Cli.Helpers;

namespace Uno.UI.DevServer.Cli;

internal class McpProxy
{
	private readonly ILogger _logger;

	public McpProxy(ILogger logger) => _logger = logger;

	public async Task<int> RunAsync(string hostPath, int port, List<string> forwardedArgs, CancellationToken ct)
	{
		try
		{
			var (flowControl, value) = await StartProcess(hostPath, port, forwardedArgs, ct);
			if (!flowControl)
			{
				return value ?? 1;
			}

			_logger.LogInformation("DevServer started on port {Port}", port);
			var remoteEndpoint = $"http://localhost:{port}/mcp";
			_logger.LogInformation("Starting MCP stdio proxy to {Endpoint}", remoteEndpoint);

			return await StartMcpStdIoProxyAsync(remoteEndpoint, ct);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "MCP proxy error: {Message}", ex.Message);
			return 1;
		}
	}

	private async Task<(bool success, int? exitCode)> StartProcess(string hostPath, int port, List<string> forwardedArgs, CancellationToken ct)
	{
		var args = new List<string>
		{
			"--command", "start",
			"--httpPort", port.ToString(System.Globalization.CultureInfo.InvariantCulture),
			"--ppid", Environment.ProcessId.ToString(System.Globalization.CultureInfo.InvariantCulture)
		};
		args.AddRange(forwardedArgs);

		var startInfo = DevServerProcessHelper.CreateHostProcessStartInfo(hostPath, args, redirectOutput: true, redirectInput: true);

		var (exitCode, stdout, stderr) = await DevServerProcessHelper.RunHostProcessAsync(startInfo, _logger);
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

	private async Task<int> StartMcpStdIoProxyAsync(string remoteEndpoint, CancellationToken ct)
	{
		var builder = Host.CreateApplicationBuilder();

		IMcpClient? upstreamClient = null;

		builder.Services
			.AddMcpServer()
			.WithStdioServerTransport()
			.WithCallToolHandler(async (ctx, ct) =>
			{
				_logger.LogDebug("Invoking MCP tool {Tool}", ctx.Params!.Name);

				var name = ctx.Params!.Name;
				var args = ctx.Params.Arguments ?? new Dictionary<string, JsonElement>();
				var adjustedArguments = args.ToDictionary(v => v.Key, v => (object?)v.Value);

				var result = await upstreamClient!.CallToolAsync(
					name,
					adjustedArguments,
					cancellationToken: ct
				);

				return result;
			})
			.WithListToolsHandler(async (ctx, ct) =>
			{
				_logger.LogTrace("Client requested tools list update");

				var list = await upstreamClient!.ListToolsAsync(cancellationToken: ct);

				_logger.LogDebug("Reporting {Count} tools", list.Count);

				return new ListToolsResult
				{
					Tools = list.Select(t => t.ProtocolTool).ToList()
				};
			});

		builder.Logging.AddConsole(consoleLogOptions =>
		{
			// Configure all logs to go to stderr
			consoleLogOptions.LogToStandardErrorThreshold = LogLevel.Trace;
		});

		var host = builder.Build();

		var logger = host.Services.GetRequiredService<ILoggerFactory>().CreateLogger("ToolPoller");

		upstreamClient = await ConnectOrDieAsync(
			remoteEndpoint,
			async () =>
			{
				await host.Services.GetRequiredService<IMcpServer>().SendNotificationAsync(
					NotificationMethods.ToolListChangedNotification,
					new ResourceUpdatedNotificationParams()
				);
			},
			host.Services.GetRequiredService<ILoggerFactory>().CreateLogger("UpstreamConnector")
		);

		try
		{
			await host.RunAsync();
		}
		finally
		{
			if (upstreamClient != null)
			{
				await upstreamClient.DisposeAsync();
			}
		}

		return 0;
	}

	static async Task<IMcpClient> ConnectOrDieAsync(string url, Action toolsListChanged, Microsoft.Extensions.Logging.ILogger log)
	{
		try
		{
			var clientTransport = new SseClientTransport(new()
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
				Capabilities = new()
				{
					NotificationHandlers =
					[
						new(NotificationMethods.ToolListChangedNotification, (notification, cancellationToken) =>
						{
							var notificationParams = JsonSerializer.Deserialize<ResourceUpdatedNotificationParams>(notification.Params, McpJsonUtilities.DefaultOptions);

							toolsListChanged();

							return default;
						})
					],
				},
			};

			log.LogInformation("Connecting to upstream MCP at {Url}", url);
			var client = await McpClientFactory.CreateAsync(clientTransport, options);
			log.LogInformation("Connected to upstream: {Name} {Version}", client.ServerInfo.Name, client.ServerInfo.Version);
			return client;
		}
		catch (Exception ex)
		{
			log.LogWarning(ex, "Upstream connect failed");
			throw;
		}
	}
}

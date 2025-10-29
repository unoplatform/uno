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

namespace Uno.UI.DevServer.Cli.Mcp;

internal class McpProxy
{
	private readonly ILogger<McpProxy> _logger;
	private readonly DevServerMonitor _devServerMonitor;
	private readonly McpClientProxy _mcpClientProxy;

	public McpProxy(ILogger<McpProxy> logger, DevServerMonitor mcpServerMonitor, McpClientProxy mcpClientProxy)
	{
		_logger = logger;
		_devServerMonitor = mcpServerMonitor;
		_mcpClientProxy = mcpClientProxy;
	}

	public async Task<int> RunAsync(string currentDirectory, int port, List<string> forwardedArgs, CancellationToken ct)
	{
		_devServerMonitor.StartMonitoring(currentDirectory, port, forwardedArgs);

		try
		{
			return await StartMcpStdIoProxyAsync(ct);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "MCP proxy error: {Message}", ex.Message);
			return 1;
		}
	}


	private async Task<int> StartMcpStdIoProxyAsync(CancellationToken ct)
	{
		var builder = Host.CreateApplicationBuilder();
		builder.Services
			.AddMcpServer()
			.WithStdioServerTransport()
			.WithCallToolHandler(async (ctx, ct) =>
			{
				var upstreamClient = _mcpClientProxy.UpstreamClient;

				if (upstreamClient is null)
				{
					throw new InvalidOperationException($"The tool {ctx.Params!.Name} is unknown");
				}

				_logger.LogDebug("Invoking MCP tool {Tool}", ctx.Params!.Name);

				var name = ctx.Params!.Name;
				var args = ctx.Params.Arguments ?? new Dictionary<string, JsonElement>();
				var adjustedArguments = args.ToDictionary(v => v.Key, v => (object?)v.Value);

				var result = await upstreamClient.CallToolAsync(
					name,
					adjustedArguments,
					cancellationToken: ct
				);

				return result;
			})
			.WithListToolsHandler(async (ctx, ct) =>
			{
				var upstreamClient = _mcpClientProxy.UpstreamClient;

				if (upstreamClient is null)
				{
					// The devserver is not started yet, so there are no tools to report.
					return new() { Tools = [] };
				}

				_logger.LogTrace("Client requested tools list update");

				var list = await upstreamClient!.ListToolsAsync(cancellationToken: ct);

				_logger.LogDebug("Reporting {Count} tools", list.Count);

				return new()
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

		_mcpClientProxy.RegisterToolListChangedCallback(async () =>
		{
			await host.Services.GetRequiredService<IMcpServer>().SendNotificationAsync(
				NotificationMethods.ToolListChangedNotification,
				new ResourceUpdatedNotificationParams()
			);
		});

		_devServerMonitor.ServerFailed += () =>
		{
			_logger.LogError("DevServer failed to start, stopping MCP proxy");
			host.StopAsync();
		};

		try
		{
			await host.RunAsync();
		}
		finally
		{
			await _mcpClientProxy.DisposeAsync();
		}

		return 0;
	}
}

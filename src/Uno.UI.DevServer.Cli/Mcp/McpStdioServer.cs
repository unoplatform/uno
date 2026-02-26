using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace Uno.UI.DevServer.Cli.Mcp;

/// <summary>
/// Builds the MCP stdio server Host with all protocol handlers (call_tool, list_tools,
/// list_resources, read_resource). Lifecycle-specific behavior is injected via delegates.
/// </summary>
internal class McpStdioServer(
	ILogger<McpStdioServer> logger,
	ToolListManager toolListManager,
	HealthService healthService,
	McpUpstreamClient mcpUpstreamClient)
{
	/// <summary>
	/// Builds the MCP stdio Host but does not start it.
	/// Lifecycle-specific behavior (roots, set_roots) is injected via delegates.
	/// </summary>
	public (IHost Host, TaskCompletionSource ToolsReadyTcs) BuildHost(
		Func<RequestContext<ListToolsRequestParams>, TaskCompletionSource, CancellationToken, Task> ensureRootsInitialized,
		Tool addRootsTool,
		bool forceRootsFallback,
		Func<string[]> getRoots,
		Func<string[], Task> setRootsHandler)
	{
		var tcs = new TaskCompletionSource();

		var builder = Host.CreateApplicationBuilder();
		builder.Services
			.AddMcpServer(options =>
			{
				options.ServerInfo = new Implementation
				{
					Name = "uno-devserver",
					Version = GetAssemblyVersion(),
				};
			})
			.WithStdioServerTransport()
			.WithCallToolHandler(async (ctx, ct) =>
			{
				var toolName = ctx.Params?.Name ?? "unknown";

				if (forceRootsFallback && toolName == addRootsTool.Name)
				{
					if (ctx.Params?.Arguments is not { } arguments ||
						!arguments.TryGetValue("roots", out var rootsElement))
					{
						return new CallToolResult()
						{
							Content = [new TextContentBlock() { Text = "Missing required 'roots' argument." }],
							IsError = true
						};
					}

					await setRootsHandler(rootsElement.Deserialize<string[]>() ?? []);
					return new CallToolResult() { Content = [new TextContentBlock() { Text = "Ok" }] };
				}

				// Handle the built-in uno_health tool before upstream is ready
				if (toolName == HealthService.HealthTool.Name)
				{
					return healthService.BuildHealthToolResponse();
				}

				var upstreamTask = mcpUpstreamClient.UpstreamClient;

				if (!upstreamTask.IsCompletedSuccessfully)
				{
					var message = healthService.ConnectionState == ConnectionState.Reconnecting
						? "DevServer host crashed and is reconnecting. Tools will be available again shortly. Call the uno_health tool for detailed diagnostics, or wait a few seconds and retry."
						: "DevServer is starting up. The host process is not yet ready. Call the uno_health tool for detailed diagnostics, or wait a few seconds and retry.";
					logger.LogDebug("Tool {Tool} called before upstream is ready (state: {State})", toolName, healthService.ConnectionState);
					return new CallToolResult()
					{
						Content = [new TextContentBlock() { Text = message }],
						IsError = true
					};
				}

				var upstreamClient = upstreamTask.Result;

				logger.LogDebug("Invoking MCP tool {Tool}", toolName);

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
				await ensureRootsInitialized(ctx, tcs, ct);

				if (forceRootsFallback && getRoots().Length == 0)
				{
					List<Tool> tools = [HealthService.HealthTool, addRootsTool];
					var cachedTools = toolListManager.GetCachedTools();
					if (cachedTools.Length > 0)
					{
						tools.AddRange(cachedTools);
					}

					logger.LogTrace("Upstream client is not connected, returning {Count} tools", tools.Count);

					// The devserver is not started yet, so there are no tools to report.
					return new() { Tools = tools };
				}

				var result = await toolListManager.ListToolsWithTimeoutAsync(ct);
				return new() { Tools = ToolListManager.AppendBuiltInTools(result.Tools) };
			})
			.WithListResourcesHandler((ctx, ct) =>
			{
				var resource = new Resource
				{
					Uri = HealthService.HealthResourceUri,
					Name = "Uno DevServer Health",
					Description = "Real-time health status of the Uno DevServer MCP bridge, including connection state, tool count, and diagnostics.",
					MimeType = "application/json",
				};

				return ValueTask.FromResult(new ListResourcesResult { Resources = [resource] });
			})
			.WithReadResourceHandler((ctx, ct) =>
			{
				var uri = ctx.Params?.Uri;
				if (!string.Equals(uri, HealthService.HealthResourceUri, StringComparison.OrdinalIgnoreCase))
				{
					throw new McpException($"Unknown resource URI: {uri}");
				}

				var report = healthService.BuildHealthReport();
				var json = JsonSerializer.Serialize(report, McpJsonUtilities.DefaultOptions);

				var contents = new TextResourceContents
				{
					Uri = HealthService.HealthResourceUri,
					Text = json,
					MimeType = "application/json",
				};

				return ValueTask.FromResult(new ReadResourceResult { Contents = [contents] });
			});

		builder.Logging.AddConsole(consoleLogOptions =>
		{
			// Configure all logs to go to stderr
			consoleLogOptions.LogToStandardErrorThreshold = LogLevel.Trace;
		});

		var host = builder.Build();

		return (host, tcs);
	}

	internal static string GetAssemblyVersion()
	{
		var attr = typeof(McpStdioServer).Assembly
			.GetCustomAttributes(typeof(System.Reflection.AssemblyInformationalVersionAttribute), false)
			.OfType<System.Reflection.AssemblyInformationalVersionAttribute>()
			.FirstOrDefault();

		if (attr is not null)
		{
			// Strip the commit hash after '+' (e.g. "5.5.100-dev.1+abc123" → "5.5.100-dev.1")
			var parts = attr.InformationalVersion.Split('+', StringSplitOptions.RemoveEmptyEntries);
			return parts[0];
		}

		return typeof(McpStdioServer).Assembly.GetName().Version?.ToString() ?? "0.0.0";
	}
}

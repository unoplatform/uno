using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Uno.UI.DevServer.Cli.Helpers;

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
	/// <summary>Maximum time to wait for an upstream tool call before returning a timeout error.</summary>
	internal const int UpstreamCallToolTimeoutMs = 5 * 60 * 1000; // 5 minutes — tool calls can involve builds

	private static void LogTimeline(ILogger logger, string stage, long elapsedMilliseconds, string details)
	{
		logger.LogDebug("TIMELINE|mcp-stdio|{Stage}|{ElapsedMs}|{Details}", stage, elapsedMilliseconds, details);
	}

	/// <summary>
	/// Builds the MCP stdio Host but does not start it.
	/// Lifecycle-specific behavior (roots, set_roots) is injected via delegates.
	/// </summary>
	public (IHost Host, TaskCompletionSource ToolsReadyTcs) BuildHost(
		Func<RequestContext<ListToolsRequestParams>, TaskCompletionSource, CancellationToken, Task>
			ensureRootsInitialized,
		Tool addRootsTool,
		Tool selectSolutionTool,
		bool forceRootsFallback,
		Func<string[]> getRoots,
		Func<string[], Task> setRootsHandler,
		Func<string, bool, Task<CallToolResult>> selectSolutionHandler)
	{
		var tcs = new TaskCompletionSource();

		var builder = Host.CreateApplicationBuilder();
		builder.Services
			.AddMcpServer(options =>
			{
				options.ServerInfo = new Implementation
				{
					Name = "uno-devserver",
					Version = AssemblyVersionHelper.GetAssemblyVersion(typeof(McpStdioServer).Assembly),
				};
			})
			.WithStdioServerTransport()
			.WithCallToolHandler(async (ctx, ct) =>
			{
				var toolName = ctx.Params?.Name ?? "unknown";
				var toolStopwatch = Stopwatch.StartNew();
				logger.LogTrace("RECV call_tool {Tool}", toolName);

				if (forceRootsFallback && toolName == addRootsTool.Name)
				{
					if (ctx.Params?.Arguments is not { } arguments
						|| !arguments.TryGetValue("roots", out var rootsElement))
					{
						return new CallToolResult()
						{
							Content = [new TextContentBlock() { Text = "Missing required 'roots' argument." }],
							IsError = true
						};
					}

					await setRootsHandler(rootsElement.Deserialize<string[]>() ?? []);
					toolStopwatch.Stop();
					LogTimeline(logger, "tool.set-roots.complete", toolStopwatch.ElapsedMilliseconds, "roots-fallback");
					logger.LogDebug("Handled MCP tool {Tool} in {ElapsedMs} ms", toolName,
						toolStopwatch.ElapsedMilliseconds);
					return new CallToolResult() { Content = [new TextContentBlock() { Text = "Ok" }] };
				}

				if (toolName == selectSolutionTool.Name)
				{
					if (!TryGetSelectSolutionPath(ctx.Params?.Arguments, out var solutionPath, out var errorResult))
					{
						return errorResult;
					}

					var forceRestart = TryGetForceRestart(ctx.Params?.Arguments);
					var selectionResult = await selectSolutionHandler(solutionPath!, forceRestart);
					toolStopwatch.Stop();
					LogTimeline(logger, "tool.select-solution.complete", toolStopwatch.ElapsedMilliseconds, toolName);
					logger.LogDebug("Handled MCP tool {Tool} in {ElapsedMs} ms", toolName,
						toolStopwatch.ElapsedMilliseconds);
					return selectionResult;
				}

				// Handle the built-in uno_health tool before upstream is ready
				if (toolName == HealthService.HealthTool.Name)
				{
					var healthResult = healthService.BuildHealthToolResponse();
					toolStopwatch.Stop();
					LogTimeline(logger, "tool.health.complete", toolStopwatch.ElapsedMilliseconds,
						healthService.ConnectionState.ToString());
					logger.LogDebug("Handled MCP tool {Tool} in {ElapsedMs} ms", toolName,
						toolStopwatch.ElapsedMilliseconds);
					return healthResult;
				}

				var upstreamTask = mcpUpstreamClient.UpstreamClient;

				if (!upstreamTask.IsCompletedSuccessfully)
				{
					var message = healthService.ConnectionState == ConnectionState.Reconnecting
						? "DevServer host crashed and is reconnecting. Tools will be available again shortly. Call the uno_health tool for detailed diagnostics, or wait a few seconds and retry."
						: "DevServer is starting up. The host process is not yet ready. Call the uno_health tool for detailed diagnostics, or wait a few seconds and retry.";
					logger.LogDebug("Tool {Tool} called before upstream is ready (state: {State})", toolName,
						healthService.ConnectionState);
					toolStopwatch.Stop();
					LogTimeline(logger, "tool.rejected-upstream-not-ready", toolStopwatch.ElapsedMilliseconds,
						healthService.ConnectionState.ToString());
					logger.LogDebug("Rejected MCP tool {Tool} before upstream was ready in {ElapsedMs} ms", toolName,
						toolStopwatch.ElapsedMilliseconds);
					return new CallToolResult()
					{
						Content = [new TextContentBlock() { Text = message }],
						IsError = true,
					};
				}

				var upstreamClient = upstreamTask.Result;

				logger.LogDebug("Invoking MCP tool {Tool}", toolName);

				var name = ctx.Params!.Name;
				var args = ctx.Params.Arguments ?? new Dictionary<string, JsonElement>();
				var adjustedArguments = args.ToDictionary(v => v.Key, v => (object?)v.Value);

				try
				{
					using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
					timeoutCts.CancelAfter(UpstreamCallToolTimeoutMs);

					var result = await upstreamClient.CallToolAsync(
						name,
						adjustedArguments,
						cancellationToken: timeoutCts.Token
					);

					toolStopwatch.Stop();
					LogTimeline(logger, "tool.forwarded-upstream.complete", toolStopwatch.ElapsedMilliseconds, toolName);
					logger.LogDebug("Forwarded MCP tool {Tool} to upstream in {ElapsedMs} ms", toolName,
						toolStopwatch.ElapsedMilliseconds);
					return result;
				}
				catch (OperationCanceledException) when (!ct.IsCancellationRequested)
				{
					toolStopwatch.Stop();
					var message = $"Tool '{toolName}' timed out after {UpstreamCallToolTimeoutMs / 1000}s waiting for the DevServer host to respond. " +
						"The host may be busy (building, restoring packages) or unresponsive. " +
						"Call uno_health for diagnostics, then retry.";
					logger.LogWarning("Tool {Tool} timed out after {ElapsedMs} ms", toolName,
						toolStopwatch.ElapsedMilliseconds);
					LogTimeline(logger, "tool.forwarded-upstream.timeout", toolStopwatch.ElapsedMilliseconds, toolName);
					return new CallToolResult()
					{
						Content = [new TextContentBlock() { Text = message }],
						IsError = true,
					};
				}
			})
			.WithListToolsHandler(async (ctx, ct) =>
			{
				var listToolsStopwatch = Stopwatch.StartNew();
				logger.LogTrace("RECV list_tools");
				await ensureRootsInitialized(ctx, tcs, ct);

				if (forceRootsFallback && getRoots().Length == 0)
				{
					List<Tool> tools = [addRootsTool];
					var cachedTools = toolListManager.GetCachedTools();
					if (cachedTools.Length > 0)
					{
						tools.AddRange(cachedTools);
					}

					logger.LogTrace("Upstream client is not connected, returning {Count} tools", tools.Count);
					listToolsStopwatch.Stop();
					LogTimeline(logger, "list-tools.roots-fallback.complete", listToolsStopwatch.ElapsedMilliseconds,
						$"count={tools.Count}");
					logger.LogDebug("Handled tools/list with roots fallback in {ElapsedMs} ms",
						listToolsStopwatch.ElapsedMilliseconds);
					return new() { Tools = ToolListManager.AppendBuiltInTools(tools) };
				}

				if (!toolListManager.HasCachedTools
					&& !mcpUpstreamClient.UpstreamClient.IsCompletedSuccessfully)
				{
					logger.LogTrace(
						"Upstream client is not ready and no cached tools are available, returning built-in tools immediately");
					listToolsStopwatch.Stop();
					LogTimeline(logger, "list-tools.builtins-only.complete", listToolsStopwatch.ElapsedMilliseconds,
						"count=0");
					logger.LogDebug("Handled tools/list with built-ins only in {ElapsedMs} ms",
						listToolsStopwatch.ElapsedMilliseconds);
					return new() { Tools = ToolListManager.AppendBuiltInTools([]) };
				}

				var result = await toolListManager.ListToolsWithTimeoutAsync(ct);
				listToolsStopwatch.Stop();
				LogTimeline(logger, "list-tools.upstream-or-cache.complete", listToolsStopwatch.ElapsedMilliseconds,
					$"count={result.Tools.Count}");
				logger.LogDebug("Handled tools/list with upstream/cached tools in {ElapsedMs} ms",
					listToolsStopwatch.ElapsedMilliseconds);
				return new() { Tools = ToolListManager.AppendBuiltInTools(result.Tools) };
			})
			.WithListResourcesHandler((ctx, ct) =>
			{
				var resource = new Resource
				{
					Uri = HealthService.HealthResourceUri,
					Name = "Uno DevServer Health",
					Description =
						"Real-time health status of the Uno DevServer MCP bridge, including connection state, tool count, and diagnostics.",
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

	internal static bool TryGetSelectSolutionPath(
		IDictionary<string, JsonElement>? arguments,
		out string? solutionPath,
		out CallToolResult errorResult)
	{
		solutionPath = null;
		errorResult = new CallToolResult()
		{
			Content = [new TextContentBlock() { Text = "Missing required 'solutionPath' argument." }],
			IsError = true
		};

		if (arguments is null || !arguments.TryGetValue("solutionPath", out var solutionPathElement))
		{
			return false;
		}

		if (solutionPathElement.ValueKind != JsonValueKind.String)
		{
			errorResult = new CallToolResult()
			{
				Content =
				[
					new TextContentBlock()
					{
						Text =
							"The 'solutionPath' argument must be a JSON string containing a non-empty absolute path."
					}
				],
				IsError = true
			};
			return false;
		}

		solutionPath = solutionPathElement.GetString();
		if (string.IsNullOrWhiteSpace(solutionPath) || !Path.IsPathRooted(solutionPath))
		{
			errorResult = new CallToolResult()
			{
				Content =
				[
					new TextContentBlock()
					{
						Text = "The 'solutionPath' argument must be a non-empty absolute path."
					}
				],
				IsError = true
			};
			return false;
		}

		return true;
	}

	internal static bool TryGetForceRestart(IDictionary<string, JsonElement>? arguments)
	{
		if (arguments is null || !arguments.TryGetValue("forceRestart", out var element))
		{
			return false;
		}

		return element.ValueKind == JsonValueKind.True;
	}
}

using System.Diagnostics;
using System.Linq;
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

	/// <summary>Maximum time to wait for the upstream to connect during uno_app_initialize.</summary>
	internal const int InitializeTimeoutMs = 120_000; // 2 minutes

	// ── Meta-tool definitions ──────────────────────────────────────────
	// These tools provide a compatibility layer for MCP clients that do not
	// re-query list_tools after a tools/list_changed notification. They are
	// included in the initial list_tools response and removed once the client
	// demonstrates support for tools/list_changed by re-querying.

	internal static readonly Tool InitializeTool = new()
	{
		Name = "uno_app_initialize",
		Description =
			"Initializes the Uno DevServer for a workspace. Sets the root directory, resolves the Uno solution, and starts the DevServer. "
			+ "Call this ONCE at the start of a session. After initialization succeeds, the Uno app tools become available. "
			+ "Do NOT call uno_app_select_solution after a successful initialization unless uno_health reports a WorkspaceAmbiguous issue.",
		InputSchema = JsonSerializer.Deserialize<JsonElement>(
			"""{"type":"object","required":["workspaceDirectory"],"properties":{"workspaceDirectory":{"type":"string","description":"Absolute path to the workspace root directory."},"solutionPath":{"type":"string","description":"Optional absolute path to the .sln or .slnx file to use."}}}"""),
		Annotations = new() { Title = "Initialize Workspace", DestructiveHint = false, IdempotentHint = true, ReadOnlyHint = false, OpenWorldHint = false },
	};

	internal static readonly Tool DiscoverToolsTool = new()
	{
		Name = "uno_discover_tools",
		Description =
			"Returns available Uno app tools with descriptions and input schemas. "
			+ "Call this after uno_app_initialize succeeds. Waits for the DevServer to be ready if needed.",
		InputSchema = JsonSerializer.Deserialize<JsonElement>("""{"type":"object","properties":{}}"""),
		Annotations = new() { Title = "Discover Tools", DestructiveHint = false, IdempotentHint = true, ReadOnlyHint = true, OpenWorldHint = false },
	};

	internal static readonly Tool ExecuteToolTool = new()
	{
		Name = "uno_execute_tool",
		Description =
			"Executes an Uno app tool by name. Use uno_discover_tools first to see available tools and their schemas.",
		InputSchema = JsonSerializer.Deserialize<JsonElement>(
			"""{"type":"object","required":["toolName"],"properties":{"toolName":{"type":"string","description":"Name of the tool to execute."},"arguments":{"type":"object","description":"Arguments to pass to the tool."}}}"""),
		Annotations = new() { Title = "Execute Tool", OpenWorldHint = true },
	};

	// ── Meta-tool tracking state ───────────────────────────────────────
	// Tracks whether the client supports tools/list_changed by observing
	// whether it re-queries list_tools after a notification.
	private volatile bool _listChangedNotificationSent;
	private volatile bool _clientReQueriedAfterListChanged;

	/// <summary>
	/// Called by ProxyLifecycleManager after tools/list_changed notification is sent downstream.
	/// </summary>
	internal void OnListChangedNotificationSent()
	{
		_listChangedNotificationSent = true;
	}

	/// <summary>
	/// Returns true when meta-tools (uno_discover_tools, uno_execute_tool) should be included
	/// in list_tools responses. Meta-tools are included until the client demonstrates support
	/// for tools/list_changed by re-querying list_tools after a notification.
	/// </summary>
	internal bool ShouldIncludeMetaTools => !_clientReQueriedAfterListChanged;

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
		Tool initializeTool,
		Tool selectSolutionTool,
		Func<bool> isForceRootsFallback,
		Func<string[]> getRoots,
		Func<string[], Task> setRootsHandler,
		Func<string, Task<CallToolResult>> selectSolutionHandler)
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

				// Handle uno_app_initialize (force-roots-fallback mode)
				if (isForceRootsFallback() && toolName == initializeTool.Name)
				{
					if (!TryGetInitializeArgs(ctx.Params?.Arguments, out var workspaceDir, out var initSolutionPath, out var initError))
					{
						return initError;
					}

					await setRootsHandler([workspaceDir!]);

					if (!string.IsNullOrWhiteSpace(initSolutionPath))
					{
						await selectSolutionHandler(initSolutionPath);
					}

					// Wait for upstream to connect (with timeout)
					using var initTimeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
					initTimeoutCts.CancelAfter(InitializeTimeoutMs);
					try
					{
						await tcs.Task.WaitAsync(initTimeoutCts.Token);
					}
					catch (OperationCanceledException) when (!ct.IsCancellationRequested)
					{
						logger.LogWarning("Timed out waiting for DevServer to connect during initialization");
					}

					toolStopwatch.Stop();
					LogTimeline(logger, "tool.initialize.complete", toolStopwatch.ElapsedMilliseconds, workspaceDir!);
					logger.LogDebug("Handled MCP tool {Tool} in {ElapsedMs} ms", toolName,
						toolStopwatch.ElapsedMilliseconds);
					return BuildInitializeResponse(workspaceDir!);
				}

				if (toolName == selectSolutionTool.Name)
				{
					if (!TryGetSelectSolutionPath(ctx.Params?.Arguments, out var solutionPath, out var errorResult))
					{
						return errorResult;
					}

					var selectionResult = await selectSolutionHandler(solutionPath!);
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

				// Handle uno_discover_tools (meta-tool for clients without list_changed)
				if (toolName == DiscoverToolsTool.Name)
				{
					try
					{
						var discoverResult = await toolListManager.ListToolsWithTimeoutAsync(ct);
						var discoveredTools = discoverResult.Tools;

						var discoveredJson = JsonSerializer.Serialize(discoveredTools, McpJsonUtilities.DefaultOptions);
						toolStopwatch.Stop();
						LogTimeline(logger, "tool.discover-tools.complete", toolStopwatch.ElapsedMilliseconds,
							$"count={discoveredTools.Count}");
						logger.LogDebug("Handled MCP tool {Tool} in {ElapsedMs} ms", toolName,
							toolStopwatch.ElapsedMilliseconds);
						return new CallToolResult { Content = [new TextContentBlock() { Text = discoveredJson }] };
					}
					catch (OperationCanceledException) when (!ct.IsCancellationRequested)
					{
						toolStopwatch.Stop();
						LogTimeline(logger, "tool.discover-tools.timeout", toolStopwatch.ElapsedMilliseconds, "timeout");
						return new CallToolResult
						{
							Content = [new TextContentBlock() { Text = "Timed out waiting for upstream tools. The DevServer may still be starting. Call uno_health for diagnostics, then retry." }],
							IsError = true
						};
					}
					catch (Exception ex)
					{
						toolStopwatch.Stop();
						logger.LogWarning(ex, "Failed to discover tools from upstream");
						LogTimeline(logger, "tool.discover-tools.error", toolStopwatch.ElapsedMilliseconds, ex.GetType().Name);
						return new CallToolResult
						{
							Content = [new TextContentBlock() { Text = $"Failed to discover tools: {ex.Message}. Call uno_health for diagnostics, then retry." }],
							IsError = true
						};
					}
				}

				// Handle uno_execute_tool (meta-tool for clients without list_changed)
				if (toolName == ExecuteToolTool.Name)
				{
					if (ctx.Params?.Arguments is not { } executeArgs
						|| !executeArgs.TryGetValue("toolName", out var targetToolNameElement)
						|| targetToolNameElement.ValueKind != JsonValueKind.String
						|| string.IsNullOrWhiteSpace(targetToolNameElement.GetString()))
					{
						return new CallToolResult
						{
							Content = [new TextContentBlock() { Text = "Missing required 'toolName' argument." }],
							IsError = true
						};
					}

					var targetToolName = targetToolNameElement.GetString()!;
					var targetArgs = new Dictionary<string, object?>();
					if (executeArgs.TryGetValue("arguments", out var targetArgsElement)
						&& targetArgsElement.ValueKind == JsonValueKind.Object)
					{
						foreach (var prop in targetArgsElement.EnumerateObject())
						{
							targetArgs[prop.Name] = prop.Value;
						}
					}

					var upstreamExecuteTask = mcpUpstreamClient.UpstreamClient;
					if (!upstreamExecuteTask.IsCompletedSuccessfully)
					{
						var executeMessage = healthService.ConnectionState == ConnectionState.Reconnecting
							? "DevServer host crashed and is reconnecting. Call uno_app_initialize or wait and retry."
							: "DevServer is not yet connected. Call uno_app_initialize first, or wait and retry.";
						toolStopwatch.Stop();
						LogTimeline(logger, "tool.execute-tool.rejected-upstream-not-ready",
							toolStopwatch.ElapsedMilliseconds, targetToolName);
						return new CallToolResult
						{
							Content = [new TextContentBlock() { Text = executeMessage }],
							IsError = true
						};
					}

					try
					{
						using var executeTimeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
						executeTimeoutCts.CancelAfter(UpstreamCallToolTimeoutMs);
						var executeResult = await upstreamExecuteTask.Result.CallToolAsync(
							targetToolName,
							targetArgs,
							cancellationToken: executeTimeoutCts.Token);
						toolStopwatch.Stop();
						LogTimeline(logger, "tool.execute-tool.complete", toolStopwatch.ElapsedMilliseconds,
							targetToolName);
						logger.LogDebug("Forwarded MCP tool {Tool} via execute_tool in {ElapsedMs} ms",
							targetToolName, toolStopwatch.ElapsedMilliseconds);
						return executeResult;
					}
					catch (OperationCanceledException) when (!ct.IsCancellationRequested)
					{
						toolStopwatch.Stop();
						return new CallToolResult
						{
							Content =
							[
								new TextContentBlock()
								{
									Text =
										$"Tool '{targetToolName}' timed out after {UpstreamCallToolTimeoutMs / 1000}s. Call uno_health for diagnostics."
								}
							],
							IsError = true
						};
					}
					catch (Exception ex)
					{
						toolStopwatch.Stop();
						logger.LogWarning(ex, "Error executing tool {Tool} via execute_tool", targetToolName);
						LogTimeline(logger, "tool.execute-tool.error", toolStopwatch.ElapsedMilliseconds, targetToolName);
						return new CallToolResult
						{
							Content =
							[
								new TextContentBlock()
								{
									Text =
										$"Tool '{targetToolName}' failed: {ex.Message}. Call uno_health for diagnostics."
								}
							],
							IsError = true
						};
					}
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

				// Track whether the client re-queried after tools/list_changed.
				// A re-query means the client supports list_changed and does not need meta-tools.
				if (_listChangedNotificationSent)
				{
					_clientReQueriedAfterListChanged = true;
				}

				await ensureRootsInitialized(ctx, tcs, ct);

				if (isForceRootsFallback() && getRoots().Length == 0)
				{
					List<Tool> tools = [initializeTool];

					logger.LogTrace("Upstream client is not connected, returning {Count} tools", tools.Count);
					listToolsStopwatch.Stop();
					LogTimeline(logger, "list-tools.roots-fallback.complete", listToolsStopwatch.ElapsedMilliseconds,
						$"count={tools.Count}");
					logger.LogDebug("Handled tools/list with roots fallback in {ElapsedMs} ms",
						listToolsStopwatch.ElapsedMilliseconds);
					var fallbackTools = ToolListManager.AppendBuiltInTools(tools);
					return new() { Tools = ShouldIncludeMetaTools ? AppendMetaTools(fallbackTools) : fallbackTools };
				}

				if (!mcpUpstreamClient.UpstreamClient.IsCompletedSuccessfully)
				{
					logger.LogTrace(
						"Upstream client is not ready, returning built-in tools immediately");
					listToolsStopwatch.Stop();
					LogTimeline(logger, "list-tools.builtins-only.complete", listToolsStopwatch.ElapsedMilliseconds,
						"count=0");
					logger.LogDebug("Handled tools/list with built-ins only in {ElapsedMs} ms",
						listToolsStopwatch.ElapsedMilliseconds);
					var builtInOnly = ToolListManager.AppendBuiltInTools([]);
					return new() { Tools = ShouldIncludeMetaTools ? AppendMetaTools(builtInOnly) : builtInOnly };
				}

				var result = await toolListManager.ListToolsWithTimeoutAsync(ct);
				listToolsStopwatch.Stop();
				LogTimeline(logger, "list-tools.upstream.complete", listToolsStopwatch.ElapsedMilliseconds,
					$"count={result.Tools.Count}");
				logger.LogDebug("Handled tools/list with upstream tools in {ElapsedMs} ms",
					listToolsStopwatch.ElapsedMilliseconds);
				var upstreamTools = ToolListManager.AppendBuiltInTools(result.Tools);
				return new() { Tools = ShouldIncludeMetaTools ? AppendMetaTools(upstreamTools) : upstreamTools };
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

	internal static bool TryGetInitializeArgs(
		IDictionary<string, JsonElement>? arguments,
		out string? workspaceDirectory,
		out string? solutionPath,
		out CallToolResult errorResult)
	{
		workspaceDirectory = null;
		solutionPath = null;
		errorResult = new CallToolResult()
		{
			Content = [new TextContentBlock() { Text = "Missing required 'workspaceDirectory' argument." }],
			IsError = true
		};

		if (arguments is null || !arguments.TryGetValue("workspaceDirectory", out var workspaceDirElement))
		{
			return false;
		}

		if (workspaceDirElement.ValueKind != JsonValueKind.String)
		{
			errorResult = new CallToolResult()
			{
				Content =
				[
					new TextContentBlock()
					{
						Text = "The 'workspaceDirectory' argument must be a string containing an absolute path."
					}
				],
				IsError = true
			};
			return false;
		}

		workspaceDirectory = workspaceDirElement.GetString();
		if (string.IsNullOrWhiteSpace(workspaceDirectory) || !Path.IsPathRooted(workspaceDirectory))
		{
			errorResult = new CallToolResult()
			{
				Content =
				[
					new TextContentBlock()
					{
						Text = "The 'workspaceDirectory' argument must be a non-empty absolute path."
					}
				],
				IsError = true
			};
			return false;
		}

		if (arguments.TryGetValue("solutionPath", out var solutionPathElement)
			&& solutionPathElement.ValueKind == JsonValueKind.String)
		{
			solutionPath = solutionPathElement.GetString();
		}

		return true;
	}

	private CallToolResult BuildInitializeResponse(string workspaceDirectory)
	{
		var upstreamTask = mcpUpstreamClient.UpstreamClient;
		var connected = upstreamTask.IsCompletedSuccessfully;
		var toolCount = toolListManager.SnapshotToolCount;
		var status = connected ? "ready" : "starting";
		var response = new
		{
			status,
			workspaceDirectory,
			toolCount,
			message = connected
				? $"DevServer initialized with {toolCount} tools available. Use uno_discover_tools for full schemas or uno_execute_tool to call tools."
				: "DevServer is starting. Use uno_discover_tools and uno_execute_tool when ready.",
		};

		var json = JsonSerializer.Serialize(response, McpJsonUtilities.DefaultOptions);
		return new CallToolResult
		{
			Content = [new TextContentBlock() { Text = json }],
		};
	}

	internal static IList<Tool> AppendMetaTools(IList<Tool> tools)
	{
		var result = new List<Tool>(tools.Count + 2);
		result.AddRange(tools);

		if (!tools.Any(t => string.Equals(t.Name, DiscoverToolsTool.Name, StringComparison.Ordinal)))
		{
			result.Add(DiscoverToolsTool);
		}

		if (!tools.Any(t => string.Equals(t.Name, ExecuteToolTool.Name, StringComparison.Ordinal)))
		{
			result.Add(ExecuteToolTool);
		}

		return result;
	}
}

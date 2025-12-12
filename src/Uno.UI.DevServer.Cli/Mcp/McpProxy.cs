using System.ComponentModel;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace Uno.UI.DevServer.Cli.Mcp;

internal class McpProxy
{
	private readonly ILogger<McpProxy> _logger;
	private readonly DevServerMonitor _devServerMonitor;
	private readonly McpClientProxy _mcpClientProxy;
	private readonly Tool _addRootsTool;
	private bool _waitForTools;
	private bool _forceRootsFallback;
	private string? _currentDirectory;
	private int _devServerPort;
	private List<string> _forwardedArgs = [];
	private string[] _roots = [];

	// Clients that don't support the list_updated notification
	private static readonly string[] ClientsWithoutListUpdateSupport = ["claude-code", "codex", "codex-mcp-client"];

	public McpProxy(ILogger<McpProxy> logger, DevServerMonitor mcpServerMonitor, McpClientProxy mcpClientProxy)
	{
		_logger = logger;
		_devServerMonitor = mcpServerMonitor;
		_mcpClientProxy = mcpClientProxy;

		_addRootsTool = McpServerTool.Create(SetRoots, new() { Name = "uno_app_set_roots" }).ProtocolTool;
	}

	public async Task<int> RunAsync(string currentDirectory, int port, List<string> forwardedArgs, bool waitForTools, bool forceRootsFallback, CancellationToken ct)
	{
		_waitForTools = waitForTools;
		_forceRootsFallback = forceRootsFallback;
		_currentDirectory = currentDirectory;
		_devServerPort = port;
		_forwardedArgs = forwardedArgs;

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

	[Description("This tool MUST be called before other uno app tools to initialize properly")]
	private async Task SetRoots([Description("Fully qualified root folder path for the workspace")] string[] roots)
	{
		if (!_forceRootsFallback)
		{
			return;
		}

		_roots = roots;

		await ProcessRoots();
	}

	private async Task ProcessRoots()
	{
		_logger.LogTrace("MCP Client Roots: {Roots}", string.Join(", ", _roots));

		if (_roots.FirstOrDefault() is { } rootUri)
		{
			var rootPath = GetRootPath(rootUri);
			if (!string.IsNullOrWhiteSpace(rootPath))
			{
				_devServerMonitor.StartMonitoring(rootPath, _devServerPort, _forwardedArgs);
			}
		}
		else if (!_forceRootsFallback)
		{
			_logger.LogWarning("No roots found and roots fallback is disabled; devserver was not started");
		}
	}

	private string? GetRootPath(string rootUri)
	{
		if (Uri.TryCreate(rootUri, UriKind.Absolute, out var absoluteUri) && absoluteUri.IsFile)
		{
			return absoluteUri.LocalPath;
		}

		if (Path.IsPathRooted(rootUri))
		{
			return Path.GetFullPath(rootUri);
		}

		if (!string.IsNullOrWhiteSpace(_currentDirectory))
		{
			var combined = Path.Combine(_currentDirectory, rootUri);
			return Path.GetFullPath(combined);
		}

		_logger.LogWarning("Unable to resolve MCP root path from '{RootUri}'", rootUri);
		return null;
	}

	private async Task<int> StartMcpStdIoProxyAsync(CancellationToken ct)
	{
		var tcs = new TaskCompletionSource();

		var builder = Host.CreateApplicationBuilder();
		builder.Services
			.AddMcpServer()
			.WithStdioServerTransport()
			.WithCallToolHandler(async (ctx, ct) =>
			{
				var upstreamClient = await _mcpClientProxy.UpstreamClient;

				if (_forceRootsFallback && ctx.Params?.Name == _addRootsTool.Name)
				{
					await SetRoots(ctx.Params.Arguments?["roots"].Deserialize<string[]>() ?? []);
					return new CallToolResult() { Content = [new TextContentBlock() { Text = "Ok" }] };
				}

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
				await EnsureRootsInitialized(ctx, tcs, ct);

				var upstreamClient = await _mcpClientProxy.UpstreamClient;

				_logger.LogTrace("Got upstream client");

				if (upstreamClient is null)
				{
					List<Tool> tools = [];

					if (_forceRootsFallback)
					{
						tools.Add(_addRootsTool);
					}

					_logger.LogTrace("Upstream client is not connected, returning {Count} tools", tools.Count);

					// The devserver is not started yet, so there are no tools to report.
					return new() { Tools = tools };
				}

				_logger.LogTrace("Client requested tools list update");

				var list = await upstreamClient!.ListToolsAsync(cancellationToken: ct);

				_logger.LogDebug("Reporting {Count} tools", list.Count);

				return new()
				{
					Tools = [.. list.Select(t => t.ProtocolTool)]
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
			_logger.LogTrace("Upstream tool list changed");

			tcs.TrySetResult();

			await host.Services.GetRequiredService<McpServer>().SendNotificationAsync(
				NotificationMethods.ToolListChangedNotification,
				new ToolListChangedNotificationParams()
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

	private async Task EnsureRootsInitialized(RequestContext<ListToolsRequestParams> ctx, TaskCompletionSource tcs, CancellationToken ct)
	{
		if (_roots.Length != 0)
		{
			// Already initialized
			return;
		}

		var clientSupportsRoots = !_forceRootsFallback && (ctx.Server.ClientCapabilities?.Roots?.ListChanged ?? false);

		if (clientSupportsRoots)
		{
			var roots = await ctx.Server.RequestRootsAsync(new(), ct);

			_logger.LogTrace("MCP Client supports roots: {Roots}", string.Join(", ", roots.Roots.Select(r => r.Uri)));

			if (roots.Roots.Count != 0)
			{
				_roots = [.. roots.Roots.Select(r => r.Uri)];
			}
			else
			{
				// convert _currentDirectory to a file uri
				if (Uri.TryCreate(_currentDirectory ?? Environment.CurrentDirectory, UriKind.RelativeOrAbsolute, out var root))
				{
					_roots = [root.ToString() ?? Environment.CurrentDirectory];
				}
			}
		}
		else
		{
			_logger.LogTrace("MCP Client does not support roots");

			_roots = [Environment.CurrentDirectory];
		}

		await ProcessRoots();

		// Claude Code and Codex do not support the list_updated notification.
		// To avoid tool invocation failures, we wait for the tools to be available
		// after the dev server has started.
		if ((!_forceRootsFallback || !clientSupportsRoots) &&
			(_waitForTools
			|| ClientsWithoutListUpdateSupport.Contains(ctx.Server.ClientInfo?.Name))
		)
		{
			_logger.LogTrace("Client without list_updated support detected, waiting for upstream server to start");

			await tcs.Task;
		}
	}
}

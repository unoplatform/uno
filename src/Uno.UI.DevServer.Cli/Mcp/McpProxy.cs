using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace Uno.UI.DevServer.Cli.Mcp;

internal class McpProxy
{
	private readonly ILogger<McpProxy> _logger;
	private readonly DevServerMonitor _devServerMonitor;
	private readonly McpClientProxy _mcpClientProxy;
	private readonly Tool _addRootsTool;
	private readonly string _toolCachePath;
	private Tool[] _toolCache = [];
	private bool _toolCacheLoaded;
	private bool _shouldRefreshToolCache = true;
	private readonly object _toolCacheLock = new();
	private bool _waitForTools;
	private bool _forceRootsFallback;
	private bool _forceGenerateToolCache;
	private string? _solutionDirectory;
	private bool _devServerStarted;
	private string? _currentDirectory;
	private int _devServerPort;
	private List<string> _forwardedArgs = [];
	private string[] _roots = [];
	private TaskCompletionSource<bool>? _toolCachePrimedTcs;
	private Task? _cachePrimedWatcher;

	private const string ToolCacheFileName = "tools-cache.json";
	private bool IsToolCacheEnabled => _forceRootsFallback || _forceGenerateToolCache;

	// Clients that don't support the list_updated notification
	private static readonly string[] ClientsWithoutListUpdateSupport = ["claude-code", "codex", "codex-mcp-client"];

	public McpProxy(ILogger<McpProxy> logger, DevServerMonitor mcpServerMonitor, McpClientProxy mcpClientProxy)
	{
		_logger = logger;
		_devServerMonitor = mcpServerMonitor;
		_mcpClientProxy = mcpClientProxy;

		_addRootsTool = McpServerTool.Create(SetRoots, new() { Name = "uno_app_set_roots" }).ProtocolTool;
		_toolCachePath = InitializeToolCachePath();
	}

	public async Task<int> RunAsync(
		string currentDirectory,
		int port,
		List<string> forwardedArgs,
		bool waitForTools,
		bool forceRootsFallback,
		bool forceGenerateToolCache,
		string? solutionDirectory,
		CancellationToken ct)
	{
		_waitForTools = waitForTools;
		_forceRootsFallback = forceRootsFallback;
		_forceGenerateToolCache = forceGenerateToolCache;
		_currentDirectory = currentDirectory;
		_devServerPort = port;
		_forwardedArgs = forwardedArgs;
		_solutionDirectory = NormalizeSolutionDirectory(solutionDirectory);
		InitializeToolCachePrimingTracker();

		EnsureDevServerStartedFromSolutionDirectory();

		try
		{
			var exitCode = await StartMcpStdIoProxyAsync(ct);

			if (!await EnsureCachePrimingCompletedAsync())
			{
				return 1;
			}

			return exitCode;
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
		var normalizedRoots = roots ?? Array.Empty<string>();
		_logger.LogTrace("SetRoots invoked with {Count} roots: {Roots}", normalizedRoots.Length, string.Join(", ", normalizedRoots));
		_roots = normalizedRoots;

		await ProcessRoots();
	}

	private static string InitializeToolCachePath()
	{
		var basePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
		if (string.IsNullOrWhiteSpace(basePath))
		{
			basePath = Path.GetTempPath();
		}

		return Path.Combine(basePath, "Uno Platform", "uno.devserver", ToolCacheFileName);
	}

	private async Task ProcessRoots()
	{
		_logger.LogTrace("Processing MCP Client Roots: {Roots}", string.Join(", ", _roots));

		if (_devServerStarted)
		{
			_logger.LogTrace("DevServer monitor already running; skipping additional root processing");
			return;
		}

		if (_roots.FirstOrDefault() is { } rootUri)
		{
			var rootPath = GetRootPath(rootUri);
			if (!string.IsNullOrWhiteSpace(rootPath))
			{
				_logger.LogTrace("Resolved MCP root '{RootUri}' into path '{RootPath}'", rootUri, rootPath);
				StartDevServerMonitor(rootPath);
			}
			else
			{
				_logger.LogWarning("Unable to resolve MCP root '{RootUri}' to a local path", rootUri);
			}
		}
		else if (!_forceRootsFallback)
		{
			_logger.LogWarning("No roots found and roots fallback is disabled; devserver was not started");
		}
	}

	private string? GetRootPath(string rootUri)
	{
		_logger.LogTrace("Attempting to resolve root URI '{RootUri}' using current directory '{CurrentDirectory}'", rootUri, _currentDirectory);

		if (Uri.TryCreate(rootUri, UriKind.Absolute, out var absoluteUri) && absoluteUri.IsFile)
		{
			var normalizedPath = NormalizeLocalFilePath(absoluteUri.LocalPath);
			return Path.GetFullPath(normalizedPath);
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

	private static string NormalizeLocalFilePath(string localPath)
	{
		if (!OperatingSystem.IsWindows())
		{
			return localPath;
		}

		// Windows file URIs (file:///c:/...) can include a leading slash in LocalPath ("/c:/...")
		// which later causes Path.GetFullPath to produce invalid "c:\c:\" style paths. Only strip
		// the leading slash when it matches the drive letter pattern `/X:/` to avoid altering other
		// path formats unnecessarily.
		if (localPath.Length >= 3
			&& localPath[0] == '/'
			&& char.IsLetter(localPath[1])
			&& localPath[2] == ':')
		{
			return localPath[1..];
		}

		return localPath;
	}

	private void EnsureDevServerStartedFromSolutionDirectory()
	{
		if (_forceRootsFallback)
		{
			_logger.LogTrace("Roots fallback is enabled; skipping initial DevServer start (waiting for roots)");
			return;
		}

		var directory = string.IsNullOrWhiteSpace(_solutionDirectory)
			? _currentDirectory
			: _solutionDirectory;

		_logger.LogTrace(
			"EnsureDevServerStartedFromSolutionDirectory (solutionDir: {SolutionDir}, currentDir: {CurrentDir})",
			_solutionDirectory,
			_currentDirectory);

		if (string.IsNullOrWhiteSpace(directory))
		{
			_logger.LogTrace("No directory available to start the DevServer monitor; skipping initial start");
			return;
		}

		StartDevServerMonitor(directory);
	}

	private void StartDevServerMonitor(string? directory)
	{
		if (_devServerStarted)
		{
			_logger.LogTrace("StartDevServerMonitor called but monitor already running");
			return;
		}

		var normalized = NormalizeSolutionDirectory(directory);
		if (string.IsNullOrWhiteSpace(normalized))
		{
			_logger.LogWarning("Unable to start DevServer monitor because the solution directory '{Directory}' is invalid", directory);
			FailToolCachePriming();
			return;
		}

		_logger.LogTrace("Starting DevServer monitor using solution directory {Directory}", normalized);
		try
		{
			_devServerMonitor.StartMonitoring(normalized, _devServerPort, _forwardedArgs);
			_devServerStarted = true;
			_logger.LogTrace("DevServer monitor started for {Directory} (port: {Port}, forwardedArgs: {Args})", normalized, _devServerPort, string.Join(" ", _forwardedArgs));
		}
		catch (Exception ex)
		{
			_logger.LogWarning(ex, "Unable to start DevServer monitor for solution directory '{Directory}'", normalized);
			FailToolCachePriming(ex);
		}
	}

	private string? NormalizeSolutionDirectory(string? directory)
	{
		if (string.IsNullOrWhiteSpace(directory))
		{
			return null;
		}

		try
		{
			var basePath = _currentDirectory ?? Environment.CurrentDirectory;
			var combined = Path.IsPathRooted(directory)
				? directory
				: Path.Combine(basePath, directory);
			return Path.GetFullPath(combined);
		}
		catch (Exception ex)
		{
			_logger.LogWarning(ex, "Unable to normalize solution directory '{Directory}'", directory);
			return null;
		}
	}

	private void InitializeToolCachePrimingTracker()
	{
		if (!_forceGenerateToolCache)
		{
			_toolCachePrimedTcs = null;
			_cachePrimedWatcher = null;
			return;
		}

		_toolCachePrimedTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
		_cachePrimedWatcher = null;
	}

	private void FailToolCachePriming(Exception? ex = null)
	{
		if (_toolCachePrimedTcs is null)
		{
			return;
		}

		if (ex is not null)
		{
			_toolCachePrimedTcs.TrySetException(ex);
		}
		else
		{
			_toolCachePrimedTcs.TrySetResult(false);
		}
	}

	private async Task<bool> EnsureCachePrimingCompletedAsync()
	{
		if (!_forceGenerateToolCache)
		{
			return true;
		}

		if (_toolCachePrimedTcs is null)
		{
			_logger.LogError("Tool cache priming tracker was not initialized");
			return false;
		}

		try
		{
			var result = await _toolCachePrimedTcs.Task;
			if (!result)
			{
				_logger.LogError("Tool cache priming failed");
			}
			return result;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Tool cache priming failed with an exception");
			return false;
		}
	}

	private void StartCachePrimingWatcher(IHost host)
	{
		if (!_forceGenerateToolCache || _toolCachePrimedTcs is null)
		{
			return;
		}

		_cachePrimedWatcher = CachePrimingWatcherAsync(host);
	}

	private async Task CachePrimingWatcherAsync(IHost host)
	{
		try
		{
			try
			{
				var result = await _toolCachePrimedTcs!.Task.ConfigureAwait(false);

				if (result)
				{
					_logger.LogInformation("Tool cache primed successfully; stopping MCP proxy");
				}
				else
				{
					_logger.LogWarning("Tool cache priming failed; stopping MCP proxy");
				}
			}
			catch (OperationCanceledException)
			{
				_logger.LogInformation("Tool cache priming was canceled; stopping MCP proxy");
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Tool cache priming failed with an exception; stopping MCP proxy");
			}

			try
			{
				await host.StopAsync().ConfigureAwait(false);
			}
			catch (Exception ex)
			{
				_logger.LogWarning(ex, "Error while stopping MCP proxy after tool cache priming");
			}
		}
		catch (Exception ex)
		{
			_logger.LogWarning(ex, "Unexpected error in cache priming watcher");
		}
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
				if (_forceRootsFallback && ctx.Params?.Name == _addRootsTool.Name)
				{
					await SetRoots(ctx.Params.Arguments?["roots"].Deserialize<string[]>() ?? []);
					return new CallToolResult() { Content = [new TextContentBlock() { Text = "Ok" }] };
				}

				var upstreamClient = await _mcpClientProxy.UpstreamClient;

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

				if (_forceRootsFallback && _roots.Length == 0)
				{
					List<Tool> tools = [_addRootsTool];
					var cachedTools = GetCachedTools();
					if (cachedTools.Length > 0)
					{
						tools.AddRange(cachedTools);
					}

					_logger.LogTrace("Upstream client is not connected, returning {Count} tools", tools.Count);

					// The devserver is not started yet, so there are no tools to report.
					return new() { Tools = tools };
				}

				var upstreamClient = await _mcpClientProxy.UpstreamClient;

				if (upstreamClient is null)
				{
					_logger.LogTrace("Upstream client is not connected, returning 0 tools");

					return new() { Tools = [] };
				}

				_logger.LogTrace("Got upstream client");

				_logger.LogTrace("Client requested tools list update");

				var list = await upstreamClient!.ListToolsAsync(cancellationToken: ct);
				Tool[] protocolTools = [.. list.Select(t => t.ProtocolTool)];

				_logger.LogDebug("Reporting {Count} tools", protocolTools.Length);

				PersistToolCacheIfNeeded(protocolTools);

				return new()
				{
					Tools = protocolTools
				};
			});

		builder.Logging.AddConsole(consoleLogOptions =>
		{
			// Configure all logs to go to stderr
			consoleLogOptions.LogToStandardErrorThreshold = LogLevel.Trace;
		});

		var host = builder.Build();

		StartCachePrimingWatcher(host);

		_mcpClientProxy.RegisterToolListChangedCallback(async () =>
		{
			_logger.LogTrace("Upstream tool list changed");

			lock (_toolCacheLock)
			{
				_shouldRefreshToolCache = true;
			}

			await RefreshCachedToolsFromUpstreamAsync();

			tcs.TrySetResult();

			await host.Services.GetRequiredService<McpServer>().SendNotificationAsync(
				NotificationMethods.ToolListChangedNotification,
				new ToolListChangedNotificationParams()
			);
		});

		_devServerMonitor.ServerFailed += () =>
		{
			_logger.LogError("DevServer failed to start, stopping MCP proxy");
			FailToolCachePriming();
			_ = Task.Run(async () =>
			{
				try
				{
					await host.StopAsync().ConfigureAwait(false);
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "Error while stopping MCP proxy host after DevServer failure.");
				}
			});
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

		if (!_forceRootsFallback)
		{
			var fallbackRoot = BuildFallbackRoot();

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
					_logger.LogTrace("MCP client returned no roots; defaulting to {FallbackRoot}", fallbackRoot);
					_roots = [fallbackRoot];
				}
			}
			else
			{
				_logger.LogTrace("MCP Client does not support roots; defaulting to {FallbackRoot}", fallbackRoot);
				_roots = [fallbackRoot];
			}

			await ProcessRoots();
		}

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

	private string BuildFallbackRoot()
	{
		var directory = _solutionDirectory ?? _currentDirectory ?? Environment.CurrentDirectory;

		try
		{
			var fullPath = Path.GetFullPath(directory);
			if (Uri.TryCreate(fullPath, UriKind.Absolute, out var uri) && uri.IsAbsoluteUri)
			{
				return uri.ToString();
			}

			return fullPath;
		}
		catch (Exception ex)
		{
			_logger.LogDebug(ex, "Unable to normalize fallback root '{Directory}'", directory);
			return directory;
		}
	}

	private async Task RefreshCachedToolsFromUpstreamAsync()
	{
		if (!IsToolCacheEnabled)
		{
			return;
		}

		try
		{
			var upstreamClient = await _mcpClientProxy.UpstreamClient;
			if (upstreamClient is null)
			{
				return;
			}

			var list = await upstreamClient.ListToolsAsync();
			Tool[] protocolTools = [.. list.Select(t => t.ProtocolTool)];

			PersistToolCacheIfNeeded(protocolTools);
		}
		catch (Exception ex)
		{
			_logger.LogWarning(ex, "Unable to refresh cached tools from upstream");
		}
	}

	private Tool[] GetCachedTools()
	{
		lock (_toolCacheLock)
		{
			if (_toolCacheLoaded)
			{
				return _toolCache;
			}

			_toolCacheLoaded = true;

			if (!IsToolCacheEnabled)
			{
				return _toolCache;
			}

			try
			{
				if (File.Exists(_toolCachePath))
				{
					var json = File.ReadAllText(_toolCachePath);
					if (ToolCacheFile.TryRead(
						json,
						_toolCachePath,
						_logger,
						out var cachedTools))
					{
						_toolCache = cachedTools;
						_logger.LogTrace("Loaded {Count} cached tools from {Path}", _toolCache.Length, _toolCachePath);
					}
					else
					{
						_logger.LogWarning("Tool cache validation failed, ignoring data from {Path}", _toolCachePath);
						_toolCache = [];
					}
				}
			}
			catch (Exception ex)
			{
				_logger.LogWarning(ex, "Unable to load tool cache from {Path}", _toolCachePath);
				_toolCache = [];
			}

			return _toolCache;
		}
	}

	private void PersistToolCacheIfNeeded(Tool[] tools)
	{
		if (!IsToolCacheEnabled || tools.Length == 0)
		{
			return;
		}

		lock (_toolCacheLock)
		{
			if (!_shouldRefreshToolCache)
			{
				return;
			}

			if (!ToolCacheFile.TryValidateCachedTools(
				tools,
				out var validationError))
			{
				_logger.LogWarning("Refusing to persist tool cache: {Reason}", validationError ?? "Unknown validation error");
				return;
			}

			var directory = Path.GetDirectoryName(_toolCachePath);
			try
			{
				if (!string.IsNullOrEmpty(directory))
				{
					Directory.CreateDirectory(directory);
				}

				var entry = ToolCacheFile.CreateEntry(tools);
				var json = JsonSerializer.Serialize(entry, McpJsonUtilities.DefaultOptions);
				File.WriteAllText(_toolCachePath, json);

				_toolCache = entry.Tools;
				_toolCacheLoaded = true;
				_shouldRefreshToolCache = false;

				_logger.LogTrace("Cached {Count} tools at {Path}", tools.Length, _toolCachePath);
				_toolCachePrimedTcs?.TrySetResult(true);
			}
			catch (Exception ex)
			{
				_logger.LogWarning(ex, "Unable to persist tool cache to {Path}", _toolCachePath);
				FailToolCachePriming(ex);
			}
		}
	}

}

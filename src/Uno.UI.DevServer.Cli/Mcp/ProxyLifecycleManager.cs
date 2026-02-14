using System.ComponentModel;
using System.IO;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace Uno.UI.DevServer.Cli.Mcp;

internal class ProxyLifecycleManager
{
	private readonly ILogger<ProxyLifecycleManager> _logger;
	private readonly DevServerMonitor _devServerMonitor;
	private readonly McpUpstreamClient _mcpClientProxy;
	private readonly HealthService _healthService;
	private readonly ToolListManager _toolListManager;
	private readonly McpStdioServer _mcpStdioServer;
	private readonly Tool _addRootsTool;
	private bool _waitForTools;
	private bool _forceRootsFallback;
	private bool _forceGenerateToolCache;
	private string? _solutionDirectory;
	private bool _devServerStarted;
	private string? _currentDirectory;
	private string? _workspaceHash;
	private int _devServerPort;
	private List<string> _forwardedArgs = [];
	private string[] _roots = [];
	private TaskCompletionSource<bool>? _toolCachePrimedTcs;
	private Task? _cachePrimedWatcher;

	// Clients that don't support the list_updated notification
	private static readonly string[] ClientsWithoutListUpdateSupport = ["claude-code", "codex", "codex-mcp-client", "antigravity"];

	public ProxyLifecycleManager(ILogger<ProxyLifecycleManager> logger, DevServerMonitor mcpServerMonitor, McpUpstreamClient mcpClientProxy, HealthService healthService, ToolListManager toolListManager, McpStdioServer mcpStdioServer)
	{
		_logger = logger;
		_devServerMonitor = mcpServerMonitor;
		_mcpClientProxy = mcpClientProxy;
		_healthService = healthService;
		_toolListManager = toolListManager;
		_mcpStdioServer = mcpStdioServer;

		_addRootsTool = McpServerTool.Create(SetRoots, new() { Name = "uno_app_set_roots" }).ProtocolTool;
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
		_workspaceHash = ToolCacheFile.ComputeWorkspaceHash(_solutionDirectory);
		_toolListManager.WorkspaceHash = _workspaceHash;
		_toolListManager.IsToolCacheEnabled = true;
		_toolListManager.OnToolCachePersisted = () => _toolCachePrimedTcs?.TrySetResult(true);
		_toolListManager.OnToolCachePersistFailed = ex => FailToolCachePriming(ex);
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
			if (string.IsNullOrEmpty(_workspaceHash))
			{
				_workspaceHash = ToolCacheFile.ComputeWorkspaceHash(normalized);
				_toolListManager.WorkspaceHash = _workspaceHash;
			}
			_devServerMonitor.StartMonitoring(normalized, _devServerPort, _forwardedArgs);
			_devServerStarted = true;
			_healthService.DevServerStarted = true;
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
		var (host, tcs) = _mcpStdioServer.BuildHost(
			EnsureRootsInitialized,
			_addRootsTool,
			_forceRootsFallback,
			() => _roots,
			async roots => await SetRoots(roots));

		StartCachePrimingWatcher(host);

		_mcpClientProxy.RegisterToolListChangedCallback(async () =>
		{
			_logger.LogTrace("Upstream tool list changed");

			_toolListManager.MarkShouldRefresh();

			await _toolListManager.RefreshCachedToolsFromUpstreamAsync();

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
			await _devServerMonitor.StopMonitoringAsync();
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

			using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
			timeoutCts.CancelAfter(ToolListManager.ListToolsTimeoutMs);

			try
			{
				await tcs.Task.WaitAsync(timeoutCts.Token);
			}
			catch (OperationCanceledException) when (!ct.IsCancellationRequested)
			{
				_logger.LogWarning("Timed out waiting for upstream tools after {Timeout}ms", ToolListManager.ListToolsTimeoutMs);
			}
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

}

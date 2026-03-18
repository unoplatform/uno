using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Uno.UI.DevServer.Cli.Helpers;

namespace Uno.UI.DevServer.Cli.Mcp;

/// <summary>
/// Top-level orchestrator for the MCP stdio-to-HTTP bridge lifecycle. Manages root initialization,
/// DevServer startup, cache priming, and event wiring between the other MCP components.
/// </summary>
/// <seealso cref="MonitorDecisions"/>
/// <seealso href="../../../specs/001-fast-devserver-startup/spec-appendix-d-mcp-improvements.md"/>
internal class ProxyLifecycleManager
{
	private readonly ILogger<ProxyLifecycleManager> _logger;
	private readonly DevServerMonitor _devServerMonitor;
	private readonly McpUpstreamClient _mcpUpstreamClient;
	private readonly HealthService _healthService;
	private readonly ToolListManager _toolListManager;
	private readonly McpStdioServer _mcpStdioServer;
	private readonly WorkspaceResolver _workspaceResolver;
	private readonly ISolutionFileFinder _solutionFileFinder;
	private readonly Tool _addRootsTool;

	internal static readonly Tool SelectSolutionTool = new()
	{
		Name = "uno_app_select_solution",
		Description =
			"Explicitly selects the Uno solution to use for this MCP session by absolute solution path. Starts or restarts the DevServer if required.",
		InputSchema = JsonSerializer.Deserialize<JsonElement>(
			"""{"type":"object","required":["solutionPath"],"properties":{"solutionPath":{"type":"string","description":"Absolute path to the .sln or .slnx file to use for this session."}}}"""),
	};

	private bool _waitForTools;
	private bool _forceRootsFallback;
	private bool _forceGenerateToolCache;
	private string? _solutionDirectory;
	private readonly MonitorDecisions.StartOnceGuard _devServerStartGuard = new();

	// ┌──────────────────────────────────────────────────────────────────┐
	// │              Observational Connection State Machine              │
	// │                                                                  │
	// │  Initializing                                                    │
	// │       │                                                          │
	// │       │ StartDevServerMonitor()                                  │
	// │       v                                                          │
	// │  Discovering ───[SDK/host resolved]───> Launching                │
	// │       │                                      │                   │
	// │       │ [resolution failed]                  │ [process ready]   │
	// │       v                                      v                   │
	// │  Degraded <──[max retries]──           Connecting                │
	// │       ^                        │             │                   │
	// │       │                        │             │ [toolListChanged] │
	// │       │                        │             v                   │
	// │       │                        └──────  Connected                │
	// │       │                                      │                   │
	// │       │                                      │ [ServerCrashed]   │
	// │       │                                      v                   │
	// │       └──[max reconnections]──         Reconnecting              │
	// │                                           │                      │
	// │                                           │ [retry]              │
	// │                                           v                      │
	// │                                       Discovering (cycle)        │
	// │                                                                  │
	// │  Any state ──[clean shutdown]──> Shutdown                        │
	// └──────────────────────────────────────────────────────────────────┘
	//
	// This state machine does NOT drive behavior — it observes the state
	// resulting from DevServerMonitor events and McpUpstreamClient callbacks.
	// Consumers: HealthService (diagnostics), McpStdioServer (error messages).
	//
	// Two separate counters:
	//   - DevServerMonitor: retry counter for initial startup failures
	//   - _reconnectionAttempts: crash→restart cycles (reset on Connected)

	private ConnectionState _connectionState = ConnectionState.Initializing;
	private int _reconnectionAttempts;
	private const int MaxReconnectionAttempts = 3;
	private string? _currentDirectory;
	private WorkspaceResolution? _workspaceResolution;
	private string? _workspaceHash;
	private int _devServerPort;
	private List<string> _forwardedArgs = [];
	private string[] _roots = [];
	private TaskCompletionSource<bool>? _toolCachePrimedTcs;
	private Task? _cachePrimedWatcher;
	private FileSystemWatcher? _workspaceMutationWatcher;
	private string? _workspaceMutationWatcherRoot;
	private CancellationTokenSource? _workspaceMutationDebounceCts;
	// Tracks whether any relevant filesystem mutation happened since the
	// last resolved snapshot.
	private int _workspaceMutationGeneration;
	// Marks which mutation generation produced the currently cached
	// workspace resolution.
	private int _workspaceResolutionGeneration;
	private readonly SemaphoreSlim _workspaceTransitionGate = new(1, 1);
	private readonly object _workspaceMutationWatcherGate = new();
	private readonly Func<string, FileSystemWatcher> _workspaceMutationWatcherFactory;

	private void LogTimeline(string stage, long elapsedMilliseconds, string details)
	{
		_logger.LogDebug("TIMELINE|proxy-lifecycle|{Stage}|{ElapsedMs}|{Details}", stage, elapsedMilliseconds, details);
	}

	public ProxyLifecycleManager(
		ILogger<ProxyLifecycleManager> logger,
		DevServerMonitor devServerMonitor,
		McpUpstreamClient mcpUpstreamClient,
		HealthService healthService,
		ToolListManager toolListManager,
		McpStdioServer mcpStdioServer,
		WorkspaceResolver workspaceResolver,
		ISolutionFileFinder? solutionFileFinder = null,
		Func<string, FileSystemWatcher>? workspaceMutationWatcherFactory = null)
	{
		_logger = logger;
		_devServerMonitor = devServerMonitor;
		_mcpUpstreamClient = mcpUpstreamClient;
		_healthService = healthService;
		_toolListManager = toolListManager;
		_mcpStdioServer = mcpStdioServer;
		_workspaceResolver = workspaceResolver;
		_solutionFileFinder = solutionFileFinder ?? new FileSystemSolutionFileFinder();
		_workspaceMutationWatcherFactory = workspaceMutationWatcherFactory ?? (root => new FileSystemWatcher(root));

		_addRootsTool = McpServerTool.Create(SetRoots, new() { Name = "uno_app_set_roots" }).ProtocolTool;
	}

	internal ConnectionState ConnectionState => _connectionState;

	private void SetConnectionState(ConnectionState newState)
	{
		var oldState = _connectionState;
		_connectionState = newState;
		_healthService.ConnectionState = newState;
		_logger.LogInformation("Connection state: {OldState} -> {NewState}", oldState, newState);
	}

	public async Task<int> RunAsync(
		string currentDirectory,
		WorkspaceResolution workspaceResolution,
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
		_workspaceResolution = workspaceResolution;
		_workspaceResolutionGeneration = Volatile.Read(ref _workspaceMutationGeneration);
		_devServerPort = port;
		_forwardedArgs = forwardedArgs;
		_solutionDirectory = NormalizeSolutionDirectory(solutionDirectory);
		_workspaceHash = ToolCacheFile.ComputeWorkspaceHash(
			workspaceResolution.EffectiveWorkspaceDirectory ?? _solutionDirectory ?? currentDirectory);
		_toolListManager.WorkspaceHash = _workspaceHash;
		_toolListManager.IsToolCacheEnabled = true;
		_toolListManager.OnToolCachePersisted = () => _toolCachePrimedTcs?.TrySetResult(true);
		_toolListManager.OnToolCachePersistFailed = ex => FailToolCachePriming(ex);
		InitializeToolCachePrimingTracker();
		StartWorkspaceMutationWatcher();

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
			_logger.LogError(ex, "Stdio server error: {Message}", ex.Message);
			return 1;
		}
	}

	[System.ComponentModel.Description("This tool MUST be called before other uno app tools to initialize properly")]
	private async Task SetRoots(
		[System.ComponentModel.Description("Fully qualified root folder path for the workspace")]
		string[] roots
	)
	{
		var normalizedRoots = roots ?? Array.Empty<string>();
		_logger.LogTrace("SetRoots invoked with {Count} roots: {Roots}", normalizedRoots.Length,
			string.Join(", ", normalizedRoots));
		_roots = normalizedRoots;

		await ProcessRoots();
	}

	internal async Task<SelectSolutionResult> SelectSolutionAsync(string solutionPath, CancellationToken ct = default)
	{
		var selectionStopwatch = Stopwatch.StartNew();

		if (string.IsNullOrWhiteSpace(solutionPath) || !Path.IsPathRooted(solutionPath))
		{
			return CreateRejectedSelectionResult(
				solutionPath,
				"The solutionPath argument must be an absolute path to a .sln or .slnx file.",
				[
					new ValidationIssue
					{
						Code = IssueCode.WorkspaceNotResolved,
						Severity = ValidationSeverity.Warning,
						Message = "The requested solution path is invalid.",
						Remediation =
							"Call uno_health to inspect candidateSolutions, then provide an absolute path to one of those solutions.",
					},
				]);
		}

		string normalizedSolutionPath;
		try
		{
			normalizedSolutionPath = Path.GetFullPath(solutionPath);
		}
		catch (Exception)
		{
			return CreateRejectedSelectionResult(
				solutionPath,
				"The solutionPath argument must be a valid absolute path to a .sln or .slnx file.",
				[
					new ValidationIssue
					{
						Code = IssueCode.WorkspaceNotResolved,
						Severity = ValidationSeverity.Warning,
						Message = "The requested solution path could not be normalized.",
						Remediation =
							"Call uno_health to inspect candidateSolutions, then provide a valid absolute path to one of those solutions.",
					},
				]);
		}

		var observationRoot =
			GetWorkspaceObservationRoot()
				?? _solutionDirectory
				?? _currentDirectory
				?? Path.GetDirectoryName(normalizedSolutionPath);

		if (string.IsNullOrWhiteSpace(observationRoot))
		{
			return CreateRejectedSelectionResult(
				normalizedSolutionPath,
				"Unable to determine the workspace root for this session.",
				[
					new ValidationIssue
					{
						Code = IssueCode.WorkspaceNotResolved,
						Severity = ValidationSeverity.Warning,
						Message = "The MCP bridge could not determine a workspace root for this session.",
						Remediation =
							"Call uno_health for diagnostics, or restart the MCP bridge from the intended repository.",
					},
				]);
		}

		var availableSolutions = GetCandidateSolutionsForSelection(observationRoot);
		LogTimeline(
			"select-solution.scan-candidates.complete",
			selectionStopwatch.ElapsedMilliseconds,
			$"root={observationRoot};count={availableSolutions.Count}");
		if (!availableSolutions.Any(candidate => PathComparison.PathsEqual(candidate, normalizedSolutionPath)))
		{
			return CreateRejectedSelectionResult(
				normalizedSolutionPath,
				"The requested solution is not one of the current candidate solutions for this session.",
				[
					new ValidationIssue
					{
						Code = IssueCode.WorkspaceNotResolved,
						Severity = ValidationSeverity.Warning,
						Message =
							"The requested solution is outside the candidate solution set for the current MCP session.",
						Remediation =
							"Call uno_health to inspect candidateSolutions, then choose one of those paths or restart the MCP bridge from a different repository root.",
					},
				]);
		}

		var nextResolution = await _workspaceResolver.ResolveSolutionAsync(
			observationRoot,
			normalizedSolutionPath,
			availableSolutions);
		LogTimeline(
			"select-solution.resolve-workspace.complete",
			selectionStopwatch.ElapsedMilliseconds,
			$"kind={nextResolution.ResolutionKind};workspace={nextResolution.EffectiveWorkspaceDirectory}");
		if (!nextResolution.IsResolved)
		{
			var discovery = await BuildDiscoveryInfoForSelectionAsync(observationRoot, nextResolution);
			return CreateRejectedSelectionResult(
				normalizedSolutionPath,
				"The requested solution does not resolve to a valid Uno workspace.",
				DiscoveryIssueMapper.MapDiscoveryIssues(discovery));
		}

		var previousResolution = _workspaceResolution;
		var transitionAction =
			await ApplyWorkspaceResolutionAsync(nextResolution, WorkspaceTransitionTrigger.UserSelection, ct);
		var selectedResolution = _workspaceResolution ?? nextResolution;
		LogTimeline(
			"select-solution.apply-transition.complete",
			selectionStopwatch.ElapsedMilliseconds,
			$"action={transitionAction};workspace={selectedResolution.EffectiveWorkspaceDirectory}");

		return new SelectSolutionResult
		{
			Status = DetermineSelectionStatus(previousResolution, selectedResolution, transitionAction),
			SelectedSolutionPath = selectedResolution.SelectedSolutionPath ?? normalizedSolutionPath,
			EffectiveWorkspaceDirectory = selectedResolution.EffectiveWorkspaceDirectory,
			DevServerAction = transitionAction.ToSelectionAction(),
			Message = BuildSelectionMessage(selectedResolution, transitionAction),
		};
	}

	private async Task<CallToolResult> SelectSolution(string solutionPath)
	{
		var result = await SelectSolutionAsync(solutionPath);
		var json = JsonSerializer.Serialize(result, McpJsonUtilities.DefaultOptions);
		return new CallToolResult
		{
			Content = [new TextContentBlock() { Text = json }],
			IsError = string.Equals(result.Status, "rejected", StringComparison.Ordinal),
		};
	}

	private async Task ProcessRoots()
	{
		var processRootsStopwatch = Stopwatch.StartNew();
		_logger.LogTrace("Processing MCP Client Roots: {Roots}", string.Join(", ", _roots));

		// The StartOnceGuard inside StartDevServerMonitor() prevents concurrent starts.
		// We don't early-return here so that roots from the MCP client can trigger
		// the monitor when no --solution-dir was provided.

		if (_roots.FirstOrDefault() is { } rootUri)
		{
			var rootPath = GetRootPath(rootUri);
			if (!string.IsNullOrWhiteSpace(rootPath))
			{
				var rootWorkspaceResolution = (await _workspaceResolver.ResolveAsync(rootPath)) with
				{
					SelectionSource = WorkspaceSelectionSource.RootsConfirmed,
				};
				LogTimeline(
					"set-roots.resolve-workspace.complete",
					processRootsStopwatch.ElapsedMilliseconds,
					$"root={rootPath};kind={rootWorkspaceResolution.ResolutionKind};workspace={rootWorkspaceResolution.EffectiveWorkspaceDirectory}");
				_logger.LogTrace("Resolved MCP root '{RootUri}' into path '{RootPath}'", rootUri, rootPath);
				await ApplyWorkspaceResolutionAsync(rootWorkspaceResolution, WorkspaceTransitionTrigger.McpRoots);
				LogTimeline(
					"set-roots.apply-transition.complete",
					processRootsStopwatch.ElapsedMilliseconds,
					$"workspace={rootWorkspaceResolution.EffectiveWorkspaceDirectory}");
				return;
			}
			else
			{
				_logger.LogWarning("Unable to resolve MCP root '{RootUri}' to a local path", rootUri);
			}
		}
		else if (_forceRootsFallback)
		{
			_logger.LogWarning(
				"No roots provided via uno_app_set_roots; DevServer startup is deferred until roots are set");
		}
	}

	internal async Task ReevaluateWorkspaceAsync(WorkspaceTransitionTrigger trigger, CancellationToken ct = default)
	{
		var workspaceRoot = GetWorkspaceObservationRoot();

		if (string.IsNullOrWhiteSpace(workspaceRoot))
		{
			_logger.LogDebug("Skipping workspace reevaluation because no workspace root is available");
			return;
		}

		var nextResolution = await _workspaceResolver.ResolveAsync(workspaceRoot);
		await ApplyWorkspaceResolutionAsync(nextResolution, trigger, ct);
	}

	internal async Task<WorkspaceTransitionAction> ApplyWorkspaceResolutionAsync(WorkspaceResolution nextResolution,
		WorkspaceTransitionTrigger trigger, CancellationToken ct = default)
	{
		var transitionStopwatch = Stopwatch.StartNew();
		await _workspaceTransitionGate.WaitAsync(ct);
		try
		{
			var transitionAction = WorkspaceTransitionDecisions.DetermineAction(
				_workspaceResolution,
				nextResolution,
				trigger,
				_healthService.DevServerStarted);

			switch (transitionAction)
			{
				case WorkspaceTransitionAction.Refresh:
					_logger.LogTrace(
						"{Trigger} confirmed existing workspace {Workspace}",
						trigger,
						_workspaceResolution?.EffectiveWorkspaceDirectory);
					_workspaceResolution = nextResolution;
					_workspaceResolutionGeneration = Volatile.Read(ref _workspaceMutationGeneration);
					UpdateWorkspaceHash(nextResolution);
					await EnsureWorkspaceMutationWatcherMatchesCurrentRootAsync();
					LogTimeline("transition.complete", transitionStopwatch.ElapsedMilliseconds,
						$"trigger={trigger};action={transitionAction};workspace={nextResolution.EffectiveWorkspaceDirectory}");
					return transitionAction;

				case WorkspaceTransitionAction.Start:
					_workspaceResolution = nextResolution;
					_workspaceResolutionGeneration = Volatile.Read(ref _workspaceMutationGeneration);
					UpdateWorkspaceHash(nextResolution);
					await EnsureWorkspaceMutationWatcherMatchesCurrentRootAsync();
					StartDevServerMonitor(nextResolution.EffectiveWorkspaceDirectory);
					LogTimeline("transition.complete", transitionStopwatch.ElapsedMilliseconds,
						$"trigger={trigger};action={transitionAction};workspace={nextResolution.EffectiveWorkspaceDirectory}");
					return transitionAction;

				case WorkspaceTransitionAction.Stop:
					_logger.LogInformation(
						"{Trigger} invalidated the current workspace {Workspace}; stopping the active DevServer session",
						trigger,
						_workspaceResolution?.EffectiveWorkspaceDirectory);
					await StopCurrentWorkspaceAsync();
					_workspaceResolution = nextResolution;
					_workspaceResolutionGeneration = Volatile.Read(ref _workspaceMutationGeneration);
					UpdateWorkspaceHash(nextResolution);
					await EnsureWorkspaceMutationWatcherMatchesCurrentRootAsync();
					SetConnectionState(ConnectionState.Degraded);
					FailToolCachePriming();
					LogTimeline("transition.complete", transitionStopwatch.ElapsedMilliseconds,
						$"trigger={trigger};action={transitionAction};workspace={nextResolution.EffectiveWorkspaceDirectory}");
					return transitionAction;

				case WorkspaceTransitionAction.Restart:
					_logger.LogInformation(
						"{Trigger} selected a new workspace {Workspace}; restarting the DevServer session",
						trigger,
						nextResolution.EffectiveWorkspaceDirectory);
					await StopCurrentWorkspaceAsync();
					_workspaceResolution = nextResolution;
					_workspaceResolutionGeneration = Volatile.Read(ref _workspaceMutationGeneration);
					UpdateWorkspaceHash(nextResolution);
					await EnsureWorkspaceMutationWatcherMatchesCurrentRootAsync();
					StartDevServerMonitor(nextResolution.EffectiveWorkspaceDirectory);
					LogTimeline("transition.complete", transitionStopwatch.ElapsedMilliseconds,
						$"trigger={trigger};action={transitionAction};workspace={nextResolution.EffectiveWorkspaceDirectory}");
					return transitionAction;

				case WorkspaceTransitionAction.Diagnose:
					if (_workspaceResolution?.IsResolved == true && nextResolution.IsResolved &&
						trigger == WorkspaceTransitionTrigger.McpRoots)
					{
						_logger.LogWarning(
							"MCP roots resolved to a different workspace ({IncomingWorkspace}) than the running workspace ({CurrentWorkspace}); keeping the existing workspace for this session",
							nextResolution.EffectiveWorkspaceDirectory,
							_workspaceResolution.EffectiveWorkspaceDirectory);
					}
					else
					{
						_logger.LogWarning(
							"{Trigger} did not resolve to a valid Uno workspace ({ResolutionKind}); staying in diagnostic mode for this session",
							trigger,
							nextResolution.ResolutionKind);
						_workspaceResolution = nextResolution;
						_workspaceResolutionGeneration = Volatile.Read(ref _workspaceMutationGeneration);
						UpdateWorkspaceHash(nextResolution);
						await EnsureWorkspaceMutationWatcherMatchesCurrentRootAsync();
					}

					SetConnectionState(ConnectionState.Degraded);
					FailToolCachePriming();
					LogTimeline("transition.complete", transitionStopwatch.ElapsedMilliseconds,
						$"trigger={trigger};action={transitionAction};workspace={nextResolution.EffectiveWorkspaceDirectory}");
					return transitionAction;

				default:
					_logger.LogWarning("Unexpected workspace transition action {Action} for trigger {Trigger}",
						transitionAction, trigger);
					SetConnectionState(ConnectionState.Degraded);
					FailToolCachePriming();
					LogTimeline("transition.complete", transitionStopwatch.ElapsedMilliseconds,
						$"trigger={trigger};action={transitionAction};workspace={nextResolution.EffectiveWorkspaceDirectory}");
					return transitionAction;
			}
		}
		finally
		{
			_workspaceTransitionGate.Release();
		}
	}

	internal static bool IsWorkspaceMutationPath(string? path)
	{
		if (string.IsNullOrWhiteSpace(path))
		{
			return false;
		}

		var fileName = Path.GetFileName(path);
		return string.Equals(fileName, "global.json", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(fileName, "project.assets.json", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(Path.GetExtension(path), ".sln", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(Path.GetExtension(path), ".slnx", StringComparison.OrdinalIgnoreCase);
	}

	private string? GetRootPath(string rootUri)
	{
		_logger.LogTrace("Attempting to resolve root URI '{RootUri}' using current directory '{CurrentDirectory}'",
			rootUri, _currentDirectory);

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
		if (localPath is ['/', _, _, ..]
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

		_logger.LogTrace(
			"EnsureDevServerStartedFromSolutionDirectory (solutionDir: {SolutionDir}, currentDir: {CurrentDir})",
			_solutionDirectory,
			_currentDirectory);

		if (_workspaceResolution?.IsResolved != true)
		{
			_logger.LogWarning(
				"Workspace resolution failed for {Directory}; DevServer startup is skipped and health remains immediately available for diagnostics",
				_currentDirectory);
			SetConnectionState(ConnectionState.Degraded);
			FailToolCachePriming();
			return;
		}

		if (!string.IsNullOrWhiteSpace(_solutionDirectory))
		{
			StartDevServerMonitor(_solutionDirectory);
			return;
		}

		_logger.LogTrace("No explicit solution directory; using resolved workspace {Directory}",
			_workspaceResolution.EffectiveWorkspaceDirectory);
		StartDevServerMonitor(_workspaceResolution.EffectiveWorkspaceDirectory);
	}

	private void StartDevServerMonitor(string? directory)
	{
		var startMonitorStopwatch = Stopwatch.StartNew();
		if (!_devServerStartGuard.TryStart())
		{
			_logger.LogTrace("StartDevServerMonitor called but monitor already running");
			LogTimeline("start-monitor.skipped-already-running", startMonitorStopwatch.ElapsedMilliseconds,
				directory ?? "<null>");
			return;
		}

		var normalized = NormalizeSolutionDirectory(directory);
		if (string.IsNullOrWhiteSpace(normalized))
		{
			_logger.LogWarning(
				"Unable to start DevServer monitor because the solution directory '{Directory}' is invalid", directory);
			_devServerStartGuard.Reset();
			FailToolCachePriming();
			LogTimeline("start-monitor.rejected-invalid-directory", startMonitorStopwatch.ElapsedMilliseconds,
				directory ?? "<null>");
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

			_devServerMonitor.StartMonitoring(normalized, _devServerPort, _forwardedArgs, _workspaceResolution);
			_healthService.DevServerStarted = true;
			SetConnectionState(ConnectionState.Discovering);
			_logger.LogTrace("DevServer monitor started for {Directory} (port: {Port}, forwardedArgs: {Args})",
				normalized, _devServerPort, string.Join(" ", _forwardedArgs));
			LogTimeline("start-monitor.dispatched", startMonitorStopwatch.ElapsedMilliseconds,
				$"directory={normalized};port={_devServerPort}");
		}
		catch (Exception ex)
		{
			_logger.LogWarning(ex, "Unable to start DevServer monitor for solution directory '{Directory}'",
				normalized);
			_devServerStartGuard.Reset();
			FailToolCachePriming(ex);
			LogTimeline("start-monitor.failed", startMonitorStopwatch.ElapsedMilliseconds, normalized);
		}
	}

	private async Task StopCurrentWorkspaceAsync()
	{
		await _devServerMonitor.StopMonitoringAsync();
		_healthService.DevServerStarted = false;
		_devServerStartGuard.Reset();
	}

	private void UpdateWorkspaceHash(WorkspaceResolution resolution)
	{
		var workspaceDirectory = resolution.EffectiveWorkspaceDirectory
			?? _solutionDirectory
			?? _currentDirectory;

		if (string.IsNullOrWhiteSpace(workspaceDirectory))
		{
			return;
		}

		_workspaceHash = ToolCacheFile.ComputeWorkspaceHash(workspaceDirectory);
		_toolListManager.WorkspaceHash = _workspaceHash;
	}

	internal void StartWorkspaceMutationWatcher()
	{
		lock (_workspaceMutationWatcherGate)
		{
			if (_workspaceMutationWatcher is not null)
			{
				return;
			}

			var workspaceRoot = GetWorkspaceObservationRoot();

			if (string.IsNullOrWhiteSpace(workspaceRoot) || !Directory.Exists(workspaceRoot))
			{
				_logger.LogTrace("Skipping workspace mutation watcher because {WorkspaceRoot} is not available",
					workspaceRoot);
				return;
			}

			FileSystemWatcher? watcher = null;
			try
			{
				watcher = _workspaceMutationWatcherFactory(workspaceRoot);
				watcher.IncludeSubdirectories = true;
				watcher.NotifyFilter = NotifyFilters.FileName
					| NotifyFilters.DirectoryName
					| NotifyFilters.LastWrite
					| NotifyFilters.CreationTime;

				watcher.Created += OnWorkspaceMutation;
				watcher.Changed += OnWorkspaceMutation;
				watcher.Deleted += OnWorkspaceMutation;
				watcher.Renamed += OnWorkspaceMutation;
				watcher.Error += OnWorkspaceMutationWatcherError;
				watcher.EnableRaisingEvents = true;

				_workspaceMutationWatcher = watcher;
				_workspaceMutationWatcherRoot = workspaceRoot;

				_logger.LogTrace("Started workspace mutation watcher for {WorkspaceRoot}", workspaceRoot);
			}
			catch (Exception ex)
			{
				if (watcher is not null)
				{
					try
					{
						watcher.Created -= OnWorkspaceMutation;
						watcher.Changed -= OnWorkspaceMutation;
						watcher.Deleted -= OnWorkspaceMutation;
						watcher.Renamed -= OnWorkspaceMutation;
						watcher.Error -= OnWorkspaceMutationWatcherError;
						watcher.Dispose();
					}
					catch
					{
						// Best-effort cleanup for partially initialized watcher instances.
					}
				}

				_workspaceMutationWatcher = null;
				_workspaceMutationWatcherRoot = null;
				_logger.LogWarning(ex, "Unable to start workspace mutation watcher for {WorkspaceRoot}", workspaceRoot);
			}
		}
	}

	internal ValueTask StopWorkspaceMutationWatcherAsync()
	{
		var debounceCts = Interlocked.Exchange(ref _workspaceMutationDebounceCts, null);
		TryCancelAndDispose(debounceCts);

		FileSystemWatcher? watcherToDispose = null;
		lock (_workspaceMutationWatcherGate)
		{
			if (_workspaceMutationWatcher is null)
			{
				return ValueTask.CompletedTask;
			}

			_workspaceMutationWatcher.EnableRaisingEvents = false;
			_workspaceMutationWatcher.Created -= OnWorkspaceMutation;
			_workspaceMutationWatcher.Changed -= OnWorkspaceMutation;
			_workspaceMutationWatcher.Deleted -= OnWorkspaceMutation;
			_workspaceMutationWatcher.Renamed -= OnWorkspaceMutation;
			_workspaceMutationWatcher.Error -= OnWorkspaceMutationWatcherError;
			watcherToDispose = _workspaceMutationWatcher;
			_workspaceMutationWatcher = null;
			_workspaceMutationWatcherRoot = null;
		}

		watcherToDispose?.Dispose();
		return ValueTask.CompletedTask;
	}

	private void OnWorkspaceMutation(object sender, FileSystemEventArgs args)
	{
		if (args is RenamedEventArgs renamedArgs)
		{
			var oldPathRelevant = IsWorkspaceMutationPath(renamedArgs.OldFullPath);
			var newPathRelevant = IsWorkspaceMutationPath(renamedArgs.FullPath);

			if (!oldPathRelevant && !newPathRelevant)
			{
				return;
			}

			_logger.LogTrace(
				"Workspace rename mutation detected from {OldPath} to {NewPath} ({ChangeType})",
				renamedArgs.OldFullPath,
				renamedArgs.FullPath,
				renamedArgs.ChangeType);
		}
		else if (!IsWorkspaceMutationPath(args.FullPath))
		{
			return;
		}
		else
		{
			_logger.LogTrace("Workspace mutation detected for {Path} ({ChangeType})", args.FullPath, args.ChangeType);
		}

		Interlocked.Increment(ref _workspaceMutationGeneration);
		var debounceCts = ReplaceWorkspaceMutationDebounceSource(ref _workspaceMutationDebounceCts);

		_ = Task.Run(async () =>
		{
			try
			{
				await Task.Delay(TimeSpan.FromMilliseconds(250), debounceCts.Token);
				await ReevaluateWorkspaceAsync(WorkspaceTransitionTrigger.FileSystem, debounceCts.Token);
			}
			catch (OperationCanceledException)
			{
				// Expected when a newer filesystem mutation supersedes the current debounce window.
			}
			catch (Exception ex)
			{
				_logger.LogWarning(ex, "Workspace reevaluation failed after filesystem mutation");
			}
			finally
			{
				var clearedCurrent = ClearCompletedWorkspaceMutationDebounceSource(
					ref _workspaceMutationDebounceCts,
					debounceCts);
				if (clearedCurrent)
				{
					TryCancel(debounceCts);
				}

				TryDispose(debounceCts);
			}
		}, CancellationToken.None);
	}

	private static void TryCancel(CancellationTokenSource? cts)
	{
		if (cts is null)
		{
			return;
		}

		try
		{
			cts.Cancel();
		}
		catch (ObjectDisposedException)
		{
			// A late watcher callback raced with teardown; disposal already completed.
		}
	}

	private static void TryDispose(CancellationTokenSource? cts)
	{
		if (cts is null)
		{
			return;
		}

		try
		{
			cts.Dispose();
		}
		catch (ObjectDisposedException)
		{
			// Ignore duplicate disposal from concurrent watcher callbacks.
		}
	}

	private static void TryCancelAndDispose(CancellationTokenSource? cts)
	{
		if (cts is null)
		{
			return;
		}

		TryCancel(cts);
		TryDispose(cts);
	}

	internal static CancellationTokenSource ReplaceWorkspaceMutationDebounceSource(ref CancellationTokenSource? field)
	{
		var next = new CancellationTokenSource();
		var previous = Interlocked.Exchange(ref field, next);
		TryCancel(previous);
		return next;
	}

	internal static bool ClearCompletedWorkspaceMutationDebounceSource(
		ref CancellationTokenSource? field,
		CancellationTokenSource completed)
	{
		var current = Interlocked.CompareExchange(ref field, null, completed);
		return ReferenceEquals(current, completed);
	}

	private void OnWorkspaceMutationWatcherError(object sender, ErrorEventArgs args)
	{
		Interlocked.Increment(ref _workspaceMutationGeneration);
		_logger.LogWarning(args.GetException(),
			"Workspace mutation watcher failed; restarting watcher and reevaluating the workspace");

		_ = Task.Run(async () =>
		{
			try
			{
				await StopWorkspaceMutationWatcherAsync();
				StartWorkspaceMutationWatcher();
				await ReevaluateWorkspaceAsync(WorkspaceTransitionTrigger.FileSystem);
			}
			catch (Exception ex)
			{
				_logger.LogWarning(ex, "Workspace reevaluation failed after watcher error");
			}
		}, CancellationToken.None);
	}

	private string? GetWorkspaceObservationRoot()
	{
		if (!string.IsNullOrWhiteSpace(_currentDirectory))
		{
			var activeWorkspace = _workspaceResolution?.EffectiveWorkspaceDirectory;
			if (string.IsNullOrWhiteSpace(activeWorkspace)
				|| IsPathWithinRoot(activeWorkspace, _currentDirectory))
			{
				return _currentDirectory;
			}
		}

		return _workspaceResolution?.RequestedWorkingDirectory
			?? _workspaceResolution?.EffectiveWorkspaceDirectory
			?? _currentDirectory;
	}

	private IReadOnlyList<string> GetCandidateSolutionsForSelection(string observationRoot)
	{
		// Reuse the latest candidate snapshot only while it is still fresh
		// for the same observation root.
		if (_workspaceResolution is { CandidateSolutions.Count: > 0 } resolution
			&& _workspaceResolutionGeneration == Volatile.Read(ref _workspaceMutationGeneration)
			&& PathComparison.PathsEqual(resolution.RequestedWorkingDirectory, observationRoot))
		{
			return resolution.CandidateSolutions;
		}

		return _solutionFileFinder.FindSolutionFiles(observationRoot);
	}

	private async Task EnsureWorkspaceMutationWatcherMatchesCurrentRootAsync()
	{
		var expectedRoot = GetWorkspaceObservationRoot();
		if (PathComparison.PathsEqual(_workspaceMutationWatcherRoot, expectedRoot))
		{
			return;
		}

		try
		{
			await StopWorkspaceMutationWatcherAsync();
			StartWorkspaceMutationWatcher();
		}
		catch (Exception ex)
		{
			_logger.LogWarning(ex, "Unable to realign workspace mutation watcher to {WorkspaceRoot}", expectedRoot);
		}
	}

	private static bool IsPathWithinRoot(string path, string root)
	{
		var fullPath = PathComparison.Normalize(path).TrimEnd('/');
		var fullRoot = PathComparison.Normalize(root).TrimEnd('/');

		return string.Equals(fullPath, fullRoot, StringComparison.Ordinal)
			|| fullPath.StartsWith($"{fullRoot}/", StringComparison.Ordinal);
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
					_logger.LogInformation("Tool cache primed successfully; stopping stdio server");
				}
				else
				{
					_logger.LogWarning("Tool cache priming failed; stopping stdio server");
				}
			}
			catch (OperationCanceledException)
			{
				_logger.LogInformation("Tool cache priming was canceled; stopping stdio server");
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Tool cache priming failed with an exception; stopping stdio server");
			}

			try
			{
				await host.StopAsync().ConfigureAwait(false);
			}
			catch (Exception ex)
			{
				_logger.LogWarning(ex, "Error while stopping stdio server after tool cache priming");
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
			SelectSolutionTool,
			_forceRootsFallback,
			() => _roots,
			async roots => await SetRoots(roots),
			async solutionPath => await SelectSolution(solutionPath));

		StartCachePrimingWatcher(host);

		_devServerMonitor.ServerLaunching += () =>
		{
			SetConnectionState(ConnectionState.Launching);
		};

		_devServerMonitor.ServerStarted += _ =>
		{
			SetConnectionState(ConnectionState.Connecting);
		};

		_mcpUpstreamClient.RegisterToolListChangedCallback(async () =>
		{
			_logger.LogTrace("Upstream tool list changed");

			SetConnectionState(ConnectionState.Connected);
			_reconnectionAttempts = 0;

			_toolListManager.MarkShouldRefresh();

			await _toolListManager.RefreshCachedToolsFromUpstreamAsync();

			tcs.TrySetResult();

			await host.Services.GetRequiredService<McpServer>().SendNotificationAsync(
				NotificationMethods.ToolListChangedNotification,
				new ToolListChangedNotificationParams()
			);
		});

		_devServerMonitor.ServerCrashed += () =>
		{
			_reconnectionAttempts++;
			if (_reconnectionAttempts > MaxReconnectionAttempts)
			{
				_logger.LogError("DevServer crashed {Attempts} times, entering degraded mode", _reconnectionAttempts);
				SetConnectionState(ConnectionState.Degraded);
			}
			else
			{
				_logger.LogWarning(
					"DevServer crashed (attempt {Attempt}/{Max}), resetting connection for reconnection",
					_reconnectionAttempts, MaxReconnectionAttempts);
				SetConnectionState(ConnectionState.Reconnecting);
				_ = _mcpUpstreamClient.ResetConnectionAsync();
			}
		};

		_devServerMonitor.ServerFailed += () =>
		{
			var discovery = _devServerMonitor.LastDiscoveryInfo;
			_logger.LogError(
				"DevServer host failed to start after retries; bridge stays alive for diagnostics via uno_health. " +
				"Host={HostPath}, SDK={SdkVersion}, Solution={Solution}, AddIns={AddInCount}",
				discovery?.HostPath ?? "<not resolved>",
				discovery?.UnoSdkVersion ?? "<unknown>",
				_workspaceResolution?.SelectedSolutionPath ?? "<none>",
				discovery?.AddIns?.Count ?? 0);
			SetConnectionState(ConnectionState.Degraded);
			FailToolCachePriming();
		};

		try
		{
			await host.RunAsync();
		}
		finally
		{
			await StopWorkspaceMutationWatcherAsync();
			SetConnectionState(ConnectionState.Shutdown);
			await _mcpUpstreamClient.DisposeAsync();
			await _devServerMonitor.StopMonitoringAsync();
		}

		return 0;
	}

	private async Task EnsureRootsInitialized(RequestContext<ListToolsRequestParams> ctx, TaskCompletionSource tcs,
		CancellationToken ct)
	{
		if (_roots.Length != 0)
		{
			// Already initialized
			return;
		}

		var clientSupportsRoots = MonitorDecisions.DetermineClientSupportsRoots(
			_forceRootsFallback,
			rootsCapabilityPresent: ctx.Server.ClientCapabilities?.Roots is not null);

		if (!_forceRootsFallback)
		{
			var fallbackRoot = BuildFallbackRoot();

			if (clientSupportsRoots)
			{
				var roots = await ctx.Server.RequestRootsAsync(new(), ct);

				_logger.LogTrace("MCP Client supports roots: {Roots}",
					string.Join(", ", roots.Roots.Select(r => r.Uri)));

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

		if (_workspaceResolution?.IsResolved != true && !_healthService.DevServerStarted)
		{
			_logger.LogTrace(
				"Workspace is unresolved; skipping upstream wait so health can report the failure immediately");
			tcs.TrySetResult();
			return;
		}

		if (ShouldWaitForUpstreamTools(_forceRootsFallback, clientSupportsRoots, _waitForTools))
		{
			_logger.LogTrace("No cached tools available, waiting for upstream server to start");

			using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
			timeoutCts.CancelAfter(ToolListManager.ListToolsTimeoutMs);

			try
			{
				await tcs.Task.WaitAsync(timeoutCts.Token);
			}
			catch (OperationCanceledException) when (!ct.IsCancellationRequested)
			{
				_logger.LogWarning("Timed out waiting for upstream tools after {Timeout}ms",
					ToolListManager.ListToolsTimeoutMs);
			}
		}
	}

	internal static bool ShouldWaitForUpstreamTools(bool forceRootsFallback, bool clientSupportsRoots,
		bool waitForTools)
		=> (!forceRootsFallback || !clientSupportsRoots) && waitForTools;

	private string BuildFallbackRoot()
	{
		var directory = _workspaceResolution?.EffectiveWorkspaceDirectory
			?? _solutionDirectory
			?? _currentDirectory
			?? Environment.CurrentDirectory;

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

	private async Task<DiscoveryInfo> BuildDiscoveryInfoForSelectionAsync(string observationRoot,
		WorkspaceResolution resolution)
	{
		var unoToolsLocator =
			new UnoToolsLocator(Microsoft.Extensions.Logging.Abstractions.NullLogger<UnoToolsLocator>.Instance);
		return await unoToolsLocator.DiscoverAsync(observationRoot, resolution);
	}

	private static string DetermineSelectionStatus(
		WorkspaceResolution? previousResolution,
		WorkspaceResolution selectedResolution,
		WorkspaceTransitionAction transitionAction)
	{
		return transitionAction switch
		{
			WorkspaceTransitionAction.Start => "started",
			WorkspaceTransitionAction.Restart => "restarted",
			WorkspaceTransitionAction.Refresh when previousResolution?.IsResolved == true
				&& WorkspaceTransitionDecisions.IsSameWorkspace(previousResolution, selectedResolution) => "already_selected",
			_ => "selected",
		};
	}

	private static string BuildSelectionMessage(WorkspaceResolution resolution, WorkspaceTransitionAction action)
	{
		return action switch
		{
			WorkspaceTransitionAction.Start =>
				$"Selected solution '{resolution.SelectedSolutionPath}' and started DevServer for '{resolution.EffectiveWorkspaceDirectory}'.",
			WorkspaceTransitionAction.Restart =>
				$"Selected solution '{resolution.SelectedSolutionPath}' and restarted DevServer for '{resolution.EffectiveWorkspaceDirectory}'.",
			WorkspaceTransitionAction.Refresh =>
				$"Solution '{resolution.SelectedSolutionPath}' is already selected for this session.",
			_ => $"Selected solution '{resolution.SelectedSolutionPath}'.",
		};
	}

	private static SelectSolutionResult CreateRejectedSelectionResult(
		string? solutionPath,
		string message,
		IReadOnlyList<ValidationIssue> issues)
	{
		return new SelectSolutionResult
		{
			Status = "rejected",
			SelectedSolutionPath = solutionPath,
			DevServerAction = "None",
			Message = message,
			Issues = issues,
		};
	}
}

internal sealed record SelectSolutionResult
{
	public required string Status { get; init; }
	public string? SelectedSolutionPath { get; init; }
	public string? EffectiveWorkspaceDirectory { get; init; }
	public required string DevServerAction { get; init; }
	public required string Message { get; init; }
	public IReadOnlyList<ValidationIssue>? Issues { get; init; }
}

internal static class WorkspaceTransitionActionExtensions
{
	public static string ToSelectionAction(this WorkspaceTransitionAction action)
		=> action switch
		{
			WorkspaceTransitionAction.Start => "Start",
			WorkspaceTransitionAction.Restart => "Restart",
			_ => "None",
		};
}

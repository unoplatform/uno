using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace Uno.UI.DevServer.Cli.Mcp;

/// <summary>
/// Manages the tool cache and list operations. Handles cached tool loading/persistence,
/// upstream tool fetching with timeout, and built-in tool injection (e.g. uno_health).
/// </summary>
internal class ToolListManager(
	ILogger<ToolListManager> logger,
	McpUpstreamClient mcpUpstreamClient,
	DevServerMonitor devServerMonitor)
{
	private readonly string _toolCachePath = InitializeToolCachePath();
	private Tool[] _toolCache = [];
	private bool _toolCacheLoaded;
	private bool _shouldRefreshToolCache = true;
	private readonly object _toolCacheLock = new();

	private const string ToolCacheFileName = "tools-cache.json";

	/// <summary>Maximum time to wait for upstream list_tools before returning cached/empty result.</summary>
	internal const int ListToolsTimeoutMs = 30_000;

	public string? WorkspaceHash { get; set; }

	public bool IsToolCacheEnabled { get; set; }

	public int CachedToolCount
	{
		get { lock (_toolCacheLock) { return _toolCache.Length; } }
	}

	public bool HasCachedTools => GetCachedTools().Length > 0;

	/// <summary>Called after the tool cache is successfully persisted.</summary>
	public Action? OnToolCachePersisted { get; set; }

	/// <summary>Called when tool cache persistence fails.</summary>
	public Action<Exception>? OnToolCachePersistFailed { get; set; }

	public void MarkShouldRefresh()
	{
		lock (_toolCacheLock)
		{
			_shouldRefreshToolCache = true;
		}
	}

	public Tool[] GetCachedTools()
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
						logger,
						out var cachedTools,
						expectedWorkspaceHash: WorkspaceHash,
						expectedUnoSdkVersion: devServerMonitor.UnoSdkVersion))
					{
						_toolCache = cachedTools;
						logger.LogTrace("Loaded {Count} cached tools from {Path}", _toolCache.Length, _toolCachePath);
					}
					else
					{
						logger.LogWarning("Tool cache validation failed, ignoring data from {Path}", _toolCachePath);
						_toolCache = [];
					}
				}
			}
			catch (Exception ex)
			{
				logger.LogWarning(ex, "Unable to load tool cache from {Path}", _toolCachePath);
				_toolCache = [];
			}

			return _toolCache;
		}
	}

	public void PersistToolCacheIfNeeded(Tool[] tools)
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
				logger.LogWarning("Refusing to persist tool cache: {Reason}", validationError ?? "Unknown validation error");
				return;
			}

			var directory = Path.GetDirectoryName(_toolCachePath);
			try
			{
				if (!string.IsNullOrEmpty(directory))
				{
					Directory.CreateDirectory(directory);
				}

				var entry = ToolCacheFile.CreateEntry(tools, WorkspaceHash, devServerMonitor.UnoSdkVersion);
				var json = JsonSerializer.Serialize(entry, McpJsonUtilities.DefaultOptions);

				// Atomic write: write to temp file then move
				var tempPath = _toolCachePath + ".tmp";
				File.WriteAllText(tempPath, json);
				File.Move(tempPath, _toolCachePath, overwrite: true);

				_toolCache = entry.Tools;
				_toolCacheLoaded = true;
				_shouldRefreshToolCache = false;

				logger.LogTrace("Cached {Count} tools at {Path}", tools.Length, _toolCachePath);
				OnToolCachePersisted?.Invoke();
			}
			catch (Exception ex)
			{
				logger.LogWarning(ex, "Unable to persist tool cache to {Path}", _toolCachePath);

				// Clean up temp file if it was left behind
				try
				{
					var tempPath = _toolCachePath + ".tmp";
					if (File.Exists(tempPath))
					{
						File.Delete(tempPath);
					}
				}
				catch
				{
					// Best-effort cleanup
				}

				OnToolCachePersistFailed?.Invoke(ex);
			}
		}
	}

	public async Task RefreshCachedToolsFromUpstreamAsync()
	{
		if (!IsToolCacheEnabled)
		{
			return;
		}

		try
		{
			var upstreamClient = await mcpUpstreamClient.UpstreamClient;

			var list = await upstreamClient.ListToolsAsync();
			Tool[] protocolTools = [.. list.Select(t => t.ProtocolTool).DistinctBy(t => t.Name)];

			PersistToolCacheIfNeeded(protocolTools);
		}
		catch (Exception ex)
		{
			logger.LogWarning(ex, "Unable to refresh cached tools from upstream");
		}
	}

	public async Task<ListToolsResult> ListToolsWithTimeoutAsync(CancellationToken ct)
	{
		var upstreamTask = mcpUpstreamClient.UpstreamClient;

		// If the upstream client is already available, use it directly
		if (upstreamTask.IsCompletedSuccessfully)
		{
			return await FetchToolsFromUpstreamAsync(upstreamTask.Result, ct);
		}

		// If we have cached tools, return them immediately without waiting
		var cachedTools = GetCachedTools();
		if (cachedTools.Length > 0)
		{
			logger.LogDebug("Returning {Count} cached tools while upstream connects", cachedTools.Length);
			return new() { Tools = cachedTools };
		}

		// No cache available — wait for upstream with a bounded timeout
		logger.LogTrace("No cached tools available, waiting for upstream with {Timeout}ms timeout", ListToolsTimeoutMs);

		try
		{
			using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
			timeoutCts.CancelAfter(ListToolsTimeoutMs);

			var client = await upstreamTask.WaitAsync(timeoutCts.Token);
			return await FetchToolsFromUpstreamAsync(client, ct);
		}
		catch (OperationCanceledException) when (!ct.IsCancellationRequested)
		{
			logger.LogWarning("Timed out waiting for upstream MCP after {Timeout}ms, returning empty tool list", ListToolsTimeoutMs);
			return new() { Tools = [] };
		}
		catch (Exception ex) when (ex is not OperationCanceledException)
		{
			logger.LogWarning(ex, "Error waiting for upstream MCP, returning empty tool list");
			return new() { Tools = [] };
		}
	}

	public async Task<ListToolsResult> FetchToolsFromUpstreamAsync(McpClient upstreamClient, CancellationToken ct)
	{
		logger.LogTrace("Client requested tools list update");

		var list = await upstreamClient.ListToolsAsync(cancellationToken: ct);
		Tool[] protocolTools = [.. list.Select(t => t.ProtocolTool).DistinctBy(t => t.Name)];

		logger.LogDebug("Reporting {Count} tools", protocolTools.Length);

		PersistToolCacheIfNeeded(protocolTools);

		return new() { Tools = protocolTools };
	}

	public static IList<Tool> AppendBuiltInTools(IList<Tool> tools)
	{
		// Avoid duplicates if the tool already appears (e.g. from cache)
		if (tools.Any(t => t.Name == HealthService.HealthTool.Name))
		{
			return tools;
		}

		// Prepend so the health tool is always visible
		var result = new List<Tool>(tools.Count + 1) { HealthService.HealthTool };
		result.AddRange(tools);
		return result;
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
}

using System.Linq;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace Uno.UI.DevServer.Cli.Mcp;

/// <summary>
/// Manages tool list operations. Handles upstream tool fetching with timeout
/// and built-in tool injection (e.g. uno_health).
/// </summary>
internal class ToolListManager(
	ILogger<ToolListManager> logger,
	McpUpstreamClient mcpUpstreamClient)
{
	private volatile int _snapshotToolCount;
	private volatile bool _hasFetchedTools;

	/// <summary>Maximum time to wait for upstream list_tools before returning empty result.</summary>
	internal const int ListToolsTimeoutMs = 60_000;

	/// <summary>Maximum time to wait for a single upstream ListToolsAsync call.</summary>
	internal const int UpstreamCallTimeoutMs = 30_000;

	/// <summary>Lock-free snapshot of tool count, safe to read from any thread without blocking.</summary>
	public int SnapshotToolCount => _snapshotToolCount;

	/// <summary>
	/// True once at least one upstream tools/list completed successfully, so a
	/// <see cref="SnapshotToolCount"/> of 0 means "upstream exposes no tools"
	/// rather than "not fetched yet".
	/// </summary>
	public bool HasFetchedTools => _hasFetchedTools;

	/// <summary>
	/// Returns true when <paramref name="toolName"/> was present in the last upstream tool list.
	/// </summary>
	public bool IsKnownUpstreamTool(string toolName)
	{
		var names = _snapshotToolNames;
		return names is not null && names.Contains(toolName);
	}

	private volatile HashSet<string>? _snapshotToolNames;

	public async Task<ListToolsResult> ListToolsWithTimeoutAsync(CancellationToken ct)
	{
		var upstreamTask = mcpUpstreamClient.UpstreamClient;

		// If the upstream client is already available, use it directly
		if (upstreamTask.IsCompletedSuccessfully)
		{
			try
			{
				return await FetchToolsFromUpstreamAsync(upstreamTask.Result, ct);
			}
			catch (OperationCanceledException) when (!ct.IsCancellationRequested)
			{
				logger.LogWarning("Timed out fetching tools from upstream after {Timeout}ms, returning empty tool list", UpstreamCallTimeoutMs);
				return new() { Tools = [] };
			}
		}

		// No upstream yet — wait with a bounded timeout
		logger.LogTrace("Upstream not ready, waiting for upstream with {Timeout}ms timeout", ListToolsTimeoutMs);

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

		using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
		timeoutCts.CancelAfter(UpstreamCallTimeoutMs);

		var list = await upstreamClient.ListToolsAsync(cancellationToken: timeoutCts.Token);
		Tool[] protocolTools = [.. list.Select(t => t.ProtocolTool).DistinctBy(t => t.Name)];

		logger.LogDebug("Reporting {Count} tools", protocolTools.Length);

		_snapshotToolCount = protocolTools.Length;
		_snapshotToolNames = new HashSet<string>(protocolTools.Select(t => t.Name), StringComparer.Ordinal);
		_hasFetchedTools = true;

		return new() { Tools = protocolTools };
	}

	public static IList<Tool> AppendBuiltInTools(IList<Tool> tools)
	{
		var result = new List<Tool>(tools.Count + 2);
		var builtInTools = new[] { HealthService.HealthTool, ProxyLifecycleManager.SelectSolutionTool };

		foreach (var builtInTool in builtInTools)
		{
			var existingTool = tools.FirstOrDefault(t => string.Equals(t.Name, builtInTool.Name, StringComparison.Ordinal));
			result.Add(existingTool ?? builtInTool);
		}

		foreach (var tool in tools)
		{
			if (!builtInTools.Any(builtInTool => string.Equals(tool.Name, builtInTool.Name, StringComparison.Ordinal)))
			{
				result.Add(tool);
			}
		}

		return result;
	}
}

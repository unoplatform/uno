using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using ModelContextProtocol.Protocol;
using Uno.UI.DevServer.Cli.Helpers;

namespace Uno.UI.DevServer.Cli.Mcp;

/// <summary>
/// Produces health reports and diagnostics for the Uno DevServer MCP bridge. Exposes the uno_health tool
/// and the uno://health resource, reporting connection state, tool count, and issues.
/// </summary>
/// <seealso href="../health-diagnostics.md"/>
internal class HealthService(
	McpUpstreamClient mcpUpstreamClient,
	DevServerMonitor devServerMonitor,
	ToolListManager toolListManager)
{
	internal const string HealthResourceUri = "uno://health";

	internal static readonly Tool HealthTool = new()
	{
		Name = "uno_health",
		Description = "Returns the health status of the Uno DevServer MCP bridge, including connection state, tool count, and any issues detected during startup. Always available, even before the upstream host is ready.",
		InputSchema = JsonSerializer.Deserialize<JsonElement>("""{"type":"object","properties":{}}"""),
	};

	/// <summary>Set by ProxyLifecycleManager when the DevServer monitor has been started.</summary>
	public bool DevServerStarted { get; set; }

	/// <summary>Set by ProxyLifecycleManager on state transitions.</summary>
	public ConnectionState ConnectionState { get; set; }

	public CallToolResult BuildHealthToolResponse()
	{
		var report = BuildHealthReport();
		var json = JsonSerializer.Serialize(report, McpJsonUtilities.DefaultOptions);
		return new CallToolResult()
		{
			Content = [new TextContentBlock() { Text = json }],
		};
	}

	public HealthReport BuildHealthReport()
	{
		var upstreamTask = mcpUpstreamClient.UpstreamClient;
		var upstreamConnected = upstreamTask.IsCompletedSuccessfully;

		var toolCount = toolListManager.GetCachedTools().Length;

		var discoveredSolutions = devServerMonitor.DiscoveredSolutions is { Count: > 0 }
			? devServerMonitor.DiscoveredSolutions
			: null;

		if (discoveredSolutions is null && devServerMonitor.SolutionNotFound)
		{
			discoveredSolutions = [];
		}

		return HealthReportFactory.Create(
			devServerMonitor.LastDiscoveryInfo,
			DevServerStarted,
			upstreamConnected,
			toolCount,
			ConnectionState,
			discoveredSolutions,
			upstreamError: upstreamTask.IsFaulted
				? upstreamTask.Exception?.InnerException?.Message ?? "Unknown error"
				: null);
	}

	internal static DiscoverySummary? MapDiscovery(DiscoveryInfo? info)
	{
		if (info is null)
		{
			return null;
		}

		return new DiscoverySummary
		{
			RequestedWorkingDirectory = info.RequestedWorkingDirectory,
			WorkingDirectory = info.WorkingDirectory,
			EffectiveWorkspaceDirectory = info.EffectiveWorkspaceDirectory,
			SelectedSolutionPath = info.SelectedSolutionPath,
			SelectedGlobalJsonPath = info.SelectedGlobalJsonPath,
			ResolutionKind = info.ResolutionKind,
			CandidateSolutions = info.CandidateSolutions.Count > 0 ? info.CandidateSolutions : null,
			DotNetVersion = info.DotNetVersion,
			UnoSdkVersion = info.UnoSdkVersion,
			UnoSdkPath = info.UnoSdkPath,
			HostPath = info.HostPath,
			SettingsPath = info.SettingsPath,
			AddIns = info.AddIns.Select(a => new AddInSummary
			{
				PackageName = a.PackageName,
				PackageVersion = a.PackageVersion,
				EntryPointDll = a.EntryPointDll,
				DiscoverySource = a.DiscoverySource,
			}).ToList(),
			ActiveServers = info.ActiveServers.Count > 0
				? info.ActiveServers.Select(s => new ActiveServerSummary
				{
					ProcessId = s.ProcessId,
					Port = s.Port,
					McpEndpoint = s.McpEndpoint,
					ParentProcessId = s.ParentProcessId,
					StartTime = s.StartTime,
					IdeChannelId = s.IdeChannelId,
					SolutionPath = s.SolutionPath,
					IsInWorkspace = s.IsInWorkspace,
				}).ToList()
				: null,
		};
	}
}

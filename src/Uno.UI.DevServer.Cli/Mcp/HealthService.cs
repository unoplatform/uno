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
		var issues = new List<ValidationIssue>();
		var upstreamTask = mcpUpstreamClient.UpstreamClient;
		var upstreamConnected = upstreamTask.IsCompletedSuccessfully;

		if (!DevServerStarted)
		{
			issues.Add(new ValidationIssue
			{
				Code = IssueCode.HostNotStarted,
				Severity = ValidationSeverity.Fatal,
				Message = "The DevServer host process has not been started yet.",
				Remediation = "Ensure the working directory contains a global.json with the Uno.Sdk, or provide roots via uno_app_set_roots.",
			});
		}

		if (ConnectionState == ConnectionState.Reconnecting)
		{
			issues.Add(new ValidationIssue
			{
				Code = IssueCode.HostCrashed,
				Severity = ValidationSeverity.Warning,
				Message = "The DevServer host process crashed and is being restarted automatically.",
				Remediation = "Wait a few seconds for the host to restart. Tools will become available again once the connection is re-established.",
			});
		}
		else if (ConnectionState == ConnectionState.Degraded)
		{
			issues.Add(new ValidationIssue
			{
				Code = IssueCode.HostCrashed,
				Severity = ValidationSeverity.Fatal,
				Message = "The DevServer host process crashed repeatedly and could not be restarted.",
				Remediation = "Check the DevServer logs for errors. You may need to restart the MCP proxy manually.",
			});
		}

		if (upstreamTask.IsFaulted)
		{
			var errorMessage = upstreamTask.Exception?.InnerException?.Message ?? "Unknown error";
			issues.Add(new ValidationIssue
			{
				Code = IssueCode.UpstreamError,
				Severity = ValidationSeverity.Fatal,
				Message = $"Failed to connect to upstream MCP server: {errorMessage}",
				Remediation = "Check that the DevServer host process started correctly and is listening on the expected port.",
			});
		}
		else if (DevServerStarted && !upstreamConnected)
		{
			issues.Add(new ValidationIssue
			{
				Code = IssueCode.HostUnreachable,
				Severity = ValidationSeverity.Warning,
				Message = "The DevServer host process is started but the upstream MCP connection is not yet established.",
				Remediation = "The host may still be initializing. Wait a few seconds and retry.",
			});
		}

		issues.AddRange(DiscoveryIssueMapper.MapDiscoveryIssues(devServerMonitor.LastDiscoveryInfo));

		var toolCount = toolListManager.GetCachedTools().Length;

		var status = issues.Any(i => i.Severity == ValidationSeverity.Fatal)
			? HealthStatus.Unhealthy
			: issues.Count > 0
				? HealthStatus.Degraded
				: HealthStatus.Healthy;

		return new HealthReport
		{
			Status = status,
			DevServerVersion = McpStdioServer.GetAssemblyVersion(),
			UpstreamConnected = upstreamConnected,
			ToolCount = toolCount,
			UnoSdkVersion = devServerMonitor.UnoSdkVersion,
			DiscoveryDurationMs = devServerMonitor.DiscoveryDurationMs,
			ConnectionState = ConnectionState,
			Issues = issues,
			Discovery = MapDiscovery(devServerMonitor.LastDiscoveryInfo),
		};
	}

	private static DiscoverySummary? MapDiscovery(DiscoveryInfo? info)
	{
		if (info is null)
		{
			return null;
		}

		return new DiscoverySummary
		{
			WorkingDirectory = info.WorkingDirectory,
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
				}).ToList()
				: null,
		};
	}
}

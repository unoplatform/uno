using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using ModelContextProtocol.Protocol;

namespace Uno.UI.DevServer.Cli.Mcp;

/// <summary>
/// Produces health reports and diagnostics for the Uno DevServer MCP bridge. Exposes the uno_health tool
/// and the uno://health resource, reporting connection state, tool count, and issues.
/// </summary>
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

		var toolCount = 0;
		if (upstreamConnected)
		{
			toolCount = toolListManager.CachedToolCount;
		}

		var status = issues.Any(i => i.Severity == ValidationSeverity.Fatal)
			? HealthStatus.Unhealthy
			: issues.Count > 0
				? HealthStatus.Degraded
				: HealthStatus.Healthy;

		return new HealthReport
		{
			Status = status,
			DevServerVersion = typeof(HealthService).Assembly.GetName().Version?.ToString(),
			UpstreamConnected = upstreamConnected,
			ToolCount = toolCount,
			UnoSdkVersion = devServerMonitor.UnoSdkVersion,
			DiscoveryDurationMs = devServerMonitor.DiscoveryDurationMs,
			Issues = issues,
		};
	}
}

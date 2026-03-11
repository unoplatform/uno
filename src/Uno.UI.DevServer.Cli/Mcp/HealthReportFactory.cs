using System.Linq;
using Uno.UI.DevServer.Cli.Helpers;

namespace Uno.UI.DevServer.Cli.Mcp;

internal static class HealthReportFactory
{
	public static HealthReport Create(
		DiscoveryInfo? discovery,
		bool devServerStarted,
		bool upstreamConnected,
		int toolCount,
		ConnectionState? connectionState,
		IReadOnlyList<string>? discoveredSolutions,
		int? hostProcessId = null,
		string? hostEndpoint = null,
		string? upstreamError = null)
	{
		var issues = new List<ValidationIssue>();

		if (!devServerStarted)
		{
			issues.Add(new ValidationIssue
			{
				Code = IssueCode.HostNotStarted,
				Severity = ValidationSeverity.Fatal,
				Message = "The DevServer host process has not been started yet.",
				Remediation = "Ensure the workspace can be resolved, or start the MCP bridge from a valid Uno workspace.",
			});
		}

		if (connectionState == ConnectionState.Reconnecting)
		{
			issues.Add(new ValidationIssue
			{
				Code = IssueCode.HostCrashed,
				Severity = ValidationSeverity.Warning,
				Message = "The DevServer host process crashed and is being restarted automatically.",
				Remediation = "Wait a few seconds for the host to restart. Tools will become available again once the connection is re-established.",
			});
		}
		else if (connectionState == ConnectionState.Degraded)
		{
			issues.Add(new ValidationIssue
			{
				Code = IssueCode.HostCrashed,
				Severity = ValidationSeverity.Fatal,
				Message = "The DevServer host process crashed repeatedly and could not be restarted.",
				Remediation = "Check the DevServer logs for errors. You may need to restart the MCP proxy manually.",
			});
		}

		if (!string.IsNullOrWhiteSpace(upstreamError))
		{
			issues.Add(new ValidationIssue
			{
				Code = IssueCode.UpstreamError,
				Severity = ValidationSeverity.Fatal,
				Message = $"Failed to connect to upstream MCP server: {upstreamError}",
				Remediation = "Check that the DevServer host process started correctly and is listening on the expected port.",
			});
		}
		else if (devServerStarted && !upstreamConnected)
		{
			issues.Add(new ValidationIssue
			{
				Code = IssueCode.HostUnreachable,
				Severity = ValidationSeverity.Warning,
				Message = "The DevServer host process is started but the upstream MCP connection is not yet established.",
				Remediation = "The host may still be initializing. Wait a few seconds and retry.",
			});
		}

		issues.AddRange(DiscoveryIssueMapper.MapDiscoveryIssues(discovery));

		var status = issues.Any(i => i.Severity == ValidationSeverity.Fatal)
			? HealthStatus.Unhealthy
			: issues.Count > 0
				? HealthStatus.Degraded
				: HealthStatus.Healthy;

		return new HealthReport
		{
			Status = status,
			DevServerVersion = GetAssemblyVersion(),
			HostProcessId = hostProcessId,
			HostEndpoint = hostEndpoint,
			UpstreamConnected = upstreamConnected,
			ToolCount = toolCount,
			UnoSdkVersion = discovery?.UnoSdkVersion,
			DiscoveryDurationMs = discovery?.AddInsDiscoveryDurationMs ?? 0,
			ConnectionState = connectionState,
			DiscoveredSolutions = discoveredSolutions,
			EffectiveWorkspaceDirectory = discovery?.EffectiveWorkspaceDirectory,
			SelectedSolutionPath = discovery?.SelectedSolutionPath,
			ResolutionKind = discovery?.ResolutionKind,
			CandidateSolutions = discovery?.CandidateSolutions,
			Issues = issues,
			Discovery = MapDiscovery(discovery),
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

	private static string GetAssemblyVersion()
	{
		var attr = typeof(HealthReportFactory).Assembly
			.GetCustomAttributes(typeof(System.Reflection.AssemblyInformationalVersionAttribute), false)
			.OfType<System.Reflection.AssemblyInformationalVersionAttribute>()
			.FirstOrDefault();

		if (attr is not null)
		{
			var parts = attr.InformationalVersion.Split('+', StringSplitOptions.RemoveEmptyEntries);
			return parts[0];
		}

		return typeof(HealthReportFactory).Assembly.GetName().Version?.ToString() ?? "0.0.0";
	}
}

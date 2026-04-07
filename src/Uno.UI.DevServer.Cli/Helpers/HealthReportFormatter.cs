using System.Globalization;
using System.Text;
using System.Text.Json;
using Uno.UI.DevServer.Cli.Mcp;

namespace Uno.UI.DevServer.Cli.Helpers;

internal static class HealthReportFormatter
{
	public static string FormatJson(HealthReport report)
		=> JsonSerializer.Serialize(report, McpJsonUtilities.DefaultOptions);

	public static string FormatPlainText(HealthReport report)
	{
		var builder = new StringBuilder();
		builder.AppendLine($"Status: {report.Status}");
		builder.AppendLine($"Connection: {report.ConnectionState?.ToString() ?? "Unknown"}");
		builder.AppendLine($"Workspace: {report.EffectiveWorkspaceDirectory ?? "<unresolved>"}");
		builder.AppendLine($"Solution: {report.SelectedSolutionPath ?? "<none>"}");
		builder.AppendLine($"Resolution: {report.ResolutionKind?.ToString() ?? "<unknown>"}");
		builder.AppendLine($"Selection: {report.SelectionSource?.ToString() ?? "<unknown>"}");
		builder.AppendLine($"Tools: {report.ToolCount}");

		if (report.CandidateSolutions is { Count: > 0 })
		{
			builder.AppendLine("Candidates:");
			foreach (var candidate in report.CandidateSolutions)
			{
				builder.AppendLine($"- {candidate}");
			}
		}

		if (report.Issues.Count > 0)
		{
			builder.AppendLine("Issues:");
			foreach (var issue in report.Issues)
			{
				builder.AppendLine($"- [{issue.Severity}] {issue.Code}: {issue.Message}");
			}
		}

		if (report.Discovery?.ActiveServers is { Count: > 0 } activeServers)
		{
			builder.AppendLine("Active Servers:");
			foreach (var server in activeServers)
			{
				builder.AppendLine($"- PID: {server.ProcessId} (Port {server.Port})");
				builder.AppendLine($"  Solution: {server.SolutionPath ?? "<none>"}");
				builder.AppendLine($"  IDE Channel: {server.IdeChannelId ?? "<none>"}");
				builder.AppendLine($"  Parent PID: {server.ParentProcessId}");
				builder.AppendLine($"  Started: {server.StartTime.ToString("yyyy-MM-dd HH:mm:ss UTC", CultureInfo.InvariantCulture)}");
				builder.AppendLine($"  Workspace Match: {server.IsInWorkspace}");

				if (server.ProcessChain is { Count: > 0 })
				{
					builder.AppendLine($"  Process Chain: {FormatProcessChain(server.ProcessChain)}");
				}
			}
		}

		return builder.ToString().TrimEnd();
	}

	private static string FormatProcessChain(IReadOnlyList<ProcessChainEntry> processChain)
		=> ProcessChainEntry.FormatChain(processChain);
}

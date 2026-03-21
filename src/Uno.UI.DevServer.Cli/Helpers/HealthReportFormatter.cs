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

		return builder.ToString().TrimEnd();
	}
}

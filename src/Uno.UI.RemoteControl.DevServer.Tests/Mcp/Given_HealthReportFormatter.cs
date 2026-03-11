using AwesomeAssertions;
using Uno.UI.DevServer.Cli.Helpers;
using Uno.UI.DevServer.Cli.Mcp;

namespace Uno.UI.RemoteControl.DevServer.Tests.Mcp;

[TestClass]
public class Given_HealthReportFormatter
{
	[TestMethod]
	public void WhenJsonRequested_SerializesSameHealthModel()
	{
		var report = new HealthReport
		{
			Status = HealthStatus.Degraded,
			UpstreamConnected = false,
			EffectiveWorkspaceDirectory = @"D:\src\studio.live\src",
			SelectedSolutionPath = @"D:\src\studio.live\src\StudioLive.slnx",
			ResolutionKind = WorkspaceResolutionKind.AutoDiscovered,
			Issues =
			[
				new ValidationIssue
				{
					Code = IssueCode.HostNotStarted,
					Severity = ValidationSeverity.Fatal,
					Message = "The DevServer host process has not been started yet.",
				},
			],
		};

		var json = HealthReportFormatter.FormatJson(report);

		json.Should().Contain("\"status\":\"Degraded\"");
		json.Should().Contain("\"effectiveWorkspaceDirectory\":\"D:\\\\src\\\\studio.live\\\\src\"");
		json.Should().Contain("\"resolutionKind\":\"AutoDiscovered\"");
	}

	[TestMethod]
	public void WhenPlainTextRequested_IncludesStatusAndWorkspaceDetails()
	{
		var report = new HealthReport
		{
			Status = HealthStatus.Healthy,
			UpstreamConnected = true,
			ToolCount = 11,
			EffectiveWorkspaceDirectory = @"D:\src\studio.live\src",
			SelectedSolutionPath = @"D:\src\studio.live\src\StudioLive.slnx",
			ResolutionKind = WorkspaceResolutionKind.AutoDiscovered,
			Issues = [],
		};

		var text = HealthReportFormatter.FormatPlainText(report);

		text.Should().Contain("Status: Healthy");
		text.Should().Contain(@"Workspace: D:\src\studio.live\src");
		text.Should().Contain(@"Solution: D:\src\studio.live\src\StudioLive.slnx");
		text.Should().Contain("Resolution: AutoDiscovered");
	}
}

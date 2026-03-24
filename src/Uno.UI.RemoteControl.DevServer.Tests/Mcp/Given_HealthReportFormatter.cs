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

	[TestMethod]
	public void WhenPlainTextRequested_IncludesActiveServerOwnershipDetails()
	{
		var report = new HealthReport
		{
			Status = HealthStatus.Degraded,
			UpstreamConnected = true,
			ToolCount = 11,
			EffectiveWorkspaceDirectory = @"D:\src\studio.live\src",
			SelectedSolutionPath = @"D:\src\studio.live\src\StudioLive.slnx",
			ResolutionKind = WorkspaceResolutionKind.AutoDiscovered,
			Issues = [],
			Discovery = new DiscoverySummary
			{
				ActiveServers =
				[
					new ActiveServerSummary
					{
						ProcessId = 100,
						Port = 61616,
						McpEndpoint = "http://localhost:61616/mcp",
						ParentProcessId = 50,
						IdeChannelId = "abc",
						SolutionPath = @"D:\src\studio.live\src\StudioLive.slnx",
						IsInWorkspace = true,
						ProcessChain =
						[
							new ProcessChainEntry { ProcessId = 100, ProcessName = "Uno.UI.RemoteControl.Host" },
							new ProcessChainEntry { ProcessId = 50, ProcessName = "dotnet" },
							new ProcessChainEntry { ProcessId = 25, ProcessName = "kiro" },
						],
					},
				],
			},
		};

		var text = HealthReportFormatter.FormatPlainText(report);

		text.Should().Contain("Active Servers:");
		text.Should().Contain("Process Chain: 100 (Uno.UI.RemoteControl.Host) -> 50 (dotnet) -> 25 (kiro)");
		text.Should().Contain("IDE Channel: abc");
	}
}

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
			EffectiveWorkspaceDirectory = @"D:\src\myapp\src",
			SelectedSolutionPath = @"D:\src\myapp\src\MyApp.slnx",
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
		json.Should().Contain("\"effectiveWorkspaceDirectory\":\"D:\\\\src\\\\myapp\\\\src\"");
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
			EffectiveWorkspaceDirectory = @"D:\src\myapp\src",
			SelectedSolutionPath = @"D:\src\myapp\src\MyApp.slnx",
			ResolutionKind = WorkspaceResolutionKind.AutoDiscovered,
			Issues = [],
		};

		var text = HealthReportFormatter.FormatPlainText(report);

		text.Should().Contain("Status: Healthy");
		text.Should().Contain(@"Workspace: D:\src\myapp\src");
		text.Should().Contain(@"Solution: D:\src\myapp\src\MyApp.slnx");
		text.Should().Contain("Resolution: AutoDiscovered");
	}

	[TestMethod]
	public void WhenJsonRequested_IncludesSdkUpdateFields()
	{
		var report = new HealthReport
		{
			Status = HealthStatus.Healthy,
			UnoSdkPackage = "Uno.Sdk",
			UnoSdkVersion = "6.4.12",
			LatestUnoSdkVersion = "6.5.31",
			UnoSdkUpdateAvailable = true,
			Issues = [],
		};

		var json = HealthReportFormatter.FormatJson(report);

		json.Should().Contain("\"unoSdkPackage\":\"Uno.Sdk\"");
		json.Should().Contain("\"unoSdkVersion\":\"6.4.12\"");
		json.Should().Contain("\"latestUnoSdkVersion\":\"6.5.31\"");
		json.Should().Contain("\"unoSdkUpdateAvailable\":true");
	}

	[TestMethod]
	public void WhenUpdateAvailable_IncludesRecommendedVersionWithSuffix()
	{
		var report = new HealthReport
		{
			Status = HealthStatus.Healthy,
			UnoSdkPackage = "Uno.Sdk",
			UnoSdkVersion = "6.4.12",
			LatestUnoSdkVersion = "6.5.31",
			UnoSdkUpdateAvailable = true,
			Issues = [],
		};

		var text = HealthReportFormatter.FormatPlainText(report);

		text.Should().Contain("Uno.Sdk: 6.4.12");
		text.Should().Contain("Recommended Uno.Sdk: 6.5.31 (update available)");
	}

	[TestMethod]
	public void WhenUpToDate_OmitsUpdateSuffix()
	{
		var report = new HealthReport
		{
			Status = HealthStatus.Healthy,
			UnoSdkPackage = "Uno.Sdk",
			UnoSdkVersion = "6.5.31",
			LatestUnoSdkVersion = "6.5.31",
			UnoSdkUpdateAvailable = false,
			Issues = [],
		};

		var text = HealthReportFormatter.FormatPlainText(report);

		text.Should().Contain("Recommended Uno.Sdk: 6.5.31");
		text.Should().NotContain("(update available)");
	}

	[TestMethod]
	public void WhenPackageIsUnoSdkPrivate_LabelUsesActualPackageId()
	{
		var report = new HealthReport
		{
			Status = HealthStatus.Healthy,
			UnoSdkPackage = "Uno.Sdk.Private",
			UnoSdkVersion = "6.4.12",
			Issues = [],
		};

		var text = HealthReportFormatter.FormatPlainText(report);

		text.Should().Contain("Uno.Sdk.Private: 6.4.12");
	}

	[TestMethod]
	public void WhenPlainTextRequested_IncludesActiveServerOwnershipDetails()
	{
		var report = new HealthReport
		{
			Status = HealthStatus.Degraded,
			UpstreamConnected = true,
			ToolCount = 11,
			EffectiveWorkspaceDirectory = @"D:\src\myapp\src",
			SelectedSolutionPath = @"D:\src\myapp\src\MyApp.slnx",
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
						SolutionPath = @"D:\src\myapp\src\MyApp.slnx",
						IsInWorkspace = true,
						ProcessChain =
						[
							new ProcessChainEntry { ProcessId = 100, ProcessName = "Uno.UI.RemoteControl.Host" },
							new ProcessChainEntry { ProcessId = 50, ProcessName = "dotnet" },
							new ProcessChainEntry { ProcessId = 25, ProcessName = "ide-process" },
						],
					},
				],
			},
		};

		var text = HealthReportFormatter.FormatPlainText(report);

		text.Should().Contain("Active Servers:");
		text.Should().Contain("Process Chain: ide-process (25) → dotnet (50) → Host (100)");
		text.Should().Contain("IDE Channel: abc");
	}
}

using System.Linq;
using AwesomeAssertions;
using Uno.UI.DevServer.Cli.Helpers;
using Uno.UI.DevServer.Cli.Mcp;

namespace Uno.UI.RemoteControl.DevServer.Tests.Mcp;

[TestClass]
public class Given_HealthReportFactory_SdkUpdate
{
	// A discovery that maps to zero issues, so the only issue in the resulting report
	// can be the SDK-update advisory — used to prove the advisory alone does not
	// degrade the health status.
	private static DiscoveryInfo CleanDiscovery(string unoSdkVersion, string unoSdkPackage = "Uno.Sdk") => new()
	{
		ResolutionKind = WorkspaceResolutionKind.CurrentDirectory,
		GlobalJsonPath = "global.json",
		UnoSdkPackage = unoSdkPackage,
		UnoSdkVersion = unoSdkVersion,
		UnoSdkPath = @"C:\cache\uno.sdk\" + unoSdkVersion,
		PackagesJsonPath = @"C:\cache\uno.sdk\packages.json",
		DotNetVersion = "9.0.100",
	};

	private static HealthReport CreateWithLatest(string currentSdk, string? latestSdk)
		=> HealthReportFactory.Create(
			CleanDiscovery(currentSdk),
			devServerStarted: true,
			upstreamConnected: true,
			toolCount: 5,
			connectionState: ConnectionState.Connected,
			discoveredSolutions: null,
			latestUnoSdkVersion: latestSdk);

	[TestMethod]
	public void WhenLatestIsNewer_ReportsUpdateAvailableWithAdvisory()
	{
		var report = CreateWithLatest("6.4.12", "6.5.31");

		report.UnoSdkUpdateAvailable.Should().BeTrue();
		report.LatestUnoSdkVersion.Should().Be("6.5.31");
		report.UnoSdkVersion.Should().Be("6.4.12");

		var advisory = report.Issues.Single(i => i.Code == IssueCode.UnoSdkUpdateAvailable);
		advisory.Severity.Should().Be(ValidationSeverity.Warning);
		advisory.Message.Should().Contain("6.5.31");
		advisory.Remediation.Should().Contain("global.json");
	}

	[TestMethod]
	public void WhenUpdateIsTheOnlyIssue_StatusStaysHealthy()
	{
		var report = CreateWithLatest("6.4.12", "6.5.31");

		report.Issues.Should().ContainSingle(i => i.Code == IssueCode.UnoSdkUpdateAvailable);
		report.Status.Should().Be(HealthStatus.Healthy);
	}

	[TestMethod]
	[DataRow("6.5.31")]  // equal to recommended
	[DataRow("6.6.29")]  // ahead of recommended
	public void WhenCurrentIsUpToDateOrAhead_NoUpdateAdvisory(string current)
	{
		var report = CreateWithLatest(current, "6.5.31");

		report.UnoSdkUpdateAvailable.Should().BeFalse();
		report.Issues.Should().NotContain(i => i.Code == IssueCode.UnoSdkUpdateAvailable);
	}

	[TestMethod]
	public void WhenPackageIsUnoSdkPrivate_NoUpdateAdvisory()
	{
		// The recommended-version manifest is for the public Uno.Sdk; a Uno.Sdk.Private
		// workspace behind that version must not be told to "update" to it.
		var report = HealthReportFactory.Create(
			CleanDiscovery("6.4.12", unoSdkPackage: "Uno.Sdk.Private"),
			devServerStarted: true,
			upstreamConnected: true,
			toolCount: 5,
			connectionState: ConnectionState.Connected,
			discoveredSolutions: null,
			latestUnoSdkVersion: "6.5.31");

		report.UnoSdkUpdateAvailable.Should().BeFalse();
		report.LatestUnoSdkVersion.Should().BeNull();
		report.Issues.Should().NotContain(i => i.Code == IssueCode.UnoSdkUpdateAvailable);
	}

	[TestMethod]
	public void WhenFatalIssueAndUpdateAvailable_StatusUnhealthyAndAdvisoryStillPresent()
	{
		// devServerStarted:false → HostNotStarted (Fatal). The advisory is still reported,
		// but Fatal dominates the overall status.
		var report = HealthReportFactory.Create(
			CleanDiscovery("6.4.12"),
			devServerStarted: false,
			upstreamConnected: false,
			toolCount: 0,
			connectionState: null,
			discoveredSolutions: null,
			latestUnoSdkVersion: "6.5.31");

		report.Status.Should().Be(HealthStatus.Unhealthy);
		report.Issues.Should().Contain(i => i.Code == IssueCode.UnoSdkUpdateAvailable);
	}

	[TestMethod]
	public void WhenRealWarningAndUpdateAvailable_StatusDegraded()
	{
		// A genuine (non-advisory) Warning must still degrade status even when the advisory
		// is also present — the exclusion is surgical to UnoSdkUpdateAvailable only. Null
		// PackagesJsonPath yields a PackagesJsonNotFound Warning.
		var discovery = new DiscoveryInfo
		{
			ResolutionKind = WorkspaceResolutionKind.CurrentDirectory,
			GlobalJsonPath = "global.json",
			UnoSdkPackage = "Uno.Sdk",
			UnoSdkVersion = "6.4.12",
			UnoSdkPath = @"C:\cache\uno.sdk\6.4.12",
			PackagesJsonPath = null,
			DotNetVersion = "9.0.100",
		};

		var report = HealthReportFactory.Create(
			discovery,
			devServerStarted: true,
			upstreamConnected: true,
			toolCount: 5,
			connectionState: ConnectionState.Connected,
			discoveredSolutions: null,
			latestUnoSdkVersion: "6.5.31");

		report.Status.Should().Be(HealthStatus.Degraded);
		report.Issues.Should().Contain(i => i.Code == IssueCode.PackagesJsonNotFound);
		report.Issues.Should().Contain(i => i.Code == IssueCode.UnoSdkUpdateAvailable);
	}

	[TestMethod]
	public void WhenManifestUnavailable_NoUpdateAdvisory()
	{
		var report = CreateWithLatest("6.4.12", latestSdk: null);

		report.UnoSdkUpdateAvailable.Should().BeFalse();
		report.LatestUnoSdkVersion.Should().BeNull();
		report.Issues.Should().NotContain(i => i.Code == IssueCode.UnoSdkUpdateAvailable);
	}
}

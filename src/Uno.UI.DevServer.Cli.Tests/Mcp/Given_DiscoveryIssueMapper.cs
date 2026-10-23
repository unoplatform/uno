using AwesomeAssertions;
using Uno.UI.DevServer.Cli.Helpers;
using Uno.UI.DevServer.Cli.Mcp;

namespace Uno.UI.DevServer.Cli.Tests.Mcp;

[TestClass]
public class Given_DiscoveryIssueMapper
{
	[TestMethod]
	[Description("SdkNotInCache names the one-call recovery: select_solution with forceRestart=true on the explicit solution path (#152)")]
	public void WhenSdkNotInCache_RemediationNamesForceRestartAndSolutionPath()
	{
		var solutionPath = Path.Combine(Path.GetTempPath(), "repo", "MyApp", "MyApp.sln");
		var discovery = new DiscoveryInfo
		{
			ResolutionKind = WorkspaceResolutionKind.CurrentDirectory,
			SelectedSolutionPath = solutionPath,
			GlobalJsonPath = Path.Combine(Path.GetTempPath(), "repo", "MyApp", "global.json"),
			UnoSdkPackage = "Uno.Sdk",
			UnoSdkVersion = "6.6.42",
			UnoSdkPath = null,
		};

		var issue = DiscoveryIssueMapper.MapDiscoveryIssues(discovery).Single();

		issue.Code.Should().Be(IssueCode.SdkNotInCache);
		issue.Remediation.Should().Contain("uno_app_select_solution");
		issue.Remediation.Should().Contain("\"forceRestart\": true");
		issue.Remediation.Should().Contain(System.Text.Json.JsonSerializer.Serialize(solutionPath));
	}
}

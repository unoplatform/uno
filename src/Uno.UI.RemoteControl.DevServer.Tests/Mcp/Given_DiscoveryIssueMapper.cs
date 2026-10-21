using AwesomeAssertions;
using Uno.UI.DevServer.Cli.Helpers;
using Uno.UI.DevServer.Cli.Mcp;

namespace Uno.UI.RemoteControl.DevServer.Tests.Mcp;

[TestClass]
public class Given_DiscoveryIssueMapper
{
	[TestMethod]
	public void WhenNull_ReturnsEmpty()
	{
		var issues = DiscoveryIssueMapper.MapDiscoveryIssues(null);

		issues.Should().BeEmpty();
	}

	[TestMethod]
	public void WhenNoCandidates_ReportsNoSolutionFound()
	{
		var discovery = new DiscoveryInfo
		{
			ResolutionKind = WorkspaceResolutionKind.NoCandidates,
		};

		var issues = DiscoveryIssueMapper.MapDiscoveryIssues(discovery);

		issues.Should().HaveCount(1);
		issues[0].Code.Should().Be(IssueCode.NoSolutionFound);
		issues[0].Severity.Should().Be(ValidationSeverity.Warning);
	}

	[TestMethod]
	public void WhenAmbiguous_ReportsWorkspaceAmbiguous()
	{
		var discovery = new DiscoveryInfo
		{
			ResolutionKind = WorkspaceResolutionKind.Ambiguous,
			CandidateSolutions = ["/tmp/a.slnx", "/tmp/b.slnx"],
		};

		var issues = DiscoveryIssueMapper.MapDiscoveryIssues(discovery);

		issues.Should().HaveCount(1);
		issues[0].Code.Should().Be(IssueCode.WorkspaceAmbiguous);
		issues[0].Severity.Should().Be(ValidationSeverity.Warning);
		issues[0].Message.Should().Contain("Multiple Uno solutions matched the current directory");
		issues[0].Remediation.Should().Contain("uno_app_select_solution", "the agent can pick the solution itself instead of restarting the bridge");
	}

	[TestMethod]
	public void WhenNoValidWorkspace_ReportsRestartGuidanceForPostStartupChanges()
	{
		var discovery = new DiscoveryInfo
		{
			ResolutionKind = WorkspaceResolutionKind.NoValidWorkspace,
			CandidateSolutions = ["/tmp/app.slnx"],
		};

		var issues = DiscoveryIssueMapper.MapDiscoveryIssues(discovery);

		issues.Should().HaveCount(1);
		issues[0].Code.Should().Be(IssueCode.WorkspaceNotResolved);
		issues[0].Remediation.Should().Contain("restart the MCP bridge");
	}

	[TestMethod]
	public void WhenGlobalJsonMissing_ReportsGlobalJsonNotFound()
	{
		var discovery = new DiscoveryInfo { GlobalJsonPath = null };

		var issues = DiscoveryIssueMapper.MapDiscoveryIssues(discovery);

		issues.Should().HaveCount(1);
		issues[0].Code.Should().Be(IssueCode.GlobalJsonNotFound);
		issues[0].Severity.Should().Be(ValidationSeverity.Fatal);
	}

	[TestMethod]
	public void WhenNoUnoSdk_ReportsUnoSdkNotInGlobalJson()
	{
		var discovery = new DiscoveryInfo
		{
			GlobalJsonPath = "/tmp/global.json",
			UnoSdkPackage = null,
			UnoSdkVersion = null,
		};

		var issues = DiscoveryIssueMapper.MapDiscoveryIssues(discovery);

		issues.Should().HaveCount(1);
		issues[0].Code.Should().Be(IssueCode.UnoSdkNotInGlobalJson);
		issues[0].Severity.Should().Be(ValidationSeverity.Fatal);
	}

	[TestMethod]
	public void WhenSdkNotInCache_ReportsSdkNotInCache()
	{
		var discovery = new DiscoveryInfo
		{
			GlobalJsonPath = "/tmp/global.json",
			UnoSdkPackage = "Uno.Sdk",
			UnoSdkVersion = "5.5.100",
			UnoSdkPath = null,
		};

		var issues = DiscoveryIssueMapper.MapDiscoveryIssues(discovery);

		issues.Should().HaveCount(1);
		issues[0].Code.Should().Be(IssueCode.SdkNotInCache);
		issues[0].Severity.Should().Be(ValidationSeverity.Fatal);
		issues[0].Remediation.Should().Contain("dotnet restore");
		issues[0].Remediation.Should().Contain("uno_app_select_solution");
	}

	[TestMethod]
	public void WhenPackagesJsonMissing_ReportsPackagesJsonNotFound()
	{
		var discovery = new DiscoveryInfo
		{
			GlobalJsonPath = "/tmp/global.json",
			UnoSdkPackage = "Uno.Sdk",
			UnoSdkVersion = "5.5.100",
			UnoSdkPath = "/nuget/uno.sdk/5.5.100",
			PackagesJsonPath = null,
			DotNetVersion = "10.0.100",
			DotNetTfm = "net10.0",
		};

		var issues = DiscoveryIssueMapper.MapDiscoveryIssues(discovery);

		issues.Should().Contain(i => i.Code == IssueCode.PackagesJsonNotFound);
		issues.First(i => i.Code == IssueCode.PackagesJsonNotFound).Severity.Should().Be(ValidationSeverity.Warning);
	}

	[TestMethod]
	public void WhenDevServerNotCached_ReportsDevServerPackageNotCached()
	{
		var discovery = new DiscoveryInfo
		{
			GlobalJsonPath = "/tmp/global.json",
			UnoSdkPackage = "Uno.Sdk",
			UnoSdkVersion = "5.5.100",
			UnoSdkPath = "/nuget/uno.sdk/5.5.100",
			PackagesJsonPath = "/nuget/uno.sdk/5.5.100/targets/netstandard2.0/packages.json",
			DevServerPackageVersion = "5.5.100",
			DevServerPackagePath = null,
			DotNetVersion = "10.0.100",
			DotNetTfm = "net10.0",
		};

		var issues = DiscoveryIssueMapper.MapDiscoveryIssues(discovery);

		issues.Should().Contain(i => i.Code == IssueCode.DevServerPackageNotCached);
		issues.First(i => i.Code == IssueCode.DevServerPackageNotCached).Severity.Should().Be(ValidationSeverity.Fatal);
		issues.First(i => i.Code == IssueCode.DevServerPackageNotCached).Remediation.Should().Contain("uno_app_select_solution");
	}

	[TestMethod]
	public void WhenHostBinaryMissing_ReportsHostBinaryNotFound()
	{
		var discovery = new DiscoveryInfo
		{
			GlobalJsonPath = "/tmp/global.json",
			UnoSdkPackage = "Uno.Sdk",
			UnoSdkVersion = "5.5.100",
			UnoSdkPath = "/nuget/uno.sdk/5.5.100",
			PackagesJsonPath = "/nuget/uno.sdk/5.5.100/targets/netstandard2.0/packages.json",
			DevServerPackageVersion = "5.5.100",
			DevServerPackagePath = "/nuget/uno.winui.devserver/5.5.100",
			DotNetVersion = "10.0.100",
			DotNetTfm = "net10.0",
			HostPath = null,
		};

		var issues = DiscoveryIssueMapper.MapDiscoveryIssues(discovery);

		issues.Should().Contain(i => i.Code == IssueCode.HostBinaryNotFound);
		issues.First(i => i.Code == IssueCode.HostBinaryNotFound).Severity.Should().Be(ValidationSeverity.Fatal);
	}

	[TestMethod]
	public void WhenDotNetMissing_ReportsDotNetNotFound()
	{
		var discovery = new DiscoveryInfo
		{
			GlobalJsonPath = "/tmp/global.json",
			UnoSdkPackage = "Uno.Sdk",
			UnoSdkVersion = "5.5.100",
			UnoSdkPath = "/nuget/uno.sdk/5.5.100",
			PackagesJsonPath = "/nuget/uno.sdk/5.5.100/targets/netstandard2.0/packages.json",
			DotNetVersion = null,
		};

		var issues = DiscoveryIssueMapper.MapDiscoveryIssues(discovery);

		issues.Should().Contain(i => i.Code == IssueCode.DotNetNotFound);
		issues.First(i => i.Code == IssueCode.DotNetNotFound).Severity.Should().Be(ValidationSeverity.Fatal);
	}

	[TestMethod]
	public void WhenFullyHealthy_ReturnsEmpty()
	{
		var discovery = new DiscoveryInfo
		{
			GlobalJsonPath = "/tmp/global.json",
			UnoSdkPackage = "Uno.Sdk",
			UnoSdkVersion = "5.5.100",
			UnoSdkPath = "/nuget/uno.sdk/5.5.100",
			PackagesJsonPath = "/nuget/uno.sdk/5.5.100/targets/netstandard2.0/packages.json",
			DevServerPackageVersion = "5.5.100",
			DevServerPackagePath = "/nuget/uno.winui.devserver/5.5.100",
			DotNetVersion = "10.0.100",
			DotNetTfm = "net10.0",
			HostPath = "/nuget/uno.winui.devserver/5.5.100/tools/rc/host/net10.0/Uno.UI.RemoteControl.Host.dll",
		};

		var issues = DiscoveryIssueMapper.MapDiscoveryIssues(discovery);

		issues.Should().BeEmpty();
	}

	[TestMethod]
	public void WhenMultipleIssues_ReportsAll()
	{
		var discovery = new DiscoveryInfo
		{
			GlobalJsonPath = "/tmp/global.json",
			UnoSdkPackage = "Uno.Sdk",
			UnoSdkVersion = "5.5.100",
			UnoSdkPath = "/nuget/uno.sdk/5.5.100",
			PackagesJsonPath = null, // Warning
			DotNetVersion = null, // Fatal
		};

		var issues = DiscoveryIssueMapper.MapDiscoveryIssues(discovery);

		issues.Should().HaveCount(2);
		issues.Should().Contain(i => i.Code == IssueCode.PackagesJsonNotFound);
		issues.Should().Contain(i => i.Code == IssueCode.DotNetNotFound);
	}

	// -------------------------------------------------------------------
	// Add-in degradation scenarios
	// -------------------------------------------------------------------

	[TestMethod]
	[Description("Settings package listed in packages.json but not in NuGet cache → AddInPackageNotCached")]
	public void WhenSettingsPackageNotCached_ReportsAddInPackageNotCached()
	{
		var discovery = new DiscoveryInfo
		{
			GlobalJsonPath = "/tmp/global.json",
			UnoSdkPackage = "Uno.Sdk",
			UnoSdkVersion = "5.5.100",
			UnoSdkPath = "/nuget/uno.sdk/5.5.100",
			PackagesJsonPath = "/nuget/uno.sdk/5.5.100/targets/netstandard2.0/packages.json",
			DevServerPackageVersion = "5.5.100",
			DevServerPackagePath = "/nuget/uno.winui.devserver/5.5.100",
			DotNetVersion = "10.0.100",
			DotNetTfm = "net10.0",
			HostPath = "/nuget/uno.winui.devserver/5.5.100/tools/rc/host/net10.0/Host.dll",
			SettingsPackageVersion = "1.2.3",
			SettingsPackagePath = null,
		};

		var issues = DiscoveryIssueMapper.MapDiscoveryIssues(discovery);

		issues.Should().Contain(i => i.Code == IssueCode.AddInPackageNotCached);
		issues.First(i => i.Code == IssueCode.AddInPackageNotCached).Severity.Should().Be(ValidationSeverity.Warning);
		issues.First(i => i.Code == IssueCode.AddInPackageNotCached).Remediation.Should().Contain("uno_app_select_solution");
	}

	[TestMethod]
	[Description("Convention-based add-in discovery threw an exception → AddInDiscoveryFallback")]
	public void WhenDiscoveryFailed_ReportsAddInDiscoveryFallback()
	{
		var discovery = new DiscoveryInfo
		{
			GlobalJsonPath = "/tmp/global.json",
			UnoSdkPackage = "Uno.Sdk",
			UnoSdkVersion = "5.5.100",
			UnoSdkPath = "/nuget/uno.sdk/5.5.100",
			PackagesJsonPath = "/nuget/uno.sdk/5.5.100/targets/netstandard2.0/packages.json",
			DevServerPackageVersion = "5.5.100",
			DevServerPackagePath = "/nuget/uno.winui.devserver/5.5.100",
			DotNetVersion = "10.0.100",
			DotNetTfm = "net10.0",
			HostPath = "/nuget/uno.winui.devserver/5.5.100/tools/rc/host/net10.0/Host.dll",
			AddInDiscoveryFailed = true,
		};

		var issues = DiscoveryIssueMapper.MapDiscoveryIssues(discovery);

		issues.Should().Contain(i => i.Code == IssueCode.AddInDiscoveryFallback);
		issues.First(i => i.Code == IssueCode.AddInDiscoveryFallback).Severity.Should().Be(ValidationSeverity.Warning);
	}

	[TestMethod]
	[Description("No global.json at all → exactly one GlobalJsonNotFound issue, no cascading errors")]
	public void FullDegradedScenario_NoGlobalJson_ReportsExactlyGlobalJsonNotFound()
	{
		var discovery = new DiscoveryInfo
		{
			GlobalJsonPath = null,
			AddInDiscoveryFailed = true, // should be ignored since global.json short-circuits
		};

		var issues = DiscoveryIssueMapper.MapDiscoveryIssues(discovery);

		issues.Should().HaveCount(1);
		issues[0].Code.Should().Be(IssueCode.GlobalJsonNotFound);
	}

	[TestMethod]
	[Description("Missing NuGet cache entries produce multiple issues without crashing")]
	public void FullDegradedScenario_MissingNuGetCache_ReportsMultipleIssues()
	{
		var discovery = new DiscoveryInfo
		{
			GlobalJsonPath = "/tmp/global.json",
			UnoSdkPackage = "Uno.Sdk",
			UnoSdkVersion = "5.5.100",
			UnoSdkPath = "/nuget/uno.sdk/5.5.100",
			PackagesJsonPath = "/nuget/uno.sdk/5.5.100/targets/netstandard2.0/packages.json",
			DevServerPackageVersion = "5.5.100",
			DevServerPackagePath = null, // Fatal
			DotNetVersion = "10.0.100",
			DotNetTfm = "net10.0",
			SettingsPackageVersion = "1.2.3",
			SettingsPackagePath = null, // Warning
			AddInDiscoveryFailed = true, // Warning
		};

		var issues = DiscoveryIssueMapper.MapDiscoveryIssues(discovery);

		issues.Should().HaveCount(3);
		issues.Should().Contain(i => i.Code == IssueCode.DevServerPackageNotCached);
		issues.Should().Contain(i => i.Code == IssueCode.AddInPackageNotCached);
		issues.Should().Contain(i => i.Code == IssueCode.AddInDiscoveryFallback);
	}

	// -------------------------------------------------------------------
	// Explicitly selected solution that does not resolve
	// -------------------------------------------------------------------

	[TestMethod]
	[Description("A selected solution that no longer exists is named as missing")]
	public void MapUnresolvedSelection_WhenSolutionIsMissing_SaysItDoesNotExist()
	{
		var solution = MissingSolution();

		var issue = DiscoveryIssueMapper.MapUnresolvedSelection(solution, selectionIsKept: true);

		issue.Code.Should().Be(IssueCode.WorkspaceNotResolved);
		issue.Severity.Should().Be(ValidationSeverity.Fatal);
		issue.Message.Should().Contain(solution).And.Contain("does not exist");
		issue.Remediation.Should().Contain("uno_app_select_solution");
	}

	[TestMethod]
	[Description("A selected solution whose nearest global.json does not declare the Uno SDK names that global.json, which the .NET SDK resolution stops at")]
	public void MapUnresolvedSelection_WhenNearestGlobalJsonHasNoUnoSdk_NamesIt()
	{
		var root = CreateTempDirectory();

		try
		{
			var globalJson = CreateNonUnoGlobalJson(root);
			var solution = CreateFile(root, "src", "App.slnx");

			var issue = DiscoveryIssueMapper.MapUnresolvedSelection(solution, selectionIsKept: true);

			issue.Code.Should().Be(IssueCode.WorkspaceNotResolved);
			issue.Message.Should().Contain(globalJson).And.Contain("Uno.Sdk");
		}
		finally
		{
			Directory.Delete(root, recursive: true);
		}
	}

	[TestMethod]
	[Description("A selected solution with no global.json above it says so, naming the solution, and asks for one next to it: only that folder is watched")]
	public void MapUnresolvedSelection_WhenNoGlobalJson_SaysNoneWasFound()
	{
		var root = CreateTempDirectory();

		try
		{
			var solution = CreateFile(root, "src", "App.slnx");

			var issue = DiscoveryIssueMapper.MapUnresolvedSelection(solution, selectionIsKept: true);

			issue.Message.Should().Contain("No global.json").And.Contain(solution);
			issue.Remediation.Should().Contain("next to the solution");
		}
		finally
		{
			Directory.Delete(root, recursive: true);
		}
	}

	[TestMethod]
	[DataRow(true, "starts by itself")]
	[DataRow(false, "uno_app_select_solution again")]
	[Description("A kept selection starts by itself once fixed; a rejected one is not kept and must be selected again")]
	public void MapUnresolvedSelection_RemediationDependsOnWhetherTheSelectionIsKept(bool selectionIsKept, string expected)
	{
		var root = CreateTempDirectory();

		try
		{
			CreateNonUnoGlobalJson(root);
			var solution = CreateFile(root, "src", "App.slnx");

			var issue = DiscoveryIssueMapper.MapUnresolvedSelection(solution, selectionIsKept);

			issue.Remediation.Should().Contain(expected);
		}
		finally
		{
			Directory.Delete(root, recursive: true);
		}
	}

	[TestMethod]
	[Description("No selection, or one that is not explicit, has no selection issue")]
	public void MapSelectionIssue_WhenSelectionIsNotExplicit_ReturnsNull()
	{
		var selection = new WorkspaceSelectionSnapshot { SelectedSolutionPath = MissingSolution() };

		DiscoveryIssueMapper.MapSelectionIssue(null, monitorFoundNoSolution: true).Should().BeNull();
		DiscoveryIssueMapper.MapSelectionIssue(selection, monitorFoundNoSolution: true).Should().BeNull();
	}

	[TestMethod]
	[Description("An explicit selection that does not resolve is reported, as a kept selection")]
	public void MapSelectionIssue_WhenExplicitSelectionIsUnresolved_ReportsIt()
	{
		var missing = MissingSolution();
		var selection = new WorkspaceSelectionSnapshot { SelectedSolutionPath = missing, IsExplicitSolutionSelection = true };

		var issue = DiscoveryIssueMapper.MapSelectionIssue(selection, monitorFoundNoSolution: false);

		issue.Should().NotBeNull();
		issue!.Message.Should().Contain(missing);
		issue.Remediation.Should().Contain("starts by itself");
	}

	[TestMethod]
	[Description("A resolved explicit selection whose file the monitor could not find (deleted before the watcher reacted) is reported missing")]
	public void MapSelectionIssue_WhenResolvedSelectionIsMissingForTheMonitor_ReportsIt()
	{
		var missing = MissingSolution();
		var selection = new WorkspaceSelectionSnapshot
		{
			SelectedSolutionPath = missing,
			EffectiveWorkspaceDirectory = Path.GetDirectoryName(missing),
			IsExplicitSolutionSelection = true,
		};

		DiscoveryIssueMapper.MapSelectionIssue(selection, monitorFoundNoSolution: true)!.Message.Should().Contain("does not exist");
		DiscoveryIssueMapper.MapSelectionIssue(selection, monitorFoundNoSolution: false).Should().BeNull();
	}

	[TestMethod]
	[Description("The monitor's no-solution flag is ignored while the resolved selection exists: it is stale from an earlier cycle, and blaming the global.json would be false")]
	public void MapSelectionIssue_WhenMonitorFlagIsStaleAndFileExists_ReturnsNull()
	{
		var root = CreateTempDirectory();

		try
		{
			var solution = CreateFile(root, "src", "App.slnx");
			var selection = new WorkspaceSelectionSnapshot
			{
				SelectedSolutionPath = solution,
				EffectiveWorkspaceDirectory = root,
				IsExplicitSolutionSelection = true,
			};

			DiscoveryIssueMapper.MapSelectionIssue(selection, monitorFoundNoSolution: true).Should().BeNull();
		}
		finally
		{
			Directory.Delete(root, recursive: true);
		}
	}

	private static string MissingSolution()
		=> Path.Combine(Path.GetTempPath(), $"uno-issue-tests-{Guid.NewGuid():N}", "App.slnx");

	private static string CreateNonUnoGlobalJson(string root)
	{
		var globalJson = Path.Combine(root, "global.json");
		File.WriteAllText(globalJson, """{"sdk":{"version":"10.0.100"}}""");
		return globalJson;
	}

	private static string CreateTempDirectory()
	{
		var path = Path.Combine(Path.GetTempPath(), $"uno-issue-tests-{Guid.NewGuid():N}");
		Directory.CreateDirectory(path);
		return path;
	}

	private static string CreateFile(string root, params string[] segments)
	{
		var path = Path.Combine([root, .. segments]);
		Directory.CreateDirectory(Path.GetDirectoryName(path)!);
		File.WriteAllText(path, string.Empty);
		return path;
	}
}

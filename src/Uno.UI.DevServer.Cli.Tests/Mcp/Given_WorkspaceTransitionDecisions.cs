using AwesomeAssertions;
using Uno.UI.DevServer.Cli.Helpers;
using Uno.UI.DevServer.Cli.Mcp;

namespace Uno.UI.DevServer.Cli.Tests.Mcp;

[TestClass]
public class Given_WorkspaceTransitionDecisions
{
	[TestMethod]
	public void WhenNoCandidatesThenUnoWorkspace_ActionIsStart()
	{
		var previous = CreateUnresolved(WorkspaceResolutionKind.NoCandidates);
		var current = CreateResolved(CreatePath("repo", "src"), CreatePath("repo", "src", "App.slnx"));

		var action = WorkspaceTransitionDecisions.DetermineAction(previous, current, WorkspaceTransitionTrigger.FileSystem, devServerStarted: false);

		action.Should().Be(WorkspaceTransitionAction.Start);
	}

	[TestMethod]
	public void WhenNoValidWorkspaceThenUnoWorkspace_ActionIsStart()
	{
		var solutionPath = CreatePath("repo", "App.slnx");
		var previous = CreateUnresolved(WorkspaceResolutionKind.NoValidWorkspace, solutionPath);
		var current = CreateResolved(CreatePath("repo"), solutionPath);

		var action = WorkspaceTransitionDecisions.DetermineAction(previous, current, WorkspaceTransitionTrigger.FileSystem, devServerStarted: false);

		action.Should().Be(WorkspaceTransitionAction.Start);
	}

	[TestMethod]
	public void WhenNoCandidatesThenNonUnoSolution_ActionIsDiagnose()
	{
		var previous = CreateUnresolved(WorkspaceResolutionKind.NoCandidates);
		var current = CreateUnresolved(WorkspaceResolutionKind.NoValidWorkspace, CreatePath("repo", "App.slnx"));

		var action = WorkspaceTransitionDecisions.DetermineAction(previous, current, WorkspaceTransitionTrigger.FileSystem, devServerStarted: false);

		action.Should().Be(WorkspaceTransitionAction.Diagnose);
	}

	[TestMethod]
	public void WhenValidWorkspaceLosesGlobalJson_ActionIsStop()
	{
		var solutionPath = CreatePath("repo", "App.slnx");
		var previous = CreateResolved(CreatePath("repo"), solutionPath);
		var current = CreateUnresolved(WorkspaceResolutionKind.NoValidWorkspace, solutionPath);

		var action = WorkspaceTransitionDecisions.DetermineAction(previous, current, WorkspaceTransitionTrigger.FileSystem, devServerStarted: true);

		action.Should().Be(WorkspaceTransitionAction.Stop);
	}

	[TestMethod]
	public void WhenValidWorkspaceLosesSolution_ActionIsStop()
	{
		var previous = CreateResolved(CreatePath("repo"), CreatePath("repo", "App.slnx"));
		var current = CreateUnresolved(WorkspaceResolutionKind.NoCandidates);

		var action = WorkspaceTransitionDecisions.DetermineAction(previous, current, WorkspaceTransitionTrigger.FileSystem, devServerStarted: true);

		action.Should().Be(WorkspaceTransitionAction.Stop);
	}

	[TestMethod]
	public void WhenWorkspaceChangesFromAToB_ActionIsRestart()
	{
		var previous = CreateResolved(CreatePath("repo", "a"), CreatePath("repo", "a", "AppA.slnx"));
		var current = CreateResolved(CreatePath("repo", "b"), CreatePath("repo", "b", "AppB.slnx"));

		var action = WorkspaceTransitionDecisions.DetermineAction(previous, current, WorkspaceTransitionTrigger.FileSystem, devServerStarted: true);

		action.Should().Be(WorkspaceTransitionAction.Restart);
	}

	[TestMethod]
	public void WhenWorkspaceIdentityIsUnchanged_ActionIsRefresh()
	{
		var workspace = CreatePath("repo", "src");
		var solution = CreatePath("repo", "src", "App.slnx");
		var previous = CreateResolved(workspace, solution);
		var current = CreateResolved(workspace, solution);

		var action = WorkspaceTransitionDecisions.DetermineAction(previous, current, WorkspaceTransitionTrigger.FileSystem, devServerStarted: true);

		action.Should().Be(WorkspaceTransitionAction.Refresh);
	}

	[TestMethod]
	public void WhenSelectedSolutionChangesWithinSameWorkspace_UserSelectionActionIsRestart()
	{
		var workspace = CreatePath("repo", "src");
		var previous = CreateResolved(workspace, CreatePath("repo", "src", "AppA.slnx"));
		var current = CreateResolved(workspace, CreatePath("repo", "src", "AppB.slnx"));

		var action = WorkspaceTransitionDecisions.DetermineAction(previous, current, WorkspaceTransitionTrigger.UserSelection, devServerStarted: true);

		action.Should().Be(WorkspaceTransitionAction.Restart);
	}

	[TestMethod]
	public void WhenWorkspaceBecomesAmbiguous_ActionIsStopForFileSystem()
	{
		var previous = CreateResolved(CreatePath("repo", "src"), CreatePath("repo", "src", "App.slnx"));
		var current = CreateUnresolved(
			WorkspaceResolutionKind.Ambiguous,
			CreatePath("repo", "srcA", "AppA.slnx"),
			CreatePath("repo", "srcB", "AppB.slnx"));

		var action = WorkspaceTransitionDecisions.DetermineAction(previous, current, WorkspaceTransitionTrigger.FileSystem, devServerStarted: true);

		action.Should().Be(WorkspaceTransitionAction.Stop);
	}

	[TestMethod]
	public void WhenAmbiguousThenSingleWorkspace_ActionIsStart()
	{
		var previous = CreateUnresolved(
			WorkspaceResolutionKind.Ambiguous,
			CreatePath("repo", "srcA", "AppA.slnx"),
			CreatePath("repo", "srcB", "AppB.slnx"));
		var current = CreateResolved(CreatePath("repo", "srcA"), CreatePath("repo", "srcA", "AppA.slnx"));

		var action = WorkspaceTransitionDecisions.DetermineAction(previous, current, WorkspaceTransitionTrigger.FileSystem, devServerStarted: false);

		action.Should().Be(WorkspaceTransitionAction.Start);
	}

	[TestMethod]
	public void WhenMcpRootsConfirmSameWorkspace_ActionIsRefresh()
	{
		var workspace = CreatePath("repo", "src");
		var solution = CreatePath("repo", "src", "App.slnx");
		var previous = CreateResolved(workspace, solution);
		var current = CreateResolved(workspace, solution);

		var action = WorkspaceTransitionDecisions.DetermineAction(previous, current, WorkspaceTransitionTrigger.McpRoots, devServerStarted: true);

		action.Should().Be(WorkspaceTransitionAction.Refresh);
	}

	[TestMethod]
	public void WhenMcpRootsConfirmSameWorkspaceAfterDeferredStartup_ActionIsStart()
	{
		var workspace = CreatePath("repo", "src");
		var solution = CreatePath("repo", "src", "App.slnx");
		var previous = CreateResolved(workspace, solution);
		var current = CreateResolved(workspace, solution);

		var action = WorkspaceTransitionDecisions.DetermineAction(previous, current, WorkspaceTransitionTrigger.McpRoots, devServerStarted: false);

		action.Should().Be(WorkspaceTransitionAction.Start);
	}

	[TestMethod]
	public void WhenMcpRootsPointToDifferentWorkspace_ActionIsDiagnose()
	{
		var previous = CreateResolved(CreatePath("repo", "a"), CreatePath("repo", "a", "AppA.slnx"));
		var current = CreateResolved(CreatePath("repo", "b"), CreatePath("repo", "b", "AppB.slnx"));

		var action = WorkspaceTransitionDecisions.DetermineAction(previous, current, WorkspaceTransitionTrigger.McpRoots, devServerStarted: true);

		action.Should().Be(WorkspaceTransitionAction.Diagnose);
	}

	[TestMethod]
	public void WhenWorkspacePathsDifferOnlyByCase_EqualityMatchesCurrentPlatformSemantics()
	{
		var workspace = CreatePath("repo", "src");
		var solution = CreatePath("repo", "src", "App.slnx");
		var previous = CreateResolved(workspace, solution);
		var current = CreateResolved(workspace.ToUpperInvariant(), solution.ToUpperInvariant());

		var isSame = WorkspaceTransitionDecisions.IsSameWorkspace(previous, current);

		isSame.Should().Be(!OperatingSystem.IsLinux());
	}

	[TestMethod]
	public void WhenResolvedWorkspacesOmitSolutionAndGlobalJsonPaths_EqualityDoesNotThrow()
	{
		var workspace = CreatePath("repo", "src");
		var left = new WorkspaceResolution
		{
			RequestedWorkingDirectory = workspace,
			EffectiveWorkspaceDirectory = workspace,
			ResolutionKind = WorkspaceResolutionKind.CurrentDirectory,
		};
		var right = new WorkspaceResolution
		{
			RequestedWorkingDirectory = workspace,
			EffectiveWorkspaceDirectory = workspace,
			ResolutionKind = WorkspaceResolutionKind.CurrentDirectory,
		};

		var action = () => WorkspaceTransitionDecisions.IsSameWorkspace(left, right);

		action.Should().NotThrow();
		WorkspaceTransitionDecisions.IsSameWorkspace(left, right).Should().BeTrue();
	}

	[TestMethod]
	public void WhenSdkVersionChanges_ActionIsRestart()
	{
		var workspace = CreatePath("repo", "src");
		var solution = CreatePath("repo", "src", "App.slnx");
		var previous = CreateResolved(workspace, solution, "6.5.29");
		var current = CreateResolved(workspace, solution, "6.6.0-dev.146");

		var action = WorkspaceTransitionDecisions.DetermineAction(previous, current, WorkspaceTransitionTrigger.FileSystem, devServerStarted: true);

		action.Should().Be(WorkspaceTransitionAction.Restart);
	}

	[TestMethod]
	public void WhenSdkVersionChanges_UserSelectionActionIsRestart()
	{
		var workspace = CreatePath("repo", "src");
		var solution = CreatePath("repo", "src", "App.slnx");
		var previous = CreateResolved(workspace, solution, "6.5.29");
		var current = CreateResolved(workspace, solution, "6.6.0-dev.146");

		var action = WorkspaceTransitionDecisions.DetermineAction(previous, current, WorkspaceTransitionTrigger.UserSelection, devServerStarted: true);

		action.Should().Be(WorkspaceTransitionAction.Restart);
	}

	[TestMethod]
	public void WhenSdkVersionIsUnchanged_ActionIsRefresh()
	{
		var workspace = CreatePath("repo", "src");
		var solution = CreatePath("repo", "src", "App.slnx");
		var previous = CreateResolved(workspace, solution, "6.6.0-dev.1");
		var current = CreateResolved(workspace, solution, "6.6.0-dev.1");

		var action = WorkspaceTransitionDecisions.DetermineAction(previous, current, WorkspaceTransitionTrigger.FileSystem, devServerStarted: true);

		action.Should().Be(WorkspaceTransitionAction.Refresh);
	}

	[TestMethod]
	public void WhenSdkVersionDiffers_IsSameWorkspaceReturnsFalse()
	{
		var workspace = CreatePath("repo", "src");
		var solution = CreatePath("repo", "src", "App.slnx");
		var left = CreateResolved(workspace, solution, "6.5.29");
		var right = CreateResolved(workspace, solution, "6.6.0-dev.146");

		WorkspaceTransitionDecisions.IsSameWorkspace(left, right).Should().BeFalse();
	}

	[TestMethod]
	public void WhenBothSdkVersionsAreNull_IsSameWorkspaceReturnsTrue()
	{
		var workspace = CreatePath("repo", "src");
		var solution = CreatePath("repo", "src", "App.slnx");
		var left = CreateResolved(workspace, solution);
		var right = CreateResolved(workspace, solution);

		WorkspaceTransitionDecisions.IsSameWorkspace(left, right).Should().BeTrue();
	}

	private static WorkspaceResolution CreateResolved(string workspaceDirectory, string solutionPath)
		=> new()
		{
			RequestedWorkingDirectory = workspaceDirectory,
			EffectiveWorkspaceDirectory = workspaceDirectory,
			SelectedSolutionPath = solutionPath,
			SelectedGlobalJsonPath = Path.Combine(workspaceDirectory, "global.json"),
			ResolutionKind = WorkspaceResolutionKind.AutoDiscovered,
			CandidateSolutions = [solutionPath],
		};

	private static WorkspaceResolution CreateResolved(string workspaceDirectory, string solutionPath, string unoSdkVersion)
		=> new()
		{
			RequestedWorkingDirectory = workspaceDirectory,
			EffectiveWorkspaceDirectory = workspaceDirectory,
			SelectedSolutionPath = solutionPath,
			SelectedGlobalJsonPath = Path.Combine(workspaceDirectory, "global.json"),
			UnoSdkPackage = "Uno.Sdk",
			UnoSdkVersion = unoSdkVersion,
			ResolutionKind = WorkspaceResolutionKind.AutoDiscovered,
			CandidateSolutions = [solutionPath],
		};

	private static WorkspaceResolution CreateUnresolved(WorkspaceResolutionKind resolutionKind, params string[] candidates)
		=> new()
		{
			RequestedWorkingDirectory = CreatePath("repo"),
			ResolutionKind = resolutionKind,
			CandidateSolutions = candidates,
		};

	private static string CreatePath(params string[] segments)
	{
		var path = Path.GetTempPath();
		foreach (var segment in segments)
		{
			path = Path.Combine(path, segment);
		}

		return path;
	}
}

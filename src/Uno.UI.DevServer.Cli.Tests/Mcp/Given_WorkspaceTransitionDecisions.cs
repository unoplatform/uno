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
	[Description("MCP roots are a hint: they never replace a workspace whose DevServer is running (#25524)")]
	public void WhenMcpRootsPointToDifferentWorkspaceWhileRunning_ActionIsIgnore()
	{
		var previous = CreateResolved(CreatePath("repo", "a"), CreatePath("repo", "a", "AppA.slnx"));
		var current = CreateResolved(CreatePath("repo", "b"), CreatePath("repo", "b", "AppB.slnx"));

		var action = WorkspaceTransitionDecisions.DetermineAction(previous, current, WorkspaceTransitionTrigger.McpRoots, devServerStarted: true);

		action.Should().Be(WorkspaceTransitionAction.Ignore);
	}

	[TestMethod]
	[Description("MCP roots that do not resolve leave a running workspace in place rather than degrading it (#25524)")]
	public void WhenMcpRootsDoNotResolveWhileRunning_ActionIsIgnore()
	{
		var previous = CreateResolved(CreatePath("repo", "a"), CreatePath("repo", "a", "AppA.slnx"));
		var current = CreateUnresolved(WorkspaceResolutionKind.Ambiguous, CreatePath("repo", "b", "B1.slnx"), CreatePath("repo", "b", "B2.slnx"));

		var action = WorkspaceTransitionDecisions.DetermineAction(previous, current, WorkspaceTransitionTrigger.McpRoots, devServerStarted: true);

		action.Should().Be(WorkspaceTransitionAction.Ignore);
	}

	[TestMethod]
	[Description("MCP roots never override an explicitly selected solution, even one that does not resolve right now (#25524)")]
	public void WhenMcpRootsArriveAfterExplicitSelection_ActionIsIgnore()
	{
		var previous = CreateUnresolved(WorkspaceResolutionKind.NoCandidates) with
		{
			SelectedSolutionPath = CreatePath("repo", "a", "AppA.slnx"),
			IsExplicitSolutionSelection = true,
		};
		var current = CreateResolved(CreatePath("repo", "b"), CreatePath("repo", "b", "AppB.slnx"));

		var action = WorkspaceTransitionDecisions.DetermineAction(previous, current, WorkspaceTransitionTrigger.McpRoots, devServerStarted: false);

		action.Should().Be(WorkspaceTransitionAction.Ignore);
	}

	[TestMethod]
	[Description("Before the DevServer starts (deferred roots startup), MCP roots naming another resolved workspace start it there")]
	public void WhenMcpRootsPointToDifferentWorkspaceBeforeStartup_ActionIsStart()
	{
		var previous = CreateResolved(CreatePath("repo", "a"), CreatePath("repo", "a", "AppA.slnx"));
		var current = CreateResolved(CreatePath("repo", "b"), CreatePath("repo", "b", "AppB.slnx"));

		var action = WorkspaceTransitionDecisions.DetermineAction(previous, current, WorkspaceTransitionTrigger.McpRoots, devServerStarted: false);

		action.Should().Be(WorkspaceTransitionAction.Start);
	}

	[TestMethod]
	[Description("Before the DevServer starts, MCP roots that do not resolve leave the session diagnosable instead of waiting")]
	public void WhenMcpRootsDoNotResolveBeforeStartup_ActionIsDiagnose()
	{
		var previous = CreateResolved(CreatePath("repo", "a"), CreatePath("repo", "a", "AppA.slnx"));
		var current = CreateUnresolved(WorkspaceResolutionKind.NoValidWorkspace, CreatePath("repo", "b", "B.slnx"));

		var action = WorkspaceTransitionDecisions.DetermineAction(previous, current, WorkspaceTransitionTrigger.McpRoots, devServerStarted: false);

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

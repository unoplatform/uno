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
		var current = CreateResolved(@"D:\repo\src", @"D:\repo\src\App.slnx");

		var action = WorkspaceTransitionDecisions.DetermineAction(previous, current, WorkspaceTransitionTrigger.FileSystem);

		action.Should().Be(WorkspaceTransitionAction.Start);
	}

	[TestMethod]
	public void WhenNoValidWorkspaceThenUnoWorkspace_ActionIsStart()
	{
		var previous = CreateUnresolved(WorkspaceResolutionKind.NoValidWorkspace, @"D:\repo\App.slnx");
		var current = CreateResolved(@"D:\repo", @"D:\repo\App.slnx");

		var action = WorkspaceTransitionDecisions.DetermineAction(previous, current, WorkspaceTransitionTrigger.FileSystem);

		action.Should().Be(WorkspaceTransitionAction.Start);
	}

	[TestMethod]
	public void WhenNoCandidatesThenNonUnoSolution_ActionIsDiagnose()
	{
		var previous = CreateUnresolved(WorkspaceResolutionKind.NoCandidates);
		var current = CreateUnresolved(WorkspaceResolutionKind.NoValidWorkspace, @"D:\repo\App.slnx");

		var action = WorkspaceTransitionDecisions.DetermineAction(previous, current, WorkspaceTransitionTrigger.FileSystem);

		action.Should().Be(WorkspaceTransitionAction.Diagnose);
	}

	[TestMethod]
	public void WhenValidWorkspaceLosesGlobalJson_ActionIsStop()
	{
		var previous = CreateResolved(@"D:\repo", @"D:\repo\App.slnx");
		var current = CreateUnresolved(WorkspaceResolutionKind.NoValidWorkspace, @"D:\repo\App.slnx");

		var action = WorkspaceTransitionDecisions.DetermineAction(previous, current, WorkspaceTransitionTrigger.FileSystem);

		action.Should().Be(WorkspaceTransitionAction.Stop);
	}

	[TestMethod]
	public void WhenValidWorkspaceLosesSolution_ActionIsStop()
	{
		var previous = CreateResolved(@"D:\repo", @"D:\repo\App.slnx");
		var current = CreateUnresolved(WorkspaceResolutionKind.NoCandidates);

		var action = WorkspaceTransitionDecisions.DetermineAction(previous, current, WorkspaceTransitionTrigger.FileSystem);

		action.Should().Be(WorkspaceTransitionAction.Stop);
	}

	[TestMethod]
	public void WhenWorkspaceChangesFromAToB_ActionIsRestart()
	{
		var previous = CreateResolved(@"D:\repo\a", @"D:\repo\a\AppA.slnx");
		var current = CreateResolved(@"D:\repo\b", @"D:\repo\b\AppB.slnx");

		var action = WorkspaceTransitionDecisions.DetermineAction(previous, current, WorkspaceTransitionTrigger.FileSystem);

		action.Should().Be(WorkspaceTransitionAction.Restart);
	}

	[TestMethod]
	public void WhenWorkspaceIdentityIsUnchanged_ActionIsRefresh()
	{
		var previous = CreateResolved(@"D:\repo\src", @"D:\repo\src\App.slnx");
		var current = CreateResolved(@"D:\repo\src", @"D:\repo\src\App.slnx");

		var action = WorkspaceTransitionDecisions.DetermineAction(previous, current, WorkspaceTransitionTrigger.FileSystem);

		action.Should().Be(WorkspaceTransitionAction.Refresh);
	}

	[TestMethod]
	public void WhenWorkspaceBecomesAmbiguous_ActionIsStopForFileSystem()
	{
		var previous = CreateResolved(@"D:\repo\src", @"D:\repo\src\App.slnx");
		var current = CreateUnresolved(
			WorkspaceResolutionKind.Ambiguous,
			@"D:\repo\srcA\AppA.slnx",
			@"D:\repo\srcB\AppB.slnx");

		var action = WorkspaceTransitionDecisions.DetermineAction(previous, current, WorkspaceTransitionTrigger.FileSystem);

		action.Should().Be(WorkspaceTransitionAction.Stop);
	}

	[TestMethod]
	public void WhenAmbiguousThenSingleWorkspace_ActionIsStart()
	{
		var previous = CreateUnresolved(
			WorkspaceResolutionKind.Ambiguous,
			@"D:\repo\srcA\AppA.slnx",
			@"D:\repo\srcB\AppB.slnx");
		var current = CreateResolved(@"D:\repo\srcA", @"D:\repo\srcA\AppA.slnx");

		var action = WorkspaceTransitionDecisions.DetermineAction(previous, current, WorkspaceTransitionTrigger.FileSystem);

		action.Should().Be(WorkspaceTransitionAction.Start);
	}

	[TestMethod]
	public void WhenMcpRootsConfirmSameWorkspace_ActionIsRefresh()
	{
		var previous = CreateResolved(@"D:\repo\src", @"D:\repo\src\App.slnx");
		var current = CreateResolved(@"D:\repo\src", @"D:\repo\src\App.slnx");

		var action = WorkspaceTransitionDecisions.DetermineAction(previous, current, WorkspaceTransitionTrigger.McpRoots);

		action.Should().Be(WorkspaceTransitionAction.Refresh);
	}

	[TestMethod]
	public void WhenMcpRootsPointToDifferentWorkspace_ActionIsDiagnose()
	{
		var previous = CreateResolved(@"D:\repo\a", @"D:\repo\a\AppA.slnx");
		var current = CreateResolved(@"D:\repo\b", @"D:\repo\b\AppB.slnx");

		var action = WorkspaceTransitionDecisions.DetermineAction(previous, current, WorkspaceTransitionTrigger.McpRoots);

		action.Should().Be(WorkspaceTransitionAction.Diagnose);
	}

	private static WorkspaceResolution CreateResolved(string workspaceDirectory, string solutionPath)
		=> new()
		{
			RequestedWorkingDirectory = Path.GetDirectoryName(solutionPath)!,
			EffectiveWorkspaceDirectory = workspaceDirectory,
			SelectedSolutionPath = solutionPath,
			SelectedGlobalJsonPath = Path.Combine(workspaceDirectory, "global.json"),
			ResolutionKind = WorkspaceResolutionKind.AutoDiscovered,
			CandidateSolutions = [solutionPath],
		};

	private static WorkspaceResolution CreateUnresolved(WorkspaceResolutionKind resolutionKind, params string[] candidates)
		=> new()
		{
			RequestedWorkingDirectory = @"D:\repo",
			ResolutionKind = resolutionKind,
			CandidateSolutions = candidates,
		};
}

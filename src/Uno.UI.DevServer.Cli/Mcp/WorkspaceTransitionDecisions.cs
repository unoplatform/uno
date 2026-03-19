using Uno.UI.DevServer.Cli.Helpers;

namespace Uno.UI.DevServer.Cli.Mcp;

/// <summary>
/// Centralizes the workspace transition policy used by the MCP bridge when
/// a new workspace resolution is computed after startup.
/// </summary>
internal static class WorkspaceTransitionDecisions
{
	internal static WorkspaceTransitionAction DetermineAction(
		WorkspaceResolution? previous,
		WorkspaceResolution current,
		WorkspaceTransitionTrigger trigger,
		bool devServerStarted)
	{
		var previousResolved = previous?.IsResolved == true;
		var currentResolved = current.IsResolved;

		if (previousResolved && currentResolved && IsSameWorkspace(previous!, current))
		{
			if ((trigger == WorkspaceTransitionTrigger.McpRoots || trigger == WorkspaceTransitionTrigger.UserSelection) && !devServerStarted)
			{
				return WorkspaceTransitionAction.Start;
			}

			return WorkspaceTransitionAction.Refresh;
		}

		if (!previousResolved && currentResolved)
		{
			return WorkspaceTransitionAction.Start;
		}

		if (previousResolved && !currentResolved)
		{
			return trigger == WorkspaceTransitionTrigger.FileSystem
				? WorkspaceTransitionAction.Stop
				: WorkspaceTransitionAction.Diagnose;
		}

		if (previousResolved && currentResolved)
		{
			if (trigger == WorkspaceTransitionTrigger.UserSelection)
			{
				return devServerStarted
					? WorkspaceTransitionAction.Restart
					: WorkspaceTransitionAction.Start;
			}

			return trigger == WorkspaceTransitionTrigger.FileSystem
				? WorkspaceTransitionAction.Restart
				: WorkspaceTransitionAction.Diagnose;
		}

		return WorkspaceTransitionAction.Diagnose;
	}

	internal static bool IsSameWorkspace(WorkspaceResolution left, WorkspaceResolution right)
		=> left.IsResolved
			&& right.IsResolved
			&& PathComparison.PathsEqual(left.EffectiveWorkspaceDirectory, right.EffectiveWorkspaceDirectory)
			&& PathComparison.PathsEqual(left.SelectedSolutionPath, right.SelectedSolutionPath)
			&& PathComparison.PathsEqual(left.SelectedGlobalJsonPath, right.SelectedGlobalJsonPath)
			&& string.Equals(left.UnoSdkVersion, right.UnoSdkVersion, StringComparison.Ordinal);
}

internal enum WorkspaceTransitionTrigger
{
	FileSystem,
	McpRoots,
	UserSelection,
}

internal enum WorkspaceTransitionAction
{
	Refresh,
	Start,
	Stop,
	Restart,
	Diagnose,
}

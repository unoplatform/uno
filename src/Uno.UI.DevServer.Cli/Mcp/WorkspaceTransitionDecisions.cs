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
		WorkspaceTransitionTrigger trigger)
	{
		var previousResolved = previous?.IsResolved == true;
		var currentResolved = current.IsResolved;

		if (previousResolved && currentResolved && IsSameWorkspace(previous!, current))
		{
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
			return trigger == WorkspaceTransitionTrigger.FileSystem
				? WorkspaceTransitionAction.Restart
				: WorkspaceTransitionAction.Diagnose;
		}

		return WorkspaceTransitionAction.Diagnose;
	}

	internal static bool IsSameWorkspace(WorkspaceResolution left, WorkspaceResolution right)
		=> left.IsResolved
			&& right.IsResolved
			&& string.Equals(
				left.EffectiveWorkspaceDirectory,
				right.EffectiveWorkspaceDirectory,
				StringComparison.OrdinalIgnoreCase);
}

internal enum WorkspaceTransitionTrigger
{
	FileSystem,
	McpRoots,
}

internal enum WorkspaceTransitionAction
{
	Refresh,
	Start,
	Stop,
	Restart,
	Diagnose,
}

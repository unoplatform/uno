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

		// MCP roots are a hint (deprecated by SEP-2577): they never override a solution selected explicitly, even one
		// that does not resolve right now (a refresh would drop the selection too).
		if (trigger == WorkspaceTransitionTrigger.McpRoots && previous?.IsExplicitSolutionSelection == true)
		{
			return WorkspaceTransitionAction.Ignore;
		}

		if (previousResolved && currentResolved && IsSameWorkspace(previous!, current))
		{
			if ((trigger == WorkspaceTransitionTrigger.McpRoots || trigger == WorkspaceTransitionTrigger.UserSelection) && !devServerStarted)
			{
				return WorkspaceTransitionAction.Start;
			}

			return WorkspaceTransitionAction.Refresh;
		}

		// Nor do roots replace or degrade another workspace whose DevServer runs. Before a DevServer starts (deferred
		// roots startup) they still decide where it starts.
		if (trigger == WorkspaceTransitionTrigger.McpRoots && previousResolved && devServerStarted)
		{
			return WorkspaceTransitionAction.Ignore;
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
				: WorkspaceTransitionAction.Start;
		}

		return WorkspaceTransitionAction.Diagnose;
	}

	internal static bool IsSameWorkspace(WorkspaceResolution left, WorkspaceResolution right)
		=> left.IsResolved
			&& right.IsResolved
			&& PathComparison.PathsEqual(left.EffectiveWorkspaceDirectory, right.EffectiveWorkspaceDirectory)
			&& PathComparison.PathsEqual(left.SelectedSolutionPath, right.SelectedSolutionPath)
			&& PathComparison.PathsEqual(left.SelectedGlobalJsonPath, right.SelectedGlobalJsonPath)
			&& string.Equals(left.UnoSdkPackage, right.UnoSdkPackage, StringComparison.Ordinal)
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

	/// <summary>The trigger only hints at a workspace; the current one is kept as is.</summary>
	Ignore,
}

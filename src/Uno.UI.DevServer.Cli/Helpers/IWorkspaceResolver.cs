namespace Uno.UI.DevServer.Cli.Helpers;

internal interface IWorkspaceResolver
{
	Task<WorkspaceResolution> ResolveAsync(string requestedDirectory);

	Task<WorkspaceResolution> ResolveExplicitWorkspaceAsync(string requestedDirectory);

	Task<WorkspaceResolution> ResolveSolutionAsync(
		string requestedDirectory,
		string solutionPath,
		WorkspaceSelectionSource selectionSource = WorkspaceSelectionSource.UserSelected);
}

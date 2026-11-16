namespace Uno.UI.DevServer.Cli.Helpers;

/// <summary>
/// What makes a path usable as an explicitly chosen solution, shared by uno_app_select_solution and the MCP bridge's
/// --solution option so both refuse the same paths; each surface words its own message.
/// </summary>
internal static class SolutionPathRules
{
	internal enum Problem
	{
		None,

		/// <summary>A network (UNC) or device path, refused before any file-system access (see <see cref="LocalPathGuard"/>).</summary>
		NotLocal,

		/// <summary>Not a .sln or .slnx path.</summary>
		NotSolutionFile,

		/// <summary>No file exists at that path.</summary>
		NotFound,
	}

	/// <summary>Checks a full (already normalized) path, touching the file system only once it is known to be local.</summary>
	public static Problem Check(string fullPath)
		=> !LocalPathGuard.IsLocalPath(fullPath) ? Problem.NotLocal
			: !SolutionFileFinder.IsSolutionFile(fullPath) ? Problem.NotSolutionFile
			: !File.Exists(fullPath) ? Problem.NotFound
			: Problem.None;
}

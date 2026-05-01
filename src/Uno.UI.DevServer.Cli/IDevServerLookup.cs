namespace Uno.UI.DevServer.Cli;

/// <summary>
/// Abstracts the lookup of running DevServer instances for testability.
/// </summary>
internal interface IDevServerLookup
{
	/// <summary>
	/// Finds an active DevServer that serves the given solution path.
	/// </summary>
	(int ProcessId, int Port, string? SolutionPath, int ParentProcessId)? FindBySolution(string solution);

	/// <summary>
	/// Finds an active DevServer listening on the given port.
	/// </summary>
	(int ProcessId, int Port, string? SolutionPath, int ParentProcessId)? FindByPort(int port);
}

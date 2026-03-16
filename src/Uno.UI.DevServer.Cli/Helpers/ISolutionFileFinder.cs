namespace Uno.UI.DevServer.Cli.Helpers;

internal interface ISolutionFileFinder
{
	string[] FindSolutionFiles(string directory, int maxDepth = 3);
}

internal sealed class FileSystemSolutionFileFinder : ISolutionFileFinder
{
	public string[] FindSolutionFiles(string directory, int maxDepth = 3)
		=> SolutionFileFinder.FindSolutionFiles(directory, maxDepth);
}

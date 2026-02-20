using System.IO;
using System.Text.Json;

namespace Uno.UI.DevServer.Cli.Helpers;

/// <summary>
/// Reads the optional <c>.unoplatform/devserverconfig.json</c> configuration file
/// that can specify a preferred solution path for the dev server.
/// </summary>
internal static class DevServerConfig
{
	private const string ConfigFolder = ".unoplatform";
	private const string ConfigFileName = "devserverconfig.json";

	/// <summary>
	/// Tries to read the <c>SolutionPath</c> from a <c>.unoplatform/devserverconfig.json</c>
	/// file in the given directory.
	/// </summary>
	/// <param name="directory">The root directory to search for the config file.</param>
	/// <param name="solutionPath">
	/// When this method returns <c>true</c>, contains the full path to the solution file
	/// specified in the config. The path is resolved relative to <paramref name="directory"/>
	/// if it is not absolute.
	/// </param>
	/// <returns><c>true</c> if a valid config was found with a <c>SolutionPath</c> pointing
	/// to an existing file; otherwise <c>false</c>.</returns>
	public static bool TryGetSolutionPath(string directory, out string? solutionPath)
	{
		solutionPath = null;

		var configPath = Path.Combine(directory, ConfigFolder, ConfigFileName);
		if (!File.Exists(configPath))
		{
			return false;
		}

		try
		{
			var json = File.ReadAllText(configPath);
			using var doc = JsonDocument.Parse(json);

			if (doc.RootElement.TryGetProperty("SolutionPath", out var solutionElement))
			{
				var rawPath = solutionElement.GetString();
				if (!string.IsNullOrWhiteSpace(rawPath))
				{
					var fullPath = Path.IsPathRooted(rawPath)
						? Path.GetFullPath(rawPath)
						: Path.GetFullPath(Path.Combine(directory, rawPath));

					if (File.Exists(fullPath))
					{
						solutionPath = fullPath;
						return true;
					}
				}
			}
		}
		catch (Exception ex)
		{
			Console.Error.WriteLine($"Warning: Failed to read dev server config at '{configPath}': {ex.Message}");
		}

		return false;
	}
}

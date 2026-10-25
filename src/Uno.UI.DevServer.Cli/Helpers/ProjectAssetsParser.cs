using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.SolutionPersistence.Serializer;

namespace Uno.UI.DevServer.Cli.Helpers;

/// <summary>
/// Parses project.assets.json files to extract package references for add-in discovery.
/// </summary>
/// <seealso href="../addin-discovery.md"/>
internal static class ProjectAssetsParser
{
	/// <summary>
	/// Parses the <c>$.libraries</c> section of a project.assets.json file
	/// and returns (packageName, version) tuples for entries with <c>type == "package"</c>.
	/// </summary>
	public static List<(string packageName, string version)> ParseLibraries(
		string projectAssetsPath, ILogger logger)
	{
		var results = new List<(string, string)>();

		try
		{
			var content = File.ReadAllText(projectAssetsPath);
			using var document = JsonDocument.Parse(content);

			if (!document.RootElement.TryGetProperty("libraries", out var libraries))
			{
				logger.LogDebug("No 'libraries' section found in {Path}", projectAssetsPath);
				return results;
			}

			foreach (var entry in libraries.EnumerateObject())
			{
				var key = entry.Name;
				var slashIndex = key.IndexOf('/');
				if (slashIndex < 0)
				{
					logger.LogDebug("Skipping malformed library key '{Key}' (no '/' separator)", key);
					continue;
				}

				var packageName = key[..slashIndex];
				var version = key[(slashIndex + 1)..];

				if (string.IsNullOrWhiteSpace(packageName) || string.IsNullOrWhiteSpace(version))
				{
					logger.LogDebug("Skipping malformed library key '{Key}'", key);
					continue;
				}

				// Only include type == "package", skip type == "project"
				if (entry.Value.TryGetProperty("type", out var typeElement))
				{
					var type = typeElement.GetString();
					if (!string.Equals(type, "package", StringComparison.OrdinalIgnoreCase))
					{
						continue;
					}
				}

				results.Add((packageName, version));
			}

			logger.LogDebug("Parsed {Count} packages from project.assets.json at {Path}", results.Count, projectAssetsPath);
		}
		catch (Exception ex)
		{
			logger.LogWarning(ex, "Failed to parse project.assets.json at {Path}", projectAssetsPath);
		}

		return results;
	}

	/// <summary>
	/// The restored assets of the projects <paramref name="solutionPath"/> lists; falls back to scanning
	/// <paramref name="workspaceDirectory"/> when the solution is missing, unreadable or not restored yet.
	/// </summary>
	public static async Task<List<string>> FindProjectAssetsFilesAsync(
		string? solutionPath, string workspaceDirectory, ILogger logger, CancellationToken ct = default)
	{
		if (!string.IsNullOrWhiteSpace(solutionPath))
		{
			var fromSolution = await FindProjectAssetsFilesFromSolutionAsync(solutionPath, logger, ct);
			if (fromSolution.Count > 0)
			{
				return fromSolution;
			}

			logger.LogDebug(
				"No restored project of {Solution} was found, scanning {Dir} for project.assets.json instead",
				solutionPath,
				workspaceDirectory);
		}

		return FindProjectAssetsFiles(workspaceDirectory, logger);
	}

	/// <summary>
	/// The <c>obj/project.assets.json</c> of each restored project <paramref name="solutionPath"/> lists, in solution
	/// order, at any depth (the folder scan misses <c>src/MyApp</c>, #24311). Empty when the solution can't be read.
	/// </summary>
	public static async Task<List<string>> FindProjectAssetsFilesFromSolutionAsync(
		string solutionPath, ILogger logger, CancellationToken ct = default)
	{
		var results = new List<string>();

		try
		{
			var fullSolutionPath = Path.GetFullPath(solutionPath);
			if (!File.Exists(fullSolutionPath))
			{
				logger.LogDebug("Solution {Solution} does not exist, no project.assets.json read from it", fullSolutionPath);
				return results;
			}

			var serializer = SolutionSerializers.GetSerializerByMoniker(fullSolutionPath);
			if (serializer is null)
			{
				logger.LogDebug("No solution serializer for {Solution}", fullSolutionPath);
				return results;
			}

			var model = await serializer.OpenAsync(fullSolutionPath, ct).ConfigureAwait(false);
			var solutionDirectory = Path.GetDirectoryName(fullSolutionPath)!;
			var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

			foreach (var project in model.SolutionProjects)
			{
				var filePath = project.FilePath;
				if (string.IsNullOrWhiteSpace(filePath))
				{
					continue;
				}

				var projectPath = Path.GetFullPath(Path.IsPathRooted(filePath) ? filePath : Path.Combine(solutionDirectory, filePath));
				var assets = Path.Combine(Path.GetDirectoryName(projectPath)!, "obj", "project.assets.json");
				if (File.Exists(assets) && seen.Add(assets))
				{
					results.Add(assets);
				}
			}

			logger.LogDebug(
				"Found {Count} project.assets.json file(s) for the {Projects} project(s) of {Solution}",
				results.Count,
				model.SolutionProjects.Count,
				fullSolutionPath);
		}
		catch (Exception ex) when (ex is not OperationCanceledException)
		{
			logger.LogWarning(ex, "Failed to read the projects of {Solution} for project.assets.json", solutionPath);
			results.Clear();
		}

		return results;
	}

	/// <summary>
	/// Scans <paramref name="startDirectory"/> and its immediate subdirectories
	/// for <c>obj/project.assets.json</c> files.
	/// </summary>
	public static List<string> FindProjectAssetsFiles(
		string startDirectory, ILogger logger)
	{
		var results = new List<string>();

		try
		{
			// Check {startDirectory}/obj/project.assets.json
			var rootAssets = Path.Combine(startDirectory, "obj", "project.assets.json");
			if (File.Exists(rootAssets))
			{
				results.Add(rootAssets);
			}

			// Check immediate subdirectories: {startDirectory}/{subdir}/obj/project.assets.json
			foreach (var subDir in Directory.EnumerateDirectories(startDirectory))
			{
				var subAssets = Path.Combine(subDir, "obj", "project.assets.json");
				if (File.Exists(subAssets))
				{
					results.Add(subAssets);
				}
			}

			logger.LogDebug("Found {Count} project.assets.json file(s) under {Dir}", results.Count, startDirectory);
		}
		catch (Exception ex)
		{
			logger.LogWarning(ex, "Failed to scan for project.assets.json under {Dir}", startDirectory);
		}

		return results;
	}
}

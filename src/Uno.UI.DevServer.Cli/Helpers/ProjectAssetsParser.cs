using System.Text.Json;
using Microsoft.Extensions.Logging;

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

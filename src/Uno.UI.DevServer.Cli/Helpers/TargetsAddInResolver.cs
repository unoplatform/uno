using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;

namespace Uno.UI.DevServer.Cli.Helpers;

internal partial class TargetsAddInResolver(ILogger<TargetsAddInResolver> logger)
{
	private readonly ILogger<TargetsAddInResolver> _logger = logger;

	[GeneratedRegex(@"\$\(([^)]+)\)")]
	private static partial Regex PropertyRefRegex();

	[GeneratedRegex(@"exists\s*\(\s*'([^']+)'\s*\)", RegexOptions.IgnoreCase)]
	private static partial Regex ExistsConditionRegex();

	[GeneratedRegex(@"'([^']*)'\s*==\s*'([^']*)'")]
	private static partial Regex EqualityConditionRegex();

	[GeneratedRegex(@"'([^']*)'\s*!=\s*'([^']*)'")]
	private static partial Regex InequalityConditionRegex();

	public List<ResolvedAddIn> ResolveAddIns(string packagesJsonPath, IReadOnlyList<string>? nugetCachePaths = null)
	{
		var results = new List<ResolvedAddIn>();

		List<(string packageName, string version)> packages;
		try
		{
			packages = ParsePackagesJson(packagesJsonPath);
		}
		catch (Exception ex)
		{
			_logger.LogWarning(ex, "Failed to parse packages.json at {Path}", packagesJsonPath);
			return results;
		}

		nugetCachePaths ??= NuGetCacheHelper.GetNuGetCachePaths();

		foreach (var (packageName, version) in packages)
		{
			var packageDir = FindPackageDirectory(packageName, version, nugetCachePaths);
			if (packageDir is null)
			{
				continue;
			}

			var targetsFiles = FindTargetsFiles(packageDir);
			if (targetsFiles.Count == 0)
			{
				continue;
			}

			foreach (var targetsFile in targetsFiles)
			{
				try
				{
					var addIns = ParseTargetsFile(targetsFile, packageName, version);
					results.AddRange(addIns);
				}
				catch (Exception ex)
				{
					_logger.LogWarning(ex, "Failed to parse targets file {File}", targetsFile);
				}
			}

			// Diagnostic: check for tools/devserver/ without add-in registration
			if (results.All(a => !a.PackageName.Equals(packageName, StringComparison.OrdinalIgnoreCase)))
			{
				var devServerToolsDir = Path.Combine(packageDir, "tools", "devserver");
				if (Directory.Exists(devServerToolsDir))
				{
					_logger.LogWarning(
						"Package {Package} has a tools/devserver/ directory but no UnoRemoteControlAddIns item was found in its .targets files",
						packageName);
				}
			}
		}

		return results;
	}

	private List<(string packageName, string version)> ParsePackagesJson(string packagesJsonPath)
	{
		var content = File.ReadAllText(packagesJsonPath);
		using var document = JsonDocument.Parse(content);
		var packages = new List<(string, string)>();

		if (document.RootElement.ValueKind == JsonValueKind.Array)
		{
			foreach (var group in document.RootElement.EnumerateArray())
			{
				if (group.TryGetProperty("packages", out var packagesElement) &&
					group.TryGetProperty("version", out var versionElement))
				{
					var version = versionElement.GetString();
					if (version is not null && packagesElement.ValueKind == JsonValueKind.Array)
					{
						foreach (var package in packagesElement.EnumerateArray())
						{
							var name = package.GetString();
							if (!string.IsNullOrWhiteSpace(name))
							{
								packages.Add((name, version));
							}
						}
					}
				}
			}
		}

		_logger.LogDebug("Parsed {Count} packages from packages.json", packages.Count);
		return packages;
	}

	private static string? FindPackageDirectory(string packageName, string version, IReadOnlyList<string> nugetCachePaths)
	{
		// NuGet cache stores package directories in lowercase
		var loweredPackageName = packageName.ToLowerInvariant();

		foreach (var cachePath in nugetCachePaths)
		{
			var packageDir = Path.Combine(cachePath, loweredPackageName, version);
			if (Directory.Exists(packageDir))
			{
				return packageDir;
			}
		}

		return null;
	}

	private static List<string> FindTargetsFiles(string packageDir)
	{
		var results = new List<string>();

		// Primary: buildTransitive/*.targets
		var buildTransitiveDir = Path.Combine(packageDir, "buildTransitive");
		if (Directory.Exists(buildTransitiveDir))
		{
			results.AddRange(Directory.GetFiles(buildTransitiveDir, "*.targets"));
		}

		// Fallback: build/*.targets (only if buildTransitive had nothing)
		if (results.Count == 0)
		{
			var buildDir = Path.Combine(packageDir, "build");
			if (Directory.Exists(buildDir))
			{
				results.AddRange(Directory.GetFiles(buildDir, "*.targets"));
			}
		}

		return results;
	}

	private List<ResolvedAddIn> ParseTargetsFile(string targetsFilePath, string packageName, string packageVersion)
	{
		var results = new List<ResolvedAddIn>();

		XDocument doc;
		try
		{
			doc = XDocument.Load(targetsFilePath);
		}
		catch (Exception ex)
		{
			_logger.LogWarning(ex, "Failed to load XML from {File}", targetsFilePath);
			return results;
		}

		if (doc.Root is null)
		{
			return results;
		}

		var ns = doc.Root.Name.Namespace;

		// Build property dictionary with MSBuild builtins
		var targetsDir = Path.GetDirectoryName(targetsFilePath)!;
		if (!targetsDir.EndsWith(Path.DirectorySeparatorChar))
		{
			targetsDir += Path.DirectorySeparatorChar;
		}

		var properties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
		{
			["MSBuildThisFileDirectory"] = targetsDir,
			["MSBuildThisFile"] = Path.GetFileName(targetsFilePath)
		};

		// Collect properties from PropertyGroup elements
		foreach (var propertyGroup in doc.Root.Elements(ns + "PropertyGroup"))
		{
			foreach (var prop in propertyGroup.Elements())
			{
				var propName = prop.Name.LocalName;
				var condition = prop.Attribute("Condition")?.Value;

				if (!string.IsNullOrWhiteSpace(condition) && !EvaluateCondition(condition, properties))
				{
					continue;
				}

				var rawValue = prop.Value.Trim();
				var resolved = ResolvePropertyReferences(rawValue, properties);
				properties[propName] = resolved;
			}
		}

		// Find UnoRemoteControlAddIns items
		foreach (var itemGroup in doc.Root.Elements(ns + "ItemGroup"))
		{
			foreach (var item in itemGroup.Elements(ns + "UnoRemoteControlAddIns"))
			{
				var include = item.Attribute("Include")?.Value;
				if (string.IsNullOrWhiteSpace(include))
				{
					continue;
				}

				var condition = item.Attribute("Condition")?.Value;
				if (!string.IsNullOrWhiteSpace(condition) && !EvaluateCondition(condition, properties))
				{
					continue;
				}

				var resolved = ResolvePropertyReferences(include, properties);

				// Normalize the path
				string fullPath;
				try
				{
					fullPath = Path.GetFullPath(resolved);
				}
				catch (Exception ex)
				{
					_logger.LogWarning(ex, "Failed to normalize path '{Path}' from {File}", resolved, targetsFilePath);
					continue;
				}

				// Verify the DLL exists
				if (!File.Exists(fullPath))
				{
					_logger.LogDebug("Add-in DLL not found at resolved path {Path} (from {File})", fullPath, targetsFilePath);
					continue;
				}

				results.Add(new ResolvedAddIn
				{
					PackageName = packageName,
					PackageVersion = packageVersion,
					EntryPointDll = fullPath,
					DiscoverySource = "targets"
				});

				_logger.LogDebug("Resolved add-in {Dll} from {Package} {Version}", fullPath, packageName, packageVersion);
			}
		}

		return results;
	}

	private static string ResolvePropertyReferences(string value, Dictionary<string, string> properties)
	{
		// Max 5 levels of resolution to prevent infinite loops
		for (int i = 0; i < 5; i++)
		{
			var resolved = PropertyRefRegex().Replace(value, match =>
			{
				var propName = match.Groups[1].Value;
				return properties.TryGetValue(propName, out var propValue) ? propValue : match.Value;
			});

			if (resolved == value)
			{
				break;
			}

			value = resolved;
		}

		return value;
	}

	private bool EvaluateCondition(string condition, Dictionary<string, string> properties)
	{
		// Resolve property references in the condition first
		var resolved = ResolvePropertyReferences(condition, properties);

		// Handle exists('path') conditions
		var existsMatch = ExistsConditionRegex().Match(resolved);
		if (existsMatch.Success)
		{
			var path = existsMatch.Groups[1].Value;
			try
			{
				return File.Exists(path) || Directory.Exists(path);
			}
			catch
			{
				return false;
			}
		}

		// Handle simple equality: 'value1'=='value2'
		var equalityMatch = EqualityConditionRegex().Match(resolved);
		if (equalityMatch.Success)
		{
			return string.Equals(
				equalityMatch.Groups[1].Value,
				equalityMatch.Groups[2].Value,
				StringComparison.OrdinalIgnoreCase);
		}

		// Handle simple inequality: 'value1'!='value2'
		var inequalityMatch = InequalityConditionRegex().Match(resolved);
		if (inequalityMatch.Success)
		{
			return !string.Equals(
				inequalityMatch.Groups[1].Value,
				inequalityMatch.Groups[2].Value,
				StringComparison.OrdinalIgnoreCase);
		}

		// If we can't evaluate the condition, assume true (optimistic)
		_logger.LogDebug("Cannot evaluate condition '{Condition}', assuming true", condition);
		return true;
	}
}

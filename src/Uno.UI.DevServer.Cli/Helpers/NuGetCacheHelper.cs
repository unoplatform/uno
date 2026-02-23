using System.Xml.Linq;

namespace Uno.UI.DevServer.Cli.Helpers;

internal static class NuGetCacheHelper
{
	internal static IReadOnlyList<string> GetNuGetCachePaths()
		=> GetNuGetCachePaths(workingDirectory: null);

	internal static IReadOnlyList<string> GetNuGetCachePaths(string? workingDirectory)
	{
		var paths = new List<string>();

		// 1. Highest priority: NUGET_PACKAGES env var
		var nugetEnv = Environment.GetEnvironmentVariable("NUGET_PACKAGES");
		if (!string.IsNullOrWhiteSpace(nugetEnv))
		{
			paths.Add(nugetEnv);
		}

		// 2. globalPackagesFolder from nuget.config (walking up from workingDirectory, then user profile)
		if (!string.IsNullOrWhiteSpace(workingDirectory))
		{
			var configOverride = TryGetGlobalPackagesFolderFromConfig(workingDirectory);
			if (configOverride is not null && !paths.Contains(configOverride, StringComparer.OrdinalIgnoreCase))
			{
				paths.Add(configOverride);
			}
		}

		// 3. Default paths
		var userProfile = Path.Combine(
			Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
			".nuget", "packages");
		if (!paths.Contains(userProfile, StringComparer.OrdinalIgnoreCase))
		{
			paths.Add(userProfile);
		}

		var commonAppData = Path.Combine(
			Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
			"NuGet", "packages");
		if (!paths.Contains(commonAppData, StringComparer.OrdinalIgnoreCase))
		{
			paths.Add(commonAppData);
		}

		return paths;
	}

	/// <summary>
	/// Searches for nuget.config files from <paramref name="startDirectory"/> up to the filesystem root,
	/// then checks the user-level NuGet.Config, looking for a globalPackagesFolder setting.
	/// </summary>
	internal static string? TryGetGlobalPackagesFolderFromConfig(string startDirectory)
	{
		// Walk up from startDirectory looking for nuget.config
		var dir = startDirectory;
		while (!string.IsNullOrEmpty(dir))
		{
			var configPath = Path.Combine(dir, "nuget.config");
			var result = TryParseGlobalPackagesFolder(configPath, dir);
			if (result is not null)
			{
				return result;
			}

			// Also try NuGet.Config (case variation used on some systems)
			configPath = Path.Combine(dir, "NuGet.Config");
			result = TryParseGlobalPackagesFolder(configPath, dir);
			if (result is not null)
			{
				return result;
			}

			var parent = Path.GetDirectoryName(dir);
			if (parent == dir)
			{
				break;
			}
			dir = parent;
		}

		// Check user-level NuGet.Config
		var userConfigDir = Path.Combine(
			Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
			"NuGet");
		var userConfigPath = Path.Combine(userConfigDir, "NuGet.Config");
		return TryParseGlobalPackagesFolder(userConfigPath, userConfigDir);
	}

	private static string? TryParseGlobalPackagesFolder(string configPath, string baseDirectory)
	{
		try
		{
			if (!File.Exists(configPath))
			{
				return null;
			}

			var doc = XDocument.Load(configPath);
			var configElement = doc.Root?.Element("config");
			if (configElement is null)
			{
				return null;
			}

			foreach (var add in configElement.Elements("add"))
			{
				var key = add.Attribute("key")?.Value;
				if (string.Equals(key, "globalPackagesFolder", StringComparison.OrdinalIgnoreCase))
				{
					var value = add.Attribute("value")?.Value;
					if (!string.IsNullOrWhiteSpace(value))
					{
						return Path.IsPathRooted(value)
							? Path.GetFullPath(value)
							: Path.GetFullPath(Path.Combine(baseDirectory, value));
					}
				}
			}
		}
		catch
		{
			// Ignore parse errors — fall through to next config
		}

		return null;
	}

	/// <summary>
	/// NuGet normalizes 2-part versions to 3-part in the cache folder name
	/// (e.g. "1.8-dev.19" becomes "1.8.0-dev.19"). This method applies the same normalization.
	/// </summary>
	internal static string NormalizeNuGetVersion(string version)
	{
		var dashIndex = version.IndexOf('-');
		var versionPart = dashIndex >= 0 ? version[..dashIndex] : version;
		var suffix = dashIndex >= 0 ? version[dashIndex..] : "";

		if (versionPart.Count(c => c == '.') == 1)
		{
			return versionPart + ".0" + suffix;
		}

		return version;
	}
}

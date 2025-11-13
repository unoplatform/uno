using System.Reflection;
using System.IO;
using System.Text.RegularExpressions;

namespace Uno.UI.RemoteControl.DevServer.Tests.Helpers;

/// <summary>
/// Provides utilities to discover the path of external DLLs built within the repository.
/// This helper encapsulates the file system probing logic so it can be reused by tests
/// that need to locate various host/process binaries.
/// </summary>
internal static class ExternalDllDiscoveryHelper
{
	/// <summary>
	/// High-level discovery that derives configuration/framework from the provided assembly and
	/// optionally honors an environment variable override.
	/// </summary>
	/// <param name="logger">Logger used for diagnostics.</param>
	/// <param name="contextAssembly">Assembly used to infer current config and target framework.</param>
	/// <param name="projectName">The project folder name in the repo (e.g., "Uno.UI.RemoteControl.Host").</param>
	/// <param name="dllFileName">The DLL file name to locate (e.g., "Uno.UI.RemoteControl.Host.dll").</param>
	/// <param name="environmentVariableName">Optional environment variable name that, when set to a valid file path, overrides discovery.</param>
	/// <param name="additionalConfigurations">Optional additional configurations to probe.</param>
	/// <param name="additionalTargetFrameworks">Optional additional target frameworks to probe.</param>
	/// <returns>The full path to the DLL if found; otherwise null.</returns>
	public static string? DiscoverExternalDllPath(
		ILogger logger,
		Assembly contextAssembly,
		string projectName,
		string dllFileName,
		string? environmentVariableName = null,
		IReadOnlyList<string>? additionalConfigurations = null,
		IReadOnlyList<string>? additionalTargetFrameworks = null)
	{
		// Strategy 0: Optional environment variable override
		if (!string.IsNullOrWhiteSpace(environmentVariableName))
		{
			var customPath = Environment.GetEnvironmentVariable(environmentVariableName);
			if (!string.IsNullOrEmpty(customPath) && File.Exists(customPath))
			{
				logger.LogInformation("Using custom DLL path from environment variable {EnvVar}: {Path}", environmentVariableName, customPath);
				return customPath;
			}
		}

		var assemblyLocation = Path.GetDirectoryName(contextAssembly.Location)!;

		// Build configurations list: current -> Debug -> Release (+ any additions)
		var configurations = new List<string>(capacity: 4);
		try
		{
			var cfg = (contextAssembly.GetCustomAttribute(typeof(AssemblyConfigurationAttribute)) as AssemblyConfigurationAttribute)?.Configuration;
			if (!string.IsNullOrWhiteSpace(cfg))
			{
				configurations.Add(cfg!);
			}
		}
		catch (Exception ex)
		{
			logger.LogWarning(ex, "Failed to get current build configuration");
		}
		configurations.Add("Debug");
		configurations.Add("Release");
		if (additionalConfigurations is not null)
		{
			foreach (var c in additionalConfigurations)
			{
				if (!string.IsNullOrWhiteSpace(c))
				{
					configurations.Add(c);
				}
			}
		}
		// De-dup while preserving order
		var uniqueConfigurations = configurations.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();

		// Determine compatible target frameworks from contextAssembly
#pragma warning disable SYSLIB1045
		var currentFramework = contextAssembly.GetCustomAttributes(true)
			.OfType<System.Runtime.Versioning.TargetFrameworkAttribute>()
			.FirstOrDefault()?.FrameworkName ?? string.Empty;
		var versionMatch = Regex.Match(currentFramework, @"Version=v(\d+\.\d+)");
		var frameworks = new List<string>();
		if (versionMatch.Success && Version.TryParse(versionMatch.Groups[1].Value, out var currentVersion))
		{
			frameworks.Add($"net{currentVersion}");
			frameworks.Add($"net{currentVersion.Major + 1}.0");
		}
		else
		{
			frameworks.Add("net10.0");
		}
#pragma warning restore SYSLIB1045
		if (additionalTargetFrameworks is not null)
		{
			foreach (var tfm in additionalTargetFrameworks)
			{
				if (!string.IsNullOrWhiteSpace(tfm))
				{
					frameworks.Add(tfm);
				}
			}
		}
		var uniqueFrameworks = frameworks.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();

		var discovered = DiscoverExternalDllPath(
			logger,
			assemblyLocation,
			uniqueConfigurations,
			uniqueFrameworks,
			projectName,
			dllFileName);

		if (discovered is null)
		{
			logger.LogError(
				"Could not discover DLL path. Tried configurations: {Configurations}, frameworks: {Frameworks} for {Project}/{Dll}",
				string.Join(", ", uniqueConfigurations), string.Join(", ", uniqueFrameworks), projectName, dllFileName);
		}

		return discovered;
	}

	/// <summary>
	/// Discovers a DLL path by probing common build output locations relative to an assembly location.
	/// </summary>
	/// <param name="logger">Logger used for diagnostics.</param>
	/// <param name="assemblyLocation">Typically the current test assembly directory.</param>
	/// <param name="configurations">Configurations to probe (e.g., Debug/Release).</param>
	/// <param name="frameworks">Target frameworks to probe (e.g., net9.0, net10.0).</param>
	/// <param name="projectName">The project folder name in the repo (e.g., "Uno.UI.RemoteControl.Host").</param>
	/// <param name="dllFileName">The DLL file name to locate (e.g., "Uno.UI.RemoteControl.Host.dll").</param>
	/// <returns>The full path to the DLL if found; otherwise null.</returns>
	public static string? DiscoverExternalDllPath(
		Microsoft.Extensions.Logging.ILogger logger,
		string assemblyLocation,
		IReadOnlyList<string> configurations,
		IReadOnlyList<string> frameworks,
		string projectName,
		string dllFileName)
	{
		// Strategy A: Probe repository build output folders
		foreach (var config in configurations)
		{
			foreach (var framework in frameworks)
			{
				var candidate = Path.GetFullPath(Path.Combine(
					assemblyLocation,
					"..", "..", "..", "..",
					projectName, "bin", config, framework,
					dllFileName));

				if (File.Exists(candidate))
				{
					logger.LogInformation("Found DLL using discovery: {Path}", candidate);
					return candidate;
				}
			}
		}

		// Strategy B: Try MSBuild output directory patterns next to test assembly
		var possiblePaths = new[]
		{
			Path.Combine(assemblyLocation, dllFileName),
			Path.Combine(assemblyLocation, "..", projectName, dllFileName),
		};

		foreach (var path in possiblePaths)
		{
			var fullPath = Path.GetFullPath(path);
			if (File.Exists(fullPath))
			{
				logger.LogInformation("Found DLL in output directory: {Path}", fullPath);
				return fullPath;
			}
		}

		return null;
	}
}

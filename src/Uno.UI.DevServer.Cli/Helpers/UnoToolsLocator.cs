using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Uno.UI.DevServer.Cli.Helpers;

internal class UnoToolsLocator(ILogger<UnoToolsLocator> logger)
{
	private readonly ILogger<UnoToolsLocator> _logger = logger;

	public async Task<string?> ResolveSettingsExecutableAsync(string workDirectory)
	{
		var sdkVersion = await GetSdkVersionFromGlobalJson(workDirectory);

		if (sdkVersion.sdkVersion == null || sdkVersion.sdkPackage == null)
		{
			_logger.LogError("Could not determine SDK version from global.json.");
			return null;
		}

		_logger.LogDebug("SDK Version: {SdkPackage} {SdkVersion}", sdkVersion.sdkPackage, sdkVersion.sdkVersion);

		var unoSdkPath = await EnsureNugetPackage(sdkVersion.sdkPackage, sdkVersion.sdkVersion);

		if (unoSdkPath == null)
		{
			_logger.LogError("Uno SDK package version {SdkPackage} {SdkVersion} not found. Please ensure the Uno SDK is properly installed.", sdkVersion.sdkPackage, sdkVersion.sdkVersion);
			return null;
		}
		_logger.LogDebug("Found Uno SDK: {UnoSdkPath}", unoSdkPath);

		var studioPath = await GetSettingsExecutable(unoSdkPath);

		if (studioPath is null)
		{
			_logger.LogError("Could not determine Settings application executable path.");
			return null;
		}

		return studioPath;
	}

	public async Task<string?> ResolveHostExecutableAsync(string workDirectory)
	{
		var sdkVersion = await GetSdkVersionFromGlobalJson(workDirectory);
		if (sdkVersion.sdkVersion == null || sdkVersion.sdkPackage == null)
		{
			_logger.LogError("Could not determine SDK version from global.json.");
			return null;
		}
		_logger.LogDebug("SDK Version: {SdkPackage} {SdkVersion}", sdkVersion.sdkPackage, sdkVersion.sdkVersion);

		var unoSdkPath = await EnsureNugetPackage(sdkVersion.sdkPackage, sdkVersion.sdkVersion);
		if (unoSdkPath == null)
		{
			_logger.LogError("Uno SDK package version {SdkPackage} {SdkVersion} not found. Please ensure the Uno SDK is properly installed.", sdkVersion.sdkPackage, sdkVersion.sdkVersion);
			return null;
		}
		_logger.LogDebug("Found Uno SDK: {UnoSdkPath}", unoSdkPath);

		var hostPath = await GetHostExecutable(unoSdkPath);
		if (hostPath is null)
		{
			_logger.LogError("Could not determine DevServer host executable path.");
			return null;
		}
		return hostPath;
	}

	private static string? FindGlobalJson(string startPath)
	{
		var currentPath = startPath;
		while (currentPath != null)
		{
			var globalJsonPath = Path.Combine(currentPath, "global.json");
			if (File.Exists(globalJsonPath))
			{
				return globalJsonPath;
			}

			var parent = Directory.GetParent(currentPath);
			currentPath = parent?.FullName;
		}

		return null;
	}

	private async Task<(string? sdkPackage, string? sdkVersion)> GetSdkVersionFromGlobalJson(string searchDirectory)
	{
		try
		{
			var globalJsonPath = FindGlobalJson(searchDirectory);
			if (globalJsonPath is null)
			{
				_logger.LogError("No global.json found in current directory or parent directories. Please run this command from within a project that uses Uno SDK.");
				return (null, null);
			}

			_logger.LogDebug("Found global.json: {GlobalJsonPath}", globalJsonPath);

			var content = await File.ReadAllTextAsync(globalJsonPath);
			using var document = JsonDocument.Parse(
				content,
				new()
				{
					CommentHandling = JsonCommentHandling.Skip,
					AllowTrailingCommas = true
				});

			if (document.RootElement.TryGetProperty("msbuild-sdks", out var sdksElement))
			{
				if (sdksElement.TryGetProperty("Uno.Sdk", out var unoSdkElement))
				{
					return ("Uno.Sdk", unoSdkElement.GetString() ?? "");
				}

				if (sdksElement.TryGetProperty("Uno.Sdk.Private", out var unoSdkPrivateElement))
				{
					return ("Uno.Sdk.Private", unoSdkPrivateElement.GetString() ?? "");
				}
			}
		}
		catch (Exception ex)
		{
			_logger.LogWarning(ex, "Failed to parse global.json: {ErrorMessage}", ex.Message);
		}

		return (null, null);
	}

	private async Task InstallUnoSdk(string packageId, string version)
	{
		// Here we pre-install the uno.sdk/uno.sdk.private package in order to get all the packages
		// that are needed for the devserver to work properly.

		var csprojContents =
			$"""
			<Project Sdk="{packageId}/{version}">
				<PropertyGroup>
					<TargetFramework>net9.0</TargetFramework>
					<UnoSingleProject>true</UnoSingleProject>
					<OutputType>exe</OutputType>
				</PropertyGroup>
			</Project>
			""";

		var tempPath = Path.Combine(Path.GetTempPath(), $"uno-sdk-installer-{Guid.NewGuid()}");

		Directory.CreateDirectory(tempPath);

		File.WriteAllText(Path.Combine(tempPath, "uno-sdk-installer.csproj"), csprojContents);

		_logger.LogDebug("Installing package {SdkPackage} version {Version} using temporary project in {TempPath}", packageId, version, tempPath);

		var result = ProcessHelpers.RunProcess("dotnet", "restore", tempPath);
		if (result.exitCode != 0)
		{
			_logger.LogWarning("Package installation returned exit code {ExitCode}. Output: {Output}. Error: {Error}", result.exitCode, result.output, result.error);
		}
		else
		{
			_logger.LogDebug("Package {SdkPackage} version {Version} installed successfully", packageId, version);
		}
	}

	private async Task<string?> EnsureNugetPackage(string packageId, string version, bool tryInstall = true)
	{
		var possiblePaths = new[]
		{
			Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nuget", "packages", packageId.ToLowerInvariant(), version),
			Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "NuGet", "packages", packageId.ToLowerInvariant(), version),
			Path.Combine(Environment.GetEnvironmentVariable("NUGET_PACKAGES") ?? "", packageId.ToLowerInvariant(), version)
		};

		foreach (var path in possiblePaths)
		{
			if (Directory.Exists(path))
			{
				_logger.LogDebug("Found package {SdkPackage} version {Version} at {Path}", packageId, version, path);
				return path;
			}
		}

		if (!tryInstall)
		{
			_logger.LogDebug("Package {SdkPackage} version {Version} not found in any NuGet cache location", packageId, version);
			return null;
		}
		else
		{
			_logger.LogInformation("Package {SdkPackage} version {Version} not found in cache, attempting to install...", packageId, version);
			await InstallUnoSdk(packageId, version);

			return await EnsureNugetPackage(packageId, version, false);
		}
	}

	private async Task<string?> GetHostExecutable(string unoSdkPath)
	{
		try
		{
			var devServerPackageVersion = await GetDevServerPackageVersion(unoSdkPath);
			if (devServerPackageVersion is null)
			{
				return null;
			}

			var dotnetVersion = await GetDotNetVersion();
			if (dotnetVersion is null)
			{
				return null;
			}

			var devServerPackagePath = await EnsureNugetPackage("Uno.WinUI.DevServer", devServerPackageVersion, false);
			if (devServerPackagePath is null)
			{
				return null;
			}

			var hostExe = Path.Combine(devServerPackagePath, "tools", "rc", "host", dotnetVersion, "Uno.UI.RemoteControl.Host.exe");
			if (File.Exists(hostExe))
			{
				_logger.LogDebug("Found DevServer Host: {Path}", hostExe);
				return hostExe;
			}

			var hostDll = Path.Combine(devServerPackagePath, "tools", "rc", "host", dotnetVersion, "Uno.UI.RemoteControl.Host.dll");
			if (File.Exists(hostDll))
			{
				_logger.LogDebug("Found DevServer Host: {Path}", hostDll);
				return hostDll;
			}

			_logger.LogError("DevServer host not found in package at expected paths");
			return null;
		}
		catch (Exception ex)
		{
			_logger.LogWarning(ex, "Failed to locate host executable: {Message}", ex.Message);
			return null;
		}
	}

	private async Task<string?> GetSettingsExecutable(string unoSdkPath)
	{
		try
		{
			var devServerPackageVersion = await GetSettingsPackageVersion(unoSdkPath);
			if (devServerPackageVersion is null)
			{
				return null;
			}

			var dotnetVersion = await GetDotNetVersion();
			if (dotnetVersion is null)
			{
				return null;
			}

			var licensingPackagePath = await EnsureNugetPackage("uno.settings.devserver", devServerPackageVersion, false);
			if (licensingPackagePath is null)
			{
				return null;
			}

			var hostExe = Path.Combine(licensingPackagePath, "tools", "manager", "Uno.Settings.dll");
			if (File.Exists(hostExe))
			{
				_logger.LogDebug("Found Settings App: {Path}", hostExe);
				return hostExe;
			}

			_logger.LogError("Settings App executable not found in package at expected paths");
			return null;
		}
		catch (Exception ex)
		{
			_logger.LogWarning(ex, "Failed to locate Settings App executable: {Message}", ex.Message);
			return null;
		}
	}

	private async Task<string?> GetDevServerPackageVersion(string unoSdkPath)
		=> await GetUnoPackageVersionFromManifest(unoSdkPath, "uno.winui.devserver");

	private async Task<string?> GetSettingsPackageVersion(string unoSdkPath)
		=> await GetUnoPackageVersionFromManifest(unoSdkPath, "uno.settings.devserver");

	private async Task<string?> GetUnoPackageVersionFromManifest(string unoSdkPath, string packageName)
	{
		try
		{
			var packagesJsonPath = Path.Combine(unoSdkPath, "targets", "netstandard2.0", "packages.json");

			if (!File.Exists(packagesJsonPath))
			{
				_logger.LogWarning("packages.json not found at {PackagesJsonPath}", packagesJsonPath);
				return null;
			}

			var content = await File.ReadAllTextAsync(packagesJsonPath);
			using var document = JsonDocument.Parse(content);

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
								var manifestPackageName = package.GetString();
								if (manifestPackageName != null && manifestPackageName.Equals(packageName, StringComparison.OrdinalIgnoreCase))
								{
									_logger.LogDebug("Found {PackageName} package version {Version} in packages.json", packageName, version);
									return version;
								}
							}
						}
					}
				}
			}
		}
		catch (Exception ex)
		{
			_logger.LogWarning(ex, "Failed to parse packages.json: {ErrorMessage}", ex.Message);
		}

		return null;
	}

	private async Task<string?> GetDotNetVersion()
	{
		try
		{
			var processInfo = new ProcessStartInfo
			{
				FileName = "dotnet",
				Arguments = "--version",
				UseShellExecute = false,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				CreateNoWindow = true
			};

			using var process = Process.Start(processInfo);
			if (process == null)
			{
				_logger.LogWarning("Failed to start dotnet --version process");
				return null;
			}

			var output = await process.StandardOutput.ReadToEndAsync();
			await process.WaitForExitAsync();

			if (process.ExitCode == 0 && !string.IsNullOrWhiteSpace(output))
			{
				var version = output.Trim();
				_logger.LogDebug("Raw .NET version output: {Version}", version);

				var sanitizedVersion = version.Split('-')[0]; // Remove any pre-release suffix

				if (Version.TryParse(sanitizedVersion, out var parsedVersion))
				{
					var targetFramework = $"net{parsedVersion.Major}.{parsedVersion.Minor}";
					_logger.LogDebug("Parsed target framework: {TargetFramework}", targetFramework);
					return targetFramework;
				}
				else
				{
					_logger.LogDebug("Failed to parse .NET Version");
				}
			}
			else
			{
				_logger.LogWarning("dotnet --version failed with exit code {ExitCode}", process.ExitCode);
			}

			return null;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error getting .NET version: {ErrorMessage}", ex.Message);
			return null;
		}
	}
}

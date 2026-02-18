using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Uno.UI.DevServer.Cli.Helpers;

internal class UnoToolsLocator(ILogger<UnoToolsLocator> logger, TargetsAddInResolver? addInResolver = null, DotNetVersionCache? dotNetVersionCache = null)
{
	private readonly ILogger<UnoToolsLocator> _logger = logger;
	private readonly TargetsAddInResolver? _addInResolver = addInResolver;
	private readonly DotNetVersionCache? _dotNetVersionCache = dotNetVersionCache;

	public async Task<DiscoveryInfo> DiscoverAsync(string workDirectory)
	{
		string? globalJsonPath = null;
		string? unoSdkSource = null;
		string? unoSdkSourcePath = null;
		string? unoSdkPackage = null;
		string? unoSdkVersion = null;
		string? unoSdkPath = null;
		string? packagesJsonPath = null;
		string? devServerPackageVersion = null;
		string? devServerPackagePath = null;
		string? settingsPackageVersion = null;
		string? settingsPackagePath = null;
		string? dotNetVersion = null;
		string? dotNetTfm = null;
		string? hostPath = null;
		string? settingsPath = null;
		var warnings = new List<string>();
		var errors = new List<string>();

		var globalJsonResult = await ParseGlobalJsonForUnoSdk(workDirectory);
		globalJsonPath = globalJsonResult.globalJsonPath;
		unoSdkPackage = globalJsonResult.sdkPackage;
		unoSdkVersion = globalJsonResult.sdkVersion;
		if (globalJsonPath is not null)
		{
			unoSdkSource = "global.json";
			unoSdkSourcePath = globalJsonPath;
		}

		if (globalJsonResult.globalJsonPath is null)
		{
			errors.Add("No global.json found in current directory or parent directories.");
		}
		else if (unoSdkPackage is null || unoSdkVersion is null)
		{
			errors.Add("global.json does not define Uno.Sdk or Uno.Sdk.Private in msbuild-sdks.");
		}

		if (!string.IsNullOrWhiteSpace(unoSdkPackage) && !string.IsNullOrWhiteSpace(unoSdkVersion))
		{
			unoSdkPath = await EnsureNugetPackage(unoSdkPackage, unoSdkVersion, tryInstall: false);
			if (unoSdkPath is null)
			{
				errors.Add($"Uno SDK package not found in NuGet cache: {unoSdkPackage} {unoSdkVersion}.");
			}
		}

		if (!string.IsNullOrWhiteSpace(unoSdkPath))
		{
			packagesJsonPath = Path.Combine(unoSdkPath, "targets", "netstandard2.0", "packages.json");
			if (!File.Exists(packagesJsonPath))
			{
				errors.Add($"packages.json not found at {packagesJsonPath}.");
				packagesJsonPath = null;
			}
		}

		devServerPackageVersion = unoSdkPath is null
			? null
			: await GetDevServerPackageVersion(unoSdkPath);
		if (unoSdkPath is not null && devServerPackageVersion is null)
		{
			errors.Add("Uno.WinUI.DevServer version not found in packages.json.");
		}

		settingsPackageVersion = unoSdkPath is null
			? null
			: await GetSettingsPackageVersion(unoSdkPath);
		if (unoSdkPath is not null && settingsPackageVersion is null)
		{
			warnings.Add("uno.settings.devserver version not found in packages.json.");
		}

		if (!string.IsNullOrWhiteSpace(devServerPackageVersion))
		{
			devServerPackagePath = await EnsureNugetPackage("Uno.WinUI.DevServer", devServerPackageVersion, tryInstall: false);
			if (devServerPackagePath is null)
			{
				errors.Add($"Uno.WinUI.DevServer not found in NuGet cache: {devServerPackageVersion}.");
			}
		}

		if (!string.IsNullOrWhiteSpace(settingsPackageVersion))
		{
			settingsPackagePath = await EnsureNugetPackage("uno.settings.devserver", settingsPackageVersion, tryInstall: false);
			if (settingsPackagePath is null)
			{
				warnings.Add($"uno.settings.devserver not found in NuGet cache: {settingsPackageVersion}.");
			}
		}

		var dotnetInfo = await TryGetDotNetVersionInfo(logErrors: false, globalJsonPath: globalJsonPath);
		dotNetVersion = dotnetInfo.rawVersion;
		dotNetTfm = dotnetInfo.tfm;
		if (dotNetVersion is null)
		{
			errors.Add("Unable to determine dotnet --version.");
		}

		if (!string.IsNullOrWhiteSpace(devServerPackagePath) && !string.IsNullOrWhiteSpace(dotNetTfm))
		{
			var hostExe = Path.Combine(devServerPackagePath, "tools", "rc", "host", dotNetTfm, "Uno.UI.RemoteControl.Host.exe");
			var hostDll = Path.Combine(devServerPackagePath, "tools", "rc", "host", dotNetTfm, "Uno.UI.RemoteControl.Host.dll");
			if (File.Exists(hostExe))
			{
				hostPath = hostExe;
			}
			else if (File.Exists(hostDll))
			{
				hostPath = hostDll;
			}
			else
			{
				errors.Add("DevServer host not found in package at expected paths.");
			}
		}

		if (!string.IsNullOrWhiteSpace(settingsPackagePath))
		{
			var settingsPathCandidate = Path.Combine(settingsPackagePath, "tools", "manager", "Uno.Settings.dll");
			if (File.Exists(settingsPathCandidate))
			{
				settingsPath = settingsPathCandidate;
			}
			else
			{
				warnings.Add("Settings application not found in package at expected path.");
			}
		}

		// Convention-based add-in discovery
		var resolvedAddIns = new List<ResolvedAddIn>();
		string? addInsDiscoveryMethod = null;
		long addInsDiscoveryDurationMs = 0;
		bool addInDiscoveryFailed = false;

		if (_addInResolver is not null && packagesJsonPath is not null)
		{
			var addInStopwatch = Stopwatch.StartNew();
			try
			{
				resolvedAddIns = _addInResolver.ResolveAddIns(packagesJsonPath);
				addInsDiscoveryMethod = resolvedAddIns.Count > 0
					? string.Join("+", resolvedAddIns.Select(a => a.DiscoverySource).Distinct())
					: null;
			}
			catch (Exception ex)
			{
				addInDiscoveryFailed = true;
				warnings.Add($"Convention-based add-in discovery failed: {ex.Message}");
			}
			addInsDiscoveryDurationMs = addInStopwatch.ElapsedMilliseconds;
		}

		return new DiscoveryInfo
		{
			WorkingDirectory = workDirectory,
			GlobalJsonPath = globalJsonPath,
			UnoSdkSource = unoSdkSource,
			UnoSdkSourcePath = unoSdkSourcePath,
			UnoSdkPackage = unoSdkPackage,
			UnoSdkVersion = unoSdkVersion,
			UnoSdkPath = unoSdkPath,
			PackagesJsonPath = packagesJsonPath,
			DevServerPackageVersion = devServerPackageVersion,
			DevServerPackagePath = devServerPackagePath,
			SettingsPackageVersion = settingsPackageVersion,
			SettingsPackagePath = settingsPackagePath,
			DotNetVersion = dotNetVersion,
			DotNetTfm = dotNetTfm,
			HostPath = hostPath,
			SettingsPath = settingsPath,
			AddIns = resolvedAddIns,
			AddInsDiscoveryMethod = addInsDiscoveryMethod,
			AddInsDiscoveryDurationMs = addInsDiscoveryDurationMs,
			AddInDiscoveryFailed = addInDiscoveryFailed,
			Warnings = warnings,
			Errors = errors
		};
	}

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
		var result = await ParseGlobalJsonForUnoSdk(searchDirectory);
		if (result.globalJsonPath is null)
		{
			_logger.LogError("No global.json found in current directory or parent directories. Please run this command from within a project that uses Uno SDK.");
			return (null, null);
		}

		_logger.LogDebug("Found global.json: {GlobalJsonPath}", result.globalJsonPath);

		if (result.sdkPackage is null || result.sdkVersion is null)
		{
			_logger.LogWarning("global.json does not define Uno.Sdk or Uno.Sdk.Private in msbuild-sdks.");
			return (null, null);
		}

		return (result.sdkPackage, result.sdkVersion);
	}

	private async Task<(string? globalJsonPath, string? sdkPackage, string? sdkVersion)> ParseGlobalJsonForUnoSdk(string searchDirectory)
	{
		try
		{
			var globalJsonPath = FindGlobalJson(searchDirectory);
			if (globalJsonPath is null)
			{
				return (null, null, null);
			}

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
					return (globalJsonPath, "Uno.Sdk", unoSdkElement.GetString() ?? "");
				}

				if (sdksElement.TryGetProperty("Uno.Sdk.Private", out var unoSdkPrivateElement))
				{
					return (globalJsonPath, "Uno.Sdk.Private", unoSdkPrivateElement.GetString() ?? "");
				}
			}

			return (globalJsonPath, null, null);
		}
		catch (Exception ex)
		{
			_logger.LogWarning(ex, "Failed to parse global.json: {ErrorMessage}", ex.Message);
		}

		return (null, null, null);
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

	internal static IReadOnlyList<string> GetNuGetCachePaths()
		=> NuGetCacheHelper.GetNuGetCachePaths();

	private async Task<string?> EnsureNugetPackage(string packageId, string version, bool tryInstall = true)
	{
		var possiblePaths = GetNuGetCachePaths()
			.Select(p => Path.Combine(p, packageId.ToLowerInvariant(), version))
			.ToArray();

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
		var result = await TryGetDotNetVersionInfo(logErrors: true);
		return result.tfm;
	}

	private async Task<(string? rawVersion, string? tfm)> TryGetDotNetVersionInfo(bool logErrors, string? globalJsonPath = null, bool force = false)
	{
		if (_dotNetVersionCache is not null)
		{
			try
			{
				return await _dotNetVersionCache.GetOrRefreshAsync(globalJsonPath, force);
			}
			catch (Exception ex)
			{
				if (logErrors)
				{
					_logger.LogWarning(ex, "DotNetVersionCache failed, falling back to direct subprocess");
				}
			}
		}

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
				if (logErrors)
				{
					_logger.LogWarning("Failed to start dotnet --version process");
				}
				return (null, null);
			}

			var output = await process.StandardOutput.ReadToEndAsync();
			await process.WaitForExitAsync();

			if (process.ExitCode == 0 && !string.IsNullOrWhiteSpace(output))
			{
				var version = output.Trim();
				if (logErrors)
				{
					_logger.LogDebug("Raw .NET version output: {Version}", version);
				}

				var sanitizedVersion = version.Split('-')[0]; // Remove any pre-release suffix

				if (Version.TryParse(sanitizedVersion, out var parsedVersion))
				{
					var targetFramework = $"net{parsedVersion.Major}.{parsedVersion.Minor}";
					if (logErrors)
					{
						_logger.LogDebug("Parsed target framework: {TargetFramework}", targetFramework);
					}
					return (version, targetFramework);
				}
				else
				{
					if (logErrors)
					{
						_logger.LogDebug("Failed to parse .NET Version");
					}
				}
			}
			else
			{
				if (logErrors)
				{
					_logger.LogWarning("dotnet --version failed with exit code {ExitCode}", process.ExitCode);
				}
			}

			return (null, null);
		}
		catch (Exception ex)
		{
			if (logErrors)
			{
				_logger.LogError(ex, "Error getting .NET version: {ErrorMessage}", ex.Message);
			}
			return (null, null);
		}
	}
}

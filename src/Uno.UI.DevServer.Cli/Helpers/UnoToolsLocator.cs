using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Uno.UI.RemoteControl.Host;

namespace Uno.UI.DevServer.Cli.Helpers;

internal class UnoToolsLocator(ILogger<UnoToolsLocator> logger, TargetsAddInResolver? addInResolver = null)
{
	private readonly ILogger<UnoToolsLocator> _logger = logger;
	private readonly TargetsAddInResolver? _addInResolver = addInResolver;
	private string? _workDirectory;

	public async Task<DiscoveryInfo> DiscoverAsync(string workDirectory)
		=> await DiscoverAsync(workDirectory, null);

	public async Task<DiscoveryInfo> DiscoverAsync(string workDirectory, WorkspaceResolution? workspaceResolution)
	{
		var discoveryStopwatch = Stopwatch.StartNew();

		workspaceResolution ??= new WorkspaceResolution
		{
			RequestedWorkingDirectory = Path.GetFullPath(workDirectory),
			EffectiveWorkspaceDirectory = Path.GetFullPath(workDirectory),
			ResolutionKind = WorkspaceResolutionKind.CurrentDirectory,
			CandidateSolutions = SolutionFileFinder.FindSolutionFiles(workDirectory),
		};

		if (!workspaceResolution.IsResolved)
		{
			discoveryStopwatch.Stop();
			return new DiscoveryInfo
			{
				RequestedWorkingDirectory = workspaceResolution.RequestedWorkingDirectory,
				WorkingDirectory = workspaceResolution.RequestedWorkingDirectory,
				EffectiveWorkspaceDirectory = workspaceResolution.EffectiveWorkspaceDirectory,
				SelectedSolutionPath = workspaceResolution.SelectedSolutionPath,
				SelectedGlobalJsonPath = workspaceResolution.SelectedGlobalJsonPath,
				ResolutionKind = workspaceResolution.ResolutionKind,
				SelectionSource = workspaceResolution.SelectionSource,
				CandidateSolutions = workspaceResolution.CandidateSolutions,
				DiscoveryDurationMs = discoveryStopwatch.ElapsedMilliseconds,
				Errors = workspaceResolution.ResolutionKind == WorkspaceResolutionKind.NoCandidates
					? []
					: ["Workspace could not be resolved."],
			};
		}

		workDirectory = workspaceResolution.EffectiveWorkspaceDirectory!;
		_workDirectory = workDirectory;
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

		var globalJsonResult = await GlobalJsonLocator.ParseGlobalJsonForUnoSdkAsync(workDirectory, _logger);
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
				var projectAssetsFiles = ProjectAssetsParser.FindProjectAssetsFiles(workDirectory, _logger);
				resolvedAddIns = _addInResolver.ResolveAddIns(packagesJsonPath, projectAssetsFiles, NuGetCacheHelper.GetNuGetCachePaths(workDirectory));
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

		// Lookup active DevServers via AmbientRegistry
		// Show all servers on this machine, with IsInWorkspace flag for matching solution or directory.
		var activeServers = new List<ActiveServerInfo>();
		try
		{
			var localSolutions = Directory.EnumerateFiles(workDirectory, "*.sln")
				.Concat(Directory.EnumerateFiles(workDirectory, "*.slnx"))
				.Select(f => Path.GetFullPath(f))
				.ToHashSet(StringComparer.OrdinalIgnoreCase);
			var workDirFull = Path.GetFullPath(workDirectory);

			var ambient = new AmbientRegistry(NullLogger.Instance);
			activeServers = ambient.GetActiveDevServers()
				.Where(s => s.MachineName == Environment.MachineName && s.UserName == Environment.UserName)
				.Select(s =>
				{
					var isInWorkspace = IsServerLocal(s.SolutionPath, localSolutions, workDirFull);
					return new ActiveServerInfo
					{
						ProcessId = s.ProcessId,
						Port = s.Port,
						McpEndpoint = $"http://localhost:{s.Port}/mcp",
						ParentProcessId = s.ParentProcessId,
						StartTime = s.StartTime,
						IdeChannelId = s.IdeChannelId,
						SolutionPath = s.SolutionPath,
						IsInWorkspace = isInWorkspace,
					};
				})
				.ToList();
		}
		catch (Exception ex)
		{
			_logger.LogDebug(ex, "AmbientRegistry lookup failed");
		}

		discoveryStopwatch.Stop();

		return new DiscoveryInfo
		{
			RequestedWorkingDirectory = workspaceResolution.RequestedWorkingDirectory,
			WorkingDirectory = workDirectory,
			EffectiveWorkspaceDirectory = workspaceResolution.EffectiveWorkspaceDirectory,
			SelectedSolutionPath = workspaceResolution.SelectedSolutionPath,
			SelectedGlobalJsonPath = workspaceResolution.SelectedGlobalJsonPath,
			ResolutionKind = workspaceResolution.ResolutionKind,
			SelectionSource = workspaceResolution.SelectionSource,
			CandidateSolutions = workspaceResolution.CandidateSolutions,
			DiscoveryDurationMs = discoveryStopwatch.ElapsedMilliseconds,
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
			ActiveServers = activeServers,
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
		=> await GlobalJsonLocator.ParseGlobalJsonForUnoSdkAsync(searchDirectory, _logger);

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

	/// <summary>
	/// Determines if a server is "local" to the working directory by matching
	/// its solution against any solution in the directory, or by checking if
	/// the solution resides within the working directory (for solution-less or
	/// multi-solution repos).
	/// </summary>
	private static bool IsServerLocal(string? serverSolutionPath, HashSet<string> localSolutions, string workDirFull)
	{
		if (string.IsNullOrWhiteSpace(serverSolutionPath))
		{
			return false;
		}

		string serverSolutionFull;
		try
		{
			serverSolutionFull = Path.GetFullPath(serverSolutionPath);
		}
		catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
		{
			// Malformed path from AmbientRegistry — treat as non-workspace
			return false;
		}

		// Direct match: server's solution is one of the solutions in the working directory
		if (localSolutions.Contains(serverSolutionFull))
		{
			return true;
		}

		// Directory match: server's solution is inside the working directory tree
		var serverDir = Path.GetDirectoryName(serverSolutionFull);
		if (serverDir is not null)
		{
			var normalizedServerDir = serverDir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
			var normalizedWorkDir = workDirFull.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
			if (normalizedServerDir.StartsWith(normalizedWorkDir, StringComparison.OrdinalIgnoreCase)
				&& (normalizedServerDir.Length == normalizedWorkDir.Length
					|| normalizedServerDir[normalizedWorkDir.Length] is '/' or '\\'))
			{
				return true;
			}
		}

		return false;
	}

	internal static IReadOnlyList<string> GetNuGetCachePaths(string? workingDirectory = null)
		=> NuGetCacheHelper.GetNuGetCachePaths(workingDirectory);

	private async Task<string?> EnsureNugetPackage(string packageId, string version, bool tryInstall = true)
	{
		// NuGet normalizes 2-part versions to 3-part (e.g. "1.8-dev.19" → "1.8.0-dev.19").
		// Try both the original version string and the normalized form.
		var versions = new List<string> { version };
		var normalized = NuGetCacheHelper.NormalizeNuGetVersion(version);
		if (normalized != version)
		{
			versions.Add(normalized);
		}

		var cachePaths = GetNuGetCachePaths(_workDirectory);
		foreach (var v in versions)
		{
			var possiblePaths = cachePaths
				.Select(p => Path.Combine(p, packageId.ToLowerInvariant(), v))
				.ToArray();

			foreach (var path in possiblePaths)
			{
				if (Directory.Exists(path))
				{
					_logger.LogDebug("Found package {SdkPackage} version {Version} at {Path}", packageId, v, path);
					return path;
				}
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

	private async Task<(string? rawVersion, string? tfm)> TryGetDotNetVersionInfo(bool logErrors, string? globalJsonPath = null)
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

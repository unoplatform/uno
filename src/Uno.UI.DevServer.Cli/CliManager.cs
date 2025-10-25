using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Uno.UI.DevServer.Cli.Helpers;

namespace Uno.UI.DevServer.Cli;

internal class CliManager
{
	private readonly ILogger<CliManager> _logger;

	public CliManager(ILogger<CliManager> logger)
	{
		_logger = logger;
	}

	public async Task<int> RunAsync(string[] originalArgs)
	{
		try
		{
			if (originalArgs.Contains("--mcp"))
			{
				return await RunMcpProxyAsync(originalArgs.Where(a => a != "--mcp").ToArray());
			}

			ShowBanner();

			if (originalArgs is { Length: > 0 } && string.Equals(originalArgs[0], "login", StringComparison.OrdinalIgnoreCase))
			{
				return await OpenSettings(originalArgs);
			}

			var hostPath = await ResolveHostExecutableAsync();
			if (hostPath is null)
			{
				return 1; // errors already logged
			}

			var isDirectOutputCommand = originalArgs.Length > 0 && (
				string.Equals(originalArgs[0], "list", StringComparison.OrdinalIgnoreCase) ||
				string.Equals(originalArgs[0], "cleanup", StringComparison.OrdinalIgnoreCase)
			);

			var startInfo = BuildHostArgs(hostPath, originalArgs, redirectOutput: !isDirectOutputCommand);

			var result = await DevServerProcessHelper.RunConsoleProcessAsync(startInfo, _logger);
			return result.ExitCode;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error running command: {ErrorMessage}", ex.Message);
			return 1;
		}
	}

	private void ShowBanner()
	{
		// get the assembly informational version
		var attrs = typeof(CliManager).Assembly.GetCustomAttributes(typeof(System.Reflection.AssemblyInformationalVersionAttribute), false);

		if (attrs.Length > 0 && attrs[0] is System.Reflection.AssemblyInformationalVersionAttribute versionAttr)
		{
			// Take only what's before a `+`, we don't want the commit hash here
			var items = versionAttr.InformationalVersion.Split('+', StringSplitOptions.RemoveEmptyEntries);

			_logger.LogInformation("Uno Platform DevServer CLI - Version {Version}", items[0]);
		}
		else
		{
			_logger.LogInformation("Uno Platform DevServer CLI - Dev Version");
		}
	}

	private async Task<int> OpenSettings(string[] originalArgs)
	{
		var studioExecutable = await ResolveSettingsExecutableAsync();

		if (studioExecutable is null)
		{
			return 1; // errors already logged
		}

		var startInfo = DevServerProcessHelper.CreateDotnetProcessStartInfo(studioExecutable, originalArgs, redirectOutput: true);

		var (exitCode, stdOut, stdErr) = await DevServerProcessHelper.RunGuiProcessAsync(startInfo, _logger, TimeSpan.FromSeconds(3));

		if (exitCode is not null)
		{
			// Display output for debugging purposes
			if (!string.IsNullOrWhiteSpace(stdOut))
			{
				_logger.LogDebug("Settings application stdout:\n{Stdout}", stdOut);
			}
			if (!string.IsNullOrWhiteSpace(stdErr))
			{
				_logger.LogError("Settings application stderr:\n{Stderr}", stdErr);
			}

			_logger.LogError("Settings application exited with code {ExitCode}", exitCode);

			return 1;
		}
		else
		{
			_logger.LogInformation("Settings application started successfully");
			return 0;
		}
	}

	private async Task<int> RunMcpProxyAsync(string[] args)
	{
		try
		{
			_logger.LogInformation("Starting MCP Mode");

			int requestedPort = 0;
			var forwardedArgs = new List<string>();
			for (int i = 0; i < args.Length; i++)
			{
				var a = args[i];
				if (a == "--port" || a == "-p")
				{
					if (i + 1 >= args.Length)
					{
						_logger.LogError($"Missing value for {a}");
						return 1;
					}
					if (!int.TryParse(args[i + 1], System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out requestedPort) || requestedPort <= 0 || requestedPort > 65535)
					{
						_logger.LogError($"Invalid port value '{args[i + 1]}'");
						return 1;
					}
					i++; // skip value
					continue; // do not forward port arguments to controller
				}
				forwardedArgs.Add(a);
			}

			if (requestedPort == 0)
			{
				requestedPort = EnsureTcpPort();
				_logger.LogDebug($"Automatically selected free port {requestedPort}");
			}
			else
			{
				_logger.LogDebug($"Using user-specified port {requestedPort}");
			}

			var hostPath = await ResolveHostExecutableAsync();
			if (hostPath is null)
			{
				return 1; // errors already logged
			}

			var proxy = new McpProxy(_logger);
			return await proxy.RunAsync(hostPath, requestedPort, forwardedArgs, CancellationToken.None);
		}
		catch (Exception ex)
		{
			_logger.LogError($"MCP proxy error: {ex.Message}");
			return 1;
		}
	}

	private async Task<string?> ResolveSettingsExecutableAsync()
	{
		var sdkVersion = await GetSdkVersionFromGlobalJson();

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

	private async Task<string?> ResolveHostExecutableAsync()
	{
		var sdkVersion = await GetSdkVersionFromGlobalJson();
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

	private ProcessStartInfo BuildHostArgs(string hostPath, string[] originalArgs, bool redirectOutput = true)
	{
		var args = new List<string> { "--command" };
		if (originalArgs.Length > 0)
		{
			args.Add(originalArgs[0]);
			for (int i = 1; i < originalArgs.Length; i++)
			{
				args.Add(originalArgs[i]);
			}
		}
		else
		{
			args.Add("start");
		}

		return DevServerProcessHelper.CreateDotnetProcessStartInfo(hostPath, args, redirectOutput);
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

	private async Task<(string? sdkPackage, string? sdkVersion)> GetSdkVersionFromGlobalJson()
	{
		try
		{
			var globalJsonPath = FindGlobalJson(Directory.GetCurrentDirectory());
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

	private static int EnsureTcpPort()
	{
		var tcp = new TcpListener(IPAddress.Any, 0) { ExclusiveAddressUse = true };
		tcp.Start();
		var port = ((IPEndPoint)tcp.LocalEndpoint).Port;
		tcp.Stop();
		return port;
	}
}

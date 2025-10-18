using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Uno.UI.DevServer.Cli.Helpers;
using System.Runtime.InteropServices;
using System.Text;

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

			_logger.LogInformation("Uno Platform DevServer CLI");

			var hostPath = await ResolveHostExecutableAsync();
			if (hostPath is null)
			{
				return 1; // errors already logged
			}

			// If the command is 'list' or 'cleanup' we want the subprocess to write directly to the console
			var isDirectOutputCommand = originalArgs.Length > 0 && (
				string.Equals(originalArgs[0], "list", StringComparison.OrdinalIgnoreCase) ||
				string.Equals(originalArgs[0], "cleanup", StringComparison.OrdinalIgnoreCase)
			);

			// Build args: prepend --command and forward user args as-is
			var forwardedArgs = BuildHostArgs(hostPath, originalArgs, redirectOutput: !isDirectOutputCommand);

			_logger.LogDebug("Starting host process: {FileName} {Arguments}", forwardedArgs.FileName, string.Join(" ", forwardedArgs.ArgumentList));
			var process = Process.Start(forwardedArgs);
			_logger.LogDebug("Started Host process: {pid}", process?.Id);

			if (process is null)
			{
				_logger.LogError("Failed to start DevServer Host process.");
				return 1;
			}

			if (isDirectOutputCommand)
			{
				// When not redirecting, just wait for exit — output goes to console directly.
				await process.WaitForExitAsync();

				if (process.ExitCode != 0)
				{
					_logger.LogError("Host exited with code {ExitCode}", process.ExitCode);
					return process.ExitCode;
				}

				_logger.LogInformation("Command completed successfully.");
				return 0;
			}

			var outputSb = new StringBuilder();
			var errorSb = new StringBuilder();

			process.OutputDataReceived += (s, e) =>
			{
				if (e.Data != null)
				{
					outputSb.AppendLine(e.Data);

					if (_logger.IsEnabled(LogLevel.Debug))
					{
						_logger.LogDebug("[DevServer:stdout] " + e.Data);
					}
				}
			};

			process.ErrorDataReceived += (s, e) =>
			{
				if (e.Data != null)
				{
					errorSb.AppendLine(e.Data);

					if (_logger.IsEnabled(LogLevel.Debug))
					{
						_logger.LogDebug("[DevServer:stderr] " + e.Data);
					}
				}
			};

			// Start asynchronous read of output streams to avoid potential deadlocks on Windows
			try
			{
				process.BeginOutputReadLine();
				process.BeginErrorReadLine();
			}
			catch (InvalidOperationException)
			{
				// Streams might not be available; fall back to not reading asynchronously
			}

			var processExited = new TaskCompletionSource();

			process.Exited += (e, s) =>
			{
				_logger.LogTrace("Host has exited (code: {ExitCode})", process.ExitCode);
				processExited.TrySetResult();
			};

			// Wait for both process exit event and WaitForExitAsync, in
			// case some std is blocking the process exit.
			await Task.WhenAny(process.WaitForExitAsync(), processExited.Task);

			var output = outputSb.ToString();
			var errorOutput = errorSb.ToString();

			if (process.ExitCode != 0)
			{
				_logger.LogError("Host exited with code {ExitCode}", process.ExitCode);

				if (!string.IsNullOrWhiteSpace(output))
				{
					_logger.LogError("Host standard output for troubleshooting:\n{Output}", output);
				}
				if (!string.IsNullOrWhiteSpace(errorOutput))
				{
					_logger.LogError("Host error output for troubleshooting:\n{ErrorOutput}", errorOutput);
				}

				return process.ExitCode;
			}

			_logger.LogInformation("Command completed successfully.");
			return 0;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error running command: {ErrorMessage}", ex.Message);
			return 1;
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
		// Use dotnet on non-Windows platforms or when the host is a DLL
		var useDotnet = !RuntimeInformation.IsOSPlatform(OSPlatform.Windows) || hostPath.EndsWith(".dll", StringComparison.OrdinalIgnoreCase);

		var psi = new ProcessStartInfo
		{
			FileName = useDotnet ? "dotnet" : hostPath,
			UseShellExecute = false,
			CreateNoWindow = true,
			RedirectStandardOutput = redirectOutput,
			RedirectStandardError = redirectOutput,
			WorkingDirectory = Directory.GetCurrentDirectory(),
		};

		var hostArgPath = hostPath;
		if (useDotnet)
		{
			// If the package provides an .exe but we're invoking via dotnet (non-Windows),
			// switch to the .dll equivalent so dotnet can run it.
			if (hostArgPath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
			{
				// Use span-based concat to avoid allocations warning (CA1845)
				hostArgPath = string.Concat(hostArgPath.AsSpan(0, hostArgPath.Length - 4), ".dll");
			}

			psi.ArgumentList.Add(hostArgPath);
		}

		psi.ArgumentList.Add("--command");
		if (originalArgs.Length > 0)
		{
			psi.ArgumentList.Add(originalArgs[0]);
			for (int i = 1; i < originalArgs.Length; i++)
			{
				psi.ArgumentList.Add(originalArgs[i]);
			}
		}
		else
		{
			psi.ArgumentList.Add("start");
		}

		return psi;
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

	private async Task<string?> GetDevServerPackageVersion(string unoSdkPath)
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
								var packageName = package.GetString();
								if (packageName != null && packageName.Equals("Uno.WinUI.DevServer", StringComparison.OrdinalIgnoreCase))
								{
									_logger.LogDebug("Found Uno.WinUI.DevServer package version {Version} in packages.json", version);
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

using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Uno.UI.DevServer.Cli.Helpers;
using Uno.UI.DevServer.Cli.Mcp;

namespace Uno.UI.DevServer.Cli;

internal class CliManager
{
	private readonly IServiceProvider _services;
	private readonly UnoToolsLocator _unoToolsLocator;
	private readonly IWorkspaceResolver _workspaceResolver;
	private readonly ILogger<CliManager> _logger;
	private static readonly JsonSerializerOptions _discoJsonOptions = new()
	{
		WriteIndented = true,
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase
	};

	public CliManager(IServiceProvider services, UnoToolsLocator unoToolsLocator, IWorkspaceResolver workspaceResolver)
	{
		_services = services;
		_unoToolsLocator = unoToolsLocator;
		_workspaceResolver = workspaceResolver;
		_logger = _services.GetRequiredService<ILogger<CliManager>>();
	}

	public async Task<int> RunAsync(string[] originalArgs)
	{
		try
		{
			var solutionDirParseResult = ExtractSolutionDirectory(originalArgs);
			// --solution-dir is applied uniformly so automation and CI environments can run any
			// command (start, stop, list, login, MCP) against a target solution even when the
			// current working directory differs from the solution root.
			if (!solutionDirParseResult.Success)
			{
				return 1;
			}

			originalArgs = solutionDirParseResult.FilteredArgs;
			var requestedWorkingDirectory = solutionDirParseResult.SolutionDirectory ?? Environment.CurrentDirectory;
			var hasExplicitWorkspaceOverride = solutionDirParseResult.SolutionDirectory is not null;

			if (originalArgs.Contains("--mcp-app"))
			{
				var mcpWorkspaceResolution = await ResolveWorkspaceAsync(requestedWorkingDirectory, hasExplicitWorkspaceOverride);
				LogVersionBanner();
				return await RunMcpProxyAsync(
					originalArgs.Where(a => a != "--mcp-app").ToArray(),
					requestedWorkingDirectory,
					mcpWorkspaceResolution);
			}

			var isDisco = originalArgs is { Length: > 0 } &&
				string.Equals(originalArgs[0], "disco", StringComparison.OrdinalIgnoreCase);
			var discoJson = isDisco &&
				originalArgs.Any(a => string.Equals(a, "--json", StringComparison.OrdinalIgnoreCase));
			var discoAddInsOnly = isDisco &&
				originalArgs.Any(a => string.Equals(a, "--addins-only", StringComparison.OrdinalIgnoreCase));
			var isHealth = originalArgs is { Length: > 0 } &&
				string.Equals(originalArgs[0], "health", StringComparison.OrdinalIgnoreCase);
			var healthJson = isHealth &&
				originalArgs.Any(a => string.Equals(a, "--json", StringComparison.OrdinalIgnoreCase));
			var command = originalArgs.Length == 0 ? "start" : originalArgs[0];
			var requiresWorkspaceResolution = RequiresWorkspaceResolution(command);
			WorkspaceResolution? workspaceResolution = null;
			string? workingDirectory = null;

			if ((!isDisco || (!discoJson && !discoAddInsOnly)) && !(isHealth && healthJson))
			{
				ShowBanner();
			}

			if (requiresWorkspaceResolution)
			{
				workspaceResolution = await ResolveWorkspaceAsync(requestedWorkingDirectory, hasExplicitWorkspaceOverride);
				workingDirectory = workspaceResolution.EffectiveWorkspaceDirectory ?? requestedWorkingDirectory;
			}

			if (isDisco && discoAddInsOnly)
			{
				return await RunDiscoAddInsOnlyAsync(workingDirectory!, outputJson: discoJson);
			}

			if (isDisco && discoJson)
			{
				// Avoid banner/log noise when emitting JSON output
				return await RunDiscoAsync(workingDirectory!, workspaceResolution!, outputJson: true);
			}

			if (isDisco)
			{
				return await RunDiscoAsync(workingDirectory!, workspaceResolution!, outputJson: false);
			}

			if (isHealth)
			{
				return await RunHealthAsync(requestedWorkingDirectory, workspaceResolution!, healthJson);
			}

			if (originalArgs is { Length: > 0 } && string.Equals(originalArgs[0], "login", StringComparison.OrdinalIgnoreCase))
			{
				var loginWorkspace = (await ResolveWorkspaceAsync(requestedWorkingDirectory, hasExplicitWorkspaceOverride)).EffectiveWorkspaceDirectory ?? requestedWorkingDirectory;
				return await OpenSettings(originalArgs, loginWorkspace);
			}

			workingDirectory ??= requestedWorkingDirectory;
			var hostPath = await _unoToolsLocator.ResolveHostExecutableAsync(workingDirectory);

			if (hostPath is null)
			{
				return 1; // errors already logged
			}

			// Resolve add-ins via convention-based discovery for the start command
			string? resolvedAddIns = null;
			var isStart = originalArgs.Length == 0 ||
				string.Equals(originalArgs[0], "start", StringComparison.OrdinalIgnoreCase);

			if (isStart)
			{
				resolvedAddIns = ResolveAddInsForCommand(workingDirectory);
			}

			var requiresHostOutputPassthrough = originalArgs.Length > 0 && (
				string.Equals(originalArgs[0], "list", StringComparison.OrdinalIgnoreCase) ||
				string.Equals(originalArgs[0], "cleanup", StringComparison.OrdinalIgnoreCase)
			);

			var startInfo = BuildHostArgs(hostPath, originalArgs, workingDirectory, redirectOutput: true, addins: resolvedAddIns);

			var result = await DevServerProcessHelper.RunConsoleProcessAsync(startInfo, _logger, forwardOutputToConsole: requiresHostOutputPassthrough);
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
		LogVersionBanner();
	}

	private static bool RequiresWorkspaceResolution(string command)
		=> string.Equals(command, "start", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(command, "stop", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(command, "disco", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(command, "health", StringComparison.OrdinalIgnoreCase);

	private Task<WorkspaceResolution> ResolveWorkspaceAsync(string requestedWorkingDirectory, bool hasExplicitWorkspaceOverride)
		=> hasExplicitWorkspaceOverride
			? _workspaceResolver.ResolveExplicitWorkspaceAsync(requestedWorkingDirectory)
			: _workspaceResolver.ResolveAsync(requestedWorkingDirectory);

	internal void LogVersionBanner()
	{
		_logger.LogInformation(GetVersionBannerText());
	}

	internal static string GetVersionBannerText()
	{
		var version = AssemblyVersionHelper.GetAssemblyVersion(typeof(CliManager).Assembly);
		return string.IsNullOrWhiteSpace(version)
			? "Uno Platform DevServer CLI - Dev Version"
			: $"Uno Platform DevServer CLI - Version {version}";
	}

	private async Task<int> RunDiscoAddInsOnlyAsync(string workingDirectory, bool outputJson)
	{
		var addInsValue = ResolveAddInsForCommand(workingDirectory);
		if (addInsValue is null)
		{
			if (outputJson)
			{
				Console.WriteLine("[]");
			}
			return 1;
		}

		var paths = addInsValue.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
		if (outputJson)
		{
			var json = JsonSerializer.Serialize(paths, _discoJsonOptions);
			Console.WriteLine(json);
		}
		else
		{
			Console.WriteLine(addInsValue);
		}

		return 0;
	}

	/// <summary>
	/// Resolves add-in DLL paths via convention-based discovery.
	/// Returns:
	///   <c>null</c>  — discovery failed (caller should fall back to MSBuild evaluation),
	///   <c>""</c>    — discovery succeeded but found zero add-ins (skip MSBuild fallback),
	///   non-empty   — semicolon-separated DLL paths.
	/// </summary>
	private string? ResolveAddInsForCommand(string workingDirectory)
	{
		try
		{
			var discovery = _unoToolsLocator.DiscoverAsync(workingDirectory).GetAwaiter().GetResult();
			if (discovery.PackagesJsonPath is null)
			{
				_logger.LogDebug("No packages.json found, skipping convention-based add-in discovery");
				return null; // discovery not possible — let MSBuild evaluate
			}

			if (discovery.AddInDiscoveryFailed)
			{
				_logger.LogDebug("Add-in discovery failed during DiscoverAsync, falling back to MSBuild");
				return null;
			}

			var addIns = discovery.AddIns;
			if (addIns.Count == 0)
			{
				_logger.LogDebug("No add-ins resolved via convention-based discovery");
				return ""; // discovery succeeded, zero add-ins — skip MSBuild fallback
			}

			var paths = string.Join(";", addIns.Select(a => a.EntryPointDll).Distinct(StringComparer.OrdinalIgnoreCase));
			_logger.LogDebug("Resolved {Count} add-in(s) via convention-based discovery", addIns.Count);
			return paths;
		}
		catch (Exception ex)
		{
			_logger.LogWarning(ex, "Convention-based add-in discovery failed, MSBuild fallback will be used");
			return null;
		}
	}

	private async Task<int> RunDiscoAsync(string workingDirectory, WorkspaceResolution workspaceResolution, bool outputJson)
	{
		var info = await _unoToolsLocator.DiscoverAsync(workingDirectory, workspaceResolution);

		if (outputJson)
		{
			var json = JsonSerializer.Serialize(info, _discoJsonOptions);
			Console.WriteLine(json);
		}
		else
		{
			DiscoveryOutputFormatter.WritePlainText(info);
		}

		return info.Errors.Count > 0 ? 1 : 0;
	}

	private async Task<int> RunHealthAsync(string requestedWorkingDirectory, WorkspaceResolution workspaceResolution, bool outputJson)
	{
		var workingDirectory = workspaceResolution.EffectiveWorkspaceDirectory ?? requestedWorkingDirectory;
		var info = await _unoToolsLocator.DiscoverAsync(workingDirectory, workspaceResolution);
		var report = HealthReportFactory.Create(
			info,
			devServerStarted: false,
			upstreamConnected: false,
			toolCount: 0,
			connectionState: null,
			discoveredSolutions: info.CandidateSolutions.Count > 0 ? info.CandidateSolutions : null);

		if (outputJson)
		{
			Console.WriteLine(HealthReportFormatter.FormatJson(report));
		}
		else
		{
			Console.WriteLine(HealthReportFormatter.FormatPlainText(report));
		}

		return report.Status == HealthStatus.Healthy ? 0 : 1;
	}

	private async Task<int> OpenSettings(string[] originalArgs, string workingDirectory)
	{
		var studioExecutable = await _unoToolsLocator.ResolveSettingsExecutableAsync(workingDirectory);

		if (studioExecutable is null)
		{
			return 1; // errors already logged
		}

		var startInfo = DevServerProcessHelper.CreateDotnetProcessStartInfo(studioExecutable, originalArgs, workingDirectory, redirectOutput: true);

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

	private async Task<int> RunMcpProxyAsync(string[] args, string requestedWorkingDirectory, WorkspaceResolution workspaceResolution)
	{
		try
		{
			_logger.LogInformation("Starting MCP Mode");

			int requestedPort = 0;
			bool mcpWaitToolsList = false;
			bool forceRootsFallback = false;
			bool forceGenerateToolCache = false;
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
				else if (a == "--mcp-wait-tools-list")
				{
					mcpWaitToolsList = true;
					continue; // do not forward mcp-specific arguments to controller
				}
				else if (a == "--force-roots-fallback")
				{
					forceRootsFallback = true;
					continue; // do not forward mcp-specific arguments to controller
				}
				else if (a == "--force-generate-tool-cache")
				{
					forceGenerateToolCache = true;
					continue; // do not forward mcp-specific arguments to controller
				}
				forwardedArgs.Add(a);
			}

			var waitForTools = mcpWaitToolsList;
			return await _services.GetRequiredService<ProxyLifecycleManager>().RunAsync(
				requestedWorkingDirectory,
				workspaceResolution,
				requestedPort,
				forwardedArgs,
				waitForTools,
				forceRootsFallback,
				forceGenerateToolCache,
				workspaceResolution.EffectiveWorkspaceDirectory ?? requestedWorkingDirectory,
				CancellationToken.None);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "MCP stdio server error: {Message}", ex.Message);
			return 1;
		}
	}

	private (bool Success, string[] FilteredArgs, string? SolutionDirectory) ExtractSolutionDirectory(string[] args)
	{
		string? rawSolutionDirectory = null;
		var filteredArgs = new List<string>(args.Length);

		for (int i = 0; i < args.Length; i++)
		{
			var arg = args[i];
			if (arg == "--solution-dir")
			{
				if (i + 1 >= args.Length)
				{
					_logger.LogError("Missing value for --solution-dir");
					return (false, Array.Empty<string>(), null);
				}

				rawSolutionDirectory = args[i + 1];
				i++; // skip value
				continue;
			}

			filteredArgs.Add(arg);
		}

		string? normalizedSolutionDirectory = null;
		if (!string.IsNullOrWhiteSpace(rawSolutionDirectory))
		{
			try
			{
				normalizedSolutionDirectory = Path.GetFullPath(rawSolutionDirectory);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Invalid solution directory '{Directory}'", rawSolutionDirectory);
				return (false, Array.Empty<string>(), null);
			}
		}

		return (true, filteredArgs.ToArray(), normalizedSolutionDirectory);
	}

	private ProcessStartInfo BuildHostArgs(string hostPath, string[] originalArgs, string workingDirectory, bool redirectOutput = true, string? addins = null)
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

		if (addins is not null)
		{
			args.Add("--addins");
			args.Add(addins);
		}

		return DevServerProcessHelper.CreateDotnetProcessStartInfo(hostPath, args, workingDirectory, redirectOutput);
	}
}

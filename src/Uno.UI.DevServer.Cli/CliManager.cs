using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Uno.UI.DevServer.Cli.Helpers;
using Uno.UI.DevServer.Cli.Mcp;
using Uno.UI.DevServer.Cli.Mcp.Setup;

namespace Uno.UI.DevServer.Cli;

internal class CliManager
{
	private readonly IServiceProvider _services;
	private readonly UnoToolsLocator _unoToolsLocator;
	private readonly ILogger<CliManager> _logger;
	private static readonly JsonSerializerOptions _discoJsonOptions = new()
	{
		WriteIndented = true,
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase
	};

	public CliManager(IServiceProvider services, UnoToolsLocator unoToolsLocator)
	{
		_services = services;
		_unoToolsLocator = unoToolsLocator;
		_logger = _services.GetRequiredService<ILogger<CliManager>>();
	}

	public async Task<int> RunAsync(string[] originalArgs)
	{
		try
		{
			var (Success, FilteredArgs, SolutionDirectory) = ExtractSolutionDirectory(originalArgs);
			// --solution-dir is applied uniformly so automation and CI environments can run any
			// command (start, stop, list, login, MCP) against a target solution even when the
			// current working directory differs from the solution root.
			if (!Success)
			{
				return 1;
			}

			originalArgs = FilteredArgs;
			var workingDirectory = SolutionDirectory ?? Environment.CurrentDirectory;

			// Route "mcp" subcommand group
			if (originalArgs is { Length: > 0 } &&
				string.Equals(originalArgs[0], "mcp", StringComparison.OrdinalIgnoreCase))
			{
				return RunMcpSubcommand(originalArgs[1..], workingDirectory, SolutionDirectory);
			}

			if (originalArgs.Contains("--mcp-app"))
			{
				return await RunMcpProxyAsync(
					[.. originalArgs.Where(a => a != "--mcp-app")],
					workingDirectory,
					SolutionDirectory);
			}

			var isDisco = originalArgs is { Length: > 0 } &&
				string.Equals(originalArgs[0], "disco", StringComparison.OrdinalIgnoreCase);
			var discoJson = isDisco &&
				originalArgs.Any(a => string.Equals(a, "--json", StringComparison.OrdinalIgnoreCase));
			var discoAddInsOnly = isDisco &&
				originalArgs.Any(a => string.Equals(a, "--addins-only", StringComparison.OrdinalIgnoreCase));

			if (!isDisco || (!discoJson && !discoAddInsOnly))
			{
				ShowBanner();
			}

			if (isDisco && discoAddInsOnly)
			{
				return await RunDiscoAddInsOnlyAsync(workingDirectory, outputJson: discoJson);
			}

			if (isDisco && discoJson)
			{
				// Avoid banner/log noise when emitting JSON output
				return await RunDiscoAsync(workingDirectory, outputJson: true);
			}

			if (isDisco)
			{
				return await RunDiscoAsync(workingDirectory, outputJson: false);
			}

			if (originalArgs is { Length: > 0 } && string.Equals(originalArgs[0], "login", StringComparison.OrdinalIgnoreCase))
			{
				return await OpenSettings(originalArgs, workingDirectory);
			}

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

			var (ExitCode, StdOut, StdErr) = await DevServerProcessHelper.RunConsoleProcessAsync(startInfo, _logger, forwardOutputToConsole: requiresHostOutputPassthrough);
			return ExitCode;
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

	private async Task<int> RunDiscoAsync(string workingDirectory, bool outputJson)
	{
		var info = await _unoToolsLocator.DiscoverAsync(workingDirectory);

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

	private async Task<int> RunMcpProxyAsync(string[] args, string workingDirectory, string? solutionDirectory)
	{
		try
		{
			_logger.LogInformation("Starting MCP Mode");

			var requestedPort = 0;
			var mcpWaitToolsList = false;
			var forceRootsFallback = false;
			var forceGenerateToolCache = false;
			var forwardedArgs = new List<string>();

			for (var i = 0; i < args.Length; i++)
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

			var normalizedSolutionDirectory = solutionDirectory ?? (forceGenerateToolCache ? workingDirectory : null);

			var waitForTools = mcpWaitToolsList;
			return await _services.GetRequiredService<ProxyLifecycleManager>().RunAsync(
				workingDirectory,
				requestedPort,
				forwardedArgs,
				waitForTools,
				forceRootsFallback,
				forceGenerateToolCache,
				normalizedSolutionDirectory,
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

		for (var i = 0; i < args.Length; i++)
		{
			var arg = args[i];
			if (arg == "--solution-dir")
			{
				if (i + 1 >= args.Length)
				{
					_logger.LogError("Missing value for --solution-dir");
					return (false, [], null);
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
				return (false, [], null);
			}
		}

		return (true, filteredArgs.ToArray(), normalizedSolutionDirectory);
	}

	internal int RunMcpSubcommand(string[] args, string workingDirectory, string? solutionDirectory)
	{
		if (args.Length == 0)
		{
			_logger.LogError("Missing mcp subcommand. Use: mcp start|status|install|uninstall");
			return 2;
		}

		var subcommand = args[0].ToLowerInvariant();

		if (subcommand == "start")
		{
			// "mcp start" is an alias for --mcp-app
			return RunMcpProxyAsync(args[1..], workingDirectory, solutionDirectory).GetAwaiter().GetResult();
		}

		if (subcommand is not ("status" or "install" or "uninstall"))
		{
			_logger.LogError("Unknown mcp subcommand '{Subcommand}'. Use: start|status|install|uninstall", subcommand);
			return 2;
		}

		// Parse common setup options
		var parsed = ParseMcpSetupArgs(args[1..], subcommand);
		if (parsed is null)
		{
			return 2; // error already logged
		}

		// When no IDE is specified, require --all-ides to confirm multi-IDE operation
		if (subcommand is "install" or "uninstall" && parsed.Value.Ide is null && !parsed.Value.AllIdes)
		{
			_logger.LogError("Missing IDE argument. Usage: mcp {Subcommand} <ide>, or use --all-ides to target all detected IDEs", subcommand);
			return 2;
		}

		// Validate mutually exclusive variant flags
		if (parsed.Value.Channel is not null &&
			!string.Equals(parsed.Value.Channel, "stable", StringComparison.OrdinalIgnoreCase) &&
			!string.Equals(parsed.Value.Channel, "prerelease", StringComparison.OrdinalIgnoreCase))
		{
			_logger.LogError("Invalid value for --channel '{Channel}'. Valid values: stable, prerelease", parsed.Value.Channel);
			return 2;
		}

		if (parsed.Value.Channel is not null && parsed.Value.ToolVersion is not null)
		{
			_logger.LogError("--channel and --tool-version are mutually exclusive");
			return 2;
		}

		try
		{
			var defs = DefinitionsLoader.Load(
				_services.GetRequiredService<IFileSystem>(),
				parsed.Value.IdeDefinitionsPath,
				parsed.Value.ServerDefinitionsPath);

			// Validate IDE if specified
			if (parsed.Value.Ide is not null && !defs.Ides.ContainsKey(parsed.Value.Ide))
			{
				_logger.LogError("Unknown IDE '{Ide}'. Known IDEs: {KnownIdes}",
					parsed.Value.Ide, string.Join(", ", defs.Ides.Keys));
				return 1;
			}

			// Validate server filter
			if (parsed.Value.Servers is not null)
			{
				foreach (var s in parsed.Value.Servers)
				{
					if (!defs.Servers.ContainsKey(s))
					{
						_logger.LogError("Unknown server '{Server}'. Known servers: {KnownServers}",
							s, string.Join(", ", defs.Servers.Keys));
						return 2;
					}
				}
			}

			var workspace = parsed.Value.Workspace ?? workingDirectory;
			if (!TryValidateWorkspace(workspace, _services.GetRequiredService<IFileSystem>(), out var normalizedWorkspace))
			{
				return 2;
			}
			var toolVersion = ServerDefinitionResolver.GetToolVersion();
			var expectedVariant = ServerDefinitionResolver.ResolveExpectedVariant(
				toolVersion, parsed.Value.Channel, parsed.Value.ToolVersion);

			var orchestrator = _services.GetRequiredService<McpSetupOrchestrator>();

			return subcommand switch
			{
				"status" => RunMcpStatus(orchestrator, defs, normalizedWorkspace, parsed.Value, expectedVariant, toolVersion),
				"install" when parsed.Value.Ide is not null => RunMcpInstall(orchestrator, defs, normalizedWorkspace, parsed.Value, expectedVariant, toolVersion),
				"install" => RunMcpInstallAllDetected(orchestrator, defs, normalizedWorkspace, parsed.Value, expectedVariant, toolVersion),
				"uninstall" when parsed.Value.Ide is not null => RunMcpUninstall(orchestrator, defs, normalizedWorkspace, parsed.Value),
				"uninstall" => RunMcpUninstallAllDetected(orchestrator, defs, normalizedWorkspace, parsed.Value),
				_ => 2,
			};
		}
		catch (FileNotFoundException ex)
		{
			_logger.LogError(ex, "Definitions file not found: {Message}", ex.Message);
			return 1;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "MCP setup error: {Message}", ex.Message);
			return 1;
		}
	}

	private static int RunMcpStatus(
		McpSetupOrchestrator orchestrator, Definitions defs, string workspace,
		McpSetupParsedArgs parsed, string expectedVariant, string toolVersion)
	{
		var result = orchestrator.Status(defs, workspace, parsed.Ide, expectedVariant, toolVersion);
		if (parsed.JsonOutput)
		{
			Console.WriteLine(JsonSerializer.Serialize(result, McpSetupJson.OutputOptions));
		}
		else
		{
			McpSetupOutputFormatter.WriteStatus(result, workspace);
		}

		return 0;
	}

	private static int RunMcpInstall(
		McpSetupOrchestrator orchestrator, Definitions defs, string workspace,
		McpSetupParsedArgs parsed, string expectedVariant, string toolVersion)
	{
		var result = orchestrator.Install(defs, workspace, parsed.Ide!, expectedVariant, toolVersion, parsed.Servers, parsed.DryRun);
		if (parsed.JsonOutput)
		{
			Console.WriteLine(JsonSerializer.Serialize(result, McpSetupJson.OutputOptions));
		}
		else
		{
			McpSetupOutputFormatter.WriteInstall(result, workspace, parsed.DryRun);
		}

		return DetermineMcpSetupExitCode(result);
	}

	private static int RunMcpUninstall(
		McpSetupOrchestrator orchestrator, Definitions defs, string workspace, McpSetupParsedArgs parsed)
	{
		var result = orchestrator.Uninstall(defs, workspace, parsed.Ide!, parsed.Servers, parsed.AllScopes, parsed.DryRun);
		if (parsed.JsonOutput)
		{
			Console.WriteLine(JsonSerializer.Serialize(result, McpSetupJson.OutputOptions));
		}
		else
		{
			McpSetupOutputFormatter.WriteUninstall(result, workspace, parsed.DryRun);
		}

		return DetermineMcpSetupExitCode(result);
	}

	private int RunMcpInstallAllDetected(
		McpSetupOrchestrator orchestrator, Definitions defs, string workspace,
		McpSetupParsedArgs parsed, string expectedVariant, string toolVersion)
	{
		var detectedIdes = GetDetectedIdes(orchestrator, defs, workspace, expectedVariant, toolVersion);
		if (detectedIdes.Count == 0)
		{
			_logger.LogError("No IDEs detected. Specify an IDE explicitly: mcp install <ide>");
			return 1;
		}

		var allOperations = new List<OperationEntry>();
		foreach (var ide in detectedIdes)
		{
			var result = orchestrator.Install(defs, workspace, ide, expectedVariant, toolVersion, parsed.Servers, parsed.DryRun);
			allOperations.AddRange(result.Operations);
		}

		var combined = new OperationResponse(McpSetupProtocol.Version, allOperations);
		if (parsed.JsonOutput)
		{
			Console.WriteLine(JsonSerializer.Serialize(combined, McpSetupJson.OutputOptions));
		}
		else
		{
			McpSetupOutputFormatter.WriteInstall(combined, workspace, parsed.DryRun);
		}

		return DetermineMcpSetupExitCode(combined);
	}

	private int RunMcpUninstallAllDetected(
		McpSetupOrchestrator orchestrator, Definitions defs, string workspace, McpSetupParsedArgs parsed)
	{
		var detectedIdes = GetDetectedIdes(orchestrator, defs, workspace, "stable", "0.0.0");
		if (detectedIdes.Count == 0)
		{
			_logger.LogError("No IDEs detected. Specify an IDE explicitly: mcp uninstall <ide>");
			return 1;
		}

		var allOperations = new List<OperationEntry>();
		foreach (var ide in detectedIdes)
		{
			var result = orchestrator.Uninstall(defs, workspace, ide, parsed.Servers, parsed.AllScopes, parsed.DryRun);
			allOperations.AddRange(result.Operations);
		}

		var combined = new OperationResponse(McpSetupProtocol.Version, allOperations);
		if (parsed.JsonOutput)
		{
			Console.WriteLine(JsonSerializer.Serialize(combined, McpSetupJson.OutputOptions));
		}
		else
		{
			McpSetupOutputFormatter.WriteUninstall(combined, workspace, parsed.DryRun);
		}

		return DetermineMcpSetupExitCode(combined);
	}

	private static IReadOnlyList<string> GetDetectedIdes(
		McpSetupOrchestrator orchestrator, Definitions defs, string workspace,
		string expectedVariant, string toolVersion)
	{
		var status = orchestrator.Status(defs, workspace, null, expectedVariant, toolVersion);
		return status.DetectedIdes;
	}

	private bool TryValidateWorkspace(string workspace, IFileSystem fs, out string normalizedWorkspace)
	{
		normalizedWorkspace = workspace;
		try
		{
			normalizedWorkspace = Path.GetFullPath(workspace);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Invalid workspace path '{Workspace}'", workspace);
			return false;
		}

		if (!fs.DirectoryExists(normalizedWorkspace))
		{
			_logger.LogError("Workspace directory does not exist: {Workspace}", normalizedWorkspace);
			return false;
		}

		if (IsFileSystemRoot(normalizedWorkspace))
		{
			_logger.LogError("Workspace cannot be a filesystem root: {Workspace}", normalizedWorkspace);
			return false;
		}

		return true;
	}

	private static bool IsFileSystemRoot(string path)
	{
		var root = Path.GetPathRoot(path);
		if (string.IsNullOrEmpty(root))
		{
			return false;
		}

		var trimChars = new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };
		return string.Equals(
			path.TrimEnd(trimChars),
			root.TrimEnd(trimChars),
			OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
	}

	internal static int DetermineMcpSetupExitCode(OperationResponse result) =>
		result.Operations.Count > 0
		&& result.Operations.All(static op => op.Action is "error" or "not_found")
			? 1
			: 0;

	internal record struct McpSetupParsedArgs(
		string? Ide,
		string? Workspace,
		string? Channel,
		string? ToolVersion,
		List<string>? Servers,
		bool JsonOutput,
		bool AllScopes,
		bool AllIdes,
		bool DryRun,
		string? IdeDefinitionsPath,
		string? ServerDefinitionsPath);

	internal McpSetupParsedArgs? ParseMcpSetupArgs(string[] args, string subcommand)
	{
		string? ide = null;
		string? workspace = null;
		string? channel = null;
		string? toolVersion = null;
		List<string>? servers = null;
		var jsonOutput = false;
		var allScopes = false;
		var dryRun = false;
		var allIdes = false;
		string? ideDefinitionsPath = null;
		string? serverDefinitionsPath = null;

		for (var i = 0; i < args.Length; i++)
		{
			var a = args[i];
			switch (a)
			{
				case "--workspace":
					if (i + 1 >= args.Length) { _logger.LogError("Missing value for --workspace"); return null; }
					workspace = args[++i];
					break;
				case "--channel":
					if (i + 1 >= args.Length) { _logger.LogError("Missing value for --channel"); return null; }
					channel = args[++i];
					break;
				case "--tool-version":
					if (i + 1 >= args.Length) { _logger.LogError("Missing value for --tool-version"); return null; }
					toolVersion = args[++i];
					break;
				case "--servers":
					if (i + 1 >= args.Length) { _logger.LogError("Missing value for --servers"); return null; }
					var parsedServers = args[++i].Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
					if (parsedServers.Length == 0)
					{
						_logger.LogError("Empty value for --servers");
						return null;
					}

					servers = [.. parsedServers];
					break;
				case "--json":
					jsonOutput = true;
					break;
				case "--all-scopes":
					allScopes = true;
					break;
				case "--dry-run":
					dryRun = true;
					break;
				case "--all-ides":
					allIdes = true;
					break;
				case "--ide-definitions":
					if (i + 1 >= args.Length) { _logger.LogError("Missing value for --ide-definitions"); return null; }
					ideDefinitionsPath = args[++i];
					break;
				case "--server-definitions":
					if (i + 1 >= args.Length) { _logger.LogError("Missing value for --server-definitions"); return null; }
					serverDefinitionsPath = args[++i];
					break;
				default:
					if (a.StartsWith('-'))
					{
						_logger.LogError("Unknown option '{Option}' for mcp {Subcommand}", a, subcommand);
						return null;
					}
					// Positional: IDE identifier
					if (ide is null)
					{
						ide = a;
					}
					else
					{
						_logger.LogError("Unexpected argument '{Arg}' for mcp {Subcommand}", a, subcommand);
						return null;
					}
					break;
			}
		}

		return new McpSetupParsedArgs(ide, workspace, channel, toolVersion,
			servers, jsonOutput, allScopes, allIdes, dryRun, ideDefinitionsPath, serverDefinitionsPath);
	}

	private ProcessStartInfo BuildHostArgs(string hostPath, string[] originalArgs, string workingDirectory, bool redirectOutput = true, string? addins = null)
	{
		var args = new List<string> { "--command" };
		if (originalArgs.Length > 0)
		{
			args.Add(originalArgs[0]);
			for (var i = 1; i < originalArgs.Length; i++)
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

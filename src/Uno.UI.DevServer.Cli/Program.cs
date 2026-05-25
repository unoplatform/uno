using System.Diagnostics;
using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Uno.UI.DevServer.Cli.Helpers;
using Uno.UI.DevServer.Cli.Logging;
using Uno.UI.DevServer.Cli.Mcp;
using Uno.UI.DevServer.Cli.Mcp.Setup;

namespace Uno.UI.DevServer.Cli;

internal class Program
{
	private static async Task<int> Main(string[] args)
	{
		if (args.Length == 0 || args.Contains("--help") || args.Contains("-h"))
		{
			Console.WriteLine("Usage: dnx -y uno.devserver [options] [command]");
			Console.WriteLine();
			Console.WriteLine("Global options:");
			WriteOption("--help, -h", "Show this help message and exit");
			WriteOption("--log-level, -l <level>", "Set the log level (Trace, Debug, Information, Warning, Error, Critical, None). Default is Information.");
			WriteOption("--file-log, -fl <path>", "Enable file logging to the provided file path (supports {Date} token). Required path argument.");
			WriteOption("--solution-dir <path>", "Explicit solution root when priming tools without client-provided roots");
			Console.WriteLine();
			Console.WriteLine("Disco options:");
			WriteOption("--json", "Emit JSON output");
			WriteOption("--addins-only", "Output only resolved add-in paths (semicolon-separated, or JSON array with --json)");
			Console.WriteLine();
			Console.WriteLine("MCP options:");
			WriteOption("--mcp-app", "Start in App MCP STDIO mode");
			WriteOption("--mcp-wait-tools-list", "Wait for upstream server tools before responding to list_tools (MCP mode only)");
			WriteOption("--force-roots-fallback", "This mode can be used when the MCP client does not support the roots feature");
			WriteOption("--force-generate-tool-cache", "Deprecated (no-op). Kept for backward compatibility.");
			Console.WriteLine();
			Console.WriteLine("Commands:");
			WriteCommand("start", "Start the DevServer for the current folder");
			WriteCommand("stop", "Stop the DevServer for the current folder");
			WriteCommand("list", "List active DevServer instances");
			WriteCommand("disco", "Discover environment and SDK details");
			WriteCommand("health", "Report Uno DevServer health for the current workspace");
			Console.WriteLine();
			Console.WriteLine("MCP setup commands:");
			WriteCommand("mcp serve", "Start the MCP STDIO proxy (alias: --mcp-app)");
			WriteCommand("mcp status", "Report installation state of MCP servers across clients");
			WriteCommand("mcp install", "Register MCP servers in client config files");
			WriteCommand("mcp uninstall", "Remove MCP servers from client config files");
			Console.WriteLine();
			Console.WriteLine("MCP setup options:");
			WriteOption("<client>", "Target client (positional, or use --all-ides): copilot-vscode, copilot-vs, copilot-cli, cursor, windsurf, kiro, gemini-antigravity, gemini-cli, junie-rider, claude-code, claude-desktop, codex-cli, jetbrains-air, opencode, unknown");
			WriteOption("--workspace <path>", "Workspace root (default: current directory)");
			WriteOption("--channel <stable|prerelease>", "Select the Uno MCP definition channel");
			WriteOption("--tool-version <ver>", "Pin the Uno MCP tool definition to a specific version");
			WriteOption("--servers <list>", "Comma-separated server names (default: all)");
			WriteOption("--all-scopes", "For mcp uninstall, remove matching registrations from every configured scope");
			WriteOption("--all-ides", "For mcp install/uninstall without <client>, target all detected clients");
			WriteOption("--dry-run", "Show what would be done without modifying any files");
			WriteOption("--json", "Emit JSON output");
			WriteOption("--ide-definitions <path>", "Override embedded MCP client profiles");
			WriteOption("--server-definitions <path>", "Override embedded server definitions");
			Console.WriteLine();
			Console.WriteLine("Note: MCP setup options choose the expected Uno MCP definition.");
			Console.WriteLine("      Any dnx --prerelease or dnx --version written to config files is derived output.");
			Console.WriteLine();
			return 0;
		}

		// Parse common options up-front
		var logLevel = GetLogLevelFromArgs(args);
		var fileLogPath = GetFileLogPathFromArgs(args, out var fileLogPathError);
		if (fileLogPathError is not null)
		{
			Console.Error.WriteLine(fileLogPathError);
			return 1;
		}

		// In MCP mode, stdout is reserved for MCP protocol messages (JSON-RPC),
		// so all diagnostic logging must go to stderr.
		// For non-MCP commands, route errors to stderr and keep stdout clean for
		// structured output (e.g. `disco --json`, `health --json`). The default
		// LogToStandardErrorThreshold is LogLevel.None, which would send everything
		// (including errors) to stdout and corrupt JSON output.
		var isMcpMode = IsMcpMode(args);

		var services = new ServiceCollection();
		services.AddLogging(builder =>
		{
			builder.AddConsole(options =>
			{
				options.FormatterName = "clean";
				options.LogToStandardErrorThreshold = isMcpMode ? LogLevel.Trace : LogLevel.Error;
			});
			builder.AddConsoleFormatter<CleanConsoleFormatter, ConsoleFormatterOptions>();

			builder.SetMinimumLevel(logLevel);

			if (fileLogPath is not null)
			{
				try
				{
					var dir = Path.GetDirectoryName(fileLogPath);
					if (!string.IsNullOrEmpty(dir))
					{
						Directory.CreateDirectory(dir);
						builder.AddFile(fileLogPath, minimumLevel: logLevel);
					}
				}
				catch (Exception e)
				{
					Console.Error.WriteLine($"Failed to initialize file logging for '{fileLogPath}': {e.Message}");
				}
			}
		});
		services.AddSingleton<CliManager>();
		services.AddSingleton<UnoToolsLocator>();
		services.AddSingleton<ISolutionFileFinder, FileSystemSolutionFileFinder>();
		services.AddSingleton<WorkspaceResolver>();
		services.AddSingleton<IWorkspaceResolver>(sp => sp.GetRequiredService<WorkspaceResolver>());
		services.AddSingleton(sp =>
		new ManifestAddInResolver(
			sp.GetRequiredService<ILogger<ManifestAddInResolver>>(),
			AssemblyVersionHelper.GetAssemblyVersion(typeof(Program).Assembly)));
		services.AddSingleton<TargetsAddInResolver>();
		services.AddSingleton<DevServerMonitor>();
		services.AddSingleton<McpUpstreamClient>();
		services.AddSingleton<ToolListManager>();
		services.AddSingleton<HealthService>();
		services.AddSingleton<McpStdioServer>();
		services.AddSingleton<ProxyLifecycleManager>();
		services.AddSingleton<IFileSystem, FileSystem>();
		services.AddSingleton<CliCommandRunner>();
		services.AddSingleton<McpSetupOrchestrator>();

		using var sp = services.BuildServiceProvider();
		var manager = sp.GetRequiredService<CliManager>();
		return await manager.RunAsync(args);
	}

	private static LogLevel GetLogLevelFromArgs(string[] args)
	{
		for (int i = 0; i < args.Length - 1; i++)
		{
			if (args[i] == "--log-level" || args[i] == "-l")
			{
				if (Enum.TryParse<LogLevel>(args[i + 1], true, out var logLevel))
				{
					return logLevel;
				}
			}
		}

		return LogLevel.Information;
	}

	private static string? GetFileLogPathFromArgs(string[] args, out string? error)
	{
		error = null;
		for (int i = 0; i < args.Length; i++)
		{
			if (args[i] == "-fl" || args[i] == "--file-log")
			{
				if (i == args.Length - 1)
				{
					error = "Missing file path after -fl option.";
					return null;
				}
				var path = args[i + 1];
				if (string.IsNullOrWhiteSpace(path))
				{
					error = "File log path cannot be empty.";
					return null;
				}
				return path;
			}
		}
		return null;
	}

	internal static bool IsMcpMode(string[] args)
	{
		if (args.Contains("--mcp-app"))
		{
			return true;
		}

		for (int i = 0; i < args.Length - 1; i++)
		{
			if (string.Equals(args[i], "mcp", StringComparison.OrdinalIgnoreCase)
				&& string.Equals(args[i + 1], "serve", StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
		}

		return false;
	}

	private static void WriteOption(string option, string description)
	{
		const int optionWidth = 32;
		Console.WriteLine($"  {option,-optionWidth} {description}");
	}

	private static void WriteCommand(string command, string description)
	{
		const int commandWidth = 16;
		Console.WriteLine($"  {command,-commandWidth} {description}");
	}
}

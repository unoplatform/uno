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
			WriteOption("--force-generate-tool-cache", "Force tool discovery and persist the cache immediately (MCP mode only)");
			Console.WriteLine();
			Console.WriteLine("Commands:");
			WriteCommand("start", "Start the DevServer for the current folder");
			WriteCommand("stop", "Stop the DevServer for the current folder");
			WriteCommand("list", "List active DevServer instances");
			WriteCommand("disco", "Discover environment and SDK details");
			Console.WriteLine();
			Console.WriteLine("MCP setup commands:");
			WriteCommand("mcp start", "Start the MCP STDIO proxy (alias: --mcp-app)");
			WriteCommand("mcp status", "Report installation state of MCP servers across IDEs");
			WriteCommand("mcp install", "Register MCP servers in IDE config files");
			WriteCommand("mcp uninstall", "Remove MCP servers from IDE config files");
			Console.WriteLine();
			Console.WriteLine("MCP setup options:");
			WriteOption("<ide>", "Target IDE (positional): vscode, cursor, windsurf, kiro, trae, antigravity, rider, claude-code, opencode, aider");
			WriteOption("--workspace <path>", "Workspace root (default: current directory)");
			WriteOption("--release", "Use stable variant");
			WriteOption("--prerelease", "Use prerelease variant");
			WriteOption("--version <ver>", "Pin to specific version");
			WriteOption("--servers <list>", "Comma-separated server names (default: all)");
			WriteOption("--json", "Emit JSON output");
			WriteOption("--ide-definitions <path>", "Override embedded IDE profiles");
			WriteOption("--server-definitions <path>", "Override embedded server definitions");
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

		var services = new ServiceCollection();
		services.AddLogging(builder =>
		{
			builder.AddConsole(options =>
			{
				options.FormatterName = "clean";
				options.LogToStandardErrorThreshold = LogLevel.Trace;
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
		services.AddSingleton(sp =>
		new ManifestAddInResolver(
			sp.GetRequiredService<ILogger<ManifestAddInResolver>>(),
			McpStdioServer.GetAssemblyVersion()));
		services.AddSingleton<TargetsAddInResolver>();
		services.AddSingleton<DevServerMonitor>();
		services.AddSingleton<McpUpstreamClient>();
		services.AddSingleton<ToolListManager>();
		services.AddSingleton<HealthService>();
		services.AddSingleton<McpStdioServer>();
		services.AddSingleton<ProxyLifecycleManager>();
		services.AddSingleton<IFileSystem, FileSystem>();
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

	private static void WriteOption(string option, string description)
	{
		const int optionWidth = 32;
		Console.WriteLine($"  {option,-optionWidth} {description}");
	}

	private static void WriteCommand(string command, string description)
	{
		const int commandWidth = 10;
		Console.WriteLine($"  {command,-commandWidth} {description}");
	}
}

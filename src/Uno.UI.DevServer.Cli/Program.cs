using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Uno.UI.DevServer.Cli.Helpers;
using Uno.UI.DevServer.Cli.Logging;
using Uno.UI.DevServer.Cli.Mcp;
using System.Globalization;

namespace Uno.UI.DevServer.Cli;

internal class Program
{
	private static async Task<int> Main(string[] args)
	{
		if (args.Length == 0 || args.Contains("--help") || args.Contains("-h"))
		{
			Console.WriteLine("Usage: dnx -y uno.devserver [options] [command]");
			Console.WriteLine();
			Console.WriteLine("Options:");
<<<<<<< HEAD
			Console.WriteLine("  --help, -h               Show this help message and exit");
			Console.WriteLine("  --log-level, -l <level>  Set the log level (Trace, Debug, Information, Warning, Error, Critical, None). Default is Information.");
			Console.WriteLine("  --file-log, -fl <path>   Enable file logging to the provided file path (supports {Date} token). Required path argument.");
			Console.WriteLine("  --mcp-app                Start in App MCP in STDIO mode");
=======
			Console.WriteLine("  --help, -h                 Show this help message and exit");
			Console.WriteLine("  --log-level, -l <level>    Set the log level (Trace, Debug, Information, Warning, Error, Critical, None). Default is Information.");
			Console.WriteLine("  --file-log, -fl <path>     Enable file logging to the provided file path (supports {Date} token). Required path argument.");
			Console.WriteLine("  --mcp                      Start in MCP STDIO mode");
			Console.WriteLine("  --mcp-wait-tools-list      Wait for upstream server tools before responding to list_tools (MCP mode only)");
>>>>>>> 6480e23a65 (fix(mcp): Adjust for no tools_update notification on claude/codex)
			Console.WriteLine();
			Console.WriteLine("Commands:");
			Console.WriteLine("  start                      Start the DevServer for the current folder");
			Console.WriteLine("  stop                       Stop the DevServer for the current folder");
			Console.WriteLine("  list                       List active DevServer instances");
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
		services.AddSingleton<DevServerMonitor>();
		services.AddSingleton<McpProxy>();
		services.AddSingleton<McpClientProxy>();

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

#if DEBUG
		return LogLevel.Information;
#else
		return LogLevel.Warning;
#endif
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
}

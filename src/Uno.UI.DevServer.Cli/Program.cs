using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Uno.UI.DevServer.Cli.Logging;

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
			Console.WriteLine("  --help, -h               Show this help message and exit");
			Console.WriteLine("  --log-level, -l <level>  Set the log level (Trace, Debug, Information, Warning, Error, Critical, None). Default is Information.");
			Console.WriteLine("  --mcp                    Start in MCP STDIO mode");
			Console.WriteLine();
			Console.WriteLine("Commands:");
			Console.WriteLine("  start                    Start the DevServer for the current folder");
			Console.WriteLine("  stop                     Stop the DevServer for the current folder");
			Console.WriteLine("  list                     List active DevServer instances");
			Console.WriteLine();
			return 0;
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
			builder.SetMinimumLevel(GetLogLevelFromArgs(args));
		});
		services.AddSingleton<CliManager>();

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
}

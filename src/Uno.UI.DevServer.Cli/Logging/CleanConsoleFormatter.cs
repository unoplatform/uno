using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;
using System.IO;

namespace Uno.UI.DevServer.Cli.Logging;

/// <summary>
/// A simple console formatter that outputs only the log level and message without timestamps or category names
/// </summary>
internal sealed class CleanConsoleFormatter : ConsoleFormatter
{
	public CleanConsoleFormatter() : base("clean")
	{
	}

	public override void Write<TState>(in LogEntry<TState> logEntry, IExternalScopeProvider? scopeProvider, TextWriter textWriter)
	{
		var logLevel = logEntry.LogLevel;
		var message = logEntry.Formatter(logEntry.State, logEntry.Exception);

		if (string.IsNullOrEmpty(message))
		{
			return;
		}

		// Get the log level name
		var logLevelString = GetLogLevelString(logLevel);

		// Write just the log level and message
		textWriter.WriteLine($"{logLevelString}{message}");

		// Write exception details if present
		if (logEntry.Exception != null)
		{
			textWriter.WriteLine(logEntry.Exception.ToString());
		}
	}

	private static string GetLogLevelString(LogLevel logLevel) => logLevel switch
	{
		LogLevel.Trace => "trce: ",
		LogLevel.Debug => "dbug: ",
		LogLevel.Information => "",
		LogLevel.Warning => "warn: ",
		LogLevel.Error => "fail: ",
		LogLevel.Critical => "crit: ",
		_ => throw new ArgumentOutOfRangeException(nameof(logLevel))
	};
}

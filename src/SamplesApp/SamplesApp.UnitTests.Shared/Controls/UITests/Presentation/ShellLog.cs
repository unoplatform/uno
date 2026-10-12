#nullable enable

using System;

#if HAS_UNO
using Uno.Foundation.Logging;
#else
using Microsoft.Extensions.Logging;
using Uno.Extensions;
using Uno.Logging;
#endif

namespace SampleControl.Presentation;

/// <summary>Failure logging for the shell.</summary>
internal static class ShellLog
{
#if HAS_UNO
	private static readonly Logger _log = Uno.Foundation.Logging.LogExtensionPoint.Log(typeof(ShellLog));
#else
	private static readonly ILogger _log = Uno.Extensions.LogExtensionPoint.Log(typeof(ShellLog));
#endif

	public static void Warn(string message, Exception? exception = null)
	{
		if (_log.IsEnabled(LogLevel.Warning))
		{
			_log.Warn(Format(message, exception));
		}
	}

	public static void Error(string message, Exception? exception = null)
	{
		if (_log.IsEnabled(LogLevel.Error))
		{
			_log.Error(Format(message, exception));
		}
	}

	// Uno's Warn(string, Exception) drops the exception, so it goes into the message.
	private static string Format(string message, Exception? exception)
		=> exception is null ? message : $"{message} {exception}";
}

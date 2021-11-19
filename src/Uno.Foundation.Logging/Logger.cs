#nullable enable

using System;
using System.Collections.Generic;
using System.Text;

namespace Uno.Foundation.Logging
{
	internal class Logger
	{
		private readonly IExternalLogger? _externalLogger;
		private readonly LogLevel _level;

		public Logger(IExternalLogger? externalLogger)
		{
			_externalLogger = externalLogger;
			_level = externalLogger?.LogLevel ?? LogLevel.None;
		}

		public void Log(LogLevel level, string? message, Exception? exception = null)
		{
			if (IsEnabled(level))
			{
				_externalLogger?.Log(level, message, exception);
			}
		}

		public void Log(LogLevel level, IFormattable formattable, Exception? exception = null)
		{
			if (IsEnabled(level))
			{
				_externalLogger?.Log(level, formattable.ToString(), exception);
			}
		}

		public void Trace(string message) => Log(LogLevel.Trace, message);
		public void Trace(IFormattable formattable) => Log(LogLevel.Trace, formattable.ToString());

		public void LogDebug(string message) => Log(LogLevel.Debug, message);
		public void LogDebug(string message, params object?[] items) => Log(LogLevel.Debug, string.Format(message, items));
		public void DebugFormat(string message) => Log(LogLevel.Debug, message);
		public void DebugFormat(string message, params object?[] items) => Log(LogLevel.Debug, string.Format(message, items));
		public void Debug(string message) => Log(LogLevel.Debug, message);
		public void Debug(IFormattable formattable) => Log(LogLevel.Debug, formattable.ToString());

		public void LogWarn(string message) => Log(LogLevel.Warning, message);
		public void LogWarning(string message) => Log(LogLevel.Warning, message);
		public void LogWarning(string message, Exception ex) => Log(LogLevel.Warning, message);
		public void Warn(string message) => Log(LogLevel.Warning, message);
		public void Warn(string message, Exception ex) => Log(LogLevel.Warning, message);
		public void Warn(IFormattable formattable) => Log(LogLevel.Warning, formattable.ToString());

		public void LogError(string message, params object?[] items) => Log(LogLevel.Error, string.Format(message, items));
		public void LogError(string message) => Log(LogLevel.Error, message);
		public void LogError(string message, Exception ex) => Log(LogLevel.Error, message, ex);
		public void Error(string message) => Log(LogLevel.Error, message);
		public void Error(string message, Exception ex) => Log(LogLevel.Error, message, ex);
		public void ErrorFormat(string message, Exception ex) => Log(LogLevel.Error, message, ex);
		public void ErrorFormat(string message, params object[] items) => Log(LogLevel.Error, string.Format(message, items));
		public void Error(IFormattable formattable) => Log(LogLevel.Error, formattable.ToString());
		public void Error(IFormattable formattable, Exception ex) => Log(LogLevel.Error, formattable.ToString(), ex);

		public void Critical(string message) => Log(LogLevel.Critical, message);
		public void Critical(IFormattable formattable) => Log(LogLevel.Critical, formattable.ToString());

		public void LogInfo(string message) => Log(LogLevel.Information, message);
		public void LogInfo(string message, Exception ex) => Log(LogLevel.Information, message, ex);
		public void InfoFormat(string message, params object?[] items) => Log(LogLevel.Information, string.Format(message, items));
		public void Info(string message) => Log(LogLevel.Information, message);
		public void Info(string message, Exception ex) => Log(LogLevel.Information, message, ex);
		public void Info(IFormattable formattable) => Log(LogLevel.Information, formattable.ToString());

		public bool IsEnabled(LogLevel logLevel) => _level <= logLevel;
		public bool IsTraceEnabled(LogLevel logLevel) => IsEnabled(LogLevel.Trace);
		public bool IsDebugEnabled(LogLevel logLevel) => IsEnabled(LogLevel.Debug);
		public bool IsErrorEnabled(LogLevel logLevel) => IsEnabled(LogLevel.Error);
		public bool IsInformationEnabled(LogLevel logLevel) => IsEnabled(LogLevel.Information);
		public bool IsWarningEnabled(LogLevel logLevel) => IsEnabled(LogLevel.Warning);
		public bool IsCriticalEnabled(LogLevel logLevel) => IsEnabled(LogLevel.Critical);
	}
}

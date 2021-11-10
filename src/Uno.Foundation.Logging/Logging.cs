#nullable enable

using System;
using System.Collections.Generic;
using System.Text;

namespace Uno.Foundation.Logging
{
	public enum LogLevel
	{
		Trace,
		Debug,
		Information,
		Warning,
		Error,
		Critical,
		None
	}

	internal class Logger
	{
		private IExternalLogger? _externalLogger;
		private LogLevel _level;

		public Logger(IExternalLogger? externalLogger)
		{
			_externalLogger = externalLogger;
			_level = externalLogger?.LogLevel ?? LogLevel.None;
		}

		public void Log(LogLevel level, string message) { }
		public void Log(LogLevel level, IFormattable formattable) { }

		public void Trace(string message) { }
		public void Trace(IFormattable formattable) { }

		public void LogDebug(string message, params object?[] items) { }
		public void DebugFormat(string message) { }
		public void DebugFormat(string message, params object?[] items) { }
		public void Debug(string message) { }
		public void Debug(IFormattable formattable) { }

		public void LogWarn(string message) { }
		public void LogWarning(string message) { }
		public void LogWarning(string message, Exception ex) { }
		public void Warn(string message) { }
		public void Warn(string message, Exception ex) { }
		public void Warn(IFormattable formattable) { }

		public void LogError(string messag, params object?[] items) { }
		public void LogError(string message) { }
		public void LogError(string message, Exception ex) { }
		public void Error(string message) { }
		public void Error(string message, Exception ex) { }
		public void ErrorFormat(string message, Exception ex) { }
		public void ErrorFormat(string message, params object[] items) { }
		public void Error(IFormattable formattable) { }
		public void Error(IFormattable formattable, Exception ex) { }

		public void Critical(string message) { }
		public void Critical(IFormattable formattable) { }

		public void LogInfo(string message) { }
		public void LogInfo(string message, Exception ex) { }
		public void InfoFormat(string message, params object?[] items) { }
		public void Info(string message) { }
		public void Info(string message, Exception ex) { }
		public void Info(IFormattable formattable) { }

		public bool IsEnabled(LogLevel logLevel) => true;
	}

	internal static class LogExtensionPoint
	{
		private static LoggerFactory _loggerFactory = new LoggerFactory();

		private static class Container<T>
		{
			internal static readonly Logger Logger = _loggerFactory.CreateLogger(typeof(T));
		}

		public static LoggerFactory Factory => _loggerFactory;

		/// <summary>
		/// Gets a <see cref="ILogger"/> for the specified type.
		/// </summary>
		/// <param name="forType"></param>
		/// <returns></returns>
		public static Logger Log(this Type forType)
			=> _loggerFactory.CreateLogger(forType);

		/// <summary>
		/// Gets a logger instance for the current types
		/// </summary>
		/// <typeparam name="T">The type for which to get the logger</typeparam>
		/// <param name="instance"></param>
		/// <returns>A logger for the type of the instance</returns>
		public static Logger Log<T>(this T instance)
			=> Container<T>.Logger;
	}

	internal interface IExternalLoggerFactory
	{
		IExternalLogger CreateLogger(string categoryName);
	}

	internal interface IExternalLogger
	{
		void Log(LogLevel logLevel, string message);
		LogLevel LogLevel { get; }
	}

	internal class LoggerFactory
	{
		public static IExternalLoggerFactory? ExternalLoggerFactory { get; set; }

		public LoggerFactory()
		{
		}

		internal Logger CreateLogger(Type type)
		{
			return CreateLogger(type.FullName ?? type.Name);
		}

		internal Logger CreateLogger(string name)
			=> ExternalLoggerFactory == null
			? new Logger(null)
			: new Logger(ExternalLoggerFactory.CreateLogger(name));
	}
}

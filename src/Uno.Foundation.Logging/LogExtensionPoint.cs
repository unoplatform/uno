#nullable enable

using System;

namespace Uno.Foundation.Logging
{
	internal static class LogExtensionPoint
	{
		private static LoggerFactory _loggerFactory = new LoggerFactory();

		public static LoggerFactory Factory => _loggerFactory;

		/// <summary>
		/// Gets a <see cref="Logger"/> for the specified type.
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
		{
			if (instance is Type t)
			{
				return _loggerFactory.CreateLogger(t);
			}
			else
			{
				return _loggerFactory.CreateLogger(typeof(T));
			}
		}

		private static Logger? Log<T>(this T instance, LogLevel level)
		{
			var logger = instance.Log();
			return logger.IsEnabled(level) ? logger : null;
		}

		public static Logger? LogError<T>(this T instance) => instance.Log(LogLevel.Error);
		public static Logger? LogWarn<T>(this T instance) => instance.Log(LogLevel.Warning);
		public static Logger? LogInfo<T>(this T instance) => instance.Log(LogLevel.Information);
		public static Logger? LogDebug<T>(this T instance) => instance.Log(LogLevel.Debug);
		public static Logger? LogTrace<T>(this T instance) => instance.Log(LogLevel.Trace);
	}
}

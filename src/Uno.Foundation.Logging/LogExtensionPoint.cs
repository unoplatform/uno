#nullable enable

using System;
using System.Runtime.CompilerServices;

namespace Uno.Foundation.Logging
{
	internal static class LogExtensionPoint
	{
		private static LoggerFactory _loggerFactory = new LoggerFactory();

		/// <summary>
		/// Per-type logger memoization. Weak-keyed: a <see cref="Type"/> key roots the
		/// LoaderAllocator of its AssemblyLoadContext, so a strongly keyed process-lifetime cache
		/// would keep a collectible context resident forever as soon as one of its types logged
		/// once. The cache is a pure memoization (evicted entries simply rebuild), which makes weak
		/// keys sufficient — no unload hook or ALC-scoped purge is needed.
		/// </summary>
		private static readonly ConditionalWeakTable<Type, Logger> _loggers = new();

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
			var type = instance as Type ?? typeof(T);
			return _loggers.GetValue(type, static t => _loggerFactory.CreateLogger(t));
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

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
		/// Memoizes the logger for one exact <typeparamref name="T"/>. Statics of a generic instantiation
		/// live in that type's own LoaderAllocator, so a collectible context is still unloadable.
		/// </summary>
		private static class Holder<T>
		{
			public static Logger? Logger;
			public static int Version = -1;
		}

		/// <summary>
		/// Drops every memoized logger. Called when the external factory changes so that types which
		/// resolved a logger beforehand pick up the new configuration.
		/// </summary>
		internal static void ResetLoggerCaches()
		{
			_loggers.Clear();
			_loggerFactory.ClearCache();
		}

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
			if (instance is Type type)
			{
				return _loggers.GetValue(type, static t => _loggerFactory.CreateLogger(t));
			}

			// Guard sites evaluate this before checking the level, so it runs on every property set and
			// every layout pass. Keyed on the static T exactly as the table lookup below is, which makes
			// the cached value equivalent; the version stamp re-resolves it if logging is reconfigured.
			var version = LoggerFactory.Version;
			if (Holder<T>.Version == version && Holder<T>.Logger is { } cached)
			{
				return cached;
			}

			var logger = _loggers.GetValue(typeof(T), static t => _loggerFactory.CreateLogger(t));
			Holder<T>.Logger = logger;
			Holder<T>.Version = version;
			return logger;
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

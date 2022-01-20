#nullable enable

using System;

namespace Uno.Foundation.Logging
{
	internal static class LogExtensionPoint
	{
		private static LoggerFactory _loggerFactory = new LoggerFactory();

		private static class Container<T>
		{
			internal static readonly Logger Logger = _loggerFactory.CreateLogger(typeof(T));
		}

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
			=> Container<T>.Logger;
	}
}

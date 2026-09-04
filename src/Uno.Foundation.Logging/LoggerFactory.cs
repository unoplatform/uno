#nullable enable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Uno.Foundation.Logging
{
	internal class LoggerFactory
	{
		private readonly ConcurrentDictionary<string, Logger> _loggers = new();
		private static Logger _nullLogger = new Logger(null);
		private static IExternalLoggerFactory? _externalLoggerFactory;

		/// <summary>
		/// Incremented whenever <see cref="ExternalLoggerFactory"/> changes, so callers that memoize a
		/// resolved <see cref="Logger"/> per type can detect that their cached value is stale.
		/// </summary>
		internal static int Version;

		/// <summary>
		/// The logger handed out while no external factory is registered. Its level is
		/// <see cref="LogLevel.None"/>, so it reports every level as disabled.
		/// </summary>
		internal static Logger NullLogger => _nullLogger;

		public static IExternalLoggerFactory? ExternalLoggerFactory
		{
			get => _externalLoggerFactory;
			set
			{
				_externalLoggerFactory = value;
				Version++;
				LogExtensionPoint.ResetLoggerCaches();
			}
		}

		internal void ClearCache() => _loggers.Clear();

		public LoggerFactory()
		{
		}

		internal Logger CreateLogger(Type type)
			=> CreateLogger(type.FullName ?? type.Name);

		internal Logger CreateLogger(string name)
		{
			var factory = _externalLoggerFactory;
			if (factory == null)
			{
				return _nullLogger;
			}

			// Append-only, and creating a provider logger twice under a race is harmless, so this needs no
			// gate. The previous lock serialised every typeof(X).Log() call in the process.
			return _loggers.GetOrAdd(name, static (n, f) => new Logger(f.CreateLogger(n)), factory);
		}
	}
}

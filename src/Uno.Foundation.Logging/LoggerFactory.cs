#nullable enable

using System;
using System.Collections.Generic;

namespace Uno.Foundation.Logging
{
	internal class LoggerFactory
	{
		private static object _gate = new();
		private Dictionary<string, Logger> _loggers = new Dictionary<string, Logger>();
		private static Logger _nullLogger = new Logger(null);

		public static IExternalLoggerFactory? ExternalLoggerFactory { get; set; }

		public LoggerFactory()
		{
		}

		internal Logger CreateLogger(Type type)
			=> CreateLogger(type.FullName ?? type.Name);

		internal Logger CreateLogger(string name)
		{
			if (ExternalLoggerFactory == null)
			{
				return _nullLogger;
			}

			lock (_gate)
			{
				if (!_loggers.TryGetValue(name, out var logger))
				{
					_loggers[name] = logger = new Logger(ExternalLoggerFactory.CreateLogger(name));
				}

				return logger;
			}
		}
	}
}

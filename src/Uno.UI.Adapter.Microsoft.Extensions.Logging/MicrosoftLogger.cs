#nullable enable

using Microsoft.Extensions.Logging;

namespace Uno.UI.Adapter.Microsoft.Extensions.Logging
{
	using System;
	using Uno.Foundation.Logging;

	class MicrosoftLogger : IExternalLogger, global::Microsoft.Extensions.Logging.ILogger
	{
		private global::Microsoft.Extensions.Logging.ILogger _logger;
		private static readonly global::Microsoft.Extensions.Logging.LogLevel[] _levelsList = new[] {
				global::Microsoft.Extensions.Logging.LogLevel.Trace,
				global::Microsoft.Extensions.Logging.LogLevel.Debug,
				global::Microsoft.Extensions.Logging.LogLevel.Information,
				global::Microsoft.Extensions.Logging.LogLevel.Warning,
				global::Microsoft.Extensions.Logging.LogLevel.Error,
				global::Microsoft.Extensions.Logging.LogLevel.Critical,
				global::Microsoft.Extensions.Logging.LogLevel.None,
			};

		public MicrosoftLogger(global::Microsoft.Extensions.Logging.ILogger logger)
		{
			_logger = logger;

			for (int i = 0; i < _levelsList.Length; i++)
			{
				if (_logger.IsEnabled(_levelsList[i]))
				{
					LogLevel = Convert(_levelsList[i]);
					break;
				}
			}
		}

		private LogLevel Convert(global::Microsoft.Extensions.Logging.LogLevel logLevel)
			=> logLevel switch
			{
				global::Microsoft.Extensions.Logging.LogLevel.Trace => LogLevel.Trace,
				global::Microsoft.Extensions.Logging.LogLevel.Debug => LogLevel.Debug,
				global::Microsoft.Extensions.Logging.LogLevel.Information => LogLevel.Information,
				global::Microsoft.Extensions.Logging.LogLevel.Warning => LogLevel.Warning,
				global::Microsoft.Extensions.Logging.LogLevel.Error => LogLevel.Error,
				global::Microsoft.Extensions.Logging.LogLevel.Critical => LogLevel.Critical,
				global::Microsoft.Extensions.Logging.LogLevel.None => LogLevel.None,
				_ => LogLevel.None,
			};

		private global::Microsoft.Extensions.Logging.LogLevel Convert(LogLevel logLevel)
			=> logLevel switch
			{
				LogLevel.Trace => global::Microsoft.Extensions.Logging.LogLevel.Trace,
				LogLevel.Debug => global::Microsoft.Extensions.Logging.LogLevel.Debug,
				LogLevel.Information => global::Microsoft.Extensions.Logging.LogLevel.Information,
				LogLevel.Warning => global::Microsoft.Extensions.Logging.LogLevel.Warning,
				LogLevel.Error => global::Microsoft.Extensions.Logging.LogLevel.Error,
				LogLevel.Critical => global::Microsoft.Extensions.Logging.LogLevel.Critical,
				LogLevel.None => global::Microsoft.Extensions.Logging.LogLevel.None,
				_ => global::Microsoft.Extensions.Logging.LogLevel.None,
			};

		public LogLevel LogLevel { get; private set; }

		public void Log(LogLevel logLevel, string? message, Exception? exception = null)
			=> _logger.Log<object>(Convert(logLevel), 0, null!, exception, (_, __) => message);

		#region Implement global::Microsoft.Extensions.Logging.ILogger to allow down cast from apps (e.g. runtime tests)
		/// <inheritdoc />
		void global::Microsoft.Extensions.Logging.ILogger.Log<TState>(global::Microsoft.Extensions.Logging.LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
			=> _logger.Log(logLevel, eventId, state, exception, formatter);

		/// <inheritdoc />
		bool global::Microsoft.Extensions.Logging.ILogger.IsEnabled(global::Microsoft.Extensions.Logging.LogLevel logLevel)
			=> _logger.IsEnabled(logLevel);

		/// <inheritdoc />
		IDisposable global::Microsoft.Extensions.Logging.ILogger.BeginScope<TState>(TState state)
			=> _logger.BeginScope(state);
		#endregion
	}
}

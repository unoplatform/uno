using System;
using Microsoft.Build.Utilities;
using Uno.Logging;
using Microsoft.Build.Framework;

namespace Uno.UI.Tasks.Helpers
{
	public class TaskLogger : Microsoft.Extensions.Logging.ILogger
    {
		private readonly TaskLoggingHelper _taskLog;

		public TaskLogger(TaskLoggingHelper taskLog)
		{
			this._taskLog = taskLog;
		}

        public IDisposable BeginScope<TState>(TState state) => new DisposableAction(Actions.Null);

        public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel) => logLevel >= Microsoft.Extensions.Logging.LogLevel.Debug;

        public void Log<TState>(Microsoft.Extensions.Logging.LogLevel logLevel, Microsoft.Extensions.Logging.EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (formatter == null)
            {
                throw new ArgumentNullException(nameof(formatter));
            }

            var message = formatter(state, exception);

            if (string.IsNullOrEmpty(message))
            {
                return;
            }

			try
			{
				switch (logLevel)
				{
					case Microsoft.Extensions.Logging.LogLevel.Error:
						_taskLog?.LogError(message);
						break;

					case Microsoft.Extensions.Logging.LogLevel.Warning:
						_taskLog?.LogWarning(message);
						break;

					case Microsoft.Extensions.Logging.LogLevel.Information:
						_taskLog?.LogMessage(MessageImportance.Normal, message);
						break;

					case Microsoft.Extensions.Logging.LogLevel.Debug:
						_taskLog?.LogMessage(MessageImportance.Low, message);
						break;

					default:
						_taskLog?.LogMessage(message);
						break;
				}
			}
			catch(Exception e)
			{
				// Logging may have failed, use the console instead
				Console.WriteLine(message);
			}
        }
    }
}

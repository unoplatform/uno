using Microsoft.Build.Utilities;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using Uno.Logging;
using Uno.Extensions;

namespace Uno.UI.Tasks.Helpers
{
    public class TaskLoggerProvider : ILoggerProvider
    {
        private TaskLoggingHelper _taskLog;
        private List<TaskLogger> _loggers = new List<TaskLogger>();

        public TaskLoggerProvider(TaskLoggingHelper taskLog)
        {
            _taskLog = taskLog;
        }

        public ILogger CreateLogger(string categoryName)
        {
            var logger = new TaskLogger(_taskLog);

            return logger;
        }

        public void Dispose()
        {

        }

		public static IDisposable Register(TaskLoggingHelper taskLog)
		{
			LogExtensionPoint.AmbientLoggerFactory.AddProvider(new TaskLoggerProvider(taskLog));

			return new DisposableAction(CleanupProvider);
		}

		private static void CleanupProvider()
		{
			var property = typeof(LogExtensionPoint).GetProperty(nameof(LogExtensionPoint.AmbientLoggerFactory));

			if (property != null)
			{
				if (property.CanWrite)
				{
					LogExtensionPoint.AmbientLoggerFactory = null;
				}
				else
				{
					// We're in the context where an older version of Uno.Core (2.1 or earlier) of the binary
					// has been loaded by another msbuild task.
					// We still need to cleanup the logggers, so we're doing this through reflection
					// from the known backing field.

					var loggerFactoryField = typeof(LogExtensionPoint).GetField(
						"_loggerFactory",
						System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic);

					if(loggerFactoryField != null)
					{
						loggerFactoryField.SetValue(null, null);
					}
				}
			}
		}
    }
}

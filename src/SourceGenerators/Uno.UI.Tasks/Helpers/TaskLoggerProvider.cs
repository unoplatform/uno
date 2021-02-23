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

			return new DisposableAction(() => LogExtensionPoint.AmbientLoggerFactory = null);
		}
    }
}

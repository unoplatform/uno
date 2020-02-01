using Microsoft.Build.Utilities;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace Uno.UI.Tasks.Helpers
{
    public class TaskLoggerProvider : ILoggerProvider
    {
        private readonly TaskLoggingHelper _taskLog;
        private readonly List<TaskLogger> _loggers = new List<TaskLogger>();

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
    }
}

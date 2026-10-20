#nullable enable

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Foundation.Logging;

namespace Uno.UI.Tests.Foundation
{
	[TestClass]
	public class Given_LoggerWarningException
	{
		[TestMethod]
		public void When_Warning_Then_Exception_Reaches_Provider()
		{
			var provider = new RecordingLogProvider();
			var logger = new Logger(provider);

			var warnException = new InvalidOperationException("Warn failed");
			logger.Warn("failed", warnException);
			Assert.AreSame(warnException, provider.ReceivedException);

			var logWarningException = new InvalidOperationException("LogWarning failed");
			logger.LogWarning("failed", logWarningException);
			Assert.AreSame(logWarningException, provider.ReceivedException);
		}

		private sealed class RecordingLogProvider : IExternalLogger
		{
			public LogLevel LogLevel
			{
				get
				{
					return LogLevel.Trace;
				}
			}

			public Exception? ReceivedException { get; private set; }

			public void Log(LogLevel logLevel, string? message, Exception? exception = null)
			{
				ReceivedException = exception;
			}
		}
	}
}

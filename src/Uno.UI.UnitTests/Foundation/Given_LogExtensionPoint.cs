#nullable enable

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Foundation.Logging;

namespace Uno.UI.Tests.Foundation
{
	[TestClass]
	public class Given_LogExtensionPoint
	{
		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/24755")]
		public void When_Type_Logs_Before_Logging_Initialized_Then_Gets_Real_Logger_After()
		{
			var original = LoggerFactory.ExternalLoggerFactory;

			try
			{
				// State during host startup or static constructors, before LoggingAdapter.Initialize() runs.
				LoggerFactory.ExternalLoggerFactory = null;

				var before = LogExtensionPoint.Log(new EarlyLoggingProbe());
				Assert.IsFalse(before.IsEnabled(LogLevel.Critical), "Pre-condition: no logger factory is configured yet.");

				LoggerFactory.ExternalLoggerFactory = new AlwaysEnabledLoggerFactory();

				var after = LogExtensionPoint.Log(new EarlyLoggingProbe());

				Assert.AreNotSame(before, after, "The null logger obtained before initialization must not be memoized for the type.");
				Assert.IsTrue(after.IsEnabled(LogLevel.Critical), "A type that logged before initialization must get a working logger once a factory is configured.");
				Assert.AreSame(after, LogExtensionPoint.Log(new EarlyLoggingProbe()), "Once a factory is configured, the logger must be memoized.");
			}
			finally
			{
				LoggerFactory.ExternalLoggerFactory = original;
			}
		}

		private class EarlyLoggingProbe
		{
		}

		private class AlwaysEnabledLoggerFactory : IExternalLoggerFactory
		{
			public IExternalLogger CreateLogger(string categoryName) => new AlwaysEnabledLogger();
		}

		private class AlwaysEnabledLogger : IExternalLogger
		{
			public LogLevel LogLevel => LogLevel.Trace;

			public void Log(LogLevel logLevel, string? message, Exception? exception = null)
			{
			}
		}
	}
}

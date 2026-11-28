#nullable enable

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Foundation.Logging;

namespace Uno.UI.Tests.Foundation
{
	[UITestClass]
	public class Given_Logger
	{
		[TestMethod]
		public void When_Warn_With_Exception()
		{
			var external = new CapturingLogger();
			var ex = new InvalidOperationException("boom");

			new Logger(external).Warn("message", ex);

			Assert.AreEqual(LogLevel.Warning, external.LastLevel);
			Assert.AreEqual("message", external.LastMessage);
			Assert.AreSame(ex, external.LastException);
		}

		[TestMethod]
		public void When_LogWarning_With_Exception()
		{
			var external = new CapturingLogger();
			var ex = new InvalidOperationException("boom");

			new Logger(external).LogWarning("message", ex);

			Assert.AreEqual(LogLevel.Warning, external.LastLevel);
			Assert.AreEqual("message", external.LastMessage);
			Assert.AreSame(ex, external.LastException);
		}

		private class CapturingLogger : IExternalLogger
		{
			public LogLevel LogLevel => LogLevel.Trace;

			public LogLevel LastLevel { get; private set; } = LogLevel.None;

			public string? LastMessage { get; private set; }

			public Exception? LastException { get; private set; }

			public void Log(LogLevel logLevel, string? message, Exception? exception = null)
			{
				LastLevel = logLevel;
				LastMessage = message;
				LastException = exception;
			}
		}
	}
}

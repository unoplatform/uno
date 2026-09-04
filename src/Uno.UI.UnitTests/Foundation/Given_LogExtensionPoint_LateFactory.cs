#nullable enable

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Foundation.Logging;

namespace Uno.UI.Tests.Foundation
{
	/// <summary>
	/// <see cref="LoggerFactory.CreateLogger(string)"/> hands out a shared disabled logger while no
	/// external factory is registered, and <see cref="LogExtensionPoint"/> memoizes what it is given.
	/// The host logs before an app's constructor calls <c>LoggingAdapter.Initialize()</c>, so without
	/// invalidation the types that logged first stay silenced for the life of the process.
	/// </summary>
	[TestClass]
	public class Given_LogExtensionPoint_LateFactory
	{
		[TestMethod]
		public void When_Type_Logs_Before_Factory_Is_Registered_Then_It_Logs_Afterwards()
		{
			var previous = LoggerFactory.ExternalLoggerFactory;
			try
			{
				LoggerFactory.ExternalLoggerFactory = null;

				Assert.IsFalse(
					LogExtensionPoint.Log(new LateFactoryProbe()).IsEnabled(LogLevel.Debug),
					"Pre-condition: with no factory registered, every level must report as disabled.");

				LoggerFactory.ExternalLoggerFactory = new StubExternalLoggerFactory(LogLevel.Debug);

				Assert.IsTrue(
					LogExtensionPoint.Log(new LateFactoryProbe()).IsEnabled(LogLevel.Debug),
					"A type that resolved a logger before the logging adapter was initialised must pick up the "
					+ "newly registered factory: memoizing the disabled logger silences that type permanently.");
			}
			finally
			{
				LoggerFactory.ExternalLoggerFactory = previous;
			}
		}

		[TestMethod]
		public void When_Factory_Is_Replaced_Then_The_New_Level_Applies()
		{
			var previous = LoggerFactory.ExternalLoggerFactory;
			try
			{
				LoggerFactory.ExternalLoggerFactory = new StubExternalLoggerFactory(LogLevel.Error);
				Assert.IsFalse(
					LogExtensionPoint.Log(new LateFactoryProbe()).IsEnabled(LogLevel.Debug),
					"Pre-condition: an Error-level factory must not enable Debug.");

				LoggerFactory.ExternalLoggerFactory = new StubExternalLoggerFactory(LogLevel.Debug);

				Assert.IsTrue(
					LogExtensionPoint.Log(new LateFactoryProbe()).IsEnabled(LogLevel.Debug),
					"Replacing the factory must re-resolve memoized loggers rather than keep the previous level.");
			}
			finally
			{
				LoggerFactory.ExternalLoggerFactory = previous;
			}
		}

		private sealed class StubExternalLoggerFactory : IExternalLoggerFactory
		{
			private readonly LogLevel _level;

			public StubExternalLoggerFactory(LogLevel level) => _level = level;

			public IExternalLogger CreateLogger(string categoryName) => new StubExternalLogger(_level);
		}

		private sealed class StubExternalLogger : IExternalLogger
		{
			public StubExternalLogger(LogLevel level) => LogLevel = level;

			public LogLevel LogLevel { get; }

			public void Log(LogLevel logLevel, string? message, Exception? exception = null)
			{
			}
		}

		/// <summary>
		/// Stand-in for a framework type that logs during host start-up, before the app registers a factory.
		/// </summary>
		private sealed class LateFactoryProbe
		{
		}
	}
}

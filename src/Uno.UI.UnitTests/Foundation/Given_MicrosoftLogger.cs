using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.Adapter.Microsoft.Extensions.Logging;
using MelLogLevel = Microsoft.Extensions.Logging.LogLevel;
using UnoLogger = Uno.Foundation.Logging.Logger;
using UnoLogLevel = Uno.Foundation.Logging.LogLevel;

namespace Uno.UI.Tests.Foundation
{
	[TestClass]
	public class Given_MicrosoftLogger
	{
		[TestMethod]
		public void When_No_Provider_Then_All_Levels_Disabled()
		{
			using var factory = LoggerFactory.Create(_ => { });

			AssertAllLevelsDisabled(factory);
		}

		[TestMethod]
		public void When_Minimum_Level_None_Then_All_Levels_Disabled()
		{
			using var factory = LoggerFactory.Create(builder => builder
				.SetMinimumLevel(MelLogLevel.None)
				.AddConsole());

			AssertAllLevelsDisabled(factory);
		}

		[TestMethod]
		public void When_Minimum_Level_Warning_Then_Lower_Levels_Disabled()
		{
			using var factory = LoggerFactory.Create(builder => builder
				.SetMinimumLevel(MelLogLevel.Warning)
				.AddConsole());

			var externalLogger = new MicrosoftLogger(factory.CreateLogger("Test"));
			var logger = new UnoLogger(externalLogger);

			Assert.AreEqual(UnoLogLevel.Warning, externalLogger.LogLevel);
			Assert.IsFalse(logger.IsEnabled(UnoLogLevel.Trace));
			Assert.IsFalse(logger.IsEnabled(UnoLogLevel.Debug));
			Assert.IsFalse(logger.IsEnabled(UnoLogLevel.Information));
			Assert.IsTrue(logger.IsEnabled(UnoLogLevel.Warning));
			Assert.IsTrue(logger.IsEnabled(UnoLogLevel.Error));
			Assert.IsTrue(logger.IsEnabled(UnoLogLevel.Critical));
		}

		private static void AssertAllLevelsDisabled(ILoggerFactory factory)
		{
			var externalLogger = new MicrosoftLogger(factory.CreateLogger("Test"));
			var logger = new UnoLogger(externalLogger);

			Assert.AreEqual(UnoLogLevel.None, externalLogger.LogLevel);
			Assert.IsFalse(logger.IsEnabled(UnoLogLevel.Trace));
			Assert.IsFalse(logger.IsEnabled(UnoLogLevel.Debug));
			Assert.IsFalse(logger.IsEnabled(UnoLogLevel.Information));
			Assert.IsFalse(logger.IsEnabled(UnoLogLevel.Warning));
			Assert.IsFalse(logger.IsEnabled(UnoLogLevel.Error));
			Assert.IsFalse(logger.IsEnabled(UnoLogLevel.Critical));
		}
	}
}

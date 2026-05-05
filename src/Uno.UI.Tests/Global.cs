using System.Globalization;
using System.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.UI.Xaml;

namespace Uno.UI.Tests
{
	[TestClass]
	public static class Global
	{
		[AssemblyInitialize]
		public static void GlobalTestInitialize(TestContext _)
		{
			// Ensure all tests are run under the same culture context
			Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
			Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;

			var factory = LoggerFactory.Create(builder =>
			{
#if false // DEBUG // debug logging is generally very verbose and slows down testing. Enable when needed.
				var logLevel = LogLevel.Debug;
#else
				var logLevel = LogLevel.None;
#endif
				builder.SetMinimumLevel(logLevel);
				builder.AddConsole();
			});

			Uno.UI.Adapter.Microsoft.Extensions.Logging.LoggingAdapter.Initialize();
			Uno.Extensions.LogExtensionPoint.AmbientLoggerFactory = factory;

			// The unit-test process does not own a real Skia dispatcher; relax the
			// dependency-property threading enforcement so the test thread can poke
			// the DP system the same way the in-memory mock dispatcher used to allow.
			global::Uno.UI.FeatureConfiguration.DependencyProperty.DisableThreadingCheck = true;
		}
	}
}

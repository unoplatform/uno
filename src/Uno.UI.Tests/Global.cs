using System.Globalization;
using System.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.UI.Xaml;

namespace Uno.UI.Tests
{
	[TestClass]
	public class Global
	{
		[AssemblyInitialize]
		public static void GlobalTestInitialize(TestContext _)
		{
			// Ensure all tests are run under the same culture context
			Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
			Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;

			var factory = LoggerFactory.Create(builder =>
			{
#if DEBUG
				var logLevel = LogLevel.Debug;
#else
				var logLevel = LogLevel.Information;
#endif
				builder.AddConsole(o => o.LogToStandardErrorThreshold = logLevel);
			});

			Uno.Extensions.LogExtensionPoint.AmbientLoggerFactory = factory;
		}
	}
}

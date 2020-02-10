using System.Globalization;
using System.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

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

			Uno.Extensions.LogExtensionPoint
				.AmbientLoggerFactory
				.AddConsole(LogLevel.Debug)
				.AddDebug(LogLevel.Debug);
		}
	}
}

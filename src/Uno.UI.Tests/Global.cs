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

			// The unit-test process does not own a real Skia host (Win32/X11/...); install
			// no-op overrides for the NativeDispatcher hooks so DependencyQueue access does
			// not throw. The Skia variant's GetHasThreadAccess() / EnqueueNative()
			// require these to be set, and the runtime hosts normally do that on startup.
			Uno.UI.Dispatching.NativeDispatcher.HasThreadAccessOverride = () => true;
			Uno.UI.Dispatching.NativeDispatcher.DispatchOverride = (action, _) => action();

			// Belt-and-suspenders: the unit-test thread is not the Skia main thread, so
			// also relax the DP threading enforcement (the in-memory mock used to make
			// HasThreadAccess return true unconditionally).
			global::Uno.UI.FeatureConfiguration.DependencyProperty.DisableThreadingCheck = true;

			// A real Skia host initializes the package manifest on startup (Application's
			// ctor does it too, but reflection-based tests such as the DependencyProperty
			// owner/type checks touch control static ctors -- e.g. CalendarView -> Calendar
			// -> ApplicationLanguages -> ApplicationData -- before any Application exists).
			// Initialize it here so those paths resolve the app data folder instead of
			// throwing "The Package.Id is not initialized yet".
			global::Windows.ApplicationModel.Package.SetEntryAssembly(typeof(Global).Assembly);

			// The real Skia hosts register a native window factory on startup. The unit-test
			// process has none, so register a headless one; without it, creating the host
			// Window throws "Window factory was not registered".
			global::Uno.Foundation.Extensibility.ApiExtensibility.Register(
				typeof(global::Uno.UI.Xaml.Controls.INativeWindowFactoryExtension),
				_ => new global::UnitTestsApp.TestNativeWindowFactoryExtension());
		}
	}
}

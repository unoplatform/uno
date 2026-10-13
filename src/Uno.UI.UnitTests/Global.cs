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

			// The unit-test process does not own a real Skia host (Win32/X11/...), so it installs the
			// NativeDispatcher hooks the hosts normally set on startup. Work dispatched from another thread
			// (e.g. a DispatcherQueueTimer tick) is queued to the UI thread instead of running layout there
			// concurrently with the test. On the UI thread it runs inline, which the tests rely on.
			Uno.UI.Dispatching.NativeDispatcher.HasThreadAccessOverride = () => true;
			Uno.UI.Dispatching.NativeDispatcher.DispatchBehindPendingOverride = () => UnitTestUIThread.IsCurrent;
			Uno.UI.Dispatching.NativeDispatcher.DispatchOverride = (action, _) =>
			{
				if (UnitTestUIThread.IsCurrent)
				{
					action();
				}
				else
				{
					UnitTestUIThread.Post(static state => ((System.Action)state!)(), action);
				}
			};

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

			// Skia text measurement loads ICU lazily and reads icudt.dat from the entry
			// assembly. A real Uno head wires this via the IcuDataInitializerGenerator
			// (gated on IsUnoHead); the unit-test runner is not a head, so initialize ICU
			// here, mirroring what the generated module initializer does. UnicodeText+ICU
			// is a private nested type, so this is done by reflection like SamplesApp.
			var icuType = System.Type.GetType("Microsoft.UI.Xaml.Documents.UnicodeText+ICU, Uno.UI");
			icuType?
				.GetMethod("SetDataAssembly", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
				?.Invoke(null, new object[] { typeof(Global).Assembly });

			// The drawing seams are installed by the host builder, which this process never runs. Register the
			// managed engine (already referenced) so XAML that materializes geometry or measures text works.
			global::Uno.UI.Composition.Drawing.GeometryFactory.RegisterDefault(new global::Uno.UI.Composition.Drawing.ManagedGeometryFactory());
			global::Uno.UI.Composition.Drawing.FontProvider.RegisterDefault(new global::Uno.UI.Composition.Drawing.ManagedFontProvider());
			global::Uno.UI.Composition.Drawing.ImageEncoderDecoder.Current = new global::Uno.UI.Composition.Drawing.ManagedImageDecoderBackend();
		}
	}
}

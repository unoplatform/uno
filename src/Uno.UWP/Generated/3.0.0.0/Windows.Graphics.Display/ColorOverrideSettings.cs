#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Graphics.Display
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class ColorOverrideSettings 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Graphics.Display.DisplayColorOverrideScenario DesiredDisplayColorOverrideScenario
		{
			get
			{
				throw new global::System.NotImplementedException("The member DisplayColorOverrideScenario ColorOverrideSettings.DesiredDisplayColorOverrideScenario is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Graphics.Display.ColorOverrideSettings.DesiredDisplayColorOverrideScenario.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Graphics.Display.ColorOverrideSettings CreateFromDisplayColorOverrideScenario( global::Windows.Graphics.Display.DisplayColorOverrideScenario overrideScenario)
		{
			throw new global::System.NotImplementedException("The member ColorOverrideSettings ColorOverrideSettings.CreateFromDisplayColorOverrideScenario(DisplayColorOverrideScenario overrideScenario) is not implemented in Uno.");
		}
		#endif
	}
}

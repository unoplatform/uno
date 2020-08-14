#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Graphics.Display
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class BrightnessOverrideSettings 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  double DesiredLevel
		{
			get
			{
				throw new global::System.NotImplementedException("The member double BrightnessOverrideSettings.DesiredLevel is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  float DesiredNits
		{
			get
			{
				throw new global::System.NotImplementedException("The member float BrightnessOverrideSettings.DesiredNits is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Graphics.Display.BrightnessOverrideSettings.DesiredLevel.get
		// Forced skipping of method Windows.Graphics.Display.BrightnessOverrideSettings.DesiredNits.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Graphics.Display.BrightnessOverrideSettings CreateFromLevel( double level)
		{
			throw new global::System.NotImplementedException("The member BrightnessOverrideSettings BrightnessOverrideSettings.CreateFromLevel(double level) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Graphics.Display.BrightnessOverrideSettings CreateFromNits( float nits)
		{
			throw new global::System.NotImplementedException("The member BrightnessOverrideSettings BrightnessOverrideSettings.CreateFromNits(float nits) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Graphics.Display.BrightnessOverrideSettings CreateFromDisplayBrightnessOverrideScenario( global::Windows.Graphics.Display.DisplayBrightnessOverrideScenario overrideScenario)
		{
			throw new global::System.NotImplementedException("The member BrightnessOverrideSettings BrightnessOverrideSettings.CreateFromDisplayBrightnessOverrideScenario(DisplayBrightnessOverrideScenario overrideScenario) is not implemented in Uno.");
		}
		#endif
	}
}

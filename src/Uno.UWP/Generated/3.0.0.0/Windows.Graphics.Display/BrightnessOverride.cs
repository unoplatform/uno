#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Graphics.Display
{
	#if false || false || false || false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class BrightnessOverride 
	{
		// Skipping already declared property BrightnessLevel
		// Skipping already declared property IsOverrideActive
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsSupported
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool BrightnessOverride.IsSupported is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Graphics.Display.BrightnessOverride.IsSupported.get
		// Forced skipping of method Windows.Graphics.Display.BrightnessOverride.IsOverrideActive.get
		// Forced skipping of method Windows.Graphics.Display.BrightnessOverride.BrightnessLevel.get
		#if false || false || NET461 || false || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("NET461", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void SetBrightnessLevel( double brightnessLevel,  global::Windows.Graphics.Display.DisplayBrightnessOverrideOptions options)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Graphics.Display.BrightnessOverride", "void BrightnessOverride.SetBrightnessLevel(double brightnessLevel, DisplayBrightnessOverrideOptions options)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void SetBrightnessScenario( global::Windows.Graphics.Display.DisplayBrightnessScenario scenario,  global::Windows.Graphics.Display.DisplayBrightnessOverrideOptions options)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Graphics.Display.BrightnessOverride", "void BrightnessOverride.SetBrightnessScenario(DisplayBrightnessScenario scenario, DisplayBrightnessOverrideOptions options)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  double GetLevelForScenario( global::Windows.Graphics.Display.DisplayBrightnessScenario scenario)
		{
			throw new global::System.NotImplementedException("The member double BrightnessOverride.GetLevelForScenario(DisplayBrightnessScenario scenario) is not implemented in Uno.");
		}
		#endif
		#if false || false || NET461 || false || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("NET461", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void StartOverride()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Graphics.Display.BrightnessOverride", "void BrightnessOverride.StartOverride()");
		}
		#endif
		#if false || false || NET461 || false || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("NET461", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void StopOverride()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Graphics.Display.BrightnessOverride", "void BrightnessOverride.StopOverride()");
		}
		#endif
		// Forced skipping of method Windows.Graphics.Display.BrightnessOverride.IsSupportedChanged.add
		// Forced skipping of method Windows.Graphics.Display.BrightnessOverride.IsSupportedChanged.remove
		// Forced skipping of method Windows.Graphics.Display.BrightnessOverride.IsOverrideActiveChanged.add
		// Forced skipping of method Windows.Graphics.Display.BrightnessOverride.IsOverrideActiveChanged.remove
		// Forced skipping of method Windows.Graphics.Display.BrightnessOverride.BrightnessLevelChanged.add
		// Forced skipping of method Windows.Graphics.Display.BrightnessOverride.BrightnessLevelChanged.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Graphics.Display.BrightnessOverride GetDefaultForSystem()
		{
			throw new global::System.NotImplementedException("The member BrightnessOverride BrightnessOverride.GetDefaultForSystem() is not implemented in Uno.");
		}
		#endif
		// Skipping already declared method Windows.Graphics.Display.BrightnessOverride.GetForCurrentView()
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<bool> SaveForSystemAsync( global::Windows.Graphics.Display.BrightnessOverride value)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<bool> BrightnessOverride.SaveForSystemAsync(BrightnessOverride value) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Graphics.Display.BrightnessOverride, object> BrightnessLevelChanged
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Graphics.Display.BrightnessOverride", "event TypedEventHandler<BrightnessOverride, object> BrightnessOverride.BrightnessLevelChanged");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Graphics.Display.BrightnessOverride", "event TypedEventHandler<BrightnessOverride, object> BrightnessOverride.BrightnessLevelChanged");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Graphics.Display.BrightnessOverride, object> IsOverrideActiveChanged
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Graphics.Display.BrightnessOverride", "event TypedEventHandler<BrightnessOverride, object> BrightnessOverride.IsOverrideActiveChanged");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Graphics.Display.BrightnessOverride", "event TypedEventHandler<BrightnessOverride, object> BrightnessOverride.IsOverrideActiveChanged");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Graphics.Display.BrightnessOverride, object> IsSupportedChanged
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Graphics.Display.BrightnessOverride", "event TypedEventHandler<BrightnessOverride, object> BrightnessOverride.IsSupportedChanged");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Graphics.Display.BrightnessOverride", "event TypedEventHandler<BrightnessOverride, object> BrightnessOverride.IsSupportedChanged");
			}
		}
		#endif
	}
}

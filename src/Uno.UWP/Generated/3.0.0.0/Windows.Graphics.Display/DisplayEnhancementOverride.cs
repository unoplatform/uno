#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Graphics.Display
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class DisplayEnhancementOverride 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Graphics.Display.ColorOverrideSettings ColorOverrideSettings
		{
			get
			{
				throw new global::System.NotImplementedException("The member ColorOverrideSettings DisplayEnhancementOverride.ColorOverrideSettings is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Graphics.Display.DisplayEnhancementOverride", "ColorOverrideSettings DisplayEnhancementOverride.ColorOverrideSettings");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Graphics.Display.BrightnessOverrideSettings BrightnessOverrideSettings
		{
			get
			{
				throw new global::System.NotImplementedException("The member BrightnessOverrideSettings DisplayEnhancementOverride.BrightnessOverrideSettings is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Graphics.Display.DisplayEnhancementOverride", "BrightnessOverrideSettings DisplayEnhancementOverride.BrightnessOverrideSettings");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool CanOverride
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool DisplayEnhancementOverride.CanOverride is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsOverrideActive
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool DisplayEnhancementOverride.IsOverrideActive is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Graphics.Display.DisplayEnhancementOverride.ColorOverrideSettings.get
		// Forced skipping of method Windows.Graphics.Display.DisplayEnhancementOverride.ColorOverrideSettings.set
		// Forced skipping of method Windows.Graphics.Display.DisplayEnhancementOverride.BrightnessOverrideSettings.get
		// Forced skipping of method Windows.Graphics.Display.DisplayEnhancementOverride.BrightnessOverrideSettings.set
		// Forced skipping of method Windows.Graphics.Display.DisplayEnhancementOverride.CanOverride.get
		// Forced skipping of method Windows.Graphics.Display.DisplayEnhancementOverride.IsOverrideActive.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Graphics.Display.DisplayEnhancementOverrideCapabilities GetCurrentDisplayEnhancementOverrideCapabilities()
		{
			throw new global::System.NotImplementedException("The member DisplayEnhancementOverrideCapabilities DisplayEnhancementOverride.GetCurrentDisplayEnhancementOverrideCapabilities() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void RequestOverride()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Graphics.Display.DisplayEnhancementOverride", "void DisplayEnhancementOverride.RequestOverride()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void StopOverride()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Graphics.Display.DisplayEnhancementOverride", "void DisplayEnhancementOverride.StopOverride()");
		}
		#endif
		// Forced skipping of method Windows.Graphics.Display.DisplayEnhancementOverride.CanOverrideChanged.add
		// Forced skipping of method Windows.Graphics.Display.DisplayEnhancementOverride.CanOverrideChanged.remove
		// Forced skipping of method Windows.Graphics.Display.DisplayEnhancementOverride.IsOverrideActiveChanged.add
		// Forced skipping of method Windows.Graphics.Display.DisplayEnhancementOverride.IsOverrideActiveChanged.remove
		// Forced skipping of method Windows.Graphics.Display.DisplayEnhancementOverride.DisplayEnhancementOverrideCapabilitiesChanged.add
		// Forced skipping of method Windows.Graphics.Display.DisplayEnhancementOverride.DisplayEnhancementOverrideCapabilitiesChanged.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Graphics.Display.DisplayEnhancementOverride GetForCurrentView()
		{
			throw new global::System.NotImplementedException("The member DisplayEnhancementOverride DisplayEnhancementOverride.GetForCurrentView() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Graphics.Display.DisplayEnhancementOverride, object> CanOverrideChanged
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Graphics.Display.DisplayEnhancementOverride", "event TypedEventHandler<DisplayEnhancementOverride, object> DisplayEnhancementOverride.CanOverrideChanged");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Graphics.Display.DisplayEnhancementOverride", "event TypedEventHandler<DisplayEnhancementOverride, object> DisplayEnhancementOverride.CanOverrideChanged");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Graphics.Display.DisplayEnhancementOverride, global::Windows.Graphics.Display.DisplayEnhancementOverrideCapabilitiesChangedEventArgs> DisplayEnhancementOverrideCapabilitiesChanged
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Graphics.Display.DisplayEnhancementOverride", "event TypedEventHandler<DisplayEnhancementOverride, DisplayEnhancementOverrideCapabilitiesChangedEventArgs> DisplayEnhancementOverride.DisplayEnhancementOverrideCapabilitiesChanged");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Graphics.Display.DisplayEnhancementOverride", "event TypedEventHandler<DisplayEnhancementOverride, DisplayEnhancementOverrideCapabilitiesChangedEventArgs> DisplayEnhancementOverride.DisplayEnhancementOverrideCapabilitiesChanged");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Graphics.Display.DisplayEnhancementOverride, object> IsOverrideActiveChanged
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Graphics.Display.DisplayEnhancementOverride", "event TypedEventHandler<DisplayEnhancementOverride, object> DisplayEnhancementOverride.IsOverrideActiveChanged");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Graphics.Display.DisplayEnhancementOverride", "event TypedEventHandler<DisplayEnhancementOverride, object> DisplayEnhancementOverride.IsOverrideActiveChanged");
			}
		}
		#endif
	}
}

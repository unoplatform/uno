#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.ViewManagement
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class AccessibilitySettings 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool HighContrast
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool AccessibilitySettings.HighContrast is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string HighContrastScheme
		{
			get
			{
				throw new global::System.NotImplementedException("The member string AccessibilitySettings.HighContrastScheme is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public AccessibilitySettings() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.ViewManagement.AccessibilitySettings", "AccessibilitySettings.AccessibilitySettings()");
		}
		#endif
		// Forced skipping of method Windows.UI.ViewManagement.AccessibilitySettings.AccessibilitySettings()
		// Forced skipping of method Windows.UI.ViewManagement.AccessibilitySettings.HighContrast.get
		// Forced skipping of method Windows.UI.ViewManagement.AccessibilitySettings.HighContrastScheme.get
		// Forced skipping of method Windows.UI.ViewManagement.AccessibilitySettings.HighContrastChanged.add
		// Forced skipping of method Windows.UI.ViewManagement.AccessibilitySettings.HighContrastChanged.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.UI.ViewManagement.AccessibilitySettings, object> HighContrastChanged
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.ViewManagement.AccessibilitySettings", "event TypedEventHandler<AccessibilitySettings, object> AccessibilitySettings.HighContrastChanged");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.ViewManagement.AccessibilitySettings", "event TypedEventHandler<AccessibilitySettings, object> AccessibilitySettings.HighContrastChanged");
			}
		}
		#endif
	}
}

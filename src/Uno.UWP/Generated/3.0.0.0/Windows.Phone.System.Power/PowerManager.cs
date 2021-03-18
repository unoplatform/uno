#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Phone.System.Power
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class PowerManager 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Phone.System.Power.PowerSavingMode PowerSavingMode
		{
			get
			{
				throw new global::System.NotImplementedException("The member PowerSavingMode PowerManager.PowerSavingMode is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static bool PowerSavingModeEnabled
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool PowerManager.PowerSavingModeEnabled is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Phone.System.Power.PowerManager.PowerSavingMode.get
		// Forced skipping of method Windows.Phone.System.Power.PowerManager.PowerSavingModeChanged.add
		// Forced skipping of method Windows.Phone.System.Power.PowerManager.PowerSavingModeChanged.remove
		// Forced skipping of method Windows.Phone.System.Power.PowerManager.PowerSavingModeEnabled.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static event global::System.EventHandler<object> PowerSavingModeChanged
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Phone.System.Power.PowerManager", "event EventHandler<object> PowerManager.PowerSavingModeChanged");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Phone.System.Power.PowerManager", "event EventHandler<object> PowerManager.PowerSavingModeChanged");
			}
		}
		#endif
	}
}

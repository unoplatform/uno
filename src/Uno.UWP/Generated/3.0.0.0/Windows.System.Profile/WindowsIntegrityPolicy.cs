#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.System.Profile
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class WindowsIntegrityPolicy 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static bool CanDisable
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool WindowsIntegrityPolicy.CanDisable is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static bool IsDisableSupported
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool WindowsIntegrityPolicy.IsDisableSupported is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static bool IsEnabled
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool WindowsIntegrityPolicy.IsEnabled is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static bool IsEnabledForTrial
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool WindowsIntegrityPolicy.IsEnabledForTrial is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.System.Profile.WindowsIntegrityPolicy.IsEnabled.get
		// Forced skipping of method Windows.System.Profile.WindowsIntegrityPolicy.IsEnabledForTrial.get
		// Forced skipping of method Windows.System.Profile.WindowsIntegrityPolicy.CanDisable.get
		// Forced skipping of method Windows.System.Profile.WindowsIntegrityPolicy.IsDisableSupported.get
		// Forced skipping of method Windows.System.Profile.WindowsIntegrityPolicy.PolicyChanged.add
		// Forced skipping of method Windows.System.Profile.WindowsIntegrityPolicy.PolicyChanged.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static event global::System.EventHandler<object> PolicyChanged
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.System.Profile.WindowsIntegrityPolicy", "event EventHandler<object> WindowsIntegrityPolicy.PolicyChanged");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.System.Profile.WindowsIntegrityPolicy", "event EventHandler<object> WindowsIntegrityPolicy.PolicyChanged");
			}
		}
		#endif
	}
}

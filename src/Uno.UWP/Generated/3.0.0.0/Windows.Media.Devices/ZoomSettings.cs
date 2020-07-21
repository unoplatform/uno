#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Devices
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class ZoomSettings 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  float Value
		{
			get
			{
				throw new global::System.NotImplementedException("The member float ZoomSettings.Value is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Devices.ZoomSettings", "float ZoomSettings.Value");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.Devices.ZoomTransitionMode Mode
		{
			get
			{
				throw new global::System.NotImplementedException("The member ZoomTransitionMode ZoomSettings.Mode is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Devices.ZoomSettings", "ZoomTransitionMode ZoomSettings.Mode");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public ZoomSettings() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Devices.ZoomSettings", "ZoomSettings.ZoomSettings()");
		}
		#endif
		// Forced skipping of method Windows.Media.Devices.ZoomSettings.ZoomSettings()
		// Forced skipping of method Windows.Media.Devices.ZoomSettings.Mode.get
		// Forced skipping of method Windows.Media.Devices.ZoomSettings.Mode.set
		// Forced skipping of method Windows.Media.Devices.ZoomSettings.Value.get
		// Forced skipping of method Windows.Media.Devices.ZoomSettings.Value.set
	}
}

#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Devices
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class AdvancedPhotoCaptureSettings 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.Devices.AdvancedPhotoMode Mode
		{
			get
			{
				throw new global::System.NotImplementedException("The member AdvancedPhotoMode AdvancedPhotoCaptureSettings.Mode is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Devices.AdvancedPhotoCaptureSettings", "AdvancedPhotoMode AdvancedPhotoCaptureSettings.Mode");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public AdvancedPhotoCaptureSettings() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Devices.AdvancedPhotoCaptureSettings", "AdvancedPhotoCaptureSettings.AdvancedPhotoCaptureSettings()");
		}
		#endif
		// Forced skipping of method Windows.Media.Devices.AdvancedPhotoCaptureSettings.AdvancedPhotoCaptureSettings()
		// Forced skipping of method Windows.Media.Devices.AdvancedPhotoCaptureSettings.Mode.get
		// Forced skipping of method Windows.Media.Devices.AdvancedPhotoCaptureSettings.Mode.set
	}
}

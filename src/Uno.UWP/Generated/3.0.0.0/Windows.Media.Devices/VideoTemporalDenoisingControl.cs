#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Devices
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class VideoTemporalDenoisingControl 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.Devices.VideoTemporalDenoisingMode Mode
		{
			get
			{
				throw new global::System.NotImplementedException("The member VideoTemporalDenoisingMode VideoTemporalDenoisingControl.Mode is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Devices.VideoTemporalDenoisingControl", "VideoTemporalDenoisingMode VideoTemporalDenoisingControl.Mode");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool Supported
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool VideoTemporalDenoisingControl.Supported is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<global::Windows.Media.Devices.VideoTemporalDenoisingMode> SupportedModes
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyList<VideoTemporalDenoisingMode> VideoTemporalDenoisingControl.SupportedModes is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Media.Devices.VideoTemporalDenoisingControl.Supported.get
		// Forced skipping of method Windows.Media.Devices.VideoTemporalDenoisingControl.SupportedModes.get
		// Forced skipping of method Windows.Media.Devices.VideoTemporalDenoisingControl.Mode.get
		// Forced skipping of method Windows.Media.Devices.VideoTemporalDenoisingControl.Mode.set
	}
}

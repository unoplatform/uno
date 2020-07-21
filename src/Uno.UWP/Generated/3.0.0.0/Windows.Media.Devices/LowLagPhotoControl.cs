#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Devices
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class LowLagPhotoControl 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.MediaProperties.MediaThumbnailFormat ThumbnailFormat
		{
			get
			{
				throw new global::System.NotImplementedException("The member MediaThumbnailFormat LowLagPhotoControl.ThumbnailFormat is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Devices.LowLagPhotoControl", "MediaThumbnailFormat LowLagPhotoControl.ThumbnailFormat");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool ThumbnailEnabled
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool LowLagPhotoControl.ThumbnailEnabled is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Devices.LowLagPhotoControl", "bool LowLagPhotoControl.ThumbnailEnabled");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  uint DesiredThumbnailSize
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint LowLagPhotoControl.DesiredThumbnailSize is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Devices.LowLagPhotoControl", "uint LowLagPhotoControl.DesiredThumbnailSize");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  uint HardwareAcceleratedThumbnailSupported
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint LowLagPhotoControl.HardwareAcceleratedThumbnailSupported is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.MediaProperties.MediaRatio GetHighestConcurrentFrameRate( global::Windows.Media.MediaProperties.IMediaEncodingProperties captureProperties)
		{
			throw new global::System.NotImplementedException("The member MediaRatio LowLagPhotoControl.GetHighestConcurrentFrameRate(IMediaEncodingProperties captureProperties) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.MediaProperties.MediaRatio GetCurrentFrameRate()
		{
			throw new global::System.NotImplementedException("The member MediaRatio LowLagPhotoControl.GetCurrentFrameRate() is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.Media.Devices.LowLagPhotoControl.ThumbnailEnabled.get
		// Forced skipping of method Windows.Media.Devices.LowLagPhotoControl.ThumbnailEnabled.set
		// Forced skipping of method Windows.Media.Devices.LowLagPhotoControl.ThumbnailFormat.get
		// Forced skipping of method Windows.Media.Devices.LowLagPhotoControl.ThumbnailFormat.set
		// Forced skipping of method Windows.Media.Devices.LowLagPhotoControl.DesiredThumbnailSize.get
		// Forced skipping of method Windows.Media.Devices.LowLagPhotoControl.DesiredThumbnailSize.set
		// Forced skipping of method Windows.Media.Devices.LowLagPhotoControl.HardwareAcceleratedThumbnailSupported.get
	}
}

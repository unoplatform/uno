#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Devices
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class AudioDeviceController : global::Windows.Media.Devices.IMediaDeviceController
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  float VolumePercent
		{
			get
			{
				throw new global::System.NotImplementedException("The member float AudioDeviceController.VolumePercent is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Devices.AudioDeviceController", "float AudioDeviceController.VolumePercent");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool Muted
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool AudioDeviceController.Muted is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Devices.AudioDeviceController", "bool AudioDeviceController.Muted");
			}
		}
		#endif
		// Forced skipping of method Windows.Media.Devices.AudioDeviceController.Muted.set
		// Forced skipping of method Windows.Media.Devices.AudioDeviceController.Muted.get
		// Forced skipping of method Windows.Media.Devices.AudioDeviceController.VolumePercent.set
		// Forced skipping of method Windows.Media.Devices.AudioDeviceController.VolumePercent.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<global::Windows.Media.MediaProperties.IMediaEncodingProperties> GetAvailableMediaStreamProperties( global::Windows.Media.Capture.MediaStreamType mediaStreamType)
		{
			throw new global::System.NotImplementedException("The member IReadOnlyList<IMediaEncodingProperties> AudioDeviceController.GetAvailableMediaStreamProperties(MediaStreamType mediaStreamType) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.MediaProperties.IMediaEncodingProperties GetMediaStreamProperties( global::Windows.Media.Capture.MediaStreamType mediaStreamType)
		{
			throw new global::System.NotImplementedException("The member IMediaEncodingProperties AudioDeviceController.GetMediaStreamProperties(MediaStreamType mediaStreamType) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction SetMediaStreamPropertiesAsync( global::Windows.Media.Capture.MediaStreamType mediaStreamType,  global::Windows.Media.MediaProperties.IMediaEncodingProperties mediaEncodingProperties)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction AudioDeviceController.SetMediaStreamPropertiesAsync(MediaStreamType mediaStreamType, IMediaEncodingProperties mediaEncodingProperties) is not implemented in Uno.");
		}
		#endif
		// Processing: Windows.Media.Devices.IMediaDeviceController
	}
}

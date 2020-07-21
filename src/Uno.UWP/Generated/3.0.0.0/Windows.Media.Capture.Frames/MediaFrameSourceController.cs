#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Capture.Frames
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class MediaFrameSourceController 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.Devices.VideoDeviceController VideoDeviceController
		{
			get
			{
				throw new global::System.NotImplementedException("The member VideoDeviceController MediaFrameSourceController.VideoDeviceController is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.Devices.AudioDeviceController AudioDeviceController
		{
			get
			{
				throw new global::System.NotImplementedException("The member AudioDeviceController MediaFrameSourceController.AudioDeviceController is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Media.Capture.Frames.MediaFrameSourceGetPropertyResult> GetPropertyAsync( string propertyId)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<MediaFrameSourceGetPropertyResult> MediaFrameSourceController.GetPropertyAsync(string propertyId) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Media.Capture.Frames.MediaFrameSourceSetPropertyStatus> SetPropertyAsync( string propertyId,  object propertyValue)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<MediaFrameSourceSetPropertyStatus> MediaFrameSourceController.SetPropertyAsync(string propertyId, object propertyValue) is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.Media.Capture.Frames.MediaFrameSourceController.VideoDeviceController.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Media.Capture.Frames.MediaFrameSourceGetPropertyResult> GetPropertyByExtendedIdAsync( byte[] extendedPropertyId,  uint? maxPropertyValueSize)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<MediaFrameSourceGetPropertyResult> MediaFrameSourceController.GetPropertyByExtendedIdAsync(byte[] extendedPropertyId, uint? maxPropertyValueSize) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Media.Capture.Frames.MediaFrameSourceSetPropertyStatus> SetPropertyByExtendedIdAsync( byte[] extendedPropertyId,  byte[] propertyValue)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<MediaFrameSourceSetPropertyStatus> MediaFrameSourceController.SetPropertyByExtendedIdAsync(byte[] extendedPropertyId, byte[] propertyValue) is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.Media.Capture.Frames.MediaFrameSourceController.AudioDeviceController.get
	}
}

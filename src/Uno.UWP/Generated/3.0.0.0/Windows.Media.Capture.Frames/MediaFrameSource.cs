#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Capture.Frames
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class MediaFrameSource 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.Capture.Frames.MediaFrameSourceController Controller
		{
			get
			{
				throw new global::System.NotImplementedException("The member MediaFrameSourceController MediaFrameSource.Controller is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.Capture.Frames.MediaFrameFormat CurrentFormat
		{
			get
			{
				throw new global::System.NotImplementedException("The member MediaFrameFormat MediaFrameSource.CurrentFormat is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.Capture.Frames.MediaFrameSourceInfo Info
		{
			get
			{
				throw new global::System.NotImplementedException("The member MediaFrameSourceInfo MediaFrameSource.Info is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<global::Windows.Media.Capture.Frames.MediaFrameFormat> SupportedFormats
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyList<MediaFrameFormat> MediaFrameSource.SupportedFormats is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Media.Capture.Frames.MediaFrameSource.Info.get
		// Forced skipping of method Windows.Media.Capture.Frames.MediaFrameSource.Controller.get
		// Forced skipping of method Windows.Media.Capture.Frames.MediaFrameSource.SupportedFormats.get
		// Forced skipping of method Windows.Media.Capture.Frames.MediaFrameSource.CurrentFormat.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction SetFormatAsync( global::Windows.Media.Capture.Frames.MediaFrameFormat format)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction MediaFrameSource.SetFormatAsync(MediaFrameFormat format) is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.Media.Capture.Frames.MediaFrameSource.FormatChanged.add
		// Forced skipping of method Windows.Media.Capture.Frames.MediaFrameSource.FormatChanged.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.Devices.Core.CameraIntrinsics TryGetCameraIntrinsics( global::Windows.Media.Capture.Frames.MediaFrameFormat format)
		{
			throw new global::System.NotImplementedException("The member CameraIntrinsics MediaFrameSource.TryGetCameraIntrinsics(MediaFrameFormat format) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Media.Capture.Frames.MediaFrameSource, object> FormatChanged
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Capture.Frames.MediaFrameSource", "event TypedEventHandler<MediaFrameSource, object> MediaFrameSource.FormatChanged");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Capture.Frames.MediaFrameSource", "event TypedEventHandler<MediaFrameSource, object> MediaFrameSource.FormatChanged");
			}
		}
		#endif
	}
}

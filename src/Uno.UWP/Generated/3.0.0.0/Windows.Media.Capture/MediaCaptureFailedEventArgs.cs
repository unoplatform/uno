#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Capture
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class MediaCaptureFailedEventArgs 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  uint Code
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint MediaCaptureFailedEventArgs.Code is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string Message
		{
			get
			{
				throw new global::System.NotImplementedException("The member string MediaCaptureFailedEventArgs.Message is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Media.Capture.MediaCaptureFailedEventArgs.Message.get
		// Forced skipping of method Windows.Media.Capture.MediaCaptureFailedEventArgs.Code.get
	}
}

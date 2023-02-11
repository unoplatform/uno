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
				throw new global::System.NotImplementedException("The member uint MediaCaptureFailedEventArgs.Code is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=uint%20MediaCaptureFailedEventArgs.Code");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string Message
		{
			get
			{
				throw new global::System.NotImplementedException("The member string MediaCaptureFailedEventArgs.Message is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=string%20MediaCaptureFailedEventArgs.Message");
			}
		}
		#endif
		// Forced skipping of method Windows.Media.Capture.MediaCaptureFailedEventArgs.Message.get
		// Forced skipping of method Windows.Media.Capture.MediaCaptureFailedEventArgs.Code.get
	}
}

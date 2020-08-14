#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Capture.Frames
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class InfraredMediaFrame 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.Capture.Frames.MediaFrameReference FrameReference
		{
			get
			{
				throw new global::System.NotImplementedException("The member MediaFrameReference InfraredMediaFrame.FrameReference is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsIlluminated
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool InfraredMediaFrame.IsIlluminated is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.Capture.Frames.VideoMediaFrame VideoMediaFrame
		{
			get
			{
				throw new global::System.NotImplementedException("The member VideoMediaFrame InfraredMediaFrame.VideoMediaFrame is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Media.Capture.Frames.InfraredMediaFrame.FrameReference.get
		// Forced skipping of method Windows.Media.Capture.Frames.InfraredMediaFrame.VideoMediaFrame.get
		// Forced skipping of method Windows.Media.Capture.Frames.InfraredMediaFrame.IsIlluminated.get
	}
}

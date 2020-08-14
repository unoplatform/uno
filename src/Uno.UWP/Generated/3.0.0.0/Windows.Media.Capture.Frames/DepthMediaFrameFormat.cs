#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Capture.Frames
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class DepthMediaFrameFormat 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  double DepthScaleInMeters
		{
			get
			{
				throw new global::System.NotImplementedException("The member double DepthMediaFrameFormat.DepthScaleInMeters is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.Capture.Frames.VideoMediaFrameFormat VideoFormat
		{
			get
			{
				throw new global::System.NotImplementedException("The member VideoMediaFrameFormat DepthMediaFrameFormat.VideoFormat is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Media.Capture.Frames.DepthMediaFrameFormat.VideoFormat.get
		// Forced skipping of method Windows.Media.Capture.Frames.DepthMediaFrameFormat.DepthScaleInMeters.get
	}
}

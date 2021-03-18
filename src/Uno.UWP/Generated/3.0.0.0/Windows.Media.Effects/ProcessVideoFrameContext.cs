#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Effects
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class ProcessVideoFrameContext 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.VideoFrame InputFrame
		{
			get
			{
				throw new global::System.NotImplementedException("The member VideoFrame ProcessVideoFrameContext.InputFrame is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.VideoFrame OutputFrame
		{
			get
			{
				throw new global::System.NotImplementedException("The member VideoFrame ProcessVideoFrameContext.OutputFrame is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Media.Effects.ProcessVideoFrameContext.InputFrame.get
		// Forced skipping of method Windows.Media.Effects.ProcessVideoFrameContext.OutputFrame.get
	}
}

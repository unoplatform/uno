#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Capture
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class VideoStreamConfiguration 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.MediaProperties.VideoEncodingProperties InputProperties
		{
			get
			{
				throw new global::System.NotImplementedException("The member VideoEncodingProperties VideoStreamConfiguration.InputProperties is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.MediaProperties.VideoEncodingProperties OutputProperties
		{
			get
			{
				throw new global::System.NotImplementedException("The member VideoEncodingProperties VideoStreamConfiguration.OutputProperties is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Media.Capture.VideoStreamConfiguration.InputProperties.get
		// Forced skipping of method Windows.Media.Capture.VideoStreamConfiguration.OutputProperties.get
	}
}

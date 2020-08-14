#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Capture.Frames
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class MediaFrameFormat 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.MediaProperties.MediaRatio FrameRate
		{
			get
			{
				throw new global::System.NotImplementedException("The member MediaRatio MediaFrameFormat.FrameRate is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string MajorType
		{
			get
			{
				throw new global::System.NotImplementedException("The member string MediaFrameFormat.MajorType is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyDictionary<global::System.Guid, object> Properties
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyDictionary<Guid, object> MediaFrameFormat.Properties is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string Subtype
		{
			get
			{
				throw new global::System.NotImplementedException("The member string MediaFrameFormat.Subtype is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.Capture.Frames.VideoMediaFrameFormat VideoFormat
		{
			get
			{
				throw new global::System.NotImplementedException("The member VideoMediaFrameFormat MediaFrameFormat.VideoFormat is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.MediaProperties.AudioEncodingProperties AudioEncodingProperties
		{
			get
			{
				throw new global::System.NotImplementedException("The member AudioEncodingProperties MediaFrameFormat.AudioEncodingProperties is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Media.Capture.Frames.MediaFrameFormat.MajorType.get
		// Forced skipping of method Windows.Media.Capture.Frames.MediaFrameFormat.Subtype.get
		// Forced skipping of method Windows.Media.Capture.Frames.MediaFrameFormat.FrameRate.get
		// Forced skipping of method Windows.Media.Capture.Frames.MediaFrameFormat.Properties.get
		// Forced skipping of method Windows.Media.Capture.Frames.MediaFrameFormat.VideoFormat.get
		// Forced skipping of method Windows.Media.Capture.Frames.MediaFrameFormat.AudioEncodingProperties.get
	}
}

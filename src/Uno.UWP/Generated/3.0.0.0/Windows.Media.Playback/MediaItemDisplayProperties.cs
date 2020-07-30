#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Playback
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class MediaItemDisplayProperties 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.MediaPlaybackType Type
		{
			get
			{
				throw new global::System.NotImplementedException("The member MediaPlaybackType MediaItemDisplayProperties.Type is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Playback.MediaItemDisplayProperties", "MediaPlaybackType MediaItemDisplayProperties.Type");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Storage.Streams.RandomAccessStreamReference Thumbnail
		{
			get
			{
				throw new global::System.NotImplementedException("The member RandomAccessStreamReference MediaItemDisplayProperties.Thumbnail is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Playback.MediaItemDisplayProperties", "RandomAccessStreamReference MediaItemDisplayProperties.Thumbnail");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.MusicDisplayProperties MusicProperties
		{
			get
			{
				throw new global::System.NotImplementedException("The member MusicDisplayProperties MediaItemDisplayProperties.MusicProperties is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.VideoDisplayProperties VideoProperties
		{
			get
			{
				throw new global::System.NotImplementedException("The member VideoDisplayProperties MediaItemDisplayProperties.VideoProperties is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Media.Playback.MediaItemDisplayProperties.Type.get
		// Forced skipping of method Windows.Media.Playback.MediaItemDisplayProperties.Type.set
		// Forced skipping of method Windows.Media.Playback.MediaItemDisplayProperties.MusicProperties.get
		// Forced skipping of method Windows.Media.Playback.MediaItemDisplayProperties.VideoProperties.get
		// Forced skipping of method Windows.Media.Playback.MediaItemDisplayProperties.Thumbnail.get
		// Forced skipping of method Windows.Media.Playback.MediaItemDisplayProperties.Thumbnail.set
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void ClearAll()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Playback.MediaItemDisplayProperties", "void MediaItemDisplayProperties.ClearAll()");
		}
		#endif
	}
}

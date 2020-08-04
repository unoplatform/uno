#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Playback
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class MediaPlaybackItemOpenedEventArgs 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.Playback.MediaPlaybackItem Item
		{
			get
			{
				throw new global::System.NotImplementedException("The member MediaPlaybackItem MediaPlaybackItemOpenedEventArgs.Item is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Media.Playback.MediaPlaybackItemOpenedEventArgs.Item.get
	}
}

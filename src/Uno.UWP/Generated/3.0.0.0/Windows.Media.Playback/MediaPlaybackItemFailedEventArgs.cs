#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Playback
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class MediaPlaybackItemFailedEventArgs 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.Playback.MediaPlaybackItemError Error
		{
			get
			{
				throw new global::System.NotImplementedException("The member MediaPlaybackItemError MediaPlaybackItemFailedEventArgs.Error is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=MediaPlaybackItemError%20MediaPlaybackItemFailedEventArgs.Error");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.Playback.MediaPlaybackItem Item
		{
			get
			{
				throw new global::System.NotImplementedException("The member MediaPlaybackItem MediaPlaybackItemFailedEventArgs.Item is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=MediaPlaybackItem%20MediaPlaybackItemFailedEventArgs.Item");
			}
		}
		#endif
		// Forced skipping of method Windows.Media.Playback.MediaPlaybackItemFailedEventArgs.Item.get
		// Forced skipping of method Windows.Media.Playback.MediaPlaybackItemFailedEventArgs.Error.get
	}
}

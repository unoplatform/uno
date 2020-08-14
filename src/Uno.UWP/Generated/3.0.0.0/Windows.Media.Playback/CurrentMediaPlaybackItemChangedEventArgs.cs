#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Playback
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class CurrentMediaPlaybackItemChangedEventArgs 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.Playback.MediaPlaybackItem NewItem
		{
			get
			{
				throw new global::System.NotImplementedException("The member MediaPlaybackItem CurrentMediaPlaybackItemChangedEventArgs.NewItem is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.Playback.MediaPlaybackItem OldItem
		{
			get
			{
				throw new global::System.NotImplementedException("The member MediaPlaybackItem CurrentMediaPlaybackItemChangedEventArgs.OldItem is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.Playback.MediaPlaybackItemChangedReason Reason
		{
			get
			{
				throw new global::System.NotImplementedException("The member MediaPlaybackItemChangedReason CurrentMediaPlaybackItemChangedEventArgs.Reason is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Media.Playback.CurrentMediaPlaybackItemChangedEventArgs.NewItem.get
		// Forced skipping of method Windows.Media.Playback.CurrentMediaPlaybackItemChangedEventArgs.OldItem.get
		// Forced skipping of method Windows.Media.Playback.CurrentMediaPlaybackItemChangedEventArgs.Reason.get
	}
}

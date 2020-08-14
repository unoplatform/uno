#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Playback
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class BackgroundMediaPlayer 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Media.Playback.MediaPlayer Current
		{
			get
			{
				throw new global::System.NotImplementedException("The member MediaPlayer BackgroundMediaPlayer.Current is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Media.Playback.BackgroundMediaPlayer.Current.get
		// Forced skipping of method Windows.Media.Playback.BackgroundMediaPlayer.MessageReceivedFromBackground.add
		// Forced skipping of method Windows.Media.Playback.BackgroundMediaPlayer.MessageReceivedFromBackground.remove
		// Forced skipping of method Windows.Media.Playback.BackgroundMediaPlayer.MessageReceivedFromForeground.add
		// Forced skipping of method Windows.Media.Playback.BackgroundMediaPlayer.MessageReceivedFromForeground.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static void SendMessageToBackground( global::Windows.Foundation.Collections.ValueSet value)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Playback.BackgroundMediaPlayer", "void BackgroundMediaPlayer.SendMessageToBackground(ValueSet value)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static void SendMessageToForeground( global::Windows.Foundation.Collections.ValueSet value)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Playback.BackgroundMediaPlayer", "void BackgroundMediaPlayer.SendMessageToForeground(ValueSet value)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static bool IsMediaPlaying()
		{
			throw new global::System.NotImplementedException("The member bool BackgroundMediaPlayer.IsMediaPlaying() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static void Shutdown()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Playback.BackgroundMediaPlayer", "void BackgroundMediaPlayer.Shutdown()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static event global::System.EventHandler<global::Windows.Media.Playback.MediaPlayerDataReceivedEventArgs> MessageReceivedFromBackground
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Playback.BackgroundMediaPlayer", "event EventHandler<MediaPlayerDataReceivedEventArgs> BackgroundMediaPlayer.MessageReceivedFromBackground");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Playback.BackgroundMediaPlayer", "event EventHandler<MediaPlayerDataReceivedEventArgs> BackgroundMediaPlayer.MessageReceivedFromBackground");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static event global::System.EventHandler<global::Windows.Media.Playback.MediaPlayerDataReceivedEventArgs> MessageReceivedFromForeground
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Playback.BackgroundMediaPlayer", "event EventHandler<MediaPlayerDataReceivedEventArgs> BackgroundMediaPlayer.MessageReceivedFromForeground");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Playback.BackgroundMediaPlayer", "event EventHandler<MediaPlayerDataReceivedEventArgs> BackgroundMediaPlayer.MessageReceivedFromForeground");
			}
		}
		#endif
	}
}

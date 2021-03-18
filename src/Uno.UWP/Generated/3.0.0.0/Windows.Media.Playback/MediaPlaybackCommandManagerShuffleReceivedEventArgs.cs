#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Playback
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class MediaPlaybackCommandManagerShuffleReceivedEventArgs 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool Handled
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool MediaPlaybackCommandManagerShuffleReceivedEventArgs.Handled is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Playback.MediaPlaybackCommandManagerShuffleReceivedEventArgs", "bool MediaPlaybackCommandManagerShuffleReceivedEventArgs.Handled");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsShuffleRequested
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool MediaPlaybackCommandManagerShuffleReceivedEventArgs.IsShuffleRequested is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Media.Playback.MediaPlaybackCommandManagerShuffleReceivedEventArgs.Handled.get
		// Forced skipping of method Windows.Media.Playback.MediaPlaybackCommandManagerShuffleReceivedEventArgs.Handled.set
		// Forced skipping of method Windows.Media.Playback.MediaPlaybackCommandManagerShuffleReceivedEventArgs.IsShuffleRequested.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.Deferral GetDeferral()
		{
			throw new global::System.NotImplementedException("The member Deferral MediaPlaybackCommandManagerShuffleReceivedEventArgs.GetDeferral() is not implemented in Uno.");
		}
		#endif
	}
}

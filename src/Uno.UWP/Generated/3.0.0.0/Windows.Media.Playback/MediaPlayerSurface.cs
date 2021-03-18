#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Playback
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class MediaPlayerSurface : global::System.IDisposable
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Composition.ICompositionSurface CompositionSurface
		{
			get
			{
				throw new global::System.NotImplementedException("The member ICompositionSurface MediaPlayerSurface.CompositionSurface is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Composition.Compositor Compositor
		{
			get
			{
				throw new global::System.NotImplementedException("The member Compositor MediaPlayerSurface.Compositor is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.Playback.MediaPlayer MediaPlayer
		{
			get
			{
				throw new global::System.NotImplementedException("The member MediaPlayer MediaPlayerSurface.MediaPlayer is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Media.Playback.MediaPlayerSurface.CompositionSurface.get
		// Forced skipping of method Windows.Media.Playback.MediaPlayerSurface.Compositor.get
		// Forced skipping of method Windows.Media.Playback.MediaPlayerSurface.MediaPlayer.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void Dispose()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Playback.MediaPlayerSurface", "void MediaPlayerSurface.Dispose()");
		}
		#endif
		// Processing: System.IDisposable
	}
}

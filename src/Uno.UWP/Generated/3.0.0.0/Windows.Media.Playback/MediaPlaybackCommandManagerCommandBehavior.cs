#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Playback
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class MediaPlaybackCommandManagerCommandBehavior 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.Playback.MediaCommandEnablingRule EnablingRule
		{
			get
			{
				throw new global::System.NotImplementedException("The member MediaCommandEnablingRule MediaPlaybackCommandManagerCommandBehavior.EnablingRule is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=MediaCommandEnablingRule%20MediaPlaybackCommandManagerCommandBehavior.EnablingRule");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Playback.MediaPlaybackCommandManagerCommandBehavior", "MediaCommandEnablingRule MediaPlaybackCommandManagerCommandBehavior.EnablingRule");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.Playback.MediaPlaybackCommandManager CommandManager
		{
			get
			{
				throw new global::System.NotImplementedException("The member MediaPlaybackCommandManager MediaPlaybackCommandManagerCommandBehavior.CommandManager is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=MediaPlaybackCommandManager%20MediaPlaybackCommandManagerCommandBehavior.CommandManager");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsEnabled
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool MediaPlaybackCommandManagerCommandBehavior.IsEnabled is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=bool%20MediaPlaybackCommandManagerCommandBehavior.IsEnabled");
			}
		}
		#endif
		// Forced skipping of method Windows.Media.Playback.MediaPlaybackCommandManagerCommandBehavior.CommandManager.get
		// Forced skipping of method Windows.Media.Playback.MediaPlaybackCommandManagerCommandBehavior.IsEnabled.get
		// Forced skipping of method Windows.Media.Playback.MediaPlaybackCommandManagerCommandBehavior.EnablingRule.get
		// Forced skipping of method Windows.Media.Playback.MediaPlaybackCommandManagerCommandBehavior.EnablingRule.set
		// Forced skipping of method Windows.Media.Playback.MediaPlaybackCommandManagerCommandBehavior.IsEnabledChanged.add
		// Forced skipping of method Windows.Media.Playback.MediaPlaybackCommandManagerCommandBehavior.IsEnabledChanged.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Media.Playback.MediaPlaybackCommandManagerCommandBehavior, object> IsEnabledChanged
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Playback.MediaPlaybackCommandManagerCommandBehavior", "event TypedEventHandler<MediaPlaybackCommandManagerCommandBehavior, object> MediaPlaybackCommandManagerCommandBehavior.IsEnabledChanged");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Playback.MediaPlaybackCommandManagerCommandBehavior", "event TypedEventHandler<MediaPlaybackCommandManagerCommandBehavior, object> MediaPlaybackCommandManagerCommandBehavior.IsEnabledChanged");
			}
		}
		#endif
	}
}

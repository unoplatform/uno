#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Playback
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class MediaBreakManager 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Media.Playback.MediaBreak CurrentBreak
		{
			get
			{
				throw new global::System.NotImplementedException("The member MediaBreak MediaBreakManager.CurrentBreak is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Media.Playback.MediaPlaybackSession PlaybackSession
		{
			get
			{
				throw new global::System.NotImplementedException("The member MediaPlaybackSession MediaBreakManager.PlaybackSession is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Media.Playback.MediaBreakManager.BreaksSeekedOver.add
		// Forced skipping of method Windows.Media.Playback.MediaBreakManager.BreaksSeekedOver.remove
		// Forced skipping of method Windows.Media.Playback.MediaBreakManager.BreakStarted.add
		// Forced skipping of method Windows.Media.Playback.MediaBreakManager.BreakStarted.remove
		// Forced skipping of method Windows.Media.Playback.MediaBreakManager.BreakEnded.add
		// Forced skipping of method Windows.Media.Playback.MediaBreakManager.BreakEnded.remove
		// Forced skipping of method Windows.Media.Playback.MediaBreakManager.BreakSkipped.add
		// Forced skipping of method Windows.Media.Playback.MediaBreakManager.BreakSkipped.remove
		// Forced skipping of method Windows.Media.Playback.MediaBreakManager.CurrentBreak.get
		// Forced skipping of method Windows.Media.Playback.MediaBreakManager.PlaybackSession.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  void PlayBreak( global::Windows.Media.Playback.MediaBreak value)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Playback.MediaBreakManager", "void MediaBreakManager.PlayBreak(MediaBreak value)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  void SkipCurrentBreak()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Playback.MediaBreakManager", "void MediaBreakManager.SkipCurrentBreak()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Media.Playback.MediaBreakManager, global::Windows.Media.Playback.MediaBreakEndedEventArgs> BreakEnded
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Playback.MediaBreakManager", "event TypedEventHandler<MediaBreakManager, MediaBreakEndedEventArgs> MediaBreakManager.BreakEnded");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Playback.MediaBreakManager", "event TypedEventHandler<MediaBreakManager, MediaBreakEndedEventArgs> MediaBreakManager.BreakEnded");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Media.Playback.MediaBreakManager, global::Windows.Media.Playback.MediaBreakSkippedEventArgs> BreakSkipped
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Playback.MediaBreakManager", "event TypedEventHandler<MediaBreakManager, MediaBreakSkippedEventArgs> MediaBreakManager.BreakSkipped");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Playback.MediaBreakManager", "event TypedEventHandler<MediaBreakManager, MediaBreakSkippedEventArgs> MediaBreakManager.BreakSkipped");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Media.Playback.MediaBreakManager, global::Windows.Media.Playback.MediaBreakStartedEventArgs> BreakStarted
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Playback.MediaBreakManager", "event TypedEventHandler<MediaBreakManager, MediaBreakStartedEventArgs> MediaBreakManager.BreakStarted");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Playback.MediaBreakManager", "event TypedEventHandler<MediaBreakManager, MediaBreakStartedEventArgs> MediaBreakManager.BreakStarted");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Media.Playback.MediaBreakManager, global::Windows.Media.Playback.MediaBreakSeekedOverEventArgs> BreaksSeekedOver
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Playback.MediaBreakManager", "event TypedEventHandler<MediaBreakManager, MediaBreakSeekedOverEventArgs> MediaBreakManager.BreaksSeekedOver");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Playback.MediaBreakManager", "event TypedEventHandler<MediaBreakManager, MediaBreakSeekedOverEventArgs> MediaBreakManager.BreaksSeekedOver");
			}
		}
		#endif
	}
}

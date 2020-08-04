#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Playback
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class MediaBreakSchedule 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.Playback.MediaBreak PrerollBreak
		{
			get
			{
				throw new global::System.NotImplementedException("The member MediaBreak MediaBreakSchedule.PrerollBreak is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Playback.MediaBreakSchedule", "MediaBreak MediaBreakSchedule.PrerollBreak");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.Playback.MediaBreak PostrollBreak
		{
			get
			{
				throw new global::System.NotImplementedException("The member MediaBreak MediaBreakSchedule.PostrollBreak is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Playback.MediaBreakSchedule", "MediaBreak MediaBreakSchedule.PostrollBreak");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<global::Windows.Media.Playback.MediaBreak> MidrollBreaks
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyList<MediaBreak> MediaBreakSchedule.MidrollBreaks is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.Playback.MediaPlaybackItem PlaybackItem
		{
			get
			{
				throw new global::System.NotImplementedException("The member MediaPlaybackItem MediaBreakSchedule.PlaybackItem is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Media.Playback.MediaBreakSchedule.ScheduleChanged.add
		// Forced skipping of method Windows.Media.Playback.MediaBreakSchedule.ScheduleChanged.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void InsertMidrollBreak( global::Windows.Media.Playback.MediaBreak mediaBreak)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Playback.MediaBreakSchedule", "void MediaBreakSchedule.InsertMidrollBreak(MediaBreak mediaBreak)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void RemoveMidrollBreak( global::Windows.Media.Playback.MediaBreak mediaBreak)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Playback.MediaBreakSchedule", "void MediaBreakSchedule.RemoveMidrollBreak(MediaBreak mediaBreak)");
		}
		#endif
		// Forced skipping of method Windows.Media.Playback.MediaBreakSchedule.MidrollBreaks.get
		// Forced skipping of method Windows.Media.Playback.MediaBreakSchedule.PrerollBreak.set
		// Forced skipping of method Windows.Media.Playback.MediaBreakSchedule.PrerollBreak.get
		// Forced skipping of method Windows.Media.Playback.MediaBreakSchedule.PostrollBreak.set
		// Forced skipping of method Windows.Media.Playback.MediaBreakSchedule.PostrollBreak.get
		// Forced skipping of method Windows.Media.Playback.MediaBreakSchedule.PlaybackItem.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Media.Playback.MediaBreakSchedule, object> ScheduleChanged
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Playback.MediaBreakSchedule", "event TypedEventHandler<MediaBreakSchedule, object> MediaBreakSchedule.ScheduleChanged");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Playback.MediaBreakSchedule", "event TypedEventHandler<MediaBreakSchedule, object> MediaBreakSchedule.ScheduleChanged");
			}
		}
		#endif
	}
}

#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Playback
{
	#if false || false || NET461 || __WASM__ || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class MediaPlaybackItem : global::Windows.Media.Playback.IMediaPlaybackSource
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Media.Playback.MediaPlaybackAudioTrackList AudioTracks
		{
			get
			{
				throw new global::System.NotImplementedException("The member MediaPlaybackAudioTrackList MediaPlaybackItem.AudioTracks is not implemented in Uno.");
			}
		}
		#endif
		#if false || false || NET461 || __WASM__ || false
		[global::Uno.NotImplemented]
		public  global::Windows.Media.Core.MediaSource Source
		{
			get
			{
				throw new global::System.NotImplementedException("The member MediaSource MediaPlaybackItem.Source is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Media.Playback.MediaPlaybackTimedMetadataTrackList TimedMetadataTracks
		{
			get
			{
				throw new global::System.NotImplementedException("The member MediaPlaybackTimedMetadataTrackList MediaPlaybackItem.TimedMetadataTracks is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Media.Playback.MediaPlaybackVideoTrackList VideoTracks
		{
			get
			{
				throw new global::System.NotImplementedException("The member MediaPlaybackVideoTrackList MediaPlaybackItem.VideoTracks is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  bool CanSkip
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool MediaPlaybackItem.CanSkip is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Playback.MediaPlaybackItem", "bool MediaPlaybackItem.CanSkip");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Media.Playback.MediaBreakSchedule BreakSchedule
		{
			get
			{
				throw new global::System.NotImplementedException("The member MediaBreakSchedule MediaPlaybackItem.BreakSchedule is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::System.TimeSpan? DurationLimit
		{
			get
			{
				throw new global::System.NotImplementedException("The member TimeSpan? MediaPlaybackItem.DurationLimit is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::System.TimeSpan StartTime
		{
			get
			{
				throw new global::System.NotImplementedException("The member TimeSpan MediaPlaybackItem.StartTime is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  bool IsDisabledInPlaybackList
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool MediaPlaybackItem.IsDisabledInPlaybackList is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Playback.MediaPlaybackItem", "bool MediaPlaybackItem.IsDisabledInPlaybackList");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Media.Playback.AutoLoadedDisplayPropertyKind AutoLoadedDisplayProperties
		{
			get
			{
				throw new global::System.NotImplementedException("The member AutoLoadedDisplayPropertyKind MediaPlaybackItem.AutoLoadedDisplayProperties is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Playback.MediaPlaybackItem", "AutoLoadedDisplayPropertyKind MediaPlaybackItem.AutoLoadedDisplayProperties");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  double TotalDownloadProgress
		{
			get
			{
				throw new global::System.NotImplementedException("The member double MediaPlaybackItem.TotalDownloadProgress is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public MediaPlaybackItem( global::Windows.Media.Core.MediaSource source,  global::System.TimeSpan startTime) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Playback.MediaPlaybackItem", "MediaPlaybackItem.MediaPlaybackItem(MediaSource source, TimeSpan startTime)");
		}
		#endif
		// Forced skipping of method Windows.Media.Playback.MediaPlaybackItem.MediaPlaybackItem(Windows.Media.Core.MediaSource, System.TimeSpan)
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public MediaPlaybackItem( global::Windows.Media.Core.MediaSource source,  global::System.TimeSpan startTime,  global::System.TimeSpan durationLimit) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Playback.MediaPlaybackItem", "MediaPlaybackItem.MediaPlaybackItem(MediaSource source, TimeSpan startTime, TimeSpan durationLimit)");
		}
		#endif
		// Forced skipping of method Windows.Media.Playback.MediaPlaybackItem.MediaPlaybackItem(Windows.Media.Core.MediaSource, System.TimeSpan, System.TimeSpan)
		#if false || false || NET461 || __WASM__ || false
		[global::Uno.NotImplemented]
		public MediaPlaybackItem( global::Windows.Media.Core.MediaSource source) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Playback.MediaPlaybackItem", "MediaPlaybackItem.MediaPlaybackItem(MediaSource source)");
		}
		#endif
		// Forced skipping of method Windows.Media.Playback.MediaPlaybackItem.MediaPlaybackItem(Windows.Media.Core.MediaSource)
		// Forced skipping of method Windows.Media.Playback.MediaPlaybackItem.AudioTracksChanged.add
		// Forced skipping of method Windows.Media.Playback.MediaPlaybackItem.AudioTracksChanged.remove
		// Forced skipping of method Windows.Media.Playback.MediaPlaybackItem.VideoTracksChanged.add
		// Forced skipping of method Windows.Media.Playback.MediaPlaybackItem.VideoTracksChanged.remove
		// Forced skipping of method Windows.Media.Playback.MediaPlaybackItem.TimedMetadataTracksChanged.add
		// Forced skipping of method Windows.Media.Playback.MediaPlaybackItem.TimedMetadataTracksChanged.remove
		// Forced skipping of method Windows.Media.Playback.MediaPlaybackItem.Source.get
		// Forced skipping of method Windows.Media.Playback.MediaPlaybackItem.AudioTracks.get
		// Forced skipping of method Windows.Media.Playback.MediaPlaybackItem.VideoTracks.get
		// Forced skipping of method Windows.Media.Playback.MediaPlaybackItem.TimedMetadataTracks.get
		// Forced skipping of method Windows.Media.Playback.MediaPlaybackItem.BreakSchedule.get
		// Forced skipping of method Windows.Media.Playback.MediaPlaybackItem.StartTime.get
		// Forced skipping of method Windows.Media.Playback.MediaPlaybackItem.DurationLimit.get
		// Forced skipping of method Windows.Media.Playback.MediaPlaybackItem.CanSkip.get
		// Forced skipping of method Windows.Media.Playback.MediaPlaybackItem.CanSkip.set
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Media.Playback.MediaItemDisplayProperties GetDisplayProperties()
		{
			throw new global::System.NotImplementedException("The member MediaItemDisplayProperties MediaPlaybackItem.GetDisplayProperties() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  void ApplyDisplayProperties( global::Windows.Media.Playback.MediaItemDisplayProperties value)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Playback.MediaPlaybackItem", "void MediaPlaybackItem.ApplyDisplayProperties(MediaItemDisplayProperties value)");
		}
		#endif
		// Forced skipping of method Windows.Media.Playback.MediaPlaybackItem.IsDisabledInPlaybackList.get
		// Forced skipping of method Windows.Media.Playback.MediaPlaybackItem.IsDisabledInPlaybackList.set
		// Forced skipping of method Windows.Media.Playback.MediaPlaybackItem.TotalDownloadProgress.get
		// Forced skipping of method Windows.Media.Playback.MediaPlaybackItem.AutoLoadedDisplayProperties.get
		// Forced skipping of method Windows.Media.Playback.MediaPlaybackItem.AutoLoadedDisplayProperties.set
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.Media.Playback.MediaPlaybackItem FindFromMediaSource( global::Windows.Media.Core.MediaSource source)
		{
			throw new global::System.NotImplementedException("The member MediaPlaybackItem MediaPlaybackItem.FindFromMediaSource(MediaSource source) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Media.Playback.MediaPlaybackItem, global::Windows.Foundation.Collections.IVectorChangedEventArgs> AudioTracksChanged
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Playback.MediaPlaybackItem", "event TypedEventHandler<MediaPlaybackItem, IVectorChangedEventArgs> MediaPlaybackItem.AudioTracksChanged");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Playback.MediaPlaybackItem", "event TypedEventHandler<MediaPlaybackItem, IVectorChangedEventArgs> MediaPlaybackItem.AudioTracksChanged");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Media.Playback.MediaPlaybackItem, global::Windows.Foundation.Collections.IVectorChangedEventArgs> TimedMetadataTracksChanged
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Playback.MediaPlaybackItem", "event TypedEventHandler<MediaPlaybackItem, IVectorChangedEventArgs> MediaPlaybackItem.TimedMetadataTracksChanged");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Playback.MediaPlaybackItem", "event TypedEventHandler<MediaPlaybackItem, IVectorChangedEventArgs> MediaPlaybackItem.TimedMetadataTracksChanged");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Media.Playback.MediaPlaybackItem, global::Windows.Foundation.Collections.IVectorChangedEventArgs> VideoTracksChanged
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Playback.MediaPlaybackItem", "event TypedEventHandler<MediaPlaybackItem, IVectorChangedEventArgs> MediaPlaybackItem.VideoTracksChanged");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Playback.MediaPlaybackItem", "event TypedEventHandler<MediaPlaybackItem, IVectorChangedEventArgs> MediaPlaybackItem.VideoTracksChanged");
			}
		}
		#endif
		// Processing: Windows.Media.Playback.IMediaPlaybackSource
	}
}

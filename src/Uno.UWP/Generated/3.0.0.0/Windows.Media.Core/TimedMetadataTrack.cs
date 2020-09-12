#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Core
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class TimedMetadataTrack : global::Windows.Media.Core.IMediaTrack
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string Label
		{
			get
			{
				throw new global::System.NotImplementedException("The member string TimedMetadataTrack.Label is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Core.TimedMetadataTrack", "string TimedMetadataTrack.Label");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string Id
		{
			get
			{
				throw new global::System.NotImplementedException("The member string TimedMetadataTrack.Id is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string Language
		{
			get
			{
				throw new global::System.NotImplementedException("The member string TimedMetadataTrack.Language is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.Core.MediaTrackKind TrackKind
		{
			get
			{
				throw new global::System.NotImplementedException("The member MediaTrackKind TimedMetadataTrack.TrackKind is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<global::Windows.Media.Core.IMediaCue> ActiveCues
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyList<IMediaCue> TimedMetadataTrack.ActiveCues is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<global::Windows.Media.Core.IMediaCue> Cues
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyList<IMediaCue> TimedMetadataTrack.Cues is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string DispatchType
		{
			get
			{
				throw new global::System.NotImplementedException("The member string TimedMetadataTrack.DispatchType is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.Core.TimedMetadataKind TimedMetadataKind
		{
			get
			{
				throw new global::System.NotImplementedException("The member TimedMetadataKind TimedMetadataTrack.TimedMetadataKind is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string Name
		{
			get
			{
				throw new global::System.NotImplementedException("The member string TimedMetadataTrack.Name is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.Playback.MediaPlaybackItem PlaybackItem
		{
			get
			{
				throw new global::System.NotImplementedException("The member MediaPlaybackItem TimedMetadataTrack.PlaybackItem is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public TimedMetadataTrack( string id,  string language,  global::Windows.Media.Core.TimedMetadataKind kind) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Core.TimedMetadataTrack", "TimedMetadataTrack.TimedMetadataTrack(string id, string language, TimedMetadataKind kind)");
		}
		#endif
		// Forced skipping of method Windows.Media.Core.TimedMetadataTrack.TimedMetadataTrack(string, string, Windows.Media.Core.TimedMetadataKind)
		// Forced skipping of method Windows.Media.Core.TimedMetadataTrack.CueEntered.add
		// Forced skipping of method Windows.Media.Core.TimedMetadataTrack.CueEntered.remove
		// Forced skipping of method Windows.Media.Core.TimedMetadataTrack.CueExited.add
		// Forced skipping of method Windows.Media.Core.TimedMetadataTrack.CueExited.remove
		// Forced skipping of method Windows.Media.Core.TimedMetadataTrack.TrackFailed.add
		// Forced skipping of method Windows.Media.Core.TimedMetadataTrack.TrackFailed.remove
		// Forced skipping of method Windows.Media.Core.TimedMetadataTrack.Cues.get
		// Forced skipping of method Windows.Media.Core.TimedMetadataTrack.ActiveCues.get
		// Forced skipping of method Windows.Media.Core.TimedMetadataTrack.TimedMetadataKind.get
		// Forced skipping of method Windows.Media.Core.TimedMetadataTrack.DispatchType.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void AddCue( global::Windows.Media.Core.IMediaCue cue)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Core.TimedMetadataTrack", "void TimedMetadataTrack.AddCue(IMediaCue cue)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void RemoveCue( global::Windows.Media.Core.IMediaCue cue)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Core.TimedMetadataTrack", "void TimedMetadataTrack.RemoveCue(IMediaCue cue)");
		}
		#endif
		// Forced skipping of method Windows.Media.Core.TimedMetadataTrack.Id.get
		// Forced skipping of method Windows.Media.Core.TimedMetadataTrack.Language.get
		// Forced skipping of method Windows.Media.Core.TimedMetadataTrack.TrackKind.get
		// Forced skipping of method Windows.Media.Core.TimedMetadataTrack.Label.set
		// Forced skipping of method Windows.Media.Core.TimedMetadataTrack.Label.get
		// Forced skipping of method Windows.Media.Core.TimedMetadataTrack.PlaybackItem.get
		// Forced skipping of method Windows.Media.Core.TimedMetadataTrack.Name.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Media.Core.TimedMetadataTrack, global::Windows.Media.Core.MediaCueEventArgs> CueEntered
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Core.TimedMetadataTrack", "event TypedEventHandler<TimedMetadataTrack, MediaCueEventArgs> TimedMetadataTrack.CueEntered");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Core.TimedMetadataTrack", "event TypedEventHandler<TimedMetadataTrack, MediaCueEventArgs> TimedMetadataTrack.CueEntered");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Media.Core.TimedMetadataTrack, global::Windows.Media.Core.MediaCueEventArgs> CueExited
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Core.TimedMetadataTrack", "event TypedEventHandler<TimedMetadataTrack, MediaCueEventArgs> TimedMetadataTrack.CueExited");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Core.TimedMetadataTrack", "event TypedEventHandler<TimedMetadataTrack, MediaCueEventArgs> TimedMetadataTrack.CueExited");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Media.Core.TimedMetadataTrack, global::Windows.Media.Core.TimedMetadataTrackFailedEventArgs> TrackFailed
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Core.TimedMetadataTrack", "event TypedEventHandler<TimedMetadataTrack, TimedMetadataTrackFailedEventArgs> TimedMetadataTrack.TrackFailed");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Core.TimedMetadataTrack", "event TypedEventHandler<TimedMetadataTrack, TimedMetadataTrackFailedEventArgs> TimedMetadataTrack.TrackFailed");
			}
		}
		#endif
		// Processing: Windows.Media.Core.IMediaTrack
	}
}

#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Core
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class AudioTrack : global::Windows.Media.Core.IMediaTrack
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string Name
		{
			get
			{
				throw new global::System.NotImplementedException("The member string AudioTrack.Name is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.Playback.MediaPlaybackItem PlaybackItem
		{
			get
			{
				throw new global::System.NotImplementedException("The member MediaPlaybackItem AudioTrack.PlaybackItem is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.Core.AudioTrackSupportInfo SupportInfo
		{
			get
			{
				throw new global::System.NotImplementedException("The member AudioTrackSupportInfo AudioTrack.SupportInfo is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string Label
		{
			get
			{
				throw new global::System.NotImplementedException("The member string AudioTrack.Label is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Core.AudioTrack", "string AudioTrack.Label");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string Id
		{
			get
			{
				throw new global::System.NotImplementedException("The member string AudioTrack.Id is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string Language
		{
			get
			{
				throw new global::System.NotImplementedException("The member string AudioTrack.Language is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.Core.MediaTrackKind TrackKind
		{
			get
			{
				throw new global::System.NotImplementedException("The member MediaTrackKind AudioTrack.TrackKind is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Media.Core.AudioTrack.Id.get
		// Forced skipping of method Windows.Media.Core.AudioTrack.Language.get
		// Forced skipping of method Windows.Media.Core.AudioTrack.TrackKind.get
		// Forced skipping of method Windows.Media.Core.AudioTrack.Label.set
		// Forced skipping of method Windows.Media.Core.AudioTrack.Label.get
		// Forced skipping of method Windows.Media.Core.AudioTrack.OpenFailed.add
		// Forced skipping of method Windows.Media.Core.AudioTrack.OpenFailed.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.MediaProperties.AudioEncodingProperties GetEncodingProperties()
		{
			throw new global::System.NotImplementedException("The member AudioEncodingProperties AudioTrack.GetEncodingProperties() is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.Media.Core.AudioTrack.PlaybackItem.get
		// Forced skipping of method Windows.Media.Core.AudioTrack.Name.get
		// Forced skipping of method Windows.Media.Core.AudioTrack.SupportInfo.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Media.Core.AudioTrack, global::Windows.Media.Core.AudioTrackOpenFailedEventArgs> OpenFailed
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Core.AudioTrack", "event TypedEventHandler<AudioTrack, AudioTrackOpenFailedEventArgs> AudioTrack.OpenFailed");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Core.AudioTrack", "event TypedEventHandler<AudioTrack, AudioTrackOpenFailedEventArgs> AudioTrack.OpenFailed");
			}
		}
		#endif
		// Processing: Windows.Media.Core.IMediaTrack
	}
}

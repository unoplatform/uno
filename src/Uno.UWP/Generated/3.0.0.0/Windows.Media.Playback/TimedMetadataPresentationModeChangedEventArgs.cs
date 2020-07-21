#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Playback
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class TimedMetadataPresentationModeChangedEventArgs 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.Playback.TimedMetadataTrackPresentationMode NewPresentationMode
		{
			get
			{
				throw new global::System.NotImplementedException("The member TimedMetadataTrackPresentationMode TimedMetadataPresentationModeChangedEventArgs.NewPresentationMode is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.Playback.TimedMetadataTrackPresentationMode OldPresentationMode
		{
			get
			{
				throw new global::System.NotImplementedException("The member TimedMetadataTrackPresentationMode TimedMetadataPresentationModeChangedEventArgs.OldPresentationMode is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.Core.TimedMetadataTrack Track
		{
			get
			{
				throw new global::System.NotImplementedException("The member TimedMetadataTrack TimedMetadataPresentationModeChangedEventArgs.Track is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Media.Playback.TimedMetadataPresentationModeChangedEventArgs.Track.get
		// Forced skipping of method Windows.Media.Playback.TimedMetadataPresentationModeChangedEventArgs.OldPresentationMode.get
		// Forced skipping of method Windows.Media.Playback.TimedMetadataPresentationModeChangedEventArgs.NewPresentationMode.get
	}
}

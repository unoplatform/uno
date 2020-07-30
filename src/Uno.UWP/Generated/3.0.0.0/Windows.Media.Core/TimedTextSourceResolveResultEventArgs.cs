#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Core
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class TimedTextSourceResolveResultEventArgs 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.Core.TimedMetadataTrackError Error
		{
			get
			{
				throw new global::System.NotImplementedException("The member TimedMetadataTrackError TimedTextSourceResolveResultEventArgs.Error is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<global::Windows.Media.Core.TimedMetadataTrack> Tracks
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyList<TimedMetadataTrack> TimedTextSourceResolveResultEventArgs.Tracks is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Media.Core.TimedTextSourceResolveResultEventArgs.Error.get
		// Forced skipping of method Windows.Media.Core.TimedTextSourceResolveResultEventArgs.Tracks.get
	}
}

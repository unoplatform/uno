#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Core
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial interface ITimedMetadataTrackProvider 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::System.Collections.Generic.IReadOnlyList<global::Windows.Media.Core.TimedMetadataTrack> TimedMetadataTracks
		{
			get;
		}
		#endif
		// Forced skipping of method Windows.Media.Core.ITimedMetadataTrackProvider.TimedMetadataTracks.get
	}
}

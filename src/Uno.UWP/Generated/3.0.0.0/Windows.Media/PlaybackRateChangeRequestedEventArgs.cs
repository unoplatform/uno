#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class PlaybackRateChangeRequestedEventArgs 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  double RequestedPlaybackRate
		{
			get
			{
				throw new global::System.NotImplementedException("The member double PlaybackRateChangeRequestedEventArgs.RequestedPlaybackRate is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=double%20PlaybackRateChangeRequestedEventArgs.RequestedPlaybackRate");
			}
		}
		#endif
		// Forced skipping of method Windows.Media.PlaybackRateChangeRequestedEventArgs.RequestedPlaybackRate.get
	}
}

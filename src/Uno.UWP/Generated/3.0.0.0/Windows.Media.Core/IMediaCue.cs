#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Core
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial interface IMediaCue 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::System.TimeSpan Duration
		{
			get;
			set;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		string Id
		{
			get;
			set;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::System.TimeSpan StartTime
		{
			get;
			set;
		}
		#endif
		// Forced skipping of method Windows.Media.Core.IMediaCue.StartTime.set
		// Forced skipping of method Windows.Media.Core.IMediaCue.StartTime.get
		// Forced skipping of method Windows.Media.Core.IMediaCue.Duration.set
		// Forced skipping of method Windows.Media.Core.IMediaCue.Duration.get
		// Forced skipping of method Windows.Media.Core.IMediaCue.Id.set
		// Forced skipping of method Windows.Media.Core.IMediaCue.Id.get
	}
}

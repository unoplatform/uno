#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Core
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public  partial interface IMediaTrack 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		string Id
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		string Label
		{
			get;
			set;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		string Language
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		global::Windows.Media.Core.MediaTrackKind TrackKind
		{
			get;
		}
		#endif
		// Forced skipping of method Windows.Media.Core.IMediaTrack.Id.get
		// Forced skipping of method Windows.Media.Core.IMediaTrack.Language.get
		// Forced skipping of method Windows.Media.Core.IMediaTrack.TrackKind.get
		// Forced skipping of method Windows.Media.Core.IMediaTrack.Label.set
		// Forced skipping of method Windows.Media.Core.IMediaTrack.Label.get
	}
}

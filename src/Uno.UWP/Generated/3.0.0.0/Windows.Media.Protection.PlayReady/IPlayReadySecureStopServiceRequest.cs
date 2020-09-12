#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Protection.PlayReady
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial interface IPlayReadySecureStopServiceRequest : global::Windows.Media.Protection.PlayReady.IPlayReadyServiceRequest,global::Windows.Media.Protection.IMediaProtectionServiceRequest
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		byte[] PublisherCertificate
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::System.Guid SessionID
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::System.DateTimeOffset StartTime
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		bool Stopped
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::System.DateTimeOffset UpdateTime
		{
			get;
		}
		#endif
		// Forced skipping of method Windows.Media.Protection.PlayReady.IPlayReadySecureStopServiceRequest.SessionID.get
		// Forced skipping of method Windows.Media.Protection.PlayReady.IPlayReadySecureStopServiceRequest.StartTime.get
		// Forced skipping of method Windows.Media.Protection.PlayReady.IPlayReadySecureStopServiceRequest.UpdateTime.get
		// Forced skipping of method Windows.Media.Protection.PlayReady.IPlayReadySecureStopServiceRequest.Stopped.get
		// Forced skipping of method Windows.Media.Protection.PlayReady.IPlayReadySecureStopServiceRequest.PublisherCertificate.get
	}
}

#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Protection.PlayReady
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial interface IPlayReadyServiceRequest : global::Windows.Media.Protection.IMediaProtectionServiceRequest
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		string ChallengeCustomData
		{
			get;
			set;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		string ResponseCustomData
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::System.Uri Uri
		{
			get;
			set;
		}
		#endif
		// Forced skipping of method Windows.Media.Protection.PlayReady.IPlayReadyServiceRequest.Uri.get
		// Forced skipping of method Windows.Media.Protection.PlayReady.IPlayReadyServiceRequest.Uri.set
		// Forced skipping of method Windows.Media.Protection.PlayReady.IPlayReadyServiceRequest.ResponseCustomData.get
		// Forced skipping of method Windows.Media.Protection.PlayReady.IPlayReadyServiceRequest.ChallengeCustomData.get
		// Forced skipping of method Windows.Media.Protection.PlayReady.IPlayReadyServiceRequest.ChallengeCustomData.set
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.Foundation.IAsyncAction BeginServiceRequest();
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.Media.Protection.PlayReady.IPlayReadyServiceRequest NextServiceRequest();
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.Media.Protection.PlayReady.PlayReadySoapMessage GenerateManualEnablingChallenge();
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::System.Exception ProcessManualEnablingResponse( byte[] responseBytes);
		#endif
	}
}

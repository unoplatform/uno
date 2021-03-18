#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Protection.PlayReady
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial interface INDMessenger 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.Foundation.IAsyncOperation<global::Windows.Media.Protection.PlayReady.INDSendResult> SendRegistrationRequestAsync( byte[] sessionIDBytes,  byte[] challengeDataBytes);
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.Foundation.IAsyncOperation<global::Windows.Media.Protection.PlayReady.INDSendResult> SendProximityDetectionStartAsync( global::Windows.Media.Protection.PlayReady.NDProximityDetectionType pdType,  byte[] transmitterChannelBytes,  byte[] sessionIDBytes,  byte[] challengeDataBytes);
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.Foundation.IAsyncOperation<global::Windows.Media.Protection.PlayReady.INDSendResult> SendProximityDetectionResponseAsync( global::Windows.Media.Protection.PlayReady.NDProximityDetectionType pdType,  byte[] transmitterChannelBytes,  byte[] sessionIDBytes,  byte[] responseDataBytes);
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.Foundation.IAsyncOperation<global::Windows.Media.Protection.PlayReady.INDSendResult> SendLicenseFetchRequestAsync( byte[] sessionIDBytes,  byte[] challengeDataBytes);
		#endif
	}
}

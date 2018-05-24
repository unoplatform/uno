#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Protection.PlayReady
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public  partial interface INDDownloadEngineNotifier 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		void OnStreamOpened();
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		void OnPlayReadyObjectReceived( byte[] dataBytes);
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		void OnContentIDReceived( global::Windows.Media.Protection.PlayReady.INDLicenseFetchDescriptor licenseFetchDescriptor);
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		void OnDataReceived( byte[] dataBytes,  uint bytesReceived);
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		void OnEndOfStream();
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		void OnNetworkError();
		#endif
	}
}

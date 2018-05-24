#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.System.RemoteSystems
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum RemoteSystemSessionDisconnectedReason 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		SessionUnavailable,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		RemovedByController,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		SessionClosed,
		#endif
	}
	#endif
}

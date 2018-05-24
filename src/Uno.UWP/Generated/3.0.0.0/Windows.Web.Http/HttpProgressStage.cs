#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Web.Http
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum HttpProgressStage 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		None,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		DetectingProxy,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ResolvingName,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ConnectingToServer,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NegotiatingSsl,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		SendingHeaders,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		SendingContent,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		WaitingForResponse,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ReceivingHeaders,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ReceivingContent,
		#endif
	}
	#endif
}

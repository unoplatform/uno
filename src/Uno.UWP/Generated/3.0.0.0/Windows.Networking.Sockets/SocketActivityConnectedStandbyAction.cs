#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Networking.Sockets
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum SocketActivityConnectedStandbyAction 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		DoNotWake,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Wake,
		#endif
	}
	#endif
}

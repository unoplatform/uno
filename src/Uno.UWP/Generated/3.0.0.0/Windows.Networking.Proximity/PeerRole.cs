#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Networking.Proximity
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum PeerRole 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Peer,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Host,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Client,
		#endif
	}
	#endif
}

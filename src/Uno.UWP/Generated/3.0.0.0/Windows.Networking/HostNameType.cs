#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Networking
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum HostNameType 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		DomainName,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Ipv4,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Ipv6,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Bluetooth,
		#endif
	}
	#endif
}

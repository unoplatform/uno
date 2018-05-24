#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Networking.Vpn
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum VpnIPProtocol 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		None,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Tcp,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Udp,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Icmp,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Ipv6Icmp,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Igmp,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Pgm,
		#endif
	}
	#endif
}

#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Networking.ServiceDiscovery.Dnssd
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum DnssdRegistrationStatus 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Success,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		InvalidServiceName,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ServerError,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		SecurityError,
		#endif
	}
	#endif
}

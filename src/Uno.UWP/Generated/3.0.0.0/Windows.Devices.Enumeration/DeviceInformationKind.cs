#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Enumeration
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum DeviceInformationKind 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Unknown,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		DeviceInterface,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		DeviceContainer,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Device,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		DeviceInterfaceClass,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		AssociationEndpoint,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		AssociationEndpointContainer,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		AssociationEndpointService,
		#endif
	}
	#endif
}

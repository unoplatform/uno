#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Networking.Sockets
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum ControlChannelTriggerStatus 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		HardwareSlotRequested,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		SoftwareSlotAllocated,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		HardwareSlotAllocated,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		PolicyError,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		SystemError,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		TransportDisconnected,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ServiceUnavailable,
		#endif
	}
	#endif
}

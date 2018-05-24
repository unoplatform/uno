#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.AllJoyn
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum AllJoynSessionLostReason 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		None,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ProducerLeftSession,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ProducerClosedAbruptly,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		RemovedByProducer,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		LinkTimeout,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Other,
		#endif
	}
	#endif
}

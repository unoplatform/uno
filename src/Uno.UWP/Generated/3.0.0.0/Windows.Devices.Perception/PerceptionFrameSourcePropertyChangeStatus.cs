#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Perception
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum PerceptionFrameSourcePropertyChangeStatus 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Unknown,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Accepted,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		LostControl,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		PropertyNotSupported,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		PropertyReadOnly,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ValueOutOfRange,
		#endif
	}
	#endif
}

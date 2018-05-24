#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.AllJoyn
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum AllJoynTrafficType 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Unknown,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Messages,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		RawUnreliable,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		RawReliable,
		#endif
	}
	#endif
}

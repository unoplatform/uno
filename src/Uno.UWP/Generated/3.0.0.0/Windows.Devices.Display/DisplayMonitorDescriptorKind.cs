#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Display
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum DisplayMonitorDescriptorKind 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Edid,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		DisplayId,
		#endif
	}
	#endif
}

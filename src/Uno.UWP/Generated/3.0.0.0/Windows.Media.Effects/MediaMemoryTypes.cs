#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Effects
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum MediaMemoryTypes 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Gpu,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Cpu,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		GpuAndCpu,
		#endif
	}
	#endif
}

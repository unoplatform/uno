#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.System
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum ProcessorArchitecture 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		X86,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Arm,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		X64,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Neutral,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Unknown,
		#endif
	}
	#endif
}

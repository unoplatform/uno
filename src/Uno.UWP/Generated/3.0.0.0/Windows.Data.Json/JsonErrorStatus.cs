#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Data.Json
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum JsonErrorStatus 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Unknown,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		InvalidJsonString,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		InvalidJsonNumber,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		JsonValueNotFound,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ImplementationLimit,
		#endif
	}
	#endif
}

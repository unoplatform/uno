#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Data.Json
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum JsonValueType 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Null,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Boolean,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Number,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		String,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Array,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Object,
		#endif
	}
	#endif
}

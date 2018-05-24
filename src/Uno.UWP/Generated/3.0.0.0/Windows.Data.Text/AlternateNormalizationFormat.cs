#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Data.Text
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum AlternateNormalizationFormat 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NotNormalized,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Number,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Currency,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Date,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Time,
		#endif
	}
	#endif
}

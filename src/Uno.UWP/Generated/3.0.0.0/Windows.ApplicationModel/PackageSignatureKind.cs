#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum PackageSignatureKind 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		None,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Developer,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Enterprise,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Store,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		System,
		#endif
	}
	#endif
}

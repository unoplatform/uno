#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Store
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum ProductPurchaseStatus 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Succeeded,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		AlreadyPurchased,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NotFulfilled,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NotPurchased,
		#endif
	}
	#endif
}

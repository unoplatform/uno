#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Store
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum FulfillmentResult 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Succeeded,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NothingToFulfill,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		PurchasePending,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		PurchaseReverted,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ServerError,
		#endif
	}
	#endif
}

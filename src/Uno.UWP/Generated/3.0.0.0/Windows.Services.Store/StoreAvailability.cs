#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Services.Store
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
	#endif
	public  partial class StoreAvailability 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.DateTimeOffset EndDate
		{
			get
			{
				throw new global::System.NotImplementedException("The member DateTimeOffset StoreAvailability.EndDate is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string ExtendedJsonData
		{
			get
			{
				throw new global::System.NotImplementedException("The member string StoreAvailability.ExtendedJsonData is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Services.Store.StorePrice Price
		{
			get
			{
				throw new global::System.NotImplementedException("The member StorePrice StoreAvailability.Price is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string StoreId
		{
			get
			{
				throw new global::System.NotImplementedException("The member string StoreAvailability.StoreId is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Services.Store.StoreAvailability.StoreId.get
		// Forced skipping of method Windows.Services.Store.StoreAvailability.EndDate.get
		// Forced skipping of method Windows.Services.Store.StoreAvailability.Price.get
		// Forced skipping of method Windows.Services.Store.StoreAvailability.ExtendedJsonData.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Services.Store.StorePurchaseResult> RequestPurchaseAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<StorePurchaseResult> StoreAvailability.RequestPurchaseAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Services.Store.StorePurchaseResult> RequestPurchaseAsync( global::Windows.Services.Store.StorePurchaseProperties storePurchaseProperties)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<StorePurchaseResult> StoreAvailability.RequestPurchaseAsync(StorePurchaseProperties storePurchaseProperties) is not implemented in Uno.");
		}
		#endif
	}
}

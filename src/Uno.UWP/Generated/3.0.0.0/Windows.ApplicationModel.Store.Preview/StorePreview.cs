#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Store.Preview
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class StorePreview 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.ApplicationModel.Store.Preview.StorePreviewPurchaseResults> RequestProductPurchaseByProductIdAndSkuIdAsync( string productId,  string skuId)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<StorePreviewPurchaseResults> StorePreview.RequestProductPurchaseByProductIdAndSkuIdAsync(string productId, string skuId) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::System.Collections.Generic.IReadOnlyList<global::Windows.ApplicationModel.Store.Preview.StorePreviewProductInfo>> LoadAddOnProductInfosAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<IReadOnlyList<StorePreviewProductInfo>> StorePreview.LoadAddOnProductInfosAsync() is not implemented in Uno.");
		}
		#endif
	}
}

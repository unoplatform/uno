#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Store
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class CurrentApp 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::System.Guid AppId
		{
			get
			{
				throw new global::System.NotImplementedException("The member Guid CurrentApp.AppId is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.ApplicationModel.Store.LicenseInformation LicenseInformation
		{
			get
			{
				throw new global::System.NotImplementedException("The member LicenseInformation CurrentApp.LicenseInformation is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::System.Uri LinkUri
		{
			get
			{
				throw new global::System.NotImplementedException("The member Uri CurrentApp.LinkUri is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<string> GetCustomerPurchaseIdAsync( string serviceTicket,  string publisherUserId)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<string> CurrentApp.GetCustomerPurchaseIdAsync(string serviceTicket, string publisherUserId) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<string> GetCustomerCollectionsIdAsync( string serviceTicket,  string publisherUserId)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<string> CurrentApp.GetCustomerCollectionsIdAsync(string serviceTicket, string publisherUserId) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<string> GetAppPurchaseCampaignIdAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<string> CurrentApp.GetAppPurchaseCampaignIdAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.ApplicationModel.Store.ListingInformation> LoadListingInformationByProductIdsAsync( global::System.Collections.Generic.IEnumerable<string> productIds)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<ListingInformation> CurrentApp.LoadListingInformationByProductIdsAsync(IEnumerable<string> productIds) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.ApplicationModel.Store.ListingInformation> LoadListingInformationByKeywordsAsync( global::System.Collections.Generic.IEnumerable<string> keywords)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<ListingInformation> CurrentApp.LoadListingInformationByKeywordsAsync(IEnumerable<string> keywords) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static void ReportProductFulfillment( string productId)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Store.CurrentApp", "void CurrentApp.ReportProductFulfillment(string productId)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.ApplicationModel.Store.FulfillmentResult> ReportConsumableFulfillmentAsync( string productId,  global::System.Guid transactionId)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<FulfillmentResult> CurrentApp.ReportConsumableFulfillmentAsync(string productId, Guid transactionId) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.ApplicationModel.Store.PurchaseResults> RequestProductPurchaseAsync( string productId)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<PurchaseResults> CurrentApp.RequestProductPurchaseAsync(string productId) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.ApplicationModel.Store.PurchaseResults> RequestProductPurchaseAsync( string productId,  string offerId,  global::Windows.ApplicationModel.Store.ProductPurchaseDisplayProperties displayProperties)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<PurchaseResults> CurrentApp.RequestProductPurchaseAsync(string productId, string offerId, ProductPurchaseDisplayProperties displayProperties) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::System.Collections.Generic.IReadOnlyList<global::Windows.ApplicationModel.Store.UnfulfilledConsumable>> GetUnfulfilledConsumablesAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<IReadOnlyList<UnfulfilledConsumable>> CurrentApp.GetUnfulfilledConsumablesAsync() is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.Store.CurrentApp.LicenseInformation.get
		// Forced skipping of method Windows.ApplicationModel.Store.CurrentApp.LinkUri.get
		// Forced skipping of method Windows.ApplicationModel.Store.CurrentApp.AppId.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<string> RequestAppPurchaseAsync( bool includeReceipt)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<string> CurrentApp.RequestAppPurchaseAsync(bool includeReceipt) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<string> RequestProductPurchaseAsync( string productId,  bool includeReceipt)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<string> CurrentApp.RequestProductPurchaseAsync(string productId, bool includeReceipt) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.ApplicationModel.Store.ListingInformation> LoadListingInformationAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<ListingInformation> CurrentApp.LoadListingInformationAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<string> GetAppReceiptAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<string> CurrentApp.GetAppReceiptAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<string> GetProductReceiptAsync( string productId)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<string> CurrentApp.GetProductReceiptAsync(string productId) is not implemented in Uno.");
		}
		#endif
	}
}

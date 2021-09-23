#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Services.Store
{
	#if false || false || false || false || false
	[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
	#endif
	public  partial class StoreContext 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.System.User User
		{
			get
			{
				throw new global::System.NotImplementedException("The member User StoreContext.User is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool CanSilentlyDownloadStorePackageUpdates
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool StoreContext.CanSilentlyDownloadStorePackageUpdates is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Services.Store.StoreContext.User.get
		// Forced skipping of method Windows.Services.Store.StoreContext.OfflineLicensesChanged.add
		// Forced skipping of method Windows.Services.Store.StoreContext.OfflineLicensesChanged.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<string> GetCustomerPurchaseIdAsync( string serviceTicket,  string publisherUserId)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<string> StoreContext.GetCustomerPurchaseIdAsync(string serviceTicket, string publisherUserId) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<string> GetCustomerCollectionsIdAsync( string serviceTicket,  string publisherUserId)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<string> StoreContext.GetCustomerCollectionsIdAsync(string serviceTicket, string publisherUserId) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Services.Store.StoreAppLicense> GetAppLicenseAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<StoreAppLicense> StoreContext.GetAppLicenseAsync() is not implemented in Uno.");
		}
		#endif
		#if false || false || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Services.Store.StoreProductResult> GetStoreProductForCurrentAppAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<StoreProductResult> StoreContext.GetStoreProductForCurrentAppAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Services.Store.StoreProductQueryResult> GetStoreProductsAsync( global::System.Collections.Generic.IEnumerable<string> productKinds,  global::System.Collections.Generic.IEnumerable<string> storeIds)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<StoreProductQueryResult> StoreContext.GetStoreProductsAsync(IEnumerable<string> productKinds, IEnumerable<string> storeIds) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Services.Store.StoreProductQueryResult> GetAssociatedStoreProductsAsync( global::System.Collections.Generic.IEnumerable<string> productKinds)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<StoreProductQueryResult> StoreContext.GetAssociatedStoreProductsAsync(IEnumerable<string> productKinds) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Services.Store.StoreProductPagedQueryResult> GetAssociatedStoreProductsWithPagingAsync( global::System.Collections.Generic.IEnumerable<string> productKinds,  uint maxItemsToRetrievePerPage)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<StoreProductPagedQueryResult> StoreContext.GetAssociatedStoreProductsWithPagingAsync(IEnumerable<string> productKinds, uint maxItemsToRetrievePerPage) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Services.Store.StoreProductQueryResult> GetUserCollectionAsync( global::System.Collections.Generic.IEnumerable<string> productKinds)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<StoreProductQueryResult> StoreContext.GetUserCollectionAsync(IEnumerable<string> productKinds) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Services.Store.StoreProductPagedQueryResult> GetUserCollectionWithPagingAsync( global::System.Collections.Generic.IEnumerable<string> productKinds,  uint maxItemsToRetrievePerPage)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<StoreProductPagedQueryResult> StoreContext.GetUserCollectionWithPagingAsync(IEnumerable<string> productKinds, uint maxItemsToRetrievePerPage) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Services.Store.StoreConsumableResult> ReportConsumableFulfillmentAsync( string productStoreId,  uint quantity,  global::System.Guid trackingId)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<StoreConsumableResult> StoreContext.ReportConsumableFulfillmentAsync(string productStoreId, uint quantity, Guid trackingId) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Services.Store.StoreConsumableResult> GetConsumableBalanceRemainingAsync( string productStoreId)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<StoreConsumableResult> StoreContext.GetConsumableBalanceRemainingAsync(string productStoreId) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Services.Store.StoreAcquireLicenseResult> AcquireStoreLicenseForOptionalPackageAsync( global::Windows.ApplicationModel.Package optionalPackage)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<StoreAcquireLicenseResult> StoreContext.AcquireStoreLicenseForOptionalPackageAsync(Package optionalPackage) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Services.Store.StorePurchaseResult> RequestPurchaseAsync( string storeId)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<StorePurchaseResult> StoreContext.RequestPurchaseAsync(string storeId) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Services.Store.StorePurchaseResult> RequestPurchaseAsync( string storeId,  global::Windows.Services.Store.StorePurchaseProperties storePurchaseProperties)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<StorePurchaseResult> StoreContext.RequestPurchaseAsync(string storeId, StorePurchaseProperties storePurchaseProperties) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::System.Collections.Generic.IReadOnlyList<global::Windows.Services.Store.StorePackageUpdate>> GetAppAndOptionalStorePackageUpdatesAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<IReadOnlyList<StorePackageUpdate>> StoreContext.GetAppAndOptionalStorePackageUpdatesAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperationWithProgress<global::Windows.Services.Store.StorePackageUpdateResult, global::Windows.Services.Store.StorePackageUpdateStatus> RequestDownloadStorePackageUpdatesAsync( global::System.Collections.Generic.IEnumerable<global::Windows.Services.Store.StorePackageUpdate> storePackageUpdates)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperationWithProgress<StorePackageUpdateResult, StorePackageUpdateStatus> StoreContext.RequestDownloadStorePackageUpdatesAsync(IEnumerable<StorePackageUpdate> storePackageUpdates) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperationWithProgress<global::Windows.Services.Store.StorePackageUpdateResult, global::Windows.Services.Store.StorePackageUpdateStatus> RequestDownloadAndInstallStorePackageUpdatesAsync( global::System.Collections.Generic.IEnumerable<global::Windows.Services.Store.StorePackageUpdate> storePackageUpdates)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperationWithProgress<StorePackageUpdateResult, StorePackageUpdateStatus> StoreContext.RequestDownloadAndInstallStorePackageUpdatesAsync(IEnumerable<StorePackageUpdate> storePackageUpdates) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperationWithProgress<global::Windows.Services.Store.StorePackageUpdateResult, global::Windows.Services.Store.StorePackageUpdateStatus> RequestDownloadAndInstallStorePackagesAsync( global::System.Collections.Generic.IEnumerable<string> storeIds)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperationWithProgress<StorePackageUpdateResult, StorePackageUpdateStatus> StoreContext.RequestDownloadAndInstallStorePackagesAsync(IEnumerable<string> storeIds) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Services.Store.StoreProductResult> FindStoreProductForPackageAsync( global::System.Collections.Generic.IEnumerable<string> productKinds,  global::Windows.ApplicationModel.Package package)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<StoreProductResult> StoreContext.FindStoreProductForPackageAsync(IEnumerable<string> productKinds, Package package) is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.Services.Store.StoreContext.CanSilentlyDownloadStorePackageUpdates.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperationWithProgress<global::Windows.Services.Store.StorePackageUpdateResult, global::Windows.Services.Store.StorePackageUpdateStatus> TrySilentDownloadStorePackageUpdatesAsync( global::System.Collections.Generic.IEnumerable<global::Windows.Services.Store.StorePackageUpdate> storePackageUpdates)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperationWithProgress<StorePackageUpdateResult, StorePackageUpdateStatus> StoreContext.TrySilentDownloadStorePackageUpdatesAsync(IEnumerable<StorePackageUpdate> storePackageUpdates) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperationWithProgress<global::Windows.Services.Store.StorePackageUpdateResult, global::Windows.Services.Store.StorePackageUpdateStatus> TrySilentDownloadAndInstallStorePackageUpdatesAsync( global::System.Collections.Generic.IEnumerable<global::Windows.Services.Store.StorePackageUpdate> storePackageUpdates)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperationWithProgress<StorePackageUpdateResult, StorePackageUpdateStatus> StoreContext.TrySilentDownloadAndInstallStorePackageUpdatesAsync(IEnumerable<StorePackageUpdate> storePackageUpdates) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Services.Store.StoreCanAcquireLicenseResult> CanAcquireStoreLicenseForOptionalPackageAsync( global::Windows.ApplicationModel.Package optionalPackage)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<StoreCanAcquireLicenseResult> StoreContext.CanAcquireStoreLicenseForOptionalPackageAsync(Package optionalPackage) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Services.Store.StoreCanAcquireLicenseResult> CanAcquireStoreLicenseAsync( string productStoreId)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<StoreCanAcquireLicenseResult> StoreContext.CanAcquireStoreLicenseAsync(string productStoreId) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Services.Store.StoreProductQueryResult> GetStoreProductsAsync( global::System.Collections.Generic.IEnumerable<string> productKinds,  global::System.Collections.Generic.IEnumerable<string> storeIds,  global::Windows.Services.Store.StoreProductOptions storeProductOptions)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<StoreProductQueryResult> StoreContext.GetStoreProductsAsync(IEnumerable<string> productKinds, IEnumerable<string> storeIds, StoreProductOptions storeProductOptions) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::System.Collections.Generic.IReadOnlyList<global::Windows.Services.Store.StoreQueueItem>> GetAssociatedStoreQueueItemsAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<IReadOnlyList<StoreQueueItem>> StoreContext.GetAssociatedStoreQueueItemsAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::System.Collections.Generic.IReadOnlyList<global::Windows.Services.Store.StoreQueueItem>> GetStoreQueueItemsAsync( global::System.Collections.Generic.IEnumerable<string> storeIds)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<IReadOnlyList<StoreQueueItem>> StoreContext.GetStoreQueueItemsAsync(IEnumerable<string> storeIds) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperationWithProgress<global::Windows.Services.Store.StorePackageUpdateResult, global::Windows.Services.Store.StorePackageUpdateStatus> RequestDownloadAndInstallStorePackagesAsync( global::System.Collections.Generic.IEnumerable<string> storeIds,  global::Windows.Services.Store.StorePackageInstallOptions storePackageInstallOptions)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperationWithProgress<StorePackageUpdateResult, StorePackageUpdateStatus> StoreContext.RequestDownloadAndInstallStorePackagesAsync(IEnumerable<string> storeIds, StorePackageInstallOptions storePackageInstallOptions) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperationWithProgress<global::Windows.Services.Store.StorePackageUpdateResult, global::Windows.Services.Store.StorePackageUpdateStatus> DownloadAndInstallStorePackagesAsync( global::System.Collections.Generic.IEnumerable<string> storeIds)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperationWithProgress<StorePackageUpdateResult, StorePackageUpdateStatus> StoreContext.DownloadAndInstallStorePackagesAsync(IEnumerable<string> storeIds) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Services.Store.StoreUninstallStorePackageResult> RequestUninstallStorePackageAsync( global::Windows.ApplicationModel.Package package)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<StoreUninstallStorePackageResult> StoreContext.RequestUninstallStorePackageAsync(Package package) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Services.Store.StoreUninstallStorePackageResult> RequestUninstallStorePackageByStoreIdAsync( string storeId)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<StoreUninstallStorePackageResult> StoreContext.RequestUninstallStorePackageByStoreIdAsync(string storeId) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Services.Store.StoreUninstallStorePackageResult> UninstallStorePackageAsync( global::Windows.ApplicationModel.Package package)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<StoreUninstallStorePackageResult> StoreContext.UninstallStorePackageAsync(Package package) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Services.Store.StoreUninstallStorePackageResult> UninstallStorePackageByStoreIdAsync( string storeId)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<StoreUninstallStorePackageResult> StoreContext.UninstallStorePackageByStoreIdAsync(string storeId) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Services.Store.StoreRateAndReviewResult> RequestRateAndReviewAppAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<StoreRateAndReviewResult> StoreContext.RequestRateAndReviewAppAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::System.Collections.Generic.IReadOnlyList<global::Windows.Services.Store.StoreQueueItem>> SetInstallOrderForAssociatedStoreQueueItemsAsync( global::System.Collections.Generic.IEnumerable<global::Windows.Services.Store.StoreQueueItem> items)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<IReadOnlyList<StoreQueueItem>> StoreContext.SetInstallOrderForAssociatedStoreQueueItemsAsync(IEnumerable<StoreQueueItem> items) is not implemented in Uno.");
		}
		#endif
		// Skipping already declared method Windows.Services.Store.StoreContext.GetDefault()
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Services.Store.StoreContext GetForUser( global::Windows.System.User user)
		{
			throw new global::System.NotImplementedException("The member StoreContext StoreContext.GetForUser(User user) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Services.Store.StoreContext, object> OfflineLicensesChanged
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Services.Store.StoreContext", "event TypedEventHandler<StoreContext, object> StoreContext.OfflineLicensesChanged");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Services.Store.StoreContext", "event TypedEventHandler<StoreContext, object> StoreContext.OfflineLicensesChanged");
			}
		}
		#endif
	}
}

#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Services.Store
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
	#endif
	public  partial class StorePackageUpdateResult 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Services.Store.StorePackageUpdateState OverallState
		{
			get
			{
				throw new global::System.NotImplementedException("The member StorePackageUpdateState StorePackageUpdateResult.OverallState is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<global::Windows.Services.Store.StorePackageUpdateStatus> StorePackageUpdateStatuses
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyList<StorePackageUpdateStatus> StorePackageUpdateResult.StorePackageUpdateStatuses is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<global::Windows.Services.Store.StoreQueueItem> StoreQueueItems
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyList<StoreQueueItem> StorePackageUpdateResult.StoreQueueItems is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Services.Store.StorePackageUpdateResult.OverallState.get
		// Forced skipping of method Windows.Services.Store.StorePackageUpdateResult.StorePackageUpdateStatuses.get
		// Forced skipping of method Windows.Services.Store.StorePackageUpdateResult.StoreQueueItems.get
	}
}

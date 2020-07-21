#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Storage.AccessCache
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class StorageApplicationPermissions 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Storage.AccessCache.StorageItemAccessList FutureAccessList
		{
			get
			{
				throw new global::System.NotImplementedException("The member StorageItemAccessList StorageApplicationPermissions.FutureAccessList is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Storage.AccessCache.StorageItemMostRecentlyUsedList MostRecentlyUsedList
		{
			get
			{
				throw new global::System.NotImplementedException("The member StorageItemMostRecentlyUsedList StorageApplicationPermissions.MostRecentlyUsedList is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Storage.AccessCache.StorageItemAccessList GetFutureAccessListForUser( global::Windows.System.User user)
		{
			throw new global::System.NotImplementedException("The member StorageItemAccessList StorageApplicationPermissions.GetFutureAccessListForUser(User user) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Storage.AccessCache.StorageItemMostRecentlyUsedList GetMostRecentlyUsedListForUser( global::Windows.System.User user)
		{
			throw new global::System.NotImplementedException("The member StorageItemMostRecentlyUsedList StorageApplicationPermissions.GetMostRecentlyUsedListForUser(User user) is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList.get
		// Forced skipping of method Windows.Storage.AccessCache.StorageApplicationPermissions.MostRecentlyUsedList.get
	}
}

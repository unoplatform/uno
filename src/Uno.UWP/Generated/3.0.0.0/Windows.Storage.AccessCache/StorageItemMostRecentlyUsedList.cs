#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Storage.AccessCache
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class StorageItemMostRecentlyUsedList : global::Windows.Storage.AccessCache.IStorageItemAccessList
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Storage.AccessCache.AccessListEntryView Entries
		{
			get
			{
				throw new global::System.NotImplementedException("The member AccessListEntryView StorageItemMostRecentlyUsedList.Entries is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  uint MaximumItemsAllowed
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint StorageItemMostRecentlyUsedList.MaximumItemsAllowed is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Storage.AccessCache.StorageItemMostRecentlyUsedList.ItemRemoved.add
		// Forced skipping of method Windows.Storage.AccessCache.StorageItemMostRecentlyUsedList.ItemRemoved.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string Add( global::Windows.Storage.IStorageItem file)
		{
			throw new global::System.NotImplementedException("The member string StorageItemMostRecentlyUsedList.Add(IStorageItem file) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string Add( global::Windows.Storage.IStorageItem file,  string metadata)
		{
			throw new global::System.NotImplementedException("The member string StorageItemMostRecentlyUsedList.Add(IStorageItem file, string metadata) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void AddOrReplace( string token,  global::Windows.Storage.IStorageItem file)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Storage.AccessCache.StorageItemMostRecentlyUsedList", "void StorageItemMostRecentlyUsedList.AddOrReplace(string token, IStorageItem file)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void AddOrReplace( string token,  global::Windows.Storage.IStorageItem file,  string metadata)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Storage.AccessCache.StorageItemMostRecentlyUsedList", "void StorageItemMostRecentlyUsedList.AddOrReplace(string token, IStorageItem file, string metadata)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Storage.IStorageItem> GetItemAsync( string token)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<IStorageItem> StorageItemMostRecentlyUsedList.GetItemAsync(string token) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Storage.StorageFile> GetFileAsync( string token)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<StorageFile> StorageItemMostRecentlyUsedList.GetFileAsync(string token) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Storage.StorageFolder> GetFolderAsync( string token)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<StorageFolder> StorageItemMostRecentlyUsedList.GetFolderAsync(string token) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Storage.IStorageItem> GetItemAsync( string token,  global::Windows.Storage.AccessCache.AccessCacheOptions options)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<IStorageItem> StorageItemMostRecentlyUsedList.GetItemAsync(string token, AccessCacheOptions options) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Storage.StorageFile> GetFileAsync( string token,  global::Windows.Storage.AccessCache.AccessCacheOptions options)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<StorageFile> StorageItemMostRecentlyUsedList.GetFileAsync(string token, AccessCacheOptions options) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Storage.StorageFolder> GetFolderAsync( string token,  global::Windows.Storage.AccessCache.AccessCacheOptions options)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<StorageFolder> StorageItemMostRecentlyUsedList.GetFolderAsync(string token, AccessCacheOptions options) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void Remove( string token)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Storage.AccessCache.StorageItemMostRecentlyUsedList", "void StorageItemMostRecentlyUsedList.Remove(string token)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool ContainsItem( string token)
		{
			throw new global::System.NotImplementedException("The member bool StorageItemMostRecentlyUsedList.ContainsItem(string token) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void Clear()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Storage.AccessCache.StorageItemMostRecentlyUsedList", "void StorageItemMostRecentlyUsedList.Clear()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool CheckAccess( global::Windows.Storage.IStorageItem file)
		{
			throw new global::System.NotImplementedException("The member bool StorageItemMostRecentlyUsedList.CheckAccess(IStorageItem file) is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.Storage.AccessCache.StorageItemMostRecentlyUsedList.Entries.get
		// Forced skipping of method Windows.Storage.AccessCache.StorageItemMostRecentlyUsedList.MaximumItemsAllowed.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string Add( global::Windows.Storage.IStorageItem file,  string metadata,  global::Windows.Storage.AccessCache.RecentStorageItemVisibility visibility)
		{
			throw new global::System.NotImplementedException("The member string StorageItemMostRecentlyUsedList.Add(IStorageItem file, string metadata, RecentStorageItemVisibility visibility) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void AddOrReplace( string token,  global::Windows.Storage.IStorageItem file,  string metadata,  global::Windows.Storage.AccessCache.RecentStorageItemVisibility visibility)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Storage.AccessCache.StorageItemMostRecentlyUsedList", "void StorageItemMostRecentlyUsedList.AddOrReplace(string token, IStorageItem file, string metadata, RecentStorageItemVisibility visibility)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Storage.AccessCache.StorageItemMostRecentlyUsedList, global::Windows.Storage.AccessCache.ItemRemovedEventArgs> ItemRemoved
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Storage.AccessCache.StorageItemMostRecentlyUsedList", "event TypedEventHandler<StorageItemMostRecentlyUsedList, ItemRemovedEventArgs> StorageItemMostRecentlyUsedList.ItemRemoved");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Storage.AccessCache.StorageItemMostRecentlyUsedList", "event TypedEventHandler<StorageItemMostRecentlyUsedList, ItemRemovedEventArgs> StorageItemMostRecentlyUsedList.ItemRemoved");
			}
		}
		#endif
		// Processing: Windows.Storage.AccessCache.IStorageItemAccessList
	}
}

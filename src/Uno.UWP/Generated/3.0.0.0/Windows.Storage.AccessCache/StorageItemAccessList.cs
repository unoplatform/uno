#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Storage.AccessCache
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class StorageItemAccessList : global::Windows.Storage.AccessCache.IStorageItemAccessList
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Storage.AccessCache.AccessListEntryView Entries
		{
			get
			{
				throw new global::System.NotImplementedException("The member AccessListEntryView StorageItemAccessList.Entries is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  uint MaximumItemsAllowed
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint StorageItemAccessList.MaximumItemsAllowed is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string Add( global::Windows.Storage.IStorageItem file)
		{
			throw new global::System.NotImplementedException("The member string StorageItemAccessList.Add(IStorageItem file) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string Add( global::Windows.Storage.IStorageItem file,  string metadata)
		{
			throw new global::System.NotImplementedException("The member string StorageItemAccessList.Add(IStorageItem file, string metadata) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void AddOrReplace( string token,  global::Windows.Storage.IStorageItem file)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Storage.AccessCache.StorageItemAccessList", "void StorageItemAccessList.AddOrReplace(string token, IStorageItem file)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void AddOrReplace( string token,  global::Windows.Storage.IStorageItem file,  string metadata)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Storage.AccessCache.StorageItemAccessList", "void StorageItemAccessList.AddOrReplace(string token, IStorageItem file, string metadata)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Storage.IStorageItem> GetItemAsync( string token)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<IStorageItem> StorageItemAccessList.GetItemAsync(string token) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Storage.StorageFile> GetFileAsync( string token)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<StorageFile> StorageItemAccessList.GetFileAsync(string token) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Storage.StorageFolder> GetFolderAsync( string token)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<StorageFolder> StorageItemAccessList.GetFolderAsync(string token) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Storage.IStorageItem> GetItemAsync( string token,  global::Windows.Storage.AccessCache.AccessCacheOptions options)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<IStorageItem> StorageItemAccessList.GetItemAsync(string token, AccessCacheOptions options) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Storage.StorageFile> GetFileAsync( string token,  global::Windows.Storage.AccessCache.AccessCacheOptions options)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<StorageFile> StorageItemAccessList.GetFileAsync(string token, AccessCacheOptions options) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Storage.StorageFolder> GetFolderAsync( string token,  global::Windows.Storage.AccessCache.AccessCacheOptions options)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<StorageFolder> StorageItemAccessList.GetFolderAsync(string token, AccessCacheOptions options) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void Remove( string token)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Storage.AccessCache.StorageItemAccessList", "void StorageItemAccessList.Remove(string token)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool ContainsItem( string token)
		{
			throw new global::System.NotImplementedException("The member bool StorageItemAccessList.ContainsItem(string token) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void Clear()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Storage.AccessCache.StorageItemAccessList", "void StorageItemAccessList.Clear()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool CheckAccess( global::Windows.Storage.IStorageItem file)
		{
			throw new global::System.NotImplementedException("The member bool StorageItemAccessList.CheckAccess(IStorageItem file) is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.Storage.AccessCache.StorageItemAccessList.Entries.get
		// Forced skipping of method Windows.Storage.AccessCache.StorageItemAccessList.MaximumItemsAllowed.get
		// Processing: Windows.Storage.AccessCache.IStorageItemAccessList
	}
}

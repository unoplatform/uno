#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Storage
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class StorageLibrary 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.Collections.IObservableVector<global::Windows.Storage.StorageFolder> Folders
		{
			get
			{
				throw new global::System.NotImplementedException("The member IObservableVector<StorageFolder> StorageLibrary.Folders is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IObservableVector%3CStorageFolder%3E%20StorageLibrary.Folders");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Storage.StorageFolder SaveFolder
		{
			get
			{
				throw new global::System.NotImplementedException("The member StorageFolder StorageLibrary.SaveFolder is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=StorageFolder%20StorageLibrary.SaveFolder");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Storage.StorageLibraryChangeTracker ChangeTracker
		{
			get
			{
				throw new global::System.NotImplementedException("The member StorageLibraryChangeTracker StorageLibrary.ChangeTracker is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=StorageLibraryChangeTracker%20StorageLibrary.ChangeTracker");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Storage.StorageFolder> RequestAddFolderAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<StorageFolder> StorageLibrary.RequestAddFolderAsync() is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncOperation%3CStorageFolder%3E%20StorageLibrary.RequestAddFolderAsync%28%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<bool> RequestRemoveFolderAsync( global::Windows.Storage.StorageFolder folder)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<bool> StorageLibrary.RequestRemoveFolderAsync(StorageFolder folder) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncOperation%3Cbool%3E%20StorageLibrary.RequestRemoveFolderAsync%28StorageFolder%20folder%29");
		}
		#endif
		// Forced skipping of method Windows.Storage.StorageLibrary.Folders.get
		// Forced skipping of method Windows.Storage.StorageLibrary.SaveFolder.get
		// Forced skipping of method Windows.Storage.StorageLibrary.DefinitionChanged.add
		// Forced skipping of method Windows.Storage.StorageLibrary.DefinitionChanged.remove
		// Forced skipping of method Windows.Storage.StorageLibrary.ChangeTracker.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<bool> AreFolderSuggestionsAvailableAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<bool> StorageLibrary.AreFolderSuggestionsAvailableAsync() is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncOperation%3Cbool%3E%20StorageLibrary.AreFolderSuggestionsAvailableAsync%28%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.Storage.StorageLibrary> GetLibraryForUserAsync( global::Windows.System.User user,  global::Windows.Storage.KnownLibraryId libraryId)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<StorageLibrary> StorageLibrary.GetLibraryForUserAsync(User user, KnownLibraryId libraryId) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncOperation%3CStorageLibrary%3E%20StorageLibrary.GetLibraryForUserAsync%28User%20user%2C%20KnownLibraryId%20libraryId%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.Storage.StorageLibrary> GetLibraryAsync( global::Windows.Storage.KnownLibraryId libraryId)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<StorageLibrary> StorageLibrary.GetLibraryAsync(KnownLibraryId libraryId) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncOperation%3CStorageLibrary%3E%20StorageLibrary.GetLibraryAsync%28KnownLibraryId%20libraryId%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Storage.StorageLibrary, object> DefinitionChanged
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Storage.StorageLibrary", "event TypedEventHandler<StorageLibrary, object> StorageLibrary.DefinitionChanged");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Storage.StorageLibrary", "event TypedEventHandler<StorageLibrary, object> StorageLibrary.DefinitionChanged");
			}
		}
		#endif
	}
}

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
				throw new global::System.NotImplementedException("The member IObservableVector<StorageFolder> StorageLibrary.Folders is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Storage.StorageFolder SaveFolder
		{
			get
			{
				throw new global::System.NotImplementedException("The member StorageFolder StorageLibrary.SaveFolder is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Storage.StorageLibraryChangeTracker ChangeTracker
		{
			get
			{
				throw new global::System.NotImplementedException("The member StorageLibraryChangeTracker StorageLibrary.ChangeTracker is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Storage.StorageFolder> RequestAddFolderAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<StorageFolder> StorageLibrary.RequestAddFolderAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<bool> RequestRemoveFolderAsync( global::Windows.Storage.StorageFolder folder)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<bool> StorageLibrary.RequestRemoveFolderAsync(StorageFolder folder) is not implemented in Uno.");
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
			throw new global::System.NotImplementedException("The member IAsyncOperation<bool> StorageLibrary.AreFolderSuggestionsAvailableAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.Storage.StorageLibrary> GetLibraryForUserAsync( global::Windows.System.User user,  global::Windows.Storage.KnownLibraryId libraryId)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<StorageLibrary> StorageLibrary.GetLibraryForUserAsync(User user, KnownLibraryId libraryId) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.Storage.StorageLibrary> GetLibraryAsync( global::Windows.Storage.KnownLibraryId libraryId)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<StorageLibrary> StorageLibrary.GetLibraryAsync(KnownLibraryId libraryId) is not implemented in Uno.");
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

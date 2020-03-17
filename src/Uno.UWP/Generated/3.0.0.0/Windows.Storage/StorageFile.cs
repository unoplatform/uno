#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Storage
{
	#if false || false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class StorageFile : global::Windows.Storage.IStorageFile,global::Windows.Storage.Streams.IInputStreamReference,global::Windows.Storage.Streams.IRandomAccessStreamReference,global::Windows.Storage.IStorageItem,global::Windows.Storage.IStorageItemProperties,global::Windows.Storage.IStorageItemProperties2,global::Windows.Storage.IStorageItem2,global::Windows.Storage.IStorageItemPropertiesWithProvider,global::Windows.Storage.IStorageFilePropertiesWithAvailability,global::Windows.Storage.IStorageFile2
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  string ContentType
		{
			get
			{
				throw new global::System.NotImplementedException("The member string StorageFile.ContentType is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  string FileType
		{
			get
			{
				throw new global::System.NotImplementedException("The member string StorageFile.FileType is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  bool IsAvailable
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool StorageFile.IsAvailable is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Storage.FileAttributes Attributes
		{
			get
			{
				throw new global::System.NotImplementedException("The member FileAttributes StorageFile.Attributes is not implemented in Uno.");
			}
		}
		#endif
		#if false || false || false || false || false
		[global::Uno.NotImplemented]
		public  global::System.DateTimeOffset DateCreated
		{
			get
			{
				throw new global::System.NotImplementedException("The member DateTimeOffset StorageFile.DateCreated is not implemented in Uno.");
			}
		}
		#endif
		// Skipping already declared property Name
		// Skipping already declared property Path
		// Skipping already declared property DisplayName
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  string DisplayType
		{
			get
			{
				throw new global::System.NotImplementedException("The member string StorageFile.DisplayType is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  string FolderRelativeId
		{
			get
			{
				throw new global::System.NotImplementedException("The member string StorageFile.FolderRelativeId is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Storage.FileProperties.StorageItemContentProperties Properties
		{
			get
			{
				throw new global::System.NotImplementedException("The member StorageItemContentProperties StorageFile.Properties is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Storage.StorageProvider Provider
		{
			get
			{
				throw new global::System.NotImplementedException("The member StorageProvider StorageFile.Provider is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Storage.StorageFile.FileType.get
		// Forced skipping of method Windows.Storage.StorageFile.ContentType.get
		// Skipping already declared method Windows.Storage.StorageFile.OpenAsync(Windows.Storage.FileAccessMode)
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Storage.StorageStreamTransaction> OpenTransactedWriteAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<StorageStreamTransaction> StorageFile.OpenTransactedWriteAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Storage.StorageFile> CopyAsync( global::Windows.Storage.IStorageFolder destinationFolder)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<StorageFile> StorageFile.CopyAsync(IStorageFolder destinationFolder) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Storage.StorageFile> CopyAsync( global::Windows.Storage.IStorageFolder destinationFolder,  string desiredNewName)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<StorageFile> StorageFile.CopyAsync(IStorageFolder destinationFolder, string desiredNewName) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Storage.StorageFile> CopyAsync( global::Windows.Storage.IStorageFolder destinationFolder,  string desiredNewName,  global::Windows.Storage.NameCollisionOption option)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<StorageFile> StorageFile.CopyAsync(IStorageFolder destinationFolder, string desiredNewName, NameCollisionOption option) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Foundation.IAsyncAction CopyAndReplaceAsync( global::Windows.Storage.IStorageFile fileToReplace)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction StorageFile.CopyAndReplaceAsync(IStorageFile fileToReplace) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Foundation.IAsyncAction MoveAsync( global::Windows.Storage.IStorageFolder destinationFolder)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction StorageFile.MoveAsync(IStorageFolder destinationFolder) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Foundation.IAsyncAction MoveAsync( global::Windows.Storage.IStorageFolder destinationFolder,  string desiredNewName)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction StorageFile.MoveAsync(IStorageFolder destinationFolder, string desiredNewName) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Foundation.IAsyncAction MoveAsync( global::Windows.Storage.IStorageFolder destinationFolder,  string desiredNewName,  global::Windows.Storage.NameCollisionOption option)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction StorageFile.MoveAsync(IStorageFolder destinationFolder, string desiredNewName, NameCollisionOption option) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Foundation.IAsyncAction MoveAndReplaceAsync( global::Windows.Storage.IStorageFile fileToReplace)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction StorageFile.MoveAndReplaceAsync(IStorageFile fileToReplace) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Foundation.IAsyncAction RenameAsync( string desiredName)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction StorageFile.RenameAsync(string desiredName) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Foundation.IAsyncAction RenameAsync( string desiredName,  global::Windows.Storage.NameCollisionOption option)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction StorageFile.RenameAsync(string desiredName, NameCollisionOption option) is not implemented in Uno.");
		}
		#endif
		#if false || false || false || false || false
		[global::Uno.NotImplemented]
		public  global::Windows.Foundation.IAsyncAction DeleteAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncAction StorageFile.DeleteAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Foundation.IAsyncAction DeleteAsync( global::Windows.Storage.StorageDeleteOption option)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction StorageFile.DeleteAsync(StorageDeleteOption option) is not implemented in Uno.");
		}
		#endif
		#if false || false || false || false || false
		[global::Uno.NotImplemented]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Storage.FileProperties.BasicProperties> GetBasicPropertiesAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<BasicProperties> StorageFile.GetBasicPropertiesAsync() is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.Storage.StorageFile.Name.get
		// Forced skipping of method Windows.Storage.StorageFile.Path.get
		// Forced skipping of method Windows.Storage.StorageFile.Attributes.get
		// Forced skipping of method Windows.Storage.StorageFile.DateCreated.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  bool IsOfType( global::Windows.Storage.StorageItemTypes type)
		{
			throw new global::System.NotImplementedException("The member bool StorageFile.IsOfType(StorageItemTypes type) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Storage.Streams.IRandomAccessStreamWithContentType> OpenReadAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<IRandomAccessStreamWithContentType> StorageFile.OpenReadAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Storage.Streams.IInputStream> OpenSequentialReadAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<IInputStream> StorageFile.OpenSequentialReadAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Storage.FileProperties.StorageItemThumbnail> GetThumbnailAsync( global::Windows.Storage.FileProperties.ThumbnailMode mode)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<StorageItemThumbnail> StorageFile.GetThumbnailAsync(ThumbnailMode mode) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Storage.FileProperties.StorageItemThumbnail> GetThumbnailAsync( global::Windows.Storage.FileProperties.ThumbnailMode mode,  uint requestedSize)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<StorageItemThumbnail> StorageFile.GetThumbnailAsync(ThumbnailMode mode, uint requestedSize) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Storage.FileProperties.StorageItemThumbnail> GetThumbnailAsync( global::Windows.Storage.FileProperties.ThumbnailMode mode,  uint requestedSize,  global::Windows.Storage.FileProperties.ThumbnailOptions options)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<StorageItemThumbnail> StorageFile.GetThumbnailAsync(ThumbnailMode mode, uint requestedSize, ThumbnailOptions options) is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.Storage.StorageFile.DisplayName.get
		// Forced skipping of method Windows.Storage.StorageFile.DisplayType.get
		// Forced skipping of method Windows.Storage.StorageFile.FolderRelativeId.get
		// Forced skipping of method Windows.Storage.StorageFile.Properties.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Storage.FileProperties.StorageItemThumbnail> GetScaledImageAsThumbnailAsync( global::Windows.Storage.FileProperties.ThumbnailMode mode)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<StorageItemThumbnail> StorageFile.GetScaledImageAsThumbnailAsync(ThumbnailMode mode) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Storage.FileProperties.StorageItemThumbnail> GetScaledImageAsThumbnailAsync( global::Windows.Storage.FileProperties.ThumbnailMode mode,  uint requestedSize)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<StorageItemThumbnail> StorageFile.GetScaledImageAsThumbnailAsync(ThumbnailMode mode, uint requestedSize) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Storage.FileProperties.StorageItemThumbnail> GetScaledImageAsThumbnailAsync( global::Windows.Storage.FileProperties.ThumbnailMode mode,  uint requestedSize,  global::Windows.Storage.FileProperties.ThumbnailOptions options)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<StorageItemThumbnail> StorageFile.GetScaledImageAsThumbnailAsync(ThumbnailMode mode, uint requestedSize, ThumbnailOptions options) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Storage.StorageFolder> GetParentAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<StorageFolder> StorageFile.GetParentAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  bool IsEqual( global::Windows.Storage.IStorageItem item)
		{
			throw new global::System.NotImplementedException("The member bool StorageFile.IsEqual(IStorageItem item) is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.Storage.StorageFile.Provider.get
		// Forced skipping of method Windows.Storage.StorageFile.IsAvailable.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Storage.Streams.IRandomAccessStream> OpenAsync( global::Windows.Storage.FileAccessMode accessMode,  global::Windows.Storage.StorageOpenOptions options)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<IRandomAccessStream> StorageFile.OpenAsync(FileAccessMode accessMode, StorageOpenOptions options) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Storage.StorageStreamTransaction> OpenTransactedWriteAsync( global::Windows.Storage.StorageOpenOptions options)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<StorageStreamTransaction> StorageFile.OpenTransactedWriteAsync(StorageOpenOptions options) is not implemented in Uno.");
		}
		#endif
		// Skipping already declared method Windows.Storage.StorageFile.GetFileFromPathAsync(string)
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.Storage.StorageFile> GetFileFromApplicationUriAsync( global::System.Uri uri)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<StorageFile> StorageFile.GetFileFromApplicationUriAsync(Uri uri) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.Storage.StorageFile> CreateStreamedFileAsync( string displayNameWithExtension,  global::Windows.Storage.StreamedFileDataRequestedHandler dataRequested,  global::Windows.Storage.Streams.IRandomAccessStreamReference thumbnail)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<StorageFile> StorageFile.CreateStreamedFileAsync(string displayNameWithExtension, StreamedFileDataRequestedHandler dataRequested, IRandomAccessStreamReference thumbnail) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.Storage.StorageFile> ReplaceWithStreamedFileAsync( global::Windows.Storage.IStorageFile fileToReplace,  global::Windows.Storage.StreamedFileDataRequestedHandler dataRequested,  global::Windows.Storage.Streams.IRandomAccessStreamReference thumbnail)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<StorageFile> StorageFile.ReplaceWithStreamedFileAsync(IStorageFile fileToReplace, StreamedFileDataRequestedHandler dataRequested, IRandomAccessStreamReference thumbnail) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.Storage.StorageFile> CreateStreamedFileFromUriAsync( string displayNameWithExtension,  global::System.Uri uri,  global::Windows.Storage.Streams.IRandomAccessStreamReference thumbnail)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<StorageFile> StorageFile.CreateStreamedFileFromUriAsync(string displayNameWithExtension, Uri uri, IRandomAccessStreamReference thumbnail) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.Storage.StorageFile> ReplaceWithStreamedFileFromUriAsync( global::Windows.Storage.IStorageFile fileToReplace,  global::System.Uri uri,  global::Windows.Storage.Streams.IRandomAccessStreamReference thumbnail)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<StorageFile> StorageFile.ReplaceWithStreamedFileFromUriAsync(IStorageFile fileToReplace, Uri uri, IRandomAccessStreamReference thumbnail) is not implemented in Uno.");
		}
		#endif
		// Processing: Windows.Storage.IStorageFile
		// Processing: Windows.Storage.IStorageItem
		// Processing: Windows.Storage.Streams.IRandomAccessStreamReference
		// Processing: Windows.Storage.Streams.IInputStreamReference
		// Processing: Windows.Storage.IStorageItemProperties
		// Processing: Windows.Storage.IStorageItemProperties2
		// Processing: Windows.Storage.IStorageItem2
		// Processing: Windows.Storage.IStorageItemPropertiesWithProvider
		// Processing: Windows.Storage.IStorageFilePropertiesWithAvailability
		// Processing: Windows.Storage.IStorageFile2
	}
}

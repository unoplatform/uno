#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Storage
{
	#if false || false || false || false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial interface IStorageFile : global::Windows.Storage.IStorageItem,global::Windows.Storage.Streams.IRandomAccessStreamReference,global::Windows.Storage.Streams.IInputStreamReference
	{
		#if false
		string ContentType
		{
			get;
		}
		#endif
		#if false
		string FileType
		{
			get;
		}
		#endif
		// Forced skipping of method Windows.Storage.IStorageFile.FileType.get
		// Forced skipping of method Windows.Storage.IStorageFile.ContentType.get
		#if false
		global::Windows.Foundation.IAsyncOperation<global::Windows.Storage.Streams.IRandomAccessStream> OpenAsync( global::Windows.Storage.FileAccessMode accessMode);
		#endif
		#if false
		global::Windows.Foundation.IAsyncOperation<global::Windows.Storage.StorageStreamTransaction> OpenTransactedWriteAsync();
		#endif
		#if false
		global::Windows.Foundation.IAsyncOperation<global::Windows.Storage.StorageFile> CopyAsync( global::Windows.Storage.IStorageFolder destinationFolder);
		#endif
		#if false
		global::Windows.Foundation.IAsyncOperation<global::Windows.Storage.StorageFile> CopyAsync( global::Windows.Storage.IStorageFolder destinationFolder,  string desiredNewName);
		#endif
		#if false
		global::Windows.Foundation.IAsyncOperation<global::Windows.Storage.StorageFile> CopyAsync( global::Windows.Storage.IStorageFolder destinationFolder,  string desiredNewName,  global::Windows.Storage.NameCollisionOption option);
		#endif
		#if false
		global::Windows.Foundation.IAsyncAction CopyAndReplaceAsync( global::Windows.Storage.IStorageFile fileToReplace);
		#endif
		#if false
		global::Windows.Foundation.IAsyncAction MoveAsync( global::Windows.Storage.IStorageFolder destinationFolder);
		#endif
		#if false
		global::Windows.Foundation.IAsyncAction MoveAsync( global::Windows.Storage.IStorageFolder destinationFolder,  string desiredNewName);
		#endif
		#if false
		global::Windows.Foundation.IAsyncAction MoveAsync( global::Windows.Storage.IStorageFolder destinationFolder,  string desiredNewName,  global::Windows.Storage.NameCollisionOption option);
		#endif
		#if false
		global::Windows.Foundation.IAsyncAction MoveAndReplaceAsync( global::Windows.Storage.IStorageFile fileToReplace);
		#endif
	}
}

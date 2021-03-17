#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Storage
{
	#if false || false || false || false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial interface IStorageFile2 
	{
		#if false
		global::Windows.Foundation.IAsyncOperation<global::Windows.Storage.Streams.IRandomAccessStream> OpenAsync( global::Windows.Storage.FileAccessMode accessMode,  global::Windows.Storage.StorageOpenOptions options);
		#endif
		#if false
		global::Windows.Foundation.IAsyncOperation<global::Windows.Storage.StorageStreamTransaction> OpenTransactedWriteAsync( global::Windows.Storage.StorageOpenOptions options);
		#endif
	}
}

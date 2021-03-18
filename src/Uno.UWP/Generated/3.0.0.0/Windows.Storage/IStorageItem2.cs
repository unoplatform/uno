#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Storage
{
	#if false
	[global::Uno.NotImplemented]
	#endif
	public  partial interface IStorageItem2 : global::Windows.Storage.IStorageItem
	{
		#if false
		global::Windows.Foundation.IAsyncOperation<global::Windows.Storage.StorageFolder> GetParentAsync();
		#endif
		#if false
		bool IsEqual( global::Windows.Storage.IStorageItem item);
		#endif
	}
}

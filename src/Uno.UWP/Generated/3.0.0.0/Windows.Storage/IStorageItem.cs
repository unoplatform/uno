#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Storage
{
	#if false
	[global::Uno.NotImplemented]
	#endif
	public  partial interface IStorageItem 
	{
		#if false
		global::Windows.Storage.FileAttributes Attributes
		{
			get;
		}
		#endif
		#if false
		global::System.DateTimeOffset DateCreated
		{
			get;
		}
		#endif
		#if false
		string Name
		{
			get;
		}
		#endif
		#if false
		string Path
		{
			get;
		}
		#endif
		#if false
		global::Windows.Foundation.IAsyncAction RenameAsync( string desiredName);
		#endif
		#if false
		global::Windows.Foundation.IAsyncAction RenameAsync( string desiredName,  global::Windows.Storage.NameCollisionOption option);
		#endif
		#if false
		global::Windows.Foundation.IAsyncAction DeleteAsync();
		#endif
		#if false
		global::Windows.Foundation.IAsyncAction DeleteAsync( global::Windows.Storage.StorageDeleteOption option);
		#endif
		#if false
		global::Windows.Foundation.IAsyncOperation<global::Windows.Storage.FileProperties.BasicProperties> GetBasicPropertiesAsync();
		#endif
		// Forced skipping of method Windows.Storage.IStorageItem.Name.get
		// Forced skipping of method Windows.Storage.IStorageItem.Path.get
		// Forced skipping of method Windows.Storage.IStorageItem.Attributes.get
		// Forced skipping of method Windows.Storage.IStorageItem.DateCreated.get
		#if false
		bool IsOfType( global::Windows.Storage.StorageItemTypes type);
		#endif
	}
}

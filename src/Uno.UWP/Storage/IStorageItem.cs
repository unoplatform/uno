using System;
using Windows.Foundation;
using Windows.Storage.FileProperties;

namespace Windows.Storage
{
	public partial interface IStorageItem 
	{
		FileAttributes Attributes { get; }
		DateTimeOffset DateCreated { get; }
		string Name { get; }
		string Path { get; }
		IAsyncAction RenameAsync( string desiredName);
		IAsyncAction RenameAsync( string desiredName, NameCollisionOption option);
		IAsyncAction DeleteAsync();
		IAsyncAction DeleteAsync(StorageDeleteOption option);
		IAsyncOperation<BasicProperties> GetBasicPropertiesAsync();
		bool IsOfType(StorageItemTypes type);
	}
}

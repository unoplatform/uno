namespace Windows.Storage.AccessCache;

public partial class StorageItemAccessList : IStorageItemAccessList
{
	internal StorageItemAccessList()
	{
	}

	public AccessListEntryView Entries
	{
		get
		{
			throw new global::System.NotImplementedException("The member AccessListEntryView StorageItemAccessList.Entries is not implemented. For more information, visit https://aka.platform.uno/notimplemented#m=AccessListEntryView%20StorageItemAccessList.Entries");
		}
	}

	public uint MaximumItemsAllowed
	{
		get
		{
			throw new global::System.NotImplementedException("The member uint StorageItemAccessList.MaximumItemsAllowed is not implemented. For more information, visit https://aka.platform.uno/notimplemented#m=uint%20StorageItemAccessList.MaximumItemsAllowed");
		}
	}

	public string Add(global::Windows.Storage.IStorageItem file)
	{
		throw new global::System.NotImplementedException("The member string StorageItemAccessList.Add(IStorageItem file) is not implemented. For more information, visit https://aka.platform.uno/notimplemented#m=string%20StorageItemAccessList.Add%28IStorageItem%20file%29");
	}

	public string Add(global::Windows.Storage.IStorageItem file, string metadata)
	{
		throw new global::System.NotImplementedException("The member string StorageItemAccessList.Add(IStorageItem file, string metadata) is not implemented. For more information, visit https://aka.platform.uno/notimplemented#m=string%20StorageItemAccessList.Add%28IStorageItem%20file%2C%20string%20metadata%29");
	}

	public void AddOrReplace(string token, global::Windows.Storage.IStorageItem file)
	{
		global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Storage.AccessCache.StorageItemAccessList", "void StorageItemAccessList.AddOrReplace(string token, IStorageItem file)");
	}

	public void AddOrReplace(string token, global::Windows.Storage.IStorageItem file, string metadata)
	{
		global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Storage.AccessCache.StorageItemAccessList", "void StorageItemAccessList.AddOrReplace(string token, IStorageItem file, string metadata)");
	}

	public global::Windows.Foundation.IAsyncOperation<global::Windows.Storage.IStorageItem> GetItemAsync(string token)
	{
		throw new global::System.NotImplementedException("The member IAsyncOperation<IStorageItem> StorageItemAccessList.GetItemAsync(string token) is not implemented. For more information, visit https://aka.platform.uno/notimplemented#m=IAsyncOperation%3CIStorageItem%3E%20StorageItemAccessList.GetItemAsync%28string%20token%29");
	}

	public global::Windows.Foundation.IAsyncOperation<global::Windows.Storage.StorageFile> GetFileAsync(string token)
	{
		throw new global::System.NotImplementedException("The member IAsyncOperation<StorageFile> StorageItemAccessList.GetFileAsync(string token) is not implemented. For more information, visit https://aka.platform.uno/notimplemented#m=IAsyncOperation%3CStorageFile%3E%20StorageItemAccessList.GetFileAsync%28string%20token%29");
	}

	public global::Windows.Foundation.IAsyncOperation<global::Windows.Storage.StorageFolder> GetFolderAsync(string token)
	{
		throw new global::System.NotImplementedException("The member IAsyncOperation<StorageFolder> StorageItemAccessList.GetFolderAsync(string token) is not implemented. For more information, visit https://aka.platform.uno/notimplemented#m=IAsyncOperation%3CStorageFolder%3E%20StorageItemAccessList.GetFolderAsync%28string%20token%29");
	}

	public global::Windows.Foundation.IAsyncOperation<global::Windows.Storage.IStorageItem> GetItemAsync(string token, AccessCacheOptions options)
	{
		throw new global::System.NotImplementedException("The member IAsyncOperation<IStorageItem> StorageItemAccessList.GetItemAsync(string token, AccessCacheOptions options) is not implemented. For more information, visit https://aka.platform.uno/notimplemented#m=IAsyncOperation%3CIStorageItem%3E%20StorageItemAccessList.GetItemAsync%28string%20token%2C%20AccessCacheOptions%20options%29");
	}

	public global::Windows.Foundation.IAsyncOperation<global::Windows.Storage.StorageFile> GetFileAsync(string token, AccessCacheOptions options)
	{
		throw new global::System.NotImplementedException("The member IAsyncOperation<StorageFile> StorageItemAccessList.GetFileAsync(string token, AccessCacheOptions options) is not implemented. For more information, visit https://aka.platform.uno/notimplemented#m=IAsyncOperation%3CStorageFile%3E%20StorageItemAccessList.GetFileAsync%28string%20token%2C%20AccessCacheOptions%20options%29");
	}

	public global::Windows.Foundation.IAsyncOperation<global::Windows.Storage.StorageFolder> GetFolderAsync(string token, AccessCacheOptions options)
	{
		throw new global::System.NotImplementedException("The member IAsyncOperation<StorageFolder> StorageItemAccessList.GetFolderAsync(string token, AccessCacheOptions options) is not implemented. For more information, visit https://aka.platform.uno/notimplemented#m=IAsyncOperation%3CStorageFolder%3E%20StorageItemAccessList.GetFolderAsync%28string%20token%2C%20AccessCacheOptions%20options%29");
	}

	public void Remove(string token)
	{
		global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Storage.AccessCache.StorageItemAccessList", "void StorageItemAccessList.Remove(string token)");
	}

	public bool ContainsItem(string token)
	{
		throw new global::System.NotImplementedException("The member bool StorageItemAccessList.ContainsItem(string token) is not implemented. For more information, visit https://aka.platform.uno/notimplemented#m=bool%20StorageItemAccessList.ContainsItem%28string%20token%29");
	}
	public void Clear()
	{
		global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Storage.AccessCache.StorageItemAccessList", "void StorageItemAccessList.Clear()");
	}

	public bool CheckAccess(global::Windows.Storage.IStorageItem file)
	{
		throw new global::System.NotImplementedException("The member bool StorageItemAccessList.CheckAccess(IStorageItem file) is not implemented. For more information, visit https://aka.platform.uno/notimplemented#m=bool%20StorageItemAccessList.CheckAccess%28IStorageItem%20file%29");
	}
}

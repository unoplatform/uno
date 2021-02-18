using System;
using System.IO;
using Uno.Foundation;
using Windows.Foundation;

namespace Windows.Storage
{
	public partial class StorageFolder
	{
		internal static StorageFolder GetFolderFromNativePathAsync(string path, Guid guid) => new StorageFolder(new NativeFileSystem(path, guid));

		private sealed class NativeFileSystem : ImplementationBase
		{
			// Used to keep track of the Folder handle on the Typescript side.
			private Guid _id;
			private const string _jsType = "Windows.Storage.StorageFolderNative";

			public NativeFileSystem(string path, Guid id)
				: base(path)
			{
				_id = id;
			}

			public override IAsyncOperation<StorageFolder> CreateFolderAsync(string folderName, CreationCollisionOption option) =>
				AsyncOperation.FromTask(async ct =>
				{
					if (folderName.Contains("\""))
					{
						throw new FileNotFoundException("The filename, directory name, or volume label syntax is incorrect.", folderName);
					}

					var folderHandleGuidString = await WebAssemblyRuntime.InvokeAsync($"{_jsType}.CreateFolderAsync(\"{_id}\", \"{folderName}\")");

					var guid = new Guid(folderHandleGuidString);

					var storageFolder = GetFolderFromNativePathAsync(Path, guid);

					return storageFolder;
				});

			public override IAsyncOperation<StorageFolder> GetFolderAsync(string name) =>
				AsyncOperation.FromTask(async ct =>
				{
					// Handling validation
					// Source: https://docs.microsoft.com/en-us/uwp/api/windows.storage.storagefolder.getfolderasync?view=winrt-19041#exceptions

					if(Uri.IsWellFormedUriString(name, UriKind.RelativeOrAbsolute))
					{
						throw new ArgumentException("The path cannot be in Uri format (for example, /Assets). Check the value of name.", nameof(name));
					}

					var folderHandleGuidString = await WebAssemblyRuntime.InvokeAsync($"{_jsType}.GetFolderAsync(\"{_id}\", \"{name}\")");

					if(folderHandleGuidString == "notfound")
					{
						throw new FileNotFoundException("The specified folder does not exist. Check the value of name.");
					}

					var guid = new Guid(folderHandleGuidString);

					var storageFolder = GetFolderFromNativePathAsync(Path, guid);

					return storageFolder;
				});
		}
	}
}

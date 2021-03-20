#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Uno.Foundation;
using Uno.Helpers.Serialization;
using Uno.Storage.Internal;
using SystemPath = global::System.IO.Path;

namespace Windows.Storage
{
	public partial class StorageFolder
	{
		internal static StorageFolder GetFromNativeInfo(NativeStorageItemInfo info, StorageFolder? parent) =>
			new StorageFolder(new NativeStorageFolder(info, parent));

		internal static Task<StorageFolder?> GetPrivateRootAsync() => NativeStorageFolder.GetPrivateRootAsync();

		internal sealed class NativeStorageFolder : ImplementationBase
		{
			private const string JsType = "Uno.Storage.NativeStorageFolder";

			// Used to keep track of the Folder handle on the Typescript side.
			private Guid _id;
			private string _name;
			private StorageFolder? _parent;

			public NativeStorageFolder(NativeStorageItemInfo info, StorageFolder? parent)
				: base(SystemPath.Combine(parent?.Path ?? string.Empty, info.Name ?? string.Empty))
			{
				if (info is null)
				{
					throw new ArgumentNullException(nameof(info));
				}

				_id = info.Id;
				_name = info.Name ?? string.Empty;
				_parent = parent;
			}

			public override StorageProvider Provider => StorageProviders.NativeWasm;

			public static async Task<StorageFolder?> GetPrivateRootAsync()
			{
				var itemInfoJson = await WebAssemblyRuntime.InvokeAsync($"{JsType}.getPrivateRootAsync()");
				if (itemInfoJson == null)
				{
					return null;
				}

				var item = JsonHelper.Deserialize<NativeStorageItemInfo>(itemInfoJson);
				return GetFromNativeInfo(item, null);
			}

			public override string Name => _name;

			public override async Task<StorageFolder> CreateFolderAsync(string folderName, CreationCollisionOption option, CancellationToken ct)
			{
				var existingItem = await TryGetItemAsync(folderName, ct);
				switch (option)
				{
					case CreationCollisionOption.ReplaceExisting:
						if (existingItem is StorageFile)
						{
							throw new UnauthorizedAccessException("There is already a file with the same name.");
						}

						if (existingItem is StorageFolder folder)
						{
							// Delete existing folder recursively
							await folder.DeleteAsync();
						}
						break;

					case CreationCollisionOption.FailIfExists:
						if (existingItem != null)
						{
							throw new UnauthorizedAccessException("There is already an item with the same name.");
						}
						break;

					case CreationCollisionOption.OpenIfExists:
						if (existingItem is StorageFolder existingFolder)
						{
							return existingFolder;
						}

						if (existingItem is StorageFile)
						{
							throw new UnauthorizedAccessException("There is already a file with the same name.");
						}
						break;

					case CreationCollisionOption.GenerateUniqueName:
						folderName = await FindAvailableNumberedFolderNameAsync(folderName);
						break;

					default:
						throw new ArgumentOutOfRangeException(nameof(option));
				}

				var newFolderNativeInfo = await WebAssemblyRuntime.InvokeAsync($"{JsType}.createFolderAsync(\"{_id}\", \"{folderName}\")");

				if (newFolderNativeInfo == null)
				{
					throw new UnauthorizedAccessException("Could not create file.");
				}

				var info = JsonHelper.Deserialize<NativeStorageItemInfo>(newFolderNativeInfo);
				return GetFromNativeInfo(info, Owner);
			}

			public override async Task<StorageFolder> GetFolderAsync(string name, CancellationToken ct)
			{
				var folderInfoJson = await WebAssemblyRuntime.InvokeAsync($"{JsType}.tryGetFolderAsync(\"{_id}\", \"{WebAssemblyRuntime.EscapeJs(name)}\")");

				if (folderInfoJson == null)
				{
					var fileInfoJson = await WebAssemblyRuntime.InvokeAsync($"{JsType}.tryGetFileAsync(\"{_id}\", \"{WebAssemblyRuntime.EscapeJs(name)}\")");

					if (fileInfoJson != null)
					{
						// File exists
						throw new ArgumentException("The item with given name is a file.", nameof(name));
					}
					else
					{
						throw new FileNotFoundException($"There is no folder with name '{name}'.");
					}
				}

				var info = JsonHelper.Deserialize<NativeStorageItemInfo>(folderInfoJson);
				var storageFolder = GetFromNativeInfo(info, Owner);

				return storageFolder;
			}

			public override async Task<IReadOnlyList<IStorageItem>> GetItemsAsync(CancellationToken ct)
			{
				var itemInfosJson = await WebAssemblyRuntime.InvokeAsync($"{JsType}.getItemsAsync(\"{_id}\")");
				var itemInfos = JsonHelper.Deserialize<NativeStorageItemInfo[]>(itemInfosJson);
				var results = new List<IStorageItem>();
				foreach (var info in itemInfos)
				{
					if (info.IsFile)
					{
						results.Add(StorageFile.GetFromNativeInfo(info));
					}
					else
					{
						results.Add(GetFromNativeInfo(info, Owner));
					}
				}
				return results.AsReadOnly();
			}

			public override async Task<IReadOnlyList<StorageFile>> GetFilesAsync(CancellationToken ct)
			{
				var itemInfosJson = await WebAssemblyRuntime.InvokeAsync($"{JsType}.getFilesAsync(\"{_id}\")");
				var itemInfos = JsonHelper.Deserialize<NativeStorageItemInfo[]>(itemInfosJson);
				var results = new List<StorageFile>();
				foreach (var info in itemInfos)
				{
					results.Add(StorageFile.GetFromNativeInfo(info));
				}
				return results.AsReadOnly();
			}

			public override async Task<IReadOnlyList<StorageFolder>> GetFoldersAsync(CancellationToken ct)
			{
				var itemInfosJson = await WebAssemblyRuntime.InvokeAsync($"{JsType}.getFoldersAsync(\"{_id}\")");
				var itemInfos = JsonHelper.Deserialize<NativeStorageItemInfo[]>(itemInfosJson);
				var results = new List<StorageFolder>();
				foreach (var info in itemInfos)
				{
					results.Add(GetFromNativeInfo(info, Owner));
				}
				return results.AsReadOnly();
			}

			public override async Task<StorageFile> CreateFileAsync(string desiredName, CreationCollisionOption option, CancellationToken cancellationToken)
			{
				var actualName = desiredName;

				var existingItem = await TryGetItemAsync(desiredName, cancellationToken);
				switch (option)
				{
					case CreationCollisionOption.ReplaceExisting:
						if (existingItem is StorageFolder)
						{
							throw new UnauthorizedAccessException("There is already a folder with the same name.");
						}

						if (existingItem is StorageFile)
						{
							// Delete existing file
							await existingItem.DeleteAsync();
						}
						break;

					case CreationCollisionOption.FailIfExists:
						if (existingItem != null)
						{
							throw new UnauthorizedAccessException("There is already an item with the same name.");
						}
						break;

					case CreationCollisionOption.OpenIfExists:
						if (existingItem is StorageFile existingFile)
						{
							return existingFile;
						}

						if (existingItem is StorageFolder)
						{
							throw new UnauthorizedAccessException("There is already a file with the same name.");
						}
						break;

					case CreationCollisionOption.GenerateUniqueName:
						actualName = await FindAvailableNumberedFileNameAsync(desiredName);
						break;

					default:
						throw new ArgumentOutOfRangeException(nameof(option));
				}

				var newFolderNativeInfo = await WebAssemblyRuntime.InvokeAsync($"{JsType}.createFileAsync(\"{_id}\", \"{WebAssemblyRuntime.EscapeJs(actualName)}\")");
				if (newFolderNativeInfo == null)
				{
					throw new UnauthorizedAccessException("Could not create file.");
				}

				var info = JsonHelper.Deserialize<NativeStorageItemInfo>(newFolderNativeInfo);
				return StorageFile.GetFromNativeInfo(info, Owner);
			}

			public override async Task<StorageFile> GetFileAsync(string name, CancellationToken token)
			{
				var fileInfoJson = await WebAssemblyRuntime.InvokeAsync($"{JsType}.tryGetFileAsync(\"{_id}\", \"{WebAssemblyRuntime.EscapeJs(name)}\")");

				if (fileInfoJson == null)
				{
					var folderInfoJson = await WebAssemblyRuntime.InvokeAsync($"{JsType}.tryGetFolderAsync(\"{_id}\", \"{WebAssemblyRuntime.EscapeJs(name)}\")");

					if (folderInfoJson != null)
					{
						// Folder exists
						throw new ArgumentException("The item with given name is a folder.", nameof(name));
					}
					else
					{
						throw new FileNotFoundException($"There is no file with name '{name}'.");
					}
				}

				// File exists
				var fileInfo = JsonHelper.Deserialize<NativeStorageItemInfo>(fileInfoJson);
				return StorageFile.GetFromNativeInfo(fileInfo, Owner);
			}

			public override async Task<IStorageItem> GetItemAsync(string name, CancellationToken token)
			{
				var item = await TryGetItemAsync(name, token);

				if (item == null)
				{
					throw new FileNotFoundException($"There is no folder or file with name '{name}'.");
				}

				return item;
			}

			public override Task<StorageFolder?> GetParentAsync(CancellationToken token) => Task.FromResult(_parent);

			public override async Task<IStorageItem?> TryGetItemAsync(string name, CancellationToken token)
			{
				var fileInfoJson = await WebAssemblyRuntime.InvokeAsync($"{JsType}.tryGetFileAsync(\"{_id}\", \"{WebAssemblyRuntime.EscapeJs(name)}\")");
				if (fileInfoJson != null)
				{
					// File exists
					var fileInfo = JsonHelper.Deserialize<NativeStorageItemInfo>(fileInfoJson);
					return StorageFile.GetFromNativeInfo(fileInfo, Owner);
				}

				var folderInfoJson = await WebAssemblyRuntime.InvokeAsync($"{JsType}.tryGetFolderAsync(\"{_id}\", \"{WebAssemblyRuntime.EscapeJs(name)}\")");
				if (folderInfoJson != null)
				{
					// Folder exists
					var folderInfo = JsonHelper.Deserialize<NativeStorageItemInfo>(folderInfoJson);
					return GetFromNativeInfo(folderInfo, Owner);
				}

				return null;
			}

			public override async Task DeleteAsync(StorageDeleteOption options, CancellationToken ct)
			{
				if (_parent == null)
				{
					throw new NotSupportedException("Cannot create a folder unless we can access its parent folder.");
				}

				var nativeParent = (NativeStorageFolder)_parent.Implementation;
				await nativeParent.DeleteItemAsync(Name);
			}

			internal async Task DeleteItemAsync(string itemName)
			{
				var result = await WebAssemblyRuntime.InvokeAsync($"{JsType}.deleteItemAsync(\"{_id}\", \"{WebAssemblyRuntime.EscapeJs(itemName)}\")");
				if (result == null)
				{
					throw new UnauthorizedAccessException($"Could not delete item {itemName}");
				}
			}

			protected override bool IsEqual(ImplementationBase implementation) => throw NotSupported();
		}
	}
}

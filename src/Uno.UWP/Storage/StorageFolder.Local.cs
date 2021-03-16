#nullable enable
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Uno.Extensions;
using Uno.Storage.Internal;
using Windows.Storage.FileProperties;
using IOPath = global::System.IO.Path;

#if __IOS__
using UIKit;
using Foundation;
#endif

namespace Windows.Storage
{
	partial class StorageFolder
	{
		internal StorageFolder(string fullPath)
			: this(new Local(string.Empty, fullPath))
		{
		}

		internal StorageFolder(string name, string path)
			: this(new Local(name, path))
		{
		}

		private sealed class Local : ImplementationBase
		{
			private string _name;

			public Local(string name, string path)
				: base(path)
			{
				if (string.IsNullOrEmpty(name))
				{
					if (!path.EndsWith("/"))
					{
						// Intentionally use GetFileName here, as the directory name
						// may be a "file-like name" e.g. myfolder.txt, in which case
						// GetDirectoryName would actually return the parent.
						name = IOPath.GetFileName(path);
					}
					else
					{
						name = IOPath.GetDirectoryName(path);
					}
				}

				_name = name;
			}

			public override StorageProvider Provider => StorageProviders.Local;

			public override string Name => _name;

			protected override bool IsEqual(ImplementationBase impl) =>
				impl is Local other && Path.Equals(other.Path);

			public override async Task<StorageFile> CreateFileAsync(string desiredName, CreationCollisionOption options, CancellationToken cancellationToken)
			{
				await TryInitializeStorage();
				var actualName = desiredName;

				if (File.Exists(IOPath.Combine(Path, desiredName)))
				{
					switch (options)
					{
						case CreationCollisionOption.FailIfExists:
							throw new Exception("Cannot create a file when that file already exists.");
						case CreationCollisionOption.OpenIfExists:
							break;
						case CreationCollisionOption.ReplaceExisting:
							File.Create(IOPath.Combine(Path, desiredName)).Close();
							break;
						case CreationCollisionOption.GenerateUniqueName:
							actualName = await FindAvailableNumberedFileNameAsync(desiredName);
							var path = IOPath.Combine(Path, actualName);
							File.Create(path).Close();
							break;
						default:
							throw new ArgumentOutOfRangeException(nameof(options));
					}
				}
				else
				{
					File.Create(IOPath.Combine(Path, desiredName)).Close();
				}

				return await StorageFile.GetFileFromPathAsync(IOPath.Combine(Path, actualName));
			}

			public override async Task<StorageFolder> CreateFolderAsync(string folderName, CreationCollisionOption option, CancellationToken token)
			{
				await TryInitializeStorage();

				var path = IOPath.Combine(Path, folderName);
				switch (option)
				{
					case CreationCollisionOption.ReplaceExisting:
						if (Directory.Exists(path))
						{
							Directory.Delete(path, true);
						}

						if (File.Exists(path))
						{
							throw new UnauthorizedAccessException("There is already a file with the same name.");
						}
						break;

					case CreationCollisionOption.FailIfExists:
						if (Directory.Exists(path) || File.Exists(path))
						{
							throw new UnauthorizedAccessException("There is already an item with the same name.");
						}
						break;

					case CreationCollisionOption.OpenIfExists:
						if (Directory.Exists(path))
						{
							return new StorageFolder(folderName, path);
						}

						if (File.Exists(path))
						{
							throw new UnauthorizedAccessException("There is already a file with the same name.");
						}
						break;

					case CreationCollisionOption.GenerateUniqueName:
						var availableName = await FindAvailableNumberedFolderNameAsync(folderName);
						path = IOPath.Combine(Path, availableName);
						break;

					default:
						throw new NotSupportedException("Unsupported value of CreationCollisionOption");
				}

				var directory = Directory.CreateDirectory(path);
				return new StorageFolder(directory.Name, path);
			}

			public override Task<BasicProperties> GetBasicPropertiesAsync(CancellationToken ct) =>
				Task.FromResult(BasicProperties.FromDirectoryPath(Owner.Path));

			public override async Task<StorageFile> GetFileAsync(string name, CancellationToken token)
			{
				await TryInitializeStorage();

				var filePath = IOPath.Combine(Path, name);

				if (!File.Exists(filePath))
				{
					throw new FileNotFoundException(filePath);
				}

				return StorageFile.GetFileFromPath(filePath);
			}

			public override async Task<IStorageItem> GetItemAsync(string name, CancellationToken token)
			{
				await TryInitializeStorage();

				var item = await TryGetItemAsync(name, token);

				if (item == null)
				{
					throw new FileNotFoundException($"There is no folder or file with name '{name}'.");
				}

				return item;
			}

			public override async Task<StorageFolder?> GetParentAsync(CancellationToken token)
			{
				await TryInitializeStorage();

				var directory = new DirectoryInfo(Path);
				if (!directory.Exists)
				{
					throw new FileNotFoundException(Path);
				}

				if (directory.Parent == null)
				{
					return null;
				}

				return await GetFolderFromPathAsync(directory.Parent.FullName);
			}

			public override async Task<IStorageItem?> TryGetItemAsync(string name, CancellationToken token)
			{
				await TryInitializeStorage();

				var itemPath = IOPath.Combine(Path, name);

				if (File.Exists(itemPath))
				{
					return await StorageFile.GetFileFromPathAsync(itemPath);
				}

				if (Directory.Exists(itemPath))
				{
					return await GetFolderFromPathAsync(itemPath);
				}

				return null;
			}

			public override async Task<StorageFolder> GetFolderAsync(string name, CancellationToken token)
			{
				await TryInitializeStorage();

				var itemPath = IOPath.Combine(Path, name);

				var directoryExists = Directory.Exists(itemPath);

				if (!directoryExists)
				{
					throw new FileNotFoundException(itemPath);
				}

				return await GetFolderFromPathAsync(itemPath);
			}

			public override async Task<IReadOnlyList<IStorageItem>> GetItemsAsync(CancellationToken ct)
			{
				var items = new List<IStorageItem>();

				foreach (var folder in Directory.EnumerateDirectories(Path))
				{
					items.Add(await GetFolderFromPathAsync(folder).AsTask(ct));
				}

				foreach (var folder in Directory.EnumerateFiles(Path))
				{
					items.Add(await StorageFile.GetFileFromPathAsync(folder).AsTask(ct));
				}

				return items.AsReadOnly();
			}

			public override async Task<IReadOnlyList<StorageFile>> GetFilesAsync(CancellationToken ct)
			{
				var items = new List<StorageFile>();

				foreach (var folder in Directory.EnumerateFiles(Path))
				{
					items.Add(await StorageFile.GetFileFromPathAsync(folder).AsTask(ct));
				}
				return items.AsReadOnly();
			}

			public override async Task<IReadOnlyList<StorageFolder>> GetFoldersAsync(CancellationToken ct)
			{
				var items = new List<StorageFolder>();

				foreach (var folder in Directory.EnumerateDirectories(Path))
				{
					items.Add(await GetFolderFromPathAsync(folder).AsTask(ct));
				}

				return items.AsReadOnly();
			}

			public override async Task DeleteAsync(CancellationToken ct)
			{
				await TryInitializeStorage();

				Directory.Delete(Path, true);
			}
		}
	}
}

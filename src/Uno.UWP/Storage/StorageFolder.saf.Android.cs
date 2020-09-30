#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Android.App;
using Android.Provider;
using AndroidX.DocumentFile.Provider;
using Uno.Storage.Internal;
using Uno.UI;
using Windows.Storage.FileProperties;
using IOPath = System.IO.Path;

namespace Windows.Storage
{
	public partial class StorageFolder
	{
		internal static StorageFolder GetFromSafUri(Android.Net.Uri safUri) =>
			new StorageFolder(new SafFolder(safUri));

		internal static StorageFolder GetFromSafDocument(DocumentFile document) =>
				new StorageFolder(new SafFolder(document));

		internal class SafFolder : ImplementationBase
		{
			private readonly Android.Net.Uri _folderUri;
			private readonly DocumentFile _directoryDocument;

			internal SafFolder(Android.Net.Uri uri) : base(uri.Path ?? string.Empty)
			{
				_folderUri = uri ?? throw new ArgumentNullException(nameof(uri));
				_directoryDocument = DocumentFile.FromTreeUri(Application.Context, uri);
				if (string.IsNullOrEmpty(Path))
				{
					Path = _directoryDocument.Name;
				}
			}

			internal SafFolder(DocumentFile directoryDocument) : base(directoryDocument.Uri.Path ?? directoryDocument.Name)
			{
				_directoryDocument = directoryDocument ?? throw new ArgumentNullException(nameof(directoryDocument));
				_folderUri = _directoryDocument.Uri;
			}

			public override StorageProvider Provider => StorageProviders.AndroidSaf;

			public override string Name => _directoryDocument?.Name ?? string.Empty;

			public override Task<BasicProperties> GetBasicPropertiesAsync(CancellationToken ct) =>
				SafHelpers.GetBasicPropertiesAsync(_folderUri, _directoryDocument, false, ct);

			public override async Task<StorageFile> CreateFileAsync(string desiredName, Windows.Storage.CreationCollisionOption options, CancellationToken cancellationToken)
			{
				return await Task.Run(async () =>
				{
					var actualName = desiredName;

					var existingItem = await TryGetItemAsync(desiredName, cancellationToken);
					switch (options)
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
							throw new ArgumentOutOfRangeException(nameof(options));
					}

					var extension = IOPath.GetExtension(actualName);
					var mimeType = MimeTypeService.GetFromExtension(extension);
					var file = _directoryDocument.CreateFile("", actualName);
					return StorageFile.GetFromSafDocument(file);
				}, cancellationToken);
			}

			public override async Task<StorageFolder> CreateFolderAsync(string folderName, Windows.Storage.CreationCollisionOption option, CancellationToken token)
			{
				return await Task.Run(async () =>
				{
					var existingItem = await TryGetItemAsync(folderName, token);
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

					var directoryDocument = _directoryDocument.CreateDirectory(folderName);
					return GetFromSafDocument(directoryDocument);
				}, token);
			}

			public override async Task DeleteAsync(StorageDeleteOption option, CancellationToken ct)
			{
				await Task.Run(() =>
				{
					_directoryDocument.Delete();
				}, ct);
			}

			public override async Task<StorageFile> GetFileAsync(string name, CancellationToken token)
			{
				return await Task.Run(() =>
				{
					var item = _directoryDocument.FindFile(name);
					if (item == null)
					{
						throw new FileNotFoundException("There is no file with this name.");
					}

					if (!item.IsFile)
					{
						throw new ArgumentException("The item with given name is a folder.", nameof(name));
					}

					return StorageFile.GetFromSafDocument(item);
				}, token);
			}

			public override async Task<IReadOnlyList<StorageFile>> GetFilesAsync(CancellationToken ct)
			{
				return await Task.Run(() =>
				{
					var contents = _directoryDocument
						.ListFiles()
						.Where(f => f.IsFile)
						.Select(d => StorageFile.GetFromSafDocument(d))
						.ToArray();
					return Task.FromResult((IReadOnlyList<StorageFile>)contents);
				}, ct);
			}

			public override async Task<StorageFolder> GetFolderAsync(string name, CancellationToken token)
			{
				return await Task.Run(() =>
				{
					var item = _directoryDocument.FindFile(name);
					if (item == null)
					{
						throw new FileNotFoundException("There is no folder with this name.");
					}

					if (item.IsFile)
					{
						throw new ArgumentException("The item with given name is a file.", nameof(name));
					}

					return new StorageFolder(new SafFolder(item));
				}, token);
			}

			public override async Task<IReadOnlyList<StorageFolder>> GetFoldersAsync(CancellationToken ct)
			{
				return await Task.Run(() =>
				{
					var contents = _directoryDocument
						.ListFiles()
						.Where(f => f.IsDirectory)
						.Select(d => GetFromSafDocument(d))
						.ToArray();
					return (IReadOnlyList<StorageFolder>)contents;
				});
			}

			public override async Task<IStorageItem> GetItemAsync(string name, CancellationToken token)
			{
				return await Task.Run<IStorageItem>(() =>
				{
					var item = _directoryDocument.FindFile(name);
					if (item == null || (!item.IsDirectory && !item.IsFile))
					{
						throw new FileNotFoundException("Not found");
					}

					if (item.IsDirectory)
					{
						return GetFromSafDocument(item);
					}

					return StorageFile.GetFromSafDocument(item);
				}, token);
			}

			public override async Task<IReadOnlyList<IStorageItem>> GetItemsAsync(CancellationToken ct)
			{
				return await Task.Run(() =>
				{
					var items = _directoryDocument.ListFiles();
					var folders = items.Where(i => i.IsDirectory).Select(i => GetFromSafDocument(i));
					var files = items.Where(i => i.IsFile).Select(i => StorageFile.GetFromSafDocument(i));
					return Task.FromResult((IReadOnlyList<IStorageItem>)folders.OfType<IStorageItem>().Union(files).ToArray());
				}, ct);
			}

			public override Task<StorageFolder?> GetParentAsync(CancellationToken token)
			{
				if (_directoryDocument.ParentFile == null)
				{
					return Task.FromResult<StorageFolder?>(null);
				}

				var parentFolder = GetFromSafDocument(_directoryDocument.ParentFile);
				return Task.FromResult<StorageFolder?>(parentFolder);
			}

			public override async Task<IStorageItem?> TryGetItemAsync(string name, CancellationToken token)
			{
				return await Task.Run<IStorageItem?>(() =>
				{
					var item = _directoryDocument.FindFile(name);
					if (item == null)
					{
						return null;
					}

					if (item.IsDirectory)
					{
						return GetFromSafDocument(item);
					}

					if (item.IsFile)
					{
						return StorageFile.GetFromSafDocument(item);
					}

					return null;
				}, token);
			}

			protected override bool IsEqual(ImplementationBase implementation)
			{
				if (implementation is SafFolder otherFolder)
				{
					var path = _directoryDocument.Uri?.ToString() ?? string.Empty;
					var otherPath = otherFolder._directoryDocument.Uri?.ToString() ?? string.Empty;
					return path.Equals(otherPath, StringComparison.InvariantCulture);
				}

				return false;
			}
		}
	}
}

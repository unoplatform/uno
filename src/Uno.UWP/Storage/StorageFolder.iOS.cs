#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Foundation;
using MobileCoreServices;
using UIKit;
using Uno.Storage.Internal;
using Windows.Storage.FileProperties;
using IOPath = System.IO.Path;

namespace Windows.Storage
{
	public partial class StorageFolder
	{
		internal static StorageFolder GetFromSecurityScopedUrl(NSUrl nsUrl, StorageFolder? parent) =>
			new StorageFolder(new SecurityScopedFolder(nsUrl, parent));

		internal class SecurityScopedFolder : ImplementationBase
		{
			private readonly NSUrl _nsUrl;
			private readonly StorageFolder? _parent;
			private readonly UIDocument _document;

			public SecurityScopedFolder(NSUrl nsUrl, StorageFolder? parent) : base(string.Empty)
			{
				if (nsUrl is null)
				{
					throw new ArgumentNullException(nameof(nsUrl));
				}

				_nsUrl = nsUrl;
				_parent = parent;
				_document = new UIDocument(_nsUrl);
				Path = _document.FileUrl?.Path ?? string.Empty;
			}

			public override string Name => _document.FileUrl?.LastPathComponent ?? string.Empty;

			public override string DisplayName => _document.LocalizedName ?? Name;

			public override StorageProvider Provider => StorageProviders.IosSecurityScoped;

			public override Task<BasicProperties> GetBasicPropertiesAsync(CancellationToken ct)
			{
				using var _ = _nsUrl.BeginSecurityScopedAccess();
				var directoryInfo = new DirectoryInfo(Path);
				return Task.FromResult(new BasicProperties(0UL, directoryInfo.LastWriteTimeUtc));
			}

			public override async Task<StorageFile> CreateFileAsync(string desiredName, CreationCollisionOption options, CancellationToken cancellationToken)
			{
				using var _ = _nsUrl.BeginSecurityScopedAccess();
				var path = IOPath.Combine(Path, desiredName);
				var actualName = desiredName;

				switch (options)
				{
					case CreationCollisionOption.FailIfExists:
						if (Directory.Exists(path) || File.Exists(path))
						{
							throw new UnauthorizedAccessException("There is already an item with the same name.");
						}
						break;

					case CreationCollisionOption.GenerateUniqueName:
						actualName = await FindAvailableNumberedFileNameAsync(desiredName);
						break;

					case CreationCollisionOption.OpenIfExists:
						if (Directory.Exists(path))
						{
							throw new UnauthorizedAccessException("There is already a folder with the same name.");
						}
						break;

					case CreationCollisionOption.ReplaceExisting:
						if (Directory.Exists(path))
						{
							throw new UnauthorizedAccessException("There is already a folder with the same name.");
						}

						if (File.Exists(path))
						{
							File.Delete(path);
						}
						break;

					default:
						throw new ArgumentOutOfRangeException(nameof(options));
				}

				var actualPath = IOPath.Combine(Path, actualName);
				if (!File.Exists(actualPath))
				{
					File.Create(actualPath).Close();
				}

				return StorageFile.GetFromSecurityScopedUrl(_nsUrl.Append(actualName, false), Owner);
			}

			public override async Task<StorageFolder> CreateFolderAsync(string folderName, CreationCollisionOption option, CancellationToken token)
			{
				using (_nsUrl.BeginSecurityScopedAccess())
				{
					var path = IOPath.Combine(Path, folderName);
					var actualName = folderName;

					switch (option)
					{
						case CreationCollisionOption.FailIfExists:
							if (Directory.Exists(path) || File.Exists(path))
							{
								throw new UnauthorizedAccessException("There is already an item with the same name.");
							}
							break;

						case CreationCollisionOption.GenerateUniqueName:
							actualName = await FindAvailableNumberedFolderNameAsync(folderName);
							break;

						case CreationCollisionOption.OpenIfExists:
							if (File.Exists(path))
							{
								throw new UnauthorizedAccessException("There is already a file with the same name.");
							}
							break;

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

						default:
							throw new ArgumentOutOfRangeException(nameof(option));
					}

					var actualPath = IOPath.Combine(Path, actualName);
					if (!Directory.Exists(actualPath))
					{
						Directory.CreateDirectory(actualPath);
					}

					return GetFromSecurityScopedUrl(_nsUrl.Append(actualName, true), Owner);
				}
			}

			public override async Task DeleteAsync(StorageDeleteOption options, CancellationToken ct)
			{
				var intent = NSFileAccessIntent.CreateWritingIntent(_nsUrl, NSFileCoordinatorWritingOptions.ForDeleting);

				using var coordinator = new NSFileCoordinator();
				await coordinator.CoordinateAsync(new[] { intent }, new NSOperationQueue(), () =>
				{
					using var _ = _nsUrl.BeginSecurityScopedAccess();
					NSError deleteError;

					NSFileManager.DefaultManager.Remove(_nsUrl, out deleteError);

					if (deleteError != null)
					{
						throw new UnauthorizedAccessException($"Can't delete file. {deleteError}");
					}
				});
			}

			public override Task<StorageFile> GetFileAsync(string name, CancellationToken token)
			{
				using var _ = _nsUrl.BeginSecurityScopedAccess();

				var filePath = IOPath.Combine(Path, name);

				if (Directory.Exists(filePath))
				{
					throw new ArgumentException("The item with given name is a folder.", nameof(name));
				}

				if (!File.Exists(filePath))
				{
					throw new FileNotFoundException("There is no file with this name.");
				}

				return Task.FromResult(StorageFile.GetFromSecurityScopedUrl(_nsUrl.Append(name, false), Owner));
			}

			public override Task<IReadOnlyList<StorageFile>> GetFilesAsync(CancellationToken ct)
			{
				using var _ = _nsUrl.BeginSecurityScopedAccess();
				var items = new List<StorageFile>();

				foreach (var file in Directory.EnumerateFiles(Path))
				{
					var fileUrl = _nsUrl.Append(IOPath.GetFileName(file), false);
					items.Add(StorageFile.GetFromSecurityScopedUrl(fileUrl, Owner));
				}

				return Task.FromResult<IReadOnlyList<StorageFile>>(items.AsReadOnly());
			}

			public override Task<StorageFolder> GetFolderAsync(string name, CancellationToken token)
			{
				using var _ = _nsUrl.BeginSecurityScopedAccess();

				var itemPath = IOPath.Combine(Path, name);

				if (File.Exists(itemPath))
				{
					throw new ArgumentException("The item with given name is a file.", nameof(name));
				}

				if (!Directory.Exists(itemPath))
				{
					throw new FileNotFoundException("There is no file with this name.");
				}


				return Task.FromResult(GetFromSecurityScopedUrl(_nsUrl.Append(name, true), Owner));
			}

			public override Task<IReadOnlyList<StorageFolder>> GetFoldersAsync(CancellationToken ct)
			{
				using var _ = _nsUrl.BeginSecurityScopedAccess();
				var items = new List<StorageFolder>();

				foreach (var folder in Directory.EnumerateDirectories(Path))
				{
					var info = new DirectoryInfo(folder);
					var folderUrl = _nsUrl.Append(info.Name, true);
					items.Add(GetFromSecurityScopedUrl(folderUrl, Owner));
				}

				return Task.FromResult<IReadOnlyList<StorageFolder>>(items.AsReadOnly());
			}

			public override async Task<IStorageItem> GetItemAsync(string name, CancellationToken token)
			{
				using var _ = _nsUrl.BeginSecurityScopedAccess();
				var item = await TryGetItemAsync(name, token);

				if (item == null)
				{
					throw new FileNotFoundException($"There is no folder or file with name '{name}'.");
				}

				return item;
			}

			public override Task<IReadOnlyList<IStorageItem>> GetItemsAsync(CancellationToken ct)
			{
				using var _ = _nsUrl.BeginSecurityScopedAccess();
				var items = new List<IStorageItem>();

				foreach (var folder in Directory.EnumerateDirectories(Path))
				{
					var info = new DirectoryInfo(folder);
					var folderUrl = _nsUrl.Append(info.Name, true);
					items.Add(GetFromSecurityScopedUrl(folderUrl, Owner));
				}

				foreach (var file in Directory.EnumerateFiles(Path))
				{
					var fileUrl = _nsUrl.Append(IOPath.GetFileName(file), false);
					items.Add(StorageFile.GetFromSecurityScopedUrl(fileUrl, Owner));
				}

				return Task.FromResult<IReadOnlyList<IStorageItem>>(items.AsReadOnly());
			}

			public override Task<StorageFolder?> GetParentAsync(CancellationToken token) => Task.FromResult(_parent);

			public override Task<IStorageItem?> TryGetItemAsync(string name, CancellationToken token)
			{
				using var _ = _nsUrl.BeginSecurityScopedAccess();
				var itemUrl = _nsUrl.Append(name, false);
				if (itemUrl.CheckPromisedItemIsReachable(out var _))
				{
					var document = new UIDocument(itemUrl);
					if (document.FileType == UTType.Folder)
					{
						return Task.FromResult<IStorageItem?>(GetFromSecurityScopedUrl(itemUrl, Owner));
					}
					else
					{
						return Task.FromResult<IStorageItem?>(StorageFile.GetFromSecurityScopedUrl(itemUrl, Owner));
					}
				}

				return Task.FromResult<IStorageItem?>(null);
			}

			protected override bool IsEqual(ImplementationBase implementation) =>
				implementation is SecurityScopedFolder otherFolder && otherFolder._nsUrl.FilePathUrl.Path == _nsUrl.FilePathUrl.Path;
		}
	}
}

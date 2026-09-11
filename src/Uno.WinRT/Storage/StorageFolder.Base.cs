#nullable enable

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage.FileProperties;
using Windows.Storage.Internal;
using IOPath = System.IO.Path;

namespace Windows.Storage
{
	public partial class StorageFolder
	{
		internal abstract class ImplementationBase
		{
			protected ImplementationBase(string path) => Path = path;

			public void InitOwner(StorageFolder owner) => Owner = owner; // Lazy initialized to avoid delegate in StorageFolder ctor

			protected StorageFolder Owner { get; private set; } = null!; // Should probably be private

			public abstract StorageProvider Provider { get; }

			public virtual string Name => global::System.IO.Path.GetDirectoryName(Path) ?? string.Empty;

			public virtual string DisplayName => Name;

			public virtual string Path { get; protected set; } = string.Empty;

			public bool IsEqual(IStorageItem item) => item is StorageFolder sf && IsEqual(sf.Implementation);

			protected abstract bool IsEqual(ImplementationBase implementation);

			public abstract Task<StorageFile> CreateFileAsync(string desiredName, CreationCollisionOption options, CancellationToken cancellationToken);

			public abstract Task<StorageFolder> CreateFolderAsync(string folderName, CreationCollisionOption option, CancellationToken token);

			public abstract Task<StorageFolder> GetFolderAsync(string name, CancellationToken token);

			public abstract Task<StorageFile> GetFileAsync(string name, CancellationToken token);

			public abstract Task<IStorageItem> GetItemAsync(string name, CancellationToken token);

			public abstract Task<StorageFolder?> GetParentAsync(CancellationToken token);

			public abstract Task<IStorageItem?> TryGetItemAsync(string name, CancellationToken token);

			public virtual Task<BasicProperties> GetBasicPropertiesAsync(CancellationToken ct) =>
				Task.FromResult(new BasicProperties(0UL, DateTimeOffset.MinValue));

			public abstract Task<IReadOnlyList<IStorageItem>> GetItemsAsync(CancellationToken ct);

			public abstract Task<IReadOnlyList<StorageFile>> GetFilesAsync(CancellationToken ct);

			public abstract Task<IReadOnlyList<StorageFolder>> GetFoldersAsync(CancellationToken ct);

			public abstract Task DeleteAsync(StorageDeleteOption options, CancellationToken ct);

			protected Exception NotSupported([CallerMemberName] string? method = null) =>
				new NotSupportedException($"{method} is not supported yet for {GetType().Name}");

			protected async Task<string> FindAvailableNumberedFolderNameAsync(string desiredName) =>
				await StorageItemNameGenerator.FindAvailableNumberedNameAsync(desiredName, Owner, number => $"{desiredName} ({number})");

			protected async Task<string> FindAvailableNumberedFileNameAsync(string desiredName)
			{
				var fileNameExtension = IOPath.GetExtension(desiredName) ?? string.Empty;
				var fileNameWithoutExtension = IOPath.GetFileNameWithoutExtension(desiredName);
				return await StorageItemNameGenerator.FindAvailableNumberedNameAsync(desiredName, Owner, number => $"{fileNameWithoutExtension} ({number}){fileNameExtension}");
			}
		}
	}
}

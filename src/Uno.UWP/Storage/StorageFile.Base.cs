#nullable enable

using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage.FileProperties;
using Windows.Storage.Streams;
using Buffer = Windows.Storage.Streams.Buffer;

namespace Windows.Storage
{
	partial class StorageFile
	{
		internal abstract class ImplementationBase
		{
			protected ImplementationBase(string path) => Path = path;

			public void InitOwner(StorageFile owner)
				=> Owner = owner; // Lazy initialized to avoid delegate in StorageFile ctor

			protected StorageFile Owner { get; private set; } = null!; // Should probably be private

			public abstract StorageProvider Provider { get; }

			public virtual string Path { get; protected set; }

			public virtual string FileType => global::System.IO.Path.GetExtension(Name);

			public virtual string Name => global::System.IO.Path.GetFileName(Path);

			public virtual string DisplayName => global::System.IO.Path.GetFileNameWithoutExtension(Name);

			public virtual string ContentType => MimeTypeService.GetFromExtension(FileType);

			public abstract DateTimeOffset DateCreated { get; }

			public bool IsEqual(IStorageItem item)
				=> item is StorageFile sf && IsEqual(sf.Implementation);

			protected abstract bool IsEqual(ImplementationBase implementation);

			public abstract Task<StorageFolder?> GetParentAsync(CancellationToken ct);

			public abstract Task<BasicProperties> GetBasicPropertiesAsync(CancellationToken ct);

			public abstract Task<IRandomAccessStreamWithContentType> OpenAsync(CancellationToken ct, FileAccessMode accessMode, StorageOpenOptions options);

			public virtual async Task<Stream> OpenStreamAsync(CancellationToken ct, FileAccessMode accessMode, StorageOpenOptions options)
				=> (await OpenAsync(ct, accessMode, options).AsTask(ct)).AsStream();

			public abstract Task<StorageStreamTransaction> OpenTransactedWriteAsync(CancellationToken ct, StorageOpenOptions option);

			public abstract Task DeleteAsync(CancellationToken ct, StorageDeleteOption options);

			public virtual async Task RenameAsync(CancellationToken ct, string desiredName, NameCollisionOption option)
			{
				var parent = await GetParentAsync(ct);
				if (parent == null)
				{
					throw new InvalidOperationException("The file's parent is not accessible, so we cannot move the file to rename it.");
				}

				await MoveAsync(ct, parent, desiredName, option);
			}

			public virtual async Task<StorageFile> CopyAsync(CancellationToken ct, IStorageFolder destinationFolder, string desiredNewName, NameCollisionOption option)
			{
				var dst = await CreateDestination(ct, destinationFolder, desiredNewName, option);
				await CopyAndReplaceAsync(ct, dst);
				return dst;
			}

			public virtual async Task CopyAndReplaceAsync(CancellationToken ct, IStorageFile target)
			{
				using (var src = await Owner.OpenStreamForReadAsync())
				using (var dst = await target.OpenStreamForReadAsync())
				{
					await src.CopyToAsync(dst, Buffer.DefaultCapacity, ct);
				}
			}

			public virtual async Task MoveAsync(CancellationToken ct, IStorageFolder destinationFolder, string desiredNewName, NameCollisionOption option)
			{
				var dst = await CreateDestination(ct, destinationFolder, desiredNewName, option);
				await MoveAndReplaceAsync(ct, dst);
			}

			public virtual async Task MoveAndReplaceAsync(CancellationToken ct, IStorageFile target)
			{
				using (var src = await Owner.OpenStreamForReadAsync())
				using (var dst = await target.OpenStreamForReadAsync())
				{
					await src.CopyToAsync(dst, Buffer.DefaultCapacity, ct);
				}

				await DeleteAsync(ct, StorageDeleteOption.PermanentDelete);

				Path = target.Path;
			}

			protected Exception NotSupported([CallerMemberName] string? method = null)
				=> new NotSupportedException($"{method} is not supported yet for {GetType().Name}");
		}
	}
}

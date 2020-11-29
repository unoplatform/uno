#nullable enable

using System;
using System.IO;
using System.Linq;
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
		private abstract class ImplementationBase
		{
			protected ImplementationBase(string path)
				=> Path = path;

			public void InitOwner(StorageFile owner)
				=> Owner = owner; // Lazy initialized to avoid delegate in StorageFile ctor

			protected StorageFile Owner { get; private set; } = null!; // Should probably be private

			public virtual string Path { get; protected set; }

			public virtual string FileType => global::System.IO.Path.GetExtension(Path);

			public virtual string Name => global::System.IO.Path.GetFileName(Path);

			public virtual string DisplayName => global::System.IO.Path.GetFileNameWithoutExtension(Path);

			public virtual string ContentType => MimeTypeService.GetFromFileExtension(FileType);

			public abstract DateTimeOffset DateCreated { get; }

			public bool IsEqual(IStorageItem item)
				=> item is StorageFile sf && IsEqual(sf._impl);

			protected abstract bool IsEqual(ImplementationBase impl);

			public abstract Task<StorageFolder> GetParent(CancellationToken ct);

			public abstract Task<BasicProperties> GetBasicProperties(CancellationToken ct);

			public abstract Task<IRandomAccessStreamWithContentType> Open(CancellationToken ct, FileAccessMode accessMode, StorageOpenOptions options);

			public virtual async Task<Stream> OpenStream(CancellationToken ct, FileAccessMode accessMode, StorageOpenOptions options)
				=> (await Open(ct, accessMode, options).AsTask(ct)).AsStream();

			public abstract Task<StorageStreamTransaction> OpenTransactedWrite(CancellationToken ct, StorageOpenOptions option);

			public abstract Task Delete(CancellationToken ct, StorageDeleteOption options);

			public virtual async Task Rename(CancellationToken ct, string desiredName, NameCollisionOption option)
			{
				var parent = await GetParent(ct);
				await Move(ct, parent, desiredName, option);
			}

			public virtual async Task<StorageFile> Copy(CancellationToken ct, IStorageFolder destinationFolder, string desiredNewName, NameCollisionOption option)
			{
				var dst = await CreateDestination(ct, destinationFolder, desiredNewName, option);
				await CopyAndReplace(ct, dst);
				return dst;
			}

			public virtual async Task CopyAndReplace(CancellationToken ct, IStorageFile target)
			{
				using (var src = await Owner.OpenStreamForReadAsync())
				using (var dst = await target.OpenStreamForReadAsync())
				{
					await src.CopyToAsync(dst, Buffer.DefaultCapacity, ct);
				}
			}

			public virtual async Task Move(CancellationToken ct, IStorageFolder destinationFolder, string desiredNewName, NameCollisionOption option)
			{
				var dst = await CreateDestination(ct, destinationFolder, desiredNewName, option);
				await MoveAndReplace(ct, dst);
			}

			public virtual async Task MoveAndReplace(CancellationToken ct, IStorageFile target)
			{
				using (var src = await Owner.OpenStreamForReadAsync())
				using (var dst = await target.OpenStreamForReadAsync())
				{
					await src.CopyToAsync(dst, Buffer.DefaultCapacity, ct);
				}

				await Delete(ct, StorageDeleteOption.PermanentDelete);

				Path = target.Path;
			}

			protected Exception NotImplemented([CallerMemberName] string? method = null)
				=> new NotSupportedException($"{method} is not supported yet for {GetType().Name}");
		}
	}
}

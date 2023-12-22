#nullable enable

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Uno.Foundation;
using Uno.Storage.Internal;
using Windows.Storage.FileProperties;
using Windows.Storage.Streams;
using SystemPath = global::System.IO.Path;

using NativeMethods = __Windows.Storage.StorageFile.NativeMethods;

namespace Windows.Storage
{
	public partial class StorageFile
	{
		internal static StorageFile GetFromNativeInfo(NativeStorageItemInfo info)
			=> new StorageFile(new NativeStorageFile(info, default));

		internal static StorageFile GetFromNativeInfo(NativeStorageItemInfo info, StorageFolder? parent)
			=> new StorageFile(new NativeStorageFile(info, parent));

		internal sealed class NativeStorageFile : ImplementationBase
		{
			// Used to keep track of the File handle on the Typescript side.
			private readonly Guid _id;
			private readonly string _fileName;
			private readonly StorageFolder? _parent;

			public NativeStorageFile(NativeStorageItemInfo nativeStorageItem, StorageFolder? parent)
				: base(SystemPath.Combine(parent?.Path ?? string.Empty, nativeStorageItem.Name ?? string.Empty))
			{
				if (parent != null && !(parent.Implementation is StorageFolder.NativeStorageFolder))
				{
					throw new ArgumentException("Parent folder of a native file must be a native folder", nameof(parent));
				}

				_id = nativeStorageItem.Id;
				_fileName = nativeStorageItem.Name ?? string.Empty;
				_parent = parent;
			}

			public override StorageProvider Provider => StorageProviders.WasmNative;

			public override string Name => _fileName;

			public override string FileType => SystemPath.GetExtension(_fileName);

			public override DateTimeOffset DateCreated => throw NotSupported();

			protected override bool IsEqual(ImplementationBase impl) => throw NotSupported();

			public override Task<StorageFolder?> GetParentAsync(CancellationToken ct) => Task.FromResult(_parent);

			public override async Task<BasicProperties> GetBasicPropertiesAsync(CancellationToken ct)
			{
				var basicPropertiesString = await NativeMethods.GetBasicPropertiesAsync(_id.ToString());

				var parts = basicPropertiesString.Split('|');

#pragma warning disable CA1806 // Do not ignore method results
				ulong.TryParse(parts[0], out ulong size);
#pragma warning restore CA1806 // Do not ignore method results

				var dateTimeModified = DateTimeOffset.UtcNow;
				if (long.TryParse(parts[1], out var dateModifiedUnixMilliseconds))
				{
					dateTimeModified = DateTimeOffset.FromUnixTimeMilliseconds(dateModifiedUnixMilliseconds);
				}

				return new BasicProperties(size, dateTimeModified);
			}

			public override async Task<IRandomAccessStreamWithContentType> OpenAsync(CancellationToken ct, FileAccessMode accessMode, StorageOpenOptions options)
				=> new RandomAccessStreamWithContentType(await FileRandomAccessStream.CreateNativeAsync(_id, accessMode), ContentType);

			public override Task<StorageStreamTransaction> OpenTransactedWriteAsync(CancellationToken ct, StorageOpenOptions option) => throw NotSupported();

			public override async Task DeleteAsync(CancellationToken ct, StorageDeleteOption options)
			{
				if (_parent == null)
				{
					throw new NotSupportedException("Cannot create a folder unless we can access its parent folder.");
				}

				var nativeParent = (StorageFolder.NativeStorageFolder)_parent.Implementation;
				await nativeParent.DeleteItemAsync(Name);
			}
		}
	}
}

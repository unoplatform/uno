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

namespace Windows.Storage
{
	public partial class StorageFile
	{
		internal static StorageFile GetFromNativeInfo(NativeStorageItemInfo info, StorageFolder? parent = null) =>
			new StorageFile(new NativeStorageFile(info, parent));

		internal sealed class NativeStorageFile : ImplementationBase
		{
			private const string JsType = "Uno.Storage.NativeStorageFile";

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

			public override StorageProvider Provider => StorageProviders.NativeWasm;

			public override string Name => _fileName;

			public override string FileType => SystemPath.GetExtension(_fileName);

			public override DateTimeOffset DateCreated => throw NotSupported();

			protected override bool IsEqual(ImplementationBase impl) => throw NotSupported();

			public override Task<StorageFolder?> GetParentAsync(CancellationToken ct) => Task.FromResult(_parent);

			public override async Task<BasicProperties> GetBasicPropertiesAsync(CancellationToken ct)
			{
				var basicPropertiesString = await WebAssemblyRuntime.InvokeAsync($"{JsType}.getBasicPropertiesAsync(\"{_id}\")");
				var parts = basicPropertiesString.Split('|');

				ulong.TryParse(parts[0], out ulong size);

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

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage.FileProperties;
using Windows.Storage.Streams;
using SystemPath = global::System.IO.Path;

namespace Windows.Storage
{
	public partial class StorageFile
	{
		internal static StorageFile GetFileFromNativePath(Guid guid, string name, string contentType) =>
			new StorageFile(new NativeStorageFile(guid, name, contentType));

		private sealed class NativeStorageFile : ImplementationBase
		{
			private const string JsType = "Windows.Storage.NativeStorageFile";

			// Used to keep track of the File handle on the Typescript side.
			private readonly Guid _id;
			private readonly string _fileName;
			private readonly string _contentType;

			public NativeStorageFile(Guid id, string fileName, string contentType)
				: base(string.Empty)
			{
				_id = id;
				_fileName = fileName;
				_contentType = contentType;
			}

			public override string Name => _fileName;

			public override string DisplayName => SystemPath.GetFileNameWithoutExtension(_fileName);

			public override string FileType => SystemPath.GetExtension(_fileName);

			public override string ContentType => _contentType;

			public override DateTimeOffset DateCreated => throw NotImplemented();

			protected override bool IsEqual(ImplementationBase impl) => throw NotImplemented();

			public override Task<StorageFolder> GetParentAsync(CancellationToken ct) => throw NotImplemented();

			public override Task<BasicProperties> GetBasicPropertiesAsync(CancellationToken ct) => throw NotImplemented();

			public override Task<Stream> OpenStreamAsync(CancellationToken ct, FileAccessMode accessMode, StorageOpenOptions options) => base.OpenStreamAsync(ct, accessMode, options);

			public override Task<IRandomAccessStreamWithContentType> OpenAsync(CancellationToken ct, FileAccessMode accessMode, StorageOpenOptions options) => throw NotImplemented();

			public override Task<StorageStreamTransaction> OpenTransactedWriteAsync(CancellationToken ct, StorageOpenOptions option) => throw NotImplemented();

			public override Task DeleteAsync(CancellationToken ct, StorageDeleteOption options) => throw NotImplemented();
		}
	}
}

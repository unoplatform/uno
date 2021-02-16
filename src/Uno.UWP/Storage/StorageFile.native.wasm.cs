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
		internal static StorageFile GetFileFromNativePathAsync(Guid guid, string name, string contentType) => new StorageFile(new NativeFileSystem(guid, name, contentType));

		private sealed class NativeFileSystem : ImplementationBase
		{
			private const string JsType = "Windows.Storage.StorageFileNative";

			// Used to keep track of the File handle on the Typescript side.
			private Guid _id;
			private string _fileName;
			private string _contentType;

			public NativeFileSystem(Guid id, string fileName, string contentType)
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

			public override DateTimeOffset DateCreated => throw new NotSupportedException("IsEqual is currently not supported on WASM native file system.");

			protected override bool IsEqual(ImplementationBase impl) => throw new NotSupportedException("IsEqual is currently not supported on WASM native file system.");

			public override Task<StorageFolder> GetParent(CancellationToken ct) => throw new NotImplementedException();

			public override Task<BasicProperties> GetBasicProperties(CancellationToken ct) => throw new NotImplementedException();

			public override Task<Stream> OpenStream(CancellationToken ct, FileAccessMode accessMode, StorageOpenOptions options) => base.OpenStream(ct, accessMode, options);

			public override Task<IRandomAccessStreamWithContentType> Open(CancellationToken ct, FileAccessMode accessMode, StorageOpenOptions options) => throw new NotImplementedException();

			public override Task<StorageStreamTransaction> OpenTransactedWrite(CancellationToken ct, StorageOpenOptions option) => throw new NotImplementedException();

			public override Task Delete(CancellationToken ct, StorageDeleteOption options) => throw new NotImplementedException();
		}
	}
}

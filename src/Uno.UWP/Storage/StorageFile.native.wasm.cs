using System;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage.FileProperties;
using Windows.Storage.Streams;

namespace Windows.Storage
{
	public partial class StorageFile
	{
		internal static StorageFile GetFileFromNativePathAsync(string path, Guid guid) => new StorageFile(new NativeFileSystem(path, guid));

		private sealed class NativeFileSystem : ImplementationBase
		{
			private const string JsType = "Windows.Storage.StorageFileNative";

			// Used to keep track of the File handle on the Typescript side.
			private Guid _id;

			public NativeFileSystem(string path, Guid id)
				: base(path)
			{
				_id = id;
			}

			public override DateTimeOffset DateCreated => throw new NotImplementedException();
			
			protected override bool IsEqual(ImplementationBase impl) => throw new NotImplementedException();
			public override Task<StorageFolder> GetParent(CancellationToken ct) => throw new NotImplementedException();
			public override Task<BasicProperties> GetBasicProperties(CancellationToken ct) => throw new NotImplementedException();
			public override Task<IRandomAccessStreamWithContentType> Open(CancellationToken ct, FileAccessMode accessMode, StorageOpenOptions options) => throw new NotImplementedException();
			public override Task<StorageStreamTransaction> OpenTransactedWrite(CancellationToken ct, StorageOpenOptions option) => throw new NotImplementedException();
			public override Task Delete(CancellationToken ct, StorageDeleteOption options) => throw new NotImplementedException();
		}
	}
}

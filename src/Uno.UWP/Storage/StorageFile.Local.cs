using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage.FileProperties;
using Windows.Storage.Streams;

namespace Windows.Storage
{
	partial class StorageFile
	{
		private sealed class Local : ImplementationBase
		{
			public Local(string path)
				: base(path)
			{
			}

			public override DateTimeOffset DateCreated => File.GetCreationTime(Path);

			protected override bool IsEqual(ImplementationBase impl)
				=> impl is Local other && Path.Equals(other.Path);

			public override async Task<StorageFolder> GetParent(CancellationToken ct)
				=> new StorageFolder(global::System.IO.Path.GetDirectoryName(Path));

			public override async Task<BasicProperties> GetBasicProperties(CancellationToken ct)
				=> new BasicProperties(Owner);

			public override async Task<IRandomAccessStreamWithContentType> Open(CancellationToken ct, FileAccessMode accessMode, StorageOpenOptions options)
				=> new RandomAccessStreamWithContentType(new FileRandomAccessStream(Path, ToFileAccess(accessMode), ToFileShare(options)), ContentType);

			public override async Task<Stream> OpenStream(CancellationToken ct, FileAccessMode accessMode, StorageOpenOptions options)
				=> File.Open(Path, FileMode.Open, ToFileAccess(accessMode), ToFileShare(options));

			public override async Task<StorageStreamTransaction> OpenTransactedWrite(CancellationToken ct, StorageOpenOptions option)
				=> new StorageStreamTransaction(Owner, ToFileShare(option));

			public override async Task Delete(CancellationToken ct, StorageDeleteOption options)
				=> File.Delete(Path);

			public override async Task CopyAndReplace(CancellationToken ct, IStorageFile target)
			{
				switch (target)
				{
					case StorageFile file when file._impl is Local:
						File.Copy(Path, file.Path, overwrite: true);
						break;

					default:
						await base.CopyAndReplace(ct, target);
						break;
				}
			}

			public override async Task MoveAndReplace(CancellationToken ct, IStorageFile target)
			{
				switch (target)
				{
					case StorageFile file when file._impl is Local:
						File.Delete(file.Path);
						File.Move(Path, file.Path);
						Path = target.Path;
						break;

					default:
						await base.MoveAndReplace(ct, target);
						break;
				}
			}
		}
	}
}

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

			public override async Task<StorageFolder> GetParentAsync(CancellationToken ct)
				=> new StorageFolder(global::System.IO.Path.GetDirectoryName(Path));

			public override async Task<BasicProperties> GetBasicPropertiesAsync(CancellationToken ct)
				=> BasicProperties.FromPath(Owner.Path);

			public override async Task<IRandomAccessStreamWithContentType> OpenAsync(CancellationToken ct, FileAccessMode accessMode, StorageOpenOptions options)
				=> new RandomAccessStreamWithContentType(FileRandomAccessStream.CreateLocal(Path, ToFileAccess(accessMode), ToFileShare(options)), ContentType);

			public override async Task<Stream> OpenStreamAsync(CancellationToken ct, FileAccessMode accessMode, StorageOpenOptions options)
				=> File.Open(Path, FileMode.Open, ToFileAccess(accessMode), ToFileShare(options));

			public override async Task<StorageStreamTransaction> OpenTransactedWriteAsync(CancellationToken ct, StorageOpenOptions option)
				=> new StorageStreamTransaction(Owner, ToFileShare(option));

			public override async Task DeleteAsync(CancellationToken ct, StorageDeleteOption options)
				=> File.Delete(Path);

			public override async Task CopyAndReplaceAsync(CancellationToken ct, IStorageFile target)
			{
				switch (target)
				{
					case StorageFile file when file._impl is Local:
						File.Copy(Path, file.Path, overwrite: true);
						break;

					default:
						await base.CopyAndReplaceAsync(ct, target);
						break;
				}
			}

			public override async Task MoveAndReplaceAsync(CancellationToken ct, IStorageFile target)
			{
				switch (target)
				{
					case StorageFile file when file._impl is Local:
						File.Delete(file.Path);
						File.Move(Path, file.Path);
						Path = target.Path;
						break;

					default:
						await base.MoveAndReplaceAsync(ct, target);
						break;
				}
			}
		}
	}
}

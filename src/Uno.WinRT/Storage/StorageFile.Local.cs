#nullable enable

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Uno.Storage.Internal;
using Windows.Storage.FileProperties;
using Windows.Storage.Streams;
using IOPath = System.IO.Path;

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

			public override StorageProvider Provider => StorageProviders.Local;

			public override DateTimeOffset DateCreated => File.GetCreationTime(Path);

			protected override bool IsEqual(ImplementationBase impl)
				=> impl is Local other && Path.Equals(other.Path);

			public override Task<StorageFolder?> GetParentAsync(CancellationToken ct)
			{
				var directoryPath = IOPath.GetDirectoryName(Path);
				return Task.FromResult(directoryPath != null ? new StorageFolder(directoryPath) : null);
			}

			public override Task<BasicProperties> GetBasicPropertiesAsync(CancellationToken ct)
				=> Task.FromResult(BasicProperties.FromFilePath(Owner.Path));

			public override Task<IRandomAccessStreamWithContentType> OpenAsync(CancellationToken ct, FileAccessMode accessMode, StorageOpenOptions options)
				=> Task.FromResult<IRandomAccessStreamWithContentType>(new RandomAccessStreamWithContentType(FileRandomAccessStream.CreateLocal(Path, ToFileAccess(accessMode), ToFileShare(options)), ContentType));

			public override Task<Stream> OpenStreamAsync(CancellationToken ct, FileAccessMode accessMode, StorageOpenOptions options)
				=> Task.FromResult<Stream>(File.Open(Path, ToFileMode(accessMode), ToFileAccess(accessMode), ToFileShare(options)));

			public override Task<StorageStreamTransaction> OpenTransactedWriteAsync(CancellationToken ct, StorageOpenOptions option)
				=> Task.FromResult(new StorageStreamTransaction(Owner, ToFileShare(option)));

			public override Task DeleteAsync(CancellationToken ct, StorageDeleteOption options)
			{
				File.Delete(Path);
				return Task.CompletedTask;
			}

			public override async Task CopyAndReplaceAsync(CancellationToken ct, IStorageFile target)
			{
				switch (target)
				{
					case StorageFile file when file.Implementation is Local:
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
					case StorageFile file when file.Implementation is Local:
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

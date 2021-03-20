using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Uno.Disposables;
using Windows.Storage;

namespace Uno.Storage.Streams.Internal
{
	internal class NativeWriteStream : Stream, IRentableStream
	{
		private const string CacheFolderName = "WasmNativeCache";

		private readonly StorageFile _cacheFile;
		private readonly Stream _cacheStream;
		private readonly Guid _fileId;
		private bool _pendingChanges = false;
		private RefCountDisposable _refCountDisposable;

		public override bool CanRead => _cacheStream.CanRead;

		public override bool CanSeek => _cacheStream.CanSeek;

		public override bool CanWrite => _cacheStream.CanWrite;

		public override long Length => _cacheStream.Length;

		public override long Position
		{
			get => _cacheStream.Position;
			set => _cacheStream.Position = value;
		}

		public StreamAccessScope AccessScope { get; } = new StreamAccessScope();

		private NativeWriteStream(StorageFile cacheFile, Stream cacheStream, Guid fileId)
		{
			_cacheFile = cacheFile;
			_cacheStream = cacheStream;
			_fileId = fileId;
			_refCountDisposable = new RefCountDisposable(Disposable.Create(() => Dispose()));
		}

		public RentedStream Rent()
		{
			var rentedStream = new RentedStream(this, _refCountDisposable.GetDisposable());
			_refCountDisposable.Dispose();
			return rentedStream;
		}

		public static async Task<NativeWriteStream> CreateAsync(Guid fileId)
		{
			var cacheFolder = await ApplicationData.Current.TemporaryFolder.CreateFolderAsync(CacheFolderName, CreationCollisionOption.OpenIfExists);
			var cacheFile = await cacheFolder.CreateFileAsync(Guid.NewGuid().ToString());
			var cacheStream = await cacheFile.OpenStream(CancellationToken.None, FileAccessMode.ReadWrite, StorageOpenOptions.None);
			var inputStream = await NativeReadStreamAdapter.CreateAsync(fileId);
			await inputStream.CopyToAsync(cacheStream);
			cacheStream.Seek(0, SeekOrigin.Begin);
			return new NativeWriteStream(cacheFile, cacheStream, fileId);
		}

		public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
		{
			return _cacheStream.ReadAsync(buffer, offset, count, cancellationToken);
		}		

		private async Task CopyToTargetAsync()
		{
			using var targetStream = await NativeWriteStreamAdapter.CreateAsync(_fileId);
			await targetStream.TruncateAsync(0);
			await _cacheStream.FlushAsync();
			_cacheStream.Seek(0, SeekOrigin.Begin);
			await _cacheStream.CopyToAsync(targetStream);
			await targetStream.FlushAsync();
			await targetStream.CloseAsync();
		}

		public override void Flush()
		{
			_cacheStream.Flush();
		}

		public override async Task FlushAsync(CancellationToken cancellationToken)
		{
			if (_pendingChanges)
			{
				await _cacheStream.FlushAsync();
				await CopyToTargetAsync();
				_pendingChanges = false;
			}
		}

		public override long Seek(long offset, SeekOrigin origin)
		{
			return _cacheStream.Seek(offset, origin);
		}

		public override void SetLength(long value)
		{
			_cacheStream.SetLength(value);
			_pendingChanges = true;
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			return _cacheStream.Read(buffer, offset, count);
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			_cacheStream.Write(buffer, offset, count);
			_pendingChanges = true;
		}

		public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
		{
			await _cacheStream.WriteAsync(buffer, offset, count, cancellationToken);
			_pendingChanges = true;
		}

		protected override async void Dispose(bool disposing)
		{
			if (_pendingChanges)
			{
				// Unfortunately need to do a fire-and-forget here, as the operations
				// are required to be asynchronous on JS side.
				await CopyToTargetAsync();
			}
			_cacheStream.Dispose();
			File.Delete(_cacheFile.Path);
		}
	}
}

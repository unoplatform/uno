using Android.App;
using Android.OS;
using Java.IO;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Uno.Disposables;
using Windows.Storage;

namespace Uno.Storage.Streams.Internal
{
	internal class SafOutputStream : Stream, IRentableStream
	{
		private const string CacheFolderName = "SafCache";

		private readonly StorageFile _cacheFile;
		private readonly Stream _cacheStream;
		private readonly Android.Net.Uri _targetUri;
		private bool _pendingChanges;
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

		private SafOutputStream(StorageFile cacheFile, Stream cacheStream, Android.Net.Uri targetUri)
		{
			_cacheFile = cacheFile;
			_cacheStream = cacheStream;
			_targetUri = targetUri;
			_refCountDisposable = new RefCountDisposable(Disposable.Create(() => Dispose()));
		}

		public RentedStream Rent()
		{
			var rentedStream = new RentedStream(this, _refCountDisposable.GetDisposable());
			_refCountDisposable.Dispose();
			return rentedStream;
		}

		public static async Task<SafOutputStream> CreateAsync(Android.Net.Uri uri)
		{
			var cacheFolder = await ApplicationData.Current.TemporaryFolder.CreateFolderAsync(CacheFolderName, CreationCollisionOption.OpenIfExists);
			var cacheFile = await cacheFolder.CreateFileAsync(Guid.NewGuid().ToString());
			var cacheStream = await cacheFile.OpenStream(CancellationToken.None, FileAccessMode.ReadWrite, StorageOpenOptions.None);
			var inputStream = Application.Context.ContentResolver!.OpenInputStream(uri);
			await inputStream!.CopyToAsync(cacheStream);
			cacheStream.Seek(0, SeekOrigin.Begin);
			return new SafOutputStream(cacheFile, cacheStream, uri);
		}

		public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
		{
			return _cacheStream.ReadAsync(buffer, offset, count, cancellationToken);
		}

		private void CopyToTarget()
		{
			TruncateTarget();
			using var targetStream = CreateTargetStream();
			_cacheStream.Flush();
			_cacheStream.Seek(0, SeekOrigin.Begin);
			_cacheStream.CopyTo(targetStream);
			targetStream.Flush();
		}

		private async Task CopyToTargetAsync()
		{
			await TruncateTargetAsync();
			using var targetStream = CreateTargetStream();
			await _cacheStream.FlushAsync();
			_cacheStream.Seek(0, SeekOrigin.Begin);
			await _cacheStream.CopyToAsync(targetStream);
			await targetStream.FlushAsync();
		}

		private Stream CreateTargetStream() => Application.Context.ContentResolver!.OpenOutputStream(_targetUri, "wt")!;

		private ParcelFileDescriptor CreateTargetDescriptor() => Application.Context.ContentResolver!.OpenFileDescriptor(_targetUri, "wt")!;

		private async Task TruncateTargetAsync()
		{
			using var descriptor = CreateTargetDescriptor();
			using var targetStream = new FileOutputStream(descriptor.FileDescriptor);
			await targetStream.Channel!.TruncateAsync(0);
		}

		private void TruncateTarget()
		{
			using var descriptor = CreateTargetDescriptor();
			using var targetStream = new FileOutputStream(descriptor.FileDescriptor);
			targetStream.Channel!.Truncate(0);
		}

		public override void Flush()
		{
			if (_pendingChanges)
			{
				_cacheStream.Flush();
				CopyToTarget();
				_pendingChanges = false;
			}
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

		public override void Close()
		{
			if (_pendingChanges)
			{
				CopyToTarget();
			}
		}

		protected override void Dispose(bool disposing)
		{
			if (_pendingChanges)
			{
				CopyToTarget();
			}
			_cacheStream.Dispose();
			System.IO.File.Delete(_cacheFile.Path);
		}

		public override async ValueTask DisposeAsync()
		{
			if (_pendingChanges)
			{
				await CopyToTargetAsync();
			}
			await _cacheStream.DisposeAsync();
			await _cacheFile.DeleteAsync();
		}
	}
}

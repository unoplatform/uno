using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Uno.Disposables;

namespace Uno.Storage.Streams.Internal
{
	internal class NativeReadStream : Stream, IRentableStream
	{
		private const string CacheFolderName = "WasmNativeCache";

		private readonly NativeReadStreamAdapter _readStreamAdapter;
		private readonly Guid _fileId;

		private RefCountDisposable _refCountDisposable;

		private NativeReadStream(Guid fileId, NativeReadStreamAdapter readStreamAdapter)
		{
			_fileId = fileId;
			_readStreamAdapter = readStreamAdapter;
			_refCountDisposable = new RefCountDisposable(Disposable.Create(() => Dispose()));
		}

		public StreamAccessScope AccessScope { get; } = new StreamAccessScope();

		public override bool CanRead => _readStreamAdapter.CanRead;

		public override bool CanSeek => _readStreamAdapter.CanSeek;

		public override bool CanWrite => _readStreamAdapter.CanWrite;

		public override long Length => _readStreamAdapter.Length;

		public override long Position
		{
			get => _readStreamAdapter.Position;
			set => _readStreamAdapter.Position = value;
		}	

		public RentedStream Rent()
		{
			var rentedStream = new RentedStream(this, _refCountDisposable.GetDisposable());
			_refCountDisposable.Dispose();
			return rentedStream;
		}

		public override void SetLength(long value) => throw new NotSupportedException("This stream is read-only");

		public override void Flush() { }

		public static async Task<NativeReadStream> CreateAsync(Guid fileId)
		{
			var inputStream = await NativeReadStreamAdapter.CreateAsync(fileId);
			return new NativeReadStream(fileId, inputStream);
		}

		public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
		{
			return _readStreamAdapter.ReadAsync(buffer, offset, count, cancellationToken);
		}

		public override long Seek(long offset, SeekOrigin origin)
		{
			return _readStreamAdapter.Seek(offset, origin);
		}

		public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException("This stream is asynchronous-only");

		public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException("This stream is read-only");

		public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => throw new NotSupportedException("This stream is read-only");

		protected override async void Dispose(bool disposing)
		{
			_readStreamAdapter.Dispose();
		}
	}
}

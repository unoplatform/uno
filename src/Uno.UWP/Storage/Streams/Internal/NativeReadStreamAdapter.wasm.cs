using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Uno.Storage.Streams.Internal
{
	internal class NativeReadStreamAdapter : Stream, INativeStreamAdapter
    {
		private readonly Guid _fileId;

		public NativeReadStreamAdapter(Guid fileId)
		{
			_fileId = fileId;
		}

		public override bool CanRead => true;

		public override bool CanSeek => true;

		public override bool CanWrite => false;

		public override long Length => throw new NotImplementedException();

		public override long Position { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

		public override void Flush() => throw new NotImplementedException();
		public override int Read(byte[] buffer, int offset, int count) => throw new NotImplementedException();
		public override long Seek(long offset, SeekOrigin origin) => throw new NotImplementedException();
		public override void SetLength(long value) => throw new NotImplementedException();
		public override void Write(byte[] buffer, int offset, int count) => throw new NotImplementedException();

		public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => base.ReadAsync(buffer, offset, count, cancellationToken);

		public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => base.WriteAsync(buffer, offset, count, cancellationToken);
		public void Rent() => throw new NotImplementedException();
	}
}

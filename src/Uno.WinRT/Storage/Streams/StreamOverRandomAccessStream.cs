#nullable enable

using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Windows.Storage.Streams
{
	internal class StreamOverRandomAccessStream : Stream, IRandomStreamWrapper
	{
		private readonly IRandomAccessStream _raStream;
		private readonly int _bufferSize;

		public StreamOverRandomAccessStream(IRandomAccessStream raStream, int bufferSize)
		{
			_raStream = raStream;
			_bufferSize = bufferSize;
		}

		IRandomAccessStream IRandomStreamWrapper.FindStream() => _raStream;
		IInputStream IInputStreamWrapper.FindStream() => _raStream;
		IOutputStream IOutputStreamWrapper.FindStream() => _raStream;

		/// <inheritdoc />
		public override bool CanRead => _raStream.CanRead;

		/// <inheritdoc />
		public override bool CanSeek => true;

		/// <inheritdoc />
		public override bool CanWrite => _raStream.CanWrite;

		/// <inheritdoc />
		public override long Length => (long)_raStream.Size;

		/// <inheritdoc />
		public override long Position
		{
			get => (long)_raStream.Position;
			set => _raStream.Seek((ulong)value);
		}

		/// <inheritdoc />
		public override long Seek(long offset, SeekOrigin origin)
		{
			switch (origin)
			{
				case SeekOrigin.Begin:
					_raStream.Seek((ulong)offset);
					break;

				case SeekOrigin.Current:
					_raStream.Seek(_raStream.Position + (ulong)offset);
					break;

				case SeekOrigin.End:
					_raStream.Seek(_raStream.Size - (ulong)offset);
					break;
			}

			return (long)_raStream.Position;
		}

		/// <inheritdoc />
		public override void SetLength(long value)
			=> throw new NotSupportedException("Cannot set length.");


		/// <inheritdoc />
		public override int Read(byte[] buffer, int offset, int count)
			=> ReadAsync(buffer, offset, count, CancellationToken.None).Result;

		/// <inheritdoc />
		public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
			=> _raStream.ReadAsync(buffer, offset, count, cancellationToken);

		/// <inheritdoc />
		public override void Write(byte[] buffer, int offset, int count)
			=> WriteAsync(buffer, offset, count, CancellationToken.None).Wait();

		/// <inheritdoc />
		public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
			=> _raStream.WriteAsync(buffer, offset, count, cancellationToken);

		/// <inheritdoc />
		public override void Flush()
			=> FlushAsync(CancellationToken.None).Wait();

		/// <inheritdoc />
		public override Task FlushAsync(CancellationToken cancellationToken)
			=> _raStream.FlushAsync().AsTask(cancellationToken);
	}
}

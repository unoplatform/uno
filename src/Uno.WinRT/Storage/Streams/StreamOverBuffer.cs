using System;
using System.IO;
using System.Linq;

namespace Windows.Storage.Streams
{
	internal class StreamOverBuffer : Stream
	{
		private readonly Buffer _buffer;
		private long _position;

		public StreamOverBuffer(Buffer buffer)
		{
			_buffer = buffer;
		}

		/// <inheritdoc />
		public override bool CanRead { get; } = true;

		/// <inheritdoc />
		public override bool CanSeek { get; } = true;

		/// <inheritdoc />
		public override bool CanWrite { get; } = true;

		/// <inheritdoc />
		public override long Length => _buffer.Length;

		/// <inheritdoc />
		public override long Position
		{
			get => _position;
			set
			{
				if (value < 0) throw new ArgumentOutOfRangeException(nameof(value), "Position cannot be lower than 0");
				if (value > Length) throw new ArgumentOutOfRangeException(nameof(value), "Position cannot be equals or greater than Length");
				_position = value;
			}
		}

		public override void Flush() { }

		/// <inheritdoc />
		public override long Seek(long offset, SeekOrigin origin)
			=> origin switch
			{
				SeekOrigin.Begin => Position = offset,
				SeekOrigin.Current => Position += offset,
				SeekOrigin.End => Position = Length - 1,
				_ => throw new ArgumentOutOfRangeException(nameof(origin), "Unknown SeekOrigin " + origin),
			};

		/// <inheritdoc />
		public override void SetLength(long value)
		{
			if (value > _buffer.Capacity)
			{
				throw new ArgumentOutOfRangeException(nameof(value), "Cannot set a length greater than the underlying buffer");
			}

			_buffer.Length = (uint)value;
		}

		/// <inheritdoc />
		public override int Read(byte[] buffer, int offset, int count)
		{
			var read = (int)Math.Min(Length - Position, count);
			_buffer.CopyTo((uint)Position, buffer, offset, read);
			Position += read;
			return read;
		}

		/// <inheritdoc />
		public override void Write(byte[] buffer, int offset, int count)
		{
			_buffer.Write((uint)Position, buffer, offset, count);
			Position += count;
		}
	}
}

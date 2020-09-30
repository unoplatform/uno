using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Windows.Storage
{
	internal class OwnedStream : Stream
	{
		private readonly Stream _stream;
		private readonly object _owner;

		public OwnedStream(Stream stream, object owner)
		{
			_stream = stream;
			_owner = owner;
		}

		public void DisposeBy(object owner)
		{
			if (_owner == owner)
			{
				_stream.Dispose();
			}
		}

		public override void Flush()
			=> _stream.Flush();

		public override int Read(byte[] buffer, int offset, int count)
			=> _stream.Read(buffer, offset, count);

		public override long Seek(long offset, SeekOrigin origin)
			=> _stream.Seek(offset, origin);

		public override void SetLength(long value)
			=> _stream.SetLength(value);

		public override void Write(byte[] buffer, int offset, int count)
			=> _stream.Write(buffer, offset, count);

		public override bool CanRead => _stream.CanRead;

		public override bool CanSeek => _stream.CanSeek;

		public override bool CanWrite => _stream.CanWrite;

		public override long Length => _stream.Length;

		public override long Position
		{
			get => _stream.Position;
			set => _stream.Position = value;
		}
	}
}

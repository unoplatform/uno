#nullable enable

using System;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Logging;
using Uno.Extensions;

namespace Windows.Storage
{
	/// <summary>
	/// A file that self deletes himself when no longer used
	/// </summary>
	internal class TemporaryFile
	{
		private readonly FileInfo _file;
		private int _users;

		public TemporaryFile()
		{
			_file = new FileInfo(Path.GetTempFileName());
		}

		/// <summary>
		/// Opens a stream to the loaded data
		/// </summary>
		public Stream Open(FileAccess mode)
		{
			Interlocked.Increment(ref _users);

			return new TempFileStream(this, _file.Open(FileMode.OpenOrCreate, mode, FileShare.ReadWrite));
		}

		public Stream OpenWeak(FileAccess mode)
		{
			return _file.Open(FileMode.OpenOrCreate, mode, FileShare.ReadWrite | FileShare.Delete);
		}

		private void OnClose()
		{
			if (Interlocked.Decrement(ref _users) == 0)
			{
				if (_file.Exists)
				{
					try
					{
						_file.Delete();
					}
					catch (Exception e)
					{
						this.Log().LogError($"Failed to delete temporary file {_file.FullName}", e);
					}
				}
			}
		}

		private class TempFileStream : Stream
		{
			private readonly TemporaryFile _tempFile;
			private readonly Stream _stream;
			private int _released = 0;

			public TempFileStream(TemporaryFile tempFile, Stream stream)
			{
				_tempFile = tempFile;
				_stream = stream;
			}

			public override void Close()
			{
				if (Interlocked.Exchange(ref _released, 1) == 0)
				{
					_tempFile.OnClose();
				}
				_stream.Close();
				base.Close();
			}

			protected override void Dispose(bool disposing)
			{
				if (Interlocked.Exchange(ref _released, 1) == 0)
				{
					_tempFile.OnClose();
				}
				base.Dispose(disposing);
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

			~TempFileStream()
				=> Dispose();
		}
	}
}

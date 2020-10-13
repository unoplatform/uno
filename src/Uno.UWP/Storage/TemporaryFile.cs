#nullable enable

using System;
using System.IO;
using System.Linq;
using System.Threading;
using Windows.Graphics.Capture;
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
			_file.Directory?.Create();
		}

		/// <summary>
		/// Gets a boolean which indicates if this temporary file is still available for read/write
		/// </summary>
		/// <remarks>
		/// This is use-full essentially for weak streams that want to check if this temp file is still
		/// active before reading or writing data in order to avoid exception.
		/// </remarks>
		public bool IsActive { get; private set; } = true;

		/// <summary>
		/// Opens a stream to the temporary file.
		/// This temporary file will remain active (cf. <see cref="IsActive"/>) until the returned stream is being disposed.
		/// </summary>
		public Stream Open(FileAccess mode)
		{
			Interlocked.Increment(ref _users);
			CheckIsActive();

			return new StrongStream(this, mode);
		}

		/// <summary>
		/// Opens a stream to the temporary file.
		/// This temporary file might become inactive (i.e. deleted - cf. <see cref="IsActive"/>) even if this stream has not been closed yet.
		/// If the temporary file becomes inactive, attempt to read / write data on the resulting stream, will throw exception.
		/// </summary>
		public Stream OpenWeak(FileAccess mode)
		{
			CheckIsActive();

			return new WeakStream(this, mode);
		}

		/// <inheritdoc />
		public override string ToString()
			=> "[TEMP]" + _file.FullName;

		private void OnStrongStreamedClosed()
		{
			if (Interlocked.Decrement(ref _users) == 0)
			{
				IsActive = false;
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

		private void CheckIsActive()
		{
			if (!IsActive)
			{
				throw new IOException("This temporary file has been deleted.");
			}
		}

		private class StrongStream : Stream
		{
			private readonly TemporaryFile _tempFile;
			private readonly Stream _stream;

			private int _released;

			public StrongStream(TemporaryFile tempFile, FileAccess mode)
			{
				_tempFile = tempFile;
				_stream = tempFile._file.Open(FileMode.OpenOrCreate, mode, FileShare.ReadWrite);
			}

			public override void Close()
			{
				_stream.Close();
				if (Interlocked.Exchange(ref _released, 1) == 0)
				{
					_tempFile.OnStrongStreamedClosed();
				}
				base.Close();
			}

			protected override void Dispose(bool disposing)
			{
				_stream.Dispose();
				if (Interlocked.Exchange(ref _released, 1) == 0)
				{
					_tempFile.OnStrongStreamedClosed();
				}
				base.Dispose(disposing);

				GC.SuppressFinalize(this);
			}

			public override bool CanRead => _stream.CanRead;

			public override bool CanSeek => _stream.CanSeek;

			public override bool CanWrite => _stream.CanWrite;

			public override long Length => _stream.Length;

			public override long Position
			{
				get => _stream.Position;
				set => _stream.Position = value;
			}

			public override void SetLength(long value)
				=> _stream.SetLength(value);

			public override long Seek(long offset, SeekOrigin origin)
				=> _stream.Seek(offset, origin);

			public override int Read(byte[] buffer, int offset, int count)
				=> _stream.Read(buffer, offset, count);

			public override void Write(byte[] buffer, int offset, int count)
				=> _stream.Write(buffer, offset, count);

			public override void Flush()
				=> _stream.Flush();

			~StrongStream()
				=> Dispose();
		}

		private class WeakStream : Stream
		{
			private readonly TemporaryFile _tempFile;
			private readonly Stream _stream;

			public WeakStream(TemporaryFile tempFile, FileAccess mode)
			{
				_tempFile = tempFile;
				_stream = tempFile._file.Open(FileMode.OpenOrCreate, mode, FileShare.ReadWrite | FileShare.Delete);
			}

			public override bool CanRead => _tempFile.IsActive && _stream.CanRead;

			public override bool CanSeek => _tempFile.IsActive && _stream.CanSeek;

			public override bool CanWrite => _tempFile.IsActive && _stream.CanWrite;

			public override long Length => _stream.Length;

			public override long Position
			{
				get => _stream.Position;
				set => _stream.Position = value;
			}

			public override void SetLength(long value)
			{
				_tempFile.CheckIsActive();
				_stream.SetLength(value);
			}

			public override long Seek(long offset, SeekOrigin origin)
			{
				_tempFile.CheckIsActive();
				return _stream.Seek(offset, origin);
			}

			public override int Read(byte[] buffer, int offset, int count)
			{
				_tempFile.CheckIsActive();
				return _stream.Read(buffer, offset, count);
			}

			public override void Write(byte[] buffer, int offset, int count)
			{
				_tempFile.CheckIsActive();
				_stream.Write(buffer, offset, count);
			}

			public override void Flush()
			{
				_tempFile.CheckIsActive();
				_stream.Flush();
			}
		}
	}
}

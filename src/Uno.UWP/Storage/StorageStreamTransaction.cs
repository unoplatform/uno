#nullable enable
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage.Streams;
using Uno.Extensions;
using Uno.Logging;

namespace Windows.Storage
{
	public sealed partial class StorageStreamTransaction : IDisposable
	{
		private readonly object _gate;
		private readonly StorageFile _file;

		private IDisposable? _fileLock;
		private Stream? _tempFile;

		internal StorageStreamTransaction(StorageFile file, FileShare share)
		{
			_file = file;
			_gate = _file.Path; // Set the gate to the file path in order to prevent concurrent access (only for this process unfortunately)

			OpenFiles();
		}

		/// <summary>
		/// Gets a wrapper of this transaction which will auto commit the changes when it is being disposed.
		/// </summary>
		internal Stream AsAutoCommitStream()
			=> new AutoCommitStream(this, _tempFile!);

		public IRandomAccessStream Stream { get; private set; } = null!;

		public IAsyncAction CommitAsync() => AsyncAction.FromTask(async ct =>
		{
			ct.ThrowIfCancellationRequested();
			Commit();
		});

		private void Commit()
		{
			lock (_gate)
			{
				if (_fileLock is null)
				{
					throw new ObjectDisposedException(nameof(StorageStreamTransaction));
				}

				CloseFiles();
				File.Replace(_file.Path + ".tmp", _file.Path, null);
				OpenFiles();
			}
		}

		private void OpenFiles()
		{
			FileStream src, dst;
			_fileLock = src = File.Open(_file.Path, FileMode.Open, FileAccess.Read, FileShare.None);
			_tempFile = dst = File.Open(_file.Path + ".tmp", FileMode.Create, FileAccess.Write, FileShare.None);

			// First we make sure to clone the content of the current file in the temp file,
			// so user can seek to append content.
			src.CopyTo(dst);
			dst.Seek(0, SeekOrigin.Begin);

			Stream = new TransactionRandomStream(_tempFile);
		}

		private void CloseFiles()
		{
			_fileLock?.Dispose();
			_fileLock = null;
			_tempFile?.Dispose();
			_tempFile = null;
		}

		public void Dispose()
		{
			GC.SuppressFinalize(this);
			Dispose(true);
		}

		private void Dispose(bool isDisposing)
		{
			if (isDisposing)
			{
			}

			lock (_gate)
			{
				CloseFiles();

				try
				{
					File.Delete(_file.Path + ".tmp");
				}
				catch (Exception e)
				{
					this.Log().Error("Could not delete temporary file.", e);
				}
			}
		}

		~StorageStreamTransaction()
		{
			Dispose(false);
		}

		private class TransactionRandomStream : RandomAccessStreamOverStream
		{
			public TransactionRandomStream(Stream stream)
				: base(stream)
			{
			}

			/// <inheritdoc />
			public override void Dispose()
			{
				// We don not close the underlying stream as it's acts as a lock
			}
		}

		private class AutoCommitStream : Stream
		{
			private readonly StorageStreamTransaction _owner;
			private readonly Stream _temp;

			private int _isDisposed;

			public AutoCommitStream(StorageStreamTransaction owner, Stream temp)
			{
				_owner = owner;
				_temp = temp;
			}

			/// <inheritdoc />
			protected override void Dispose(bool disposing)
			{
				if (Interlocked.Exchange(ref _isDisposed, 1) == 0)
				{
					_owner.Commit();
					_owner.Dispose();
				}

				base.Dispose(disposing);
			}
				

			/// <inheritdoc />
			public override void Flush()
				=> _temp.Flush();

			/// <inheritdoc />
			public override int Read(byte[] buffer, int offset, int count)
				=> _temp.Read(buffer, offset, count);

			/// <inheritdoc />
			public override long Seek(long offset, SeekOrigin origin)
				=> _temp.Seek(offset, origin);

			/// <inheritdoc />
			public override void SetLength(long value)
				=> _temp.SetLength(value);

			/// <inheritdoc />
			public override void Write(byte[] buffer, int offset, int count)
				=> _temp.Write(buffer, offset, count);

			/// <inheritdoc />
			public override bool CanRead => _temp.CanRead;

			/// <inheritdoc />
			public override bool CanSeek => _temp.CanSeek;

			/// <inheritdoc />
			public override bool CanWrite => _temp.CanWrite;

			/// <inheritdoc />
			public override long Length => _temp.Length;

			/// <inheritdoc />
			public override long Position
			{
				get => _temp.Position;
				set => _temp.Position = value;
			}
		}
	}
}

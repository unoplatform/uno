using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Uno.Disposables;
using Uno.Foundation.Logging;

namespace Uno.Storage.Streams.Internal
{
	internal class NativeWriteStream : Stream, IRentableStream
	{
		private const int CopyBufferSize = 2 * 1024 * 1024;

		// The File System Access writable is write-only and its pending content cannot be
		// read back, while this stream must honor the ReadWrite random-access contract.
		// Writes are therefore staged in a JS-side chunked buffer (readable, seekable,
		// off the WASM heap and free of contiguous-allocation ceilings) and copied to
		// the target on flush/dispose.
		private readonly ChunkedBufferStream _cacheStream;
		private readonly Guid _fileId;
		private bool _pendingChanges;
		private long _dirtyStart = long.MaxValue;
		private long _dirtyEnd;
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

		private NativeWriteStream(ChunkedBufferStream cacheStream, Guid fileId)
		{
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
			var cacheStream = ChunkedBufferStream.Create();
			try
			{
				// Stage the existing target content so it can be read back and partially overwritten.
				using var inputStream = await NativeReadStreamAdapter.CreateAsync(fileId);
				await inputStream.CopyToAsync(cacheStream);
				cacheStream.Seek(0, SeekOrigin.Begin);
				return new NativeWriteStream(cacheStream, fileId);
			}
			catch
			{
				cacheStream.Dispose();
				throw;
			}
		}

		public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
		{
			return _cacheStream.ReadAsync(buffer, offset, count, cancellationToken);
		}

		private void MarkDirty(long start, long end)
		{
			_dirtyStart = Math.Min(_dirtyStart, start);
			_dirtyEnd = Math.Max(_dirtyEnd, end);
			_pendingChanges = true;
		}

		private async Task CopyToTargetAsync()
		{
			using var targetStream = await NativeWriteStreamAdapter.CreateAsync(_fileId);

			// Committing must not move the caller's cursor.
			var callerPosition = _cacheStream.Position;
			try
			{
				await CopyDirtyRangeToTargetAsync(targetStream);
			}
			finally
			{
				_cacheStream.Seek(callerPosition, SeekOrigin.Begin);
			}

			await targetStream.CloseAsync();

			_dirtyStart = long.MaxValue;
			_dirtyEnd = 0;
		}

		private async Task CopyDirtyRangeToTargetAsync(NativeWriteStreamAdapter targetStream)
		{
			var cacheLength = _cacheStream.Length;
			var dirtyEnd = Math.Min(_dirtyEnd, cacheLength);

			// Only the range written since the last commit needs copying - the rest of
			// the target's content is preserved by the writable (keepExistingData).
			if (dirtyEnd > _dirtyStart)
			{
				_cacheStream.Seek(_dirtyStart, SeekOrigin.Begin);
				targetStream.Seek(_dirtyStart, SeekOrigin.Begin);

				var buffer = new byte[(int)Math.Min(CopyBufferSize, dirtyEnd - _dirtyStart)];
				var remaining = dirtyEnd - _dirtyStart;
				while (remaining > 0)
				{
					var read = await _cacheStream.ReadAsync(buffer, 0, (int)Math.Min(buffer.Length, remaining), CancellationToken.None);
					if (read <= 0)
					{
						// Committing fewer bytes than were staged would silently truncate the file.
						throw new IOException(
							$"The staged content ended {remaining} bytes before the end of the range to commit.");
					}

					await targetStream.WriteAsync(buffer, 0, read, CancellationToken.None);
					remaining -= read;
				}
			}

			if (targetStream.Length != cacheLength)
			{
				// Shrinks or zero-pad extends the target to the staged length.
				await targetStream.TruncateAsync(cacheLength);
			}
		}

		public override void Flush()
		{
			_cacheStream.Flush();
		}

		public override async Task FlushAsync(CancellationToken cancellationToken)
		{
			if (_pendingChanges)
			{
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
			var previousLength = _cacheStream.Length;
			_cacheStream.SetLength(value);

			// A resize can turn existing target bytes into a zero-filled gap in the staged
			// copy. The target keeps its own bytes there (the writable preserves existing
			// data), so the resized range has to be committed rather than inferred from the
			// final length alone.
			MarkDirty(Math.Min(previousLength, value), Math.Max(previousLength, value));
			_pendingChanges = true;
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			return _cacheStream.Read(buffer, offset, count);
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			var start = _cacheStream.Position;
			_cacheStream.Write(buffer, offset, count);
			MarkDirty(start, start + count);
		}

		public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
		{
			var start = _cacheStream.Position;
			await _cacheStream.WriteAsync(buffer, offset, count, cancellationToken);
			MarkDirty(start, start + count);
		}

		protected override async void Dispose(bool disposing)
		{
			try
			{
				if (_pendingChanges)
				{
					// Unfortunately need to do a fire-and-forget here, as the operations
					// are required to be asynchronous on JS side.
					await CopyToTargetAsync();
				}
			}
			catch (Exception e)
			{
				// Must not throw - an exception escaping an async void method is fatal.
				if (this.Log().IsEnabled(LogLevel.Warning))
				{
					this.Log().Warn("Failed to write pending changes to the target file.", e);
				}
			}

			try
			{
				// Release the staged chunks even when the copy failed.
				_cacheStream.Dispose();
			}
			catch (Exception e)
			{
				if (this.Log().IsEnabled(LogLevel.Warning))
				{
					this.Log().Warn("Failed to release the write stream cache buffer.", e);
				}
			}
		}
	}
}

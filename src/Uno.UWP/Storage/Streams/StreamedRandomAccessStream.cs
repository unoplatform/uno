#nullable enable

using System;
using System.IO;
using System.Linq;
using System.Threading;
using Windows.Foundation;

namespace Windows.Storage.Streams
{
	internal class StreamedRandomAccessStream : IRandomAccessStreamWithContentType
	{
		private readonly IStreamedDataLoader? _loader;
		private readonly IStreamedDataUploader? _uploader;
		private readonly Stream _temp;
		private readonly StreamedInputStream? _input;
		private readonly StreamedOutputStream? _output;
		private readonly string? _contentType;

		private int _isDisposed;

		public StreamedRandomAccessStream(
			IStreamedDataLoader? loader = null,
			IStreamedDataUploader? uploader = null,
			string? contentType = null)
		{
			_loader = loader;
			_uploader = uploader;
			_contentType = contentType;

			if (loader is null && uploader is null)
			{
				throw new InvalidOperationException("You must provide at one valid access mode (read or write).");
			}

			var file = loader?.File ?? uploader!.File;
			if (uploader?.File is {} upFile && file != upFile)
			{
				throw new InvalidOperationException(
					"If StreamedRandomAccessStream can be Read and Write, "
					+ "provided loader and uploader must use the same temporary file.");
			}

			_temp = file.Open(FileAccess.ReadWrite);

			if (_loader is {})
			{
				_input = new StreamedInputStream(_temp, _loader);
			}

			if (_uploader is {})
			{
				_output = new StreamedOutputStream(_temp, _uploader);
			}
		}

		/// <inheritdoc />
		public string ContentType => _contentType ?? _loader?.ContentType ?? "application/octet-stream";

		/// <inheritdoc />
		public bool CanRead => _input is {};

		/// <inheritdoc />
		public bool CanWrite => _output is { };

		/// <inheritdoc />
		public ulong Position => (ulong)_temp.Position;

		/// <inheritdoc />
		public ulong Size => (ulong)_temp.Length;

		/// <inheritdoc />
		public IAsyncOperationWithProgress<IBuffer, uint> ReadAsync(IBuffer buffer, uint count, InputStreamOptions options)
		{
			CheckCanRead();
			return _input!.ReadAsync(buffer, count, options);
		}

		/// <inheritdoc />
		public IAsyncOperationWithProgress<uint, uint> WriteAsync(IBuffer buffer)
		{
			CheckCanWrite();
			return _output!.WriteAsync(buffer);
		}

		/// <inheritdoc />
		public IAsyncOperation<bool> FlushAsync()
		{
			CheckCanWrite();
			return _output!.FlushAsync();
		}

		/// <inheritdoc />
		public IInputStream GetInputStreamAt(ulong position)
		{
			CheckCanRead();
			return new StreamedInputStream(_loader!, position);
		}

		/// <inheritdoc />
		public IOutputStream GetOutputStreamAt(ulong position)
		{
			CheckCanWrite();
			return new StreamedOutputStream(_uploader!, position);
		}

		/// <inheritdoc />
		public void Seek(ulong position)
			=> _temp.Seek((long)position, SeekOrigin.Begin);

		/// <inheritdoc />
		public IRandomAccessStream CloneStream()
			=> new StreamedRandomAccessStream(_loader, _uploader, _contentType);

		/// <inheritdoc />
		public void Dispose()
		{
			// Note: We DO NOT dispose the _loader as it might been used by some other stream (it's designed to be self-disposable)

			if (Interlocked.Exchange(ref _isDisposed, 1) == 0)
			{
				_input?.Dispose();
				_output?.Dispose();

				_temp.Dispose();
			}
		}

		private void CheckCanRead()
		{
			_loader?.CheckState();
			_uploader?.CheckState();

			if (_isDisposed != 0)
			{
				throw new ObjectDisposedException(nameof(StreamedRandomAccessStream));
			}

			if (!CanRead)
			{
				throw new InvalidOperationException("This stream does not support read operations");
			}
		}

		private void CheckCanWrite()
		{
			_loader?.CheckState();
			_uploader?.CheckState();

			if (_isDisposed != 0)
			{
				throw new ObjectDisposedException(nameof(StreamedRandomAccessStream));
			}

			if (!CanWrite)
			{
				throw new InvalidOperationException("This stream does not support write operations");
			}
		}

		~StreamedRandomAccessStream()
		{
			Dispose();
		}
	}
}

#nullable enable

using System;
using System.Linq;
using Windows.Foundation;

namespace Windows.Storage.Streams
{
	internal class RandomAccessStreamWithContentType : IRandomAccessStreamWithContentType
	{
		private readonly IRandomAccessStream _stream;

		public RandomAccessStreamWithContentType(IRandomAccessStream stream, string contentType = "application/octet-stream")
		{
			ContentType = contentType;
			_stream = stream;
		}

		/// <inheritdoc />
		public string ContentType { get; }

		/// <inheritdoc />
		public void Dispose()
			=> _stream.Dispose();

		/// <inheritdoc />
		public IAsyncOperationWithProgress<IBuffer, uint> ReadAsync(IBuffer buffer, uint count, InputStreamOptions options)
			=> _stream.ReadAsync(buffer, count, options);

		/// <inheritdoc />
		public IAsyncOperationWithProgress<uint, uint> WriteAsync(IBuffer buffer)
			=> _stream.WriteAsync(buffer);

		/// <inheritdoc />
		public IAsyncOperation<bool> FlushAsync()
			=> _stream.FlushAsync();

		/// <inheritdoc />
		public bool CanRead => _stream.CanRead;

		/// <inheritdoc />
		public bool CanWrite => _stream.CanWrite;

		/// <inheritdoc />
		public ulong Position => _stream.Position;

		/// <inheritdoc />
		public ulong Size => _stream.Size;

		/// <inheritdoc />
		public IInputStream GetInputStreamAt(ulong position)
			=> _stream.GetInputStreamAt(position);

		/// <inheritdoc />
		public IOutputStream GetOutputStreamAt(ulong position)
			=> _stream.GetOutputStreamAt(position);

		/// <inheritdoc />
		public void Seek(ulong position)
			=> _stream.Seek(position);

		/// <inheritdoc />
		public IRandomAccessStream CloneStream()
			=> _stream.CloneStream();
	}
}

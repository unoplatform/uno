#nullable enable

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Windows.Foundation;

namespace Windows.Storage.Streams
{
	internal class RandomAccessStreamWithContentType : IRandomAccessStreamWithContentType, IStreamWrapper, IRandomStreamWrapper
	{
		public const string DefaultContentType = "application/octet-stream";

		private readonly IRandomAccessStream _stream;

		/// <summary>
		/// -- DO NOT USE -- Prefer to use the System.IO.WindowsRuntimeStreamExtensions.TrySetContentType
		/// The only valid use-case is when you explicitly know that the provided stream does not implement IRandomAccessStreamWithContentType
		/// </summary>
		public RandomAccessStreamWithContentType(IRandomAccessStream stream, string contentType = DefaultContentType)
		{
			Debug.Assert(!(stream is IRandomAccessStreamWithContentType));

			ContentType = contentType;
			_stream = stream;
		}

		public RandomAccessStreamWithContentType(Stream stream, string contentType = DefaultContentType)
			: this(new RandomAccessStreamOverStream(stream), contentType)
		{
		}

		Stream? IStreamWrapper.FindStream() => (_stream as IStreamWrapper)?.FindStream();
		IRandomAccessStream? IRandomStreamWrapper.FindStream() => (_stream as IRandomStreamWrapper)?.FindStream() ?? _stream;
		IInputStream? IInputStreamWrapper.FindStream() => (_stream as IInputStreamWrapper)?.FindStream() ?? _stream;
		IOutputStream? IOutputStreamWrapper.FindStream() => (_stream as IOutputStreamWrapper)?.FindStream() ?? _stream;

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

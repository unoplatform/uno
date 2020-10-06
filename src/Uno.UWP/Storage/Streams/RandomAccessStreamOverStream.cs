#nullable enable

using System;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using Windows.Foundation;
using Windows.Storage.Streams;
using Uno;

namespace Windows.Storage.Streams
{
	public partial class RandomAccessStreamOverStream : IRandomAccessStream, IInputStream, IOutputStream, IDisposable, IStreamWrapper
	{
		private readonly Stream _stream;

		internal RandomAccessStreamOverStream(Stream stream)
		{
			_stream = stream;
		}

		Stream IStreamWrapper.GetStream() => _stream;

		/// <inheritdoc />
		public bool CanRead => _stream.CanRead;

		/// <inheritdoc />
		public bool CanWrite => _stream.CanWrite;

		/// <inheritdoc />
		public ulong Position => (ulong)_stream.Position;

		/// <inheritdoc />
		public ulong Size
		{
			get => (ulong)_stream.Length;
			set => throw new NotSupportedException();
		}

		public IInputStream GetInputStreamAt(ulong position)
		{
			Seek(position);
			return this;
		}

		public IOutputStream GetOutputStreamAt(ulong position)
		{
			Seek(position);
			return this;
		}

		public void Seek(ulong position)
			=> _stream.Seek((long)position, SeekOrigin.Begin);

		/// <inheritdoc />
		public IAsyncOperationWithProgress<IBuffer, uint> ReadAsync(IBuffer buffer, uint count, InputStreamOptions options)
			=> _stream.ReadAsync(buffer, count, options);

		/// <inheritdoc />
		public IAsyncOperationWithProgress<uint, uint> WriteAsync(IBuffer buffer)
			=> _stream.WriteAsync(buffer);

		/// <inheritdoc />
		public IAsyncOperation<bool> FlushAsync()
			=> _stream.FlushAsyncOp();

		/// <inheritdoc />
		public IRandomAccessStream CloneStream()
			=> throw new NotSupportedException($"Cannot clone a {nameof(RandomAccessStreamOverStream)}");

		/// <inheritdoc />
		public virtual void Dispose()
			=> _stream.Dispose();
	}
}

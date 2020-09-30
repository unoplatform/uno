using System;
using System.IO;
using Windows.Foundation;

namespace Windows.Storage.Streams
{
	public partial class FileRandomAccessStream
    {
		private abstract class ImplementationBase : IRandomAccessStream, IInputStream, IOutputStream, IDisposable, IStreamWrapper
		{
			protected Stream _stream;

			protected ImplementationBase(Stream stream)
			{
				_stream = stream ?? throw new ArgumentNullException(nameof(stream));
			}

			public Stream FindStream() => _stream;

			public ulong Size
			{
				get => (ulong)_stream.Length;
				set => _stream.SetLength((long)value);
			}

			public bool CanRead => _stream.CanRead;

			public bool CanWrite => _stream.CanWrite;

			public ulong Position => (ulong)_stream.Position;

			public abstract IInputStream GetInputStreamAt(ulong position);

			public abstract IOutputStream GetOutputStreamAt(ulong position);

			public void Seek(ulong position) =>
				_stream.Seek((long)position, SeekOrigin.Begin);

			public abstract IRandomAccessStream CloneStream();

			public void Dispose() => _stream.Dispose();

			public IAsyncOperationWithProgress<IBuffer, uint> ReadAsync(IBuffer buffer, uint count, InputStreamOptions options) =>
				_stream.ReadAsyncOperation(buffer, count, options);

			public IAsyncOperationWithProgress<uint, uint> WriteAsync(IBuffer buffer) =>
				_stream.WriteAsyncOperation(buffer);

			public IAsyncOperation<bool> FlushAsync() =>
				_stream.FlushAsyncOperation();
		}
	}
}

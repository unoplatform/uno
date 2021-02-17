using System;
using System.IO;
using Windows.Foundation;

namespace Windows.Storage.Streams
{
	public partial class FileRandomAccessStream : IRandomAccessStream, IInputStream, IOutputStream, IDisposable, IStreamWrapper
	{
		private ImplementationBase _implementation;

		public bool CanRead => throw new NotImplementedException();

		public bool CanWrite => throw new NotImplementedException();

		public ulong Position => throw new NotImplementedException();

		public ulong Size => throw new NotImplementedException();

		private FileRandomAccessStream(ImplementationBase implementation) => _implementation = implementation;

		public Stream FindStream() => throw new NotImplementedException();

		public IAsyncOperationWithProgress<uint, uint> WriteAsync(IBuffer buffer) => _implementation.WriteAsync(buffer);

		public IAsyncOperation<bool> FlushAsync() => _implementation.FlushAsync();

		public void Dispose() => _implementation.Dispose();

		public IAsyncOperationWithProgress<IBuffer, uint> ReadAsync(IBuffer buffer, uint count, InputStreamOptions options) => _implementation.ReadAsync(buffer, count, options);

		public IInputStream GetInputStreamAt(ulong position) => _implementation.GetInputStreamAt(position);

		public IOutputStream GetOutputStreamAt(ulong position) => _implementation.GetOutputStreamAt(position);

		public void Seek(ulong position) => _implementation.Seek(position);

		public IRandomAccessStream CloneStream() => _implementation.CloneStream();
	}
}

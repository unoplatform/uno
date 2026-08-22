#nullable enable

using System;
using System.IO;
using Windows.Foundation;

namespace Windows.Storage.Streams
{
	public sealed partial class FileRandomAccessStream : IRandomAccessStream, IInputStream, IOutputStream, IDisposable, IStreamWrapper
	{
		private readonly ImplementationBase _implementation;

		private FileRandomAccessStream(ImplementationBase implementation)
		{
			_implementation = implementation;
		}

		public bool CanRead => _implementation.CanRead;

		public bool CanWrite => _implementation.CanWrite;

		public ulong Position => _implementation.Position;

		public ulong Size
		{
			get => _implementation.Size;
			set => _implementation.Size = value;
		}

		public IRandomAccessStream CloneStream() => _implementation.CloneStream();

		public void Dispose() => _implementation.Dispose();

		public IAsyncOperation<bool> FlushAsync() => _implementation.FlushAsync();

		public IInputStream GetInputStreamAt(ulong position) => _implementation.GetInputStreamAt(position);

		public IOutputStream GetOutputStreamAt(ulong position) => _implementation.GetOutputStreamAt(position);

		public IAsyncOperationWithProgress<IBuffer, uint> ReadAsync(IBuffer buffer, uint count, InputStreamOptions options) => _implementation.ReadAsync(buffer, count, options);

		public void Seek(ulong position) => _implementation.Seek(position);

		public IAsyncOperationWithProgress<uint, uint> WriteAsync(IBuffer buffer) => _implementation.WriteAsync(buffer);

		Stream? IStreamWrapper.FindStream() => _implementation.FindStream();
	}
}

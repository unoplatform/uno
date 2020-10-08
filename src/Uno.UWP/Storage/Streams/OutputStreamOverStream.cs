#nullable enable

using System;
using System.IO;
using System.Linq;
using Windows.Foundation;

namespace Windows.Storage.Streams
{
	public partial class OutputStreamOverStream : IOutputStream, IDisposable, IStreamWrapper
	{
		private readonly Stream _stream;

		public OutputStreamOverStream(Stream stream)
		{
			_stream = stream;
		}

		Stream IStreamWrapper.FindStream() => _stream;

		/// <inheritdoc />
		public void Dispose()
			=> _stream.Dispose();

		/// <inheritdoc />
		public IAsyncOperationWithProgress<uint, uint> WriteAsync(IBuffer buffer)
			=> _stream.WriteAsyncOperation(buffer);

		/// <inheritdoc />
		public IAsyncOperation<bool> FlushAsync()
			=> _stream.FlushAsyncOperation();
	}
}

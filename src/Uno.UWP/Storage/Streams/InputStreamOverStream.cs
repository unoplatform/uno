#nullable enable

using System;
using System.IO;
using System.Linq;
using Windows.Foundation;

namespace Windows.Storage.Streams
{
	public partial class InputStreamOverStream : IInputStream, IDisposable, IStreamWrapper
	{
		private readonly Stream _stream;

		internal InputStreamOverStream(Stream stream)
		{
			_stream = stream;
		}

		Stream IStreamWrapper.FindStream() => _stream;

		/// <inheritdoc />
		public void Dispose()
			=> _stream.Dispose();

		/// <inheritdoc />
		public IAsyncOperationWithProgress<IBuffer, uint> ReadAsync(IBuffer buffer, uint count, InputStreamOptions options)
			=> _stream.ReadAsyncOperation(buffer, count, options);
	}
}

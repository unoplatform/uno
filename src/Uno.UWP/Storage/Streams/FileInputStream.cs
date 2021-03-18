#nullable enable

using System;
using System.IO;
using Windows.Foundation;

namespace Windows.Storage.Streams
{
	public sealed partial class FileInputStream : IInputStream, IDisposable, IStreamWrapper
	{
		private readonly Stream _stream;

		internal FileInputStream(Stream stream) =>
			_stream = stream ?? throw new ArgumentNullException(nameof(stream));

		Stream IStreamWrapper.FindStream() => _stream;

		/// <inheritdoc />
		public void Dispose()
			=> _stream.Dispose();

		/// <inheritdoc />
		public IAsyncOperationWithProgress<IBuffer, uint> ReadAsync(IBuffer buffer, uint count, InputStreamOptions options)
			=> _stream.ReadAsyncOperation(buffer, count, options);
	}
}

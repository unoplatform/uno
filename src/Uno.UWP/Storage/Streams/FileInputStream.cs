#nullable enable

using System;
using System.IO;
using Windows.Foundation;

namespace Windows.Storage.Streams
{
	public sealed partial class FileInputStream : IInputStream, IDisposable, IStreamWrapper
	{
		private readonly Stream _stream;

		internal FileInputStream(string path, ulong position)
		{
			_stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
			_stream.Seek((long)position, SeekOrigin.Begin);
		}

		Stream IStreamWrapper.GetStream() => _stream;

		/// <inheritdoc />
		public void Dispose()
			=> _stream.Dispose();

		/// <inheritdoc />
		public IAsyncOperationWithProgress<IBuffer, uint> ReadAsync(IBuffer buffer, uint count, InputStreamOptions options)
			=> _stream.ReadAsync(buffer, count, options);
	}
}

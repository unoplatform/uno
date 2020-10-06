#nullable enable

using System;
using System.IO;
using Windows.Foundation;

namespace Windows.Storage.Streams
{
	public sealed partial class FileOutputStream : IOutputStream, IDisposable, IStreamWrapper
	{
		private readonly Stream _stream;

		internal FileOutputStream(string path, ulong position)
		{
			_stream = File.Open(path, FileMode.Open, FileAccess.Write, FileShare.Write);
			_stream.Seek((long)position, SeekOrigin.Begin);
		}

		Stream IStreamWrapper.GetStream() => _stream;

		/// <inheritdoc />
		public void Dispose()
			=> _stream.Dispose();

		/// <inheritdoc />
		public IAsyncOperationWithProgress<uint, uint> WriteAsync(IBuffer buffer)
			=> _stream.WriteAsync(buffer);

		/// <inheritdoc />
		public IAsyncOperation<bool> FlushAsync()
			=> _stream.FlushAsyncOp();
	}
}

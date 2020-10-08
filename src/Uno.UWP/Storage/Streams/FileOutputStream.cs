#nullable enable

using System;
using System.IO;
using Windows.Foundation;

namespace Windows.Storage.Streams
{
	public sealed partial class FileOutputStream : IOutputStream, IDisposable, IStreamWrapper
	{
		private readonly Stream _stream;

		internal FileOutputStream(string path, FileShare fileShare, ulong position)
		{
			_stream = File.Open(path, FileMode.Open, FileAccess.Write, fileShare);
			_stream.Seek((long)position, SeekOrigin.Begin);
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

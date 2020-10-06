#nullable enable

using System;
using System.IO;
using Windows.Foundation;

namespace Windows.Storage.Streams
{
	public sealed partial class FileRandomAccessStream : IRandomAccessStream, IInputStream, IOutputStream, IDisposable, IStreamWrapper
	{
		private readonly string _path;
		private readonly FileAccess _access;
		private readonly FileShare _share;

		private readonly Stream _source;

		internal FileRandomAccessStream(string path, FileAccess access, FileShare share)
		{
			_path = path;
			_access = access;
			_share = share;

			// In order to be able to CloneStream() and Get<Input|Output>Stream(),
			// no matter the provided share, we enforce to 'share' the 'access'.
			var readWriteAccess = (FileShare)(access & FileAccess.ReadWrite);
			share &= ~readWriteAccess;
			share |= readWriteAccess;

			_source = File.Open(_path, FileMode.Open, access, share);
		}

		Stream IStreamWrapper.GetStream() => _source;

		public ulong Size
		{
			get => (ulong)_source.Length;
			set => throw new NotSupportedException("Setting the stream size is not supported");
		}

		public bool CanRead => _source.CanRead;

		public bool CanWrite => _source.CanWrite;

		public ulong Position => (ulong)_source.Position;

		public IInputStream GetInputStreamAt(ulong position)
		{
			if (!CanRead)
			{
				throw new InvalidOperationException("The file has been opened for read.");
			}

			return new FileInputStream(_path, position);
		}

		public IOutputStream GetOutputStreamAt(ulong position)
		{
			if (!CanWrite)
			{
				throw new InvalidOperationException("The file has been opened for write.");
			}

			return new FileOutputStream(_path, position);
		}

		public void Seek(ulong position)
			=> _source.Seek((long)position, SeekOrigin.Begin);

		public IRandomAccessStream CloneStream()
			=> new FileRandomAccessStream(_path, _access, _share);

		public void Dispose()
			=> _source.Dispose();

		public IAsyncOperationWithProgress<IBuffer, uint> ReadAsync(IBuffer buffer, uint count, InputStreamOptions options)
			=> _source.ReadAsync(buffer, count, options);

		public IAsyncOperationWithProgress<uint, uint> WriteAsync(IBuffer buffer)
			=> _source.WriteAsync(buffer);

		public IAsyncOperation<bool> FlushAsync()
			=> _source.FlushAsyncOp();
	}
}

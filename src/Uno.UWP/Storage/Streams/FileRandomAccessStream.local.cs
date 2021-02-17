#nullable enable

using System;
using System.IO;
using Windows.Foundation;

namespace Windows.Storage.Streams
{
	public sealed partial class FileRandomAccessStream
	{
		internal static FileRandomAccessStream CreateLocal(string path, FileAccess access, FileShare share)
		{
			var localImplementation = new Local(path, access, share);
			return new FileRandomAccessStream(localImplementation);
		}

		private class Local : ImplementationBase
		{
			private readonly string _path;
			private readonly FileAccess _access;
			private readonly FileShare _share;

			private readonly Stream _source;

			internal Local(string path, FileAccess access, FileShare share)
			{
				_path = path;
				_access = access;

				// In order to be able to CloneStream() and Get<Input|Output>Stream(),
				// no matter the provided share, we enforce to 'share' the 'access'.
				var readWriteAccess = (FileShare)(access & FileAccess.ReadWrite);
				share &= ~readWriteAccess;
				share |= readWriteAccess;

				_share = share;
				_source = File.Open(_path, FileMode.OpenOrCreate, access, share);
			}

			Stream FindStream() => _source;

			public override ulong Size
			{
				get => (ulong)_source.Length;
				set => _source.SetLength((long)value);
			}

			public override bool CanRead => _source.CanRead;

			public override bool CanWrite => _source.CanWrite;

			public override ulong Position => (ulong)_source.Position;

			public override IInputStream GetInputStreamAt(ulong position)
			{
				if (!CanRead)
				{
					throw new NotSupportedException("The file has been opened for read.");
				}

				return new FileInputStream(_path, _share, position);
			}

			public override IOutputStream GetOutputStreamAt(ulong position)
			{
				if (!CanWrite)
				{
					throw new NotSupportedException("The file has been opened for write.");
				}

				return new FileOutputStream(_path, _share, position);
			}

			public override void Seek(ulong position)
				=> _source.Seek((long)position, SeekOrigin.Begin);

			public override IRandomAccessStream CloneStream()
				=> FileRandomAccessStream.CreateLocal(_path, _access, _share);

			public override void Dispose()
				=> _source.Dispose();

			public override IAsyncOperationWithProgress<IBuffer, uint> ReadAsync(IBuffer buffer, uint count, InputStreamOptions options)
				=> _source.ReadAsyncOperation(buffer, count, options);

			public override IAsyncOperationWithProgress<uint, uint> WriteAsync(IBuffer buffer)
				=> _source.WriteAsyncOperation(buffer);

			public override IAsyncOperation<bool> FlushAsync()
				=> _source.FlushAsyncOperation();
		}
	}
}

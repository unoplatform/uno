
using Windows.Foundation;
using Windows.Storage.Streams;

namespace System.IO
{
	public static class WindowsRuntimeStreamExtensions
	{
		[Uno.NotImplemented]
		public static IInputStream AsInputStream(this Stream stream) { throw new NotImplementedException(); }
		[Uno.NotImplemented]
		public static IOutputStream AsOutputStream(this Stream stream) { throw new NotImplementedException(); }

		public static IRandomAccessStream AsRandomAccessStream(this Stream stream)
		{
			if (stream is null)
			{
				throw new ArgumentNullException(nameof(stream));
			}
			return new RandomAccessStreamWrapper(stream);
		}

		public static Stream AsStream(this IRandomAccessStream stream)
		{
			if (stream is null)
			{
				throw new ArgumentNullException(nameof(stream));
			}

			if (stream is RandomAccessStreamWrapper wrapper)
			{
				return wrapper.GetSourceStream();
			}
			else
			{
				throw new ArgumentException($"stream must be a {nameof(RandomAccessStreamWrapper)}", nameof(stream));
			}
		}

		[Uno.NotImplemented]
		public static Stream AsStream(this IRandomAccessStream windowsRuntimeStream, int bufferSize) { throw new NotImplementedException(); }
		[Uno.NotImplemented]
		public static Stream AsStreamForRead(this IInputStream windowsRuntimeStream) { throw new NotImplementedException(); }
		[Uno.NotImplemented]
		public static Stream AsStreamForRead(this IInputStream windowsRuntimeStream, int bufferSize) { throw new NotImplementedException(); }
		[Uno.NotImplemented]
		public static Stream AsStreamForWrite(this IOutputStream windowsRuntimeStream) { throw new NotImplementedException(); }
		[Uno.NotImplemented]
		public static Stream AsStreamForWrite(this IOutputStream windowsRuntimeStream, int bufferSize) { throw new NotImplementedException(); }

		private class RandomAccessStreamWrapper : IRandomAccessStream
		{
			private readonly Stream stream;

			public RandomAccessStreamWrapper(Stream stream)
			{
				this.stream = stream ?? throw new ArgumentNullException(nameof(stream));
			}

			internal Stream GetSourceStream() => stream;

			public bool CanRead => throw new NotImplementedException();

			public bool CanWrite => throw new NotImplementedException();

			public ulong Position => throw new NotImplementedException();

			public ulong Size { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

			public IRandomAccessStream CloneStream()
			{
				throw new NotImplementedException();
			}

			public void Dispose()
			{
				stream.Dispose();
			}

			public IAsyncOperation<bool> FlushAsync()
			{
				throw new NotImplementedException();
			}

			public IInputStream GetInputStreamAt(ulong position)
			{
				throw new NotImplementedException();
			}

			public IOutputStream GetOutputStreamAt(ulong position)
			{
				throw new NotImplementedException();
			}

			public IAsyncOperationWithProgress<IBuffer, uint> ReadAsync(IBuffer buffer, uint count, InputStreamOptions options)
			{
				throw new NotImplementedException();
			}

			public void Seek(ulong position)
			{
				throw new NotImplementedException();
			}

			public IAsyncOperationWithProgress<uint, uint> WriteAsync(IBuffer buffer)
			{
				throw new NotImplementedException();
			}
		}
	}
}

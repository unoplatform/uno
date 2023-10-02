#nullable enable

using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage.Streams;

namespace System.IO
{
	public static partial class WindowsRuntimeStreamExtensions
	{
		public static IInputStream AsInputStream(this Stream stream)
		{
			if (!stream.CanRead)
			{
				throw new NotSupportedException("Cannot convert stream to a IInputStream because stream does not support reading");
			}

			if (stream is IInputStreamWrapper wrapper
				&& wrapper.FindStream() is { } raw)
			{
				return raw;
			}

			return new InputStreamOverStream(stream);
		}

		public static IOutputStream AsOutputStream(this Stream stream)
		{
			if (!stream.CanWrite)
			{
				throw new NotSupportedException("Cannot convert stream to a IOutputStream because stream does not support writing");
			}

			if (stream is IOutputStreamWrapper wrapper
				&& wrapper.FindStream() is { } raw)
			{
				return raw;
			}

			return new OutputStreamOverStream(stream);
		}

		public static IRandomAccessStream AsRandomAccessStream(this Stream stream)
		{
			if (!stream.CanSeek)
			{
				throw new NotSupportedException("Cannot convert stream to a IRandomAccessStream because stream does not support seeking");
			}

			if (stream is IRandomStreamWrapper wrapper
				&& wrapper.FindStream() is { } raw)
			{
				return raw;
			}

			return new RandomAccessStreamOverStream(stream);
		}

		public static Stream AsStream(this IRandomAccessStream windowsRuntimeStream)
			=> AsStream(windowsRuntimeStream, global::Windows.Storage.Streams.Buffer.DefaultCapacity);

		public static Stream AsStream(this IRandomAccessStream windowsRuntimeStream, int bufferSize)
		{
			if (windowsRuntimeStream is IStreamWrapper wrapper
				&& wrapper.FindStream() is { } raw)
			{
				return raw;
			}

			return new StreamOverRandomAccessStream(windowsRuntimeStream, bufferSize);
		}

		public static Stream AsStreamForRead(this IInputStream windowsRuntimeStream)
			=> AsStreamForRead(windowsRuntimeStream, global::Windows.Storage.Streams.Buffer.DefaultCapacity);

		public static Stream AsStreamForRead(this IInputStream windowsRuntimeStream, int bufferSize)
		{
			if (windowsRuntimeStream is IStreamWrapper wrapper
				&& wrapper.FindStream() is { } raw)
			{
				return raw;
			}

			return new StreamOverInputStream(windowsRuntimeStream, bufferSize);
		}

		public static Stream AsStreamForWrite(this IOutputStream windowsRuntimeStream)
			=> AsStreamForWrite(windowsRuntimeStream, global::Windows.Storage.Streams.Buffer.DefaultCapacity);

		public static Stream AsStreamForWrite(this IOutputStream windowsRuntimeStream, int bufferSize)
		{
			if (windowsRuntimeStream is IStreamWrapper wrapper
				&& wrapper.FindStream() is { } raw)
			{
				return raw;
			}

			return new StreamOverOutputStream(windowsRuntimeStream, bufferSize);
		}

		internal static IAsyncOperationWithProgress<IBuffer, uint> ReadAsyncOperation(this Stream stream, IBuffer buffer, uint count, InputStreamOptions options)
			=> AsyncOperationWithProgress.FromFuncAsync<IBuffer, uint>(async (ct, op) =>
			{
				await stream.ReadAsync(buffer, count, options, ct);

				return buffer;
			});

		internal static async Task ReadAsync(this Stream stream, IBuffer buffer, uint count, InputStreamOptions options, CancellationToken ct)
		{
			var data = global::Windows.Storage.Streams.Buffer.Cast(buffer).GetSegment();
			if (count > data.Count)
			{
				throw new ArgumentOutOfRangeException(nameof(count), "Count is greater than the available space.");
			}

			buffer.Length = (uint)await stream.ReadAsync(data.Array!, data.Offset, (int)count, ct);
		}

		internal static IAsyncOperationWithProgress<uint, uint> WriteAsyncOperation(this Stream stream, IBuffer buffer)
			=> AsyncOperationWithProgress.FromFuncAsync<uint, uint>(async (ct, op) =>
			{
				await stream.WriteAsync(buffer, ct);

				return buffer.Length;
			});

		internal static async Task WriteAsync(this Stream stream, IBuffer buffer, CancellationToken ct)
		{
			var data = global::Windows.Storage.Streams.Buffer.Cast(buffer).GetSegment();

			await stream.WriteAsync(data.Array!, data.Offset, (int)buffer.Length, ct);
		}

		public static IAsyncOperation<bool> FlushAsyncOperation(this Stream stream)
			=> AsyncOperation.FromTask(async ct =>
			{
				await stream.FlushAsync(ct);
				return true;
			});

		internal static async Task<int> ReadAsync(this IInputStream stream, byte[] buffer, int offset, int count, CancellationToken cancellationToken)
		{
			var dst = new global::Windows.Storage.Streams.Buffer(new Memory<byte>(buffer, offset, count));
			var read = await stream.ReadAsync(dst, (uint)count, InputStreamOptions.None).AsTask(cancellationToken);

			if (read != dst)
			{
				// Unfortunately the stream decided to not write data in the provided dst buffer.
				// We now have to copy the data.
				global::Windows.Storage.Streams.Buffer.Cast(read).CopyTo(0, buffer, offset, (int)read.Length);
			}

			return (int)read.Length;
		}

		internal static Task WriteAsync(this IOutputStream stream, byte[] buffer, int offset, int count, CancellationToken cancellationToken)
		{
			var src = new global::Windows.Storage.Streams.Buffer(new Memory<byte>(buffer, offset, count));
			return stream.WriteAsync(src).AsTask(cancellationToken);
		}

		/// <summary>
		/// Wraps the stream with the provided ContentType if the provided stream does not have a ContentType defined,
		/// returns the provided stream otherwise.
		/// </summary>
		internal static IRandomAccessStreamWithContentType TrySetContentType(this IRandomAccessStream stream, string contentType = RandomAccessStreamWithContentType.DefaultContentType)
			=> stream is IRandomAccessStreamWithContentType rasWithType
				? rasWithType
				: new RandomAccessStreamWithContentType(stream, contentType);
	}
}

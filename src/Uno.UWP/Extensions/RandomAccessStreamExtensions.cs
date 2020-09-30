#nullable enable
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage.Streams;

using SystemBuffer = System.Buffer;

namespace Uno.Extensions
{
	public static class RandomAccessStreamExtensions
	{
		private const int DEFAULT_BUFFER_LENGTH = 4096;

		public static async Task<byte[]> ReadBytesAsync(this IRandomAccessStream stream, CancellationToken ct, ulong startPosition = 0, Action<ulong, ulong?>? progressCallback = null)
		{
			if (stream == null)
			{
				throw new ArgumentNullException(nameof(stream));
			}

			if (!stream.CanRead)
			{
				throw new InvalidOperationException("This stream can't read.");
			}

			return await stream.AsStream().ReadBytesAsync(ct, startPosition, progressCallback);
		}

		public static async Task<byte[]> ReadBytesAsync(
			this Stream stream,
			CancellationToken ct,
			ulong startPosition = 0,
			Action<ulong, ulong?>? progressCallback = null)
		{
			if (!stream.CanRead)
			{
				throw new InvalidOperationException("This stream can't read.");
			}

			ulong? length;

			try
			{
				length = (ulong)stream.Length - startPosition;

				if (length <= 0) // empty stream or position not before end of stream
				{
					return new byte[] { };
				}
			}
			catch (NotSupportedException)
			{
				length = null;
			}

			var bufferSize = (int?)length ?? DEFAULT_BUFFER_LENGTH;

			var classicStream = stream;

			if (classicStream is MemoryStream {Position: 0} memStream)
			{
				// MemoryStream.ToArray() is already optimized, so use it when possible
				return memStream.ToArray();
			}

			var readBuffer = new byte[bufferSize];

			var totalBytesRead = 0;
			int bytesRead;

			progressCallback?.Invoke(0, length);

			while ((bytesRead = await stream.ReadAsync(readBuffer, totalBytesRead, bufferSize - totalBytesRead, ct)) > 0)
			{
				totalBytesRead += bytesRead;

				progressCallback?.Invoke((ulong) totalBytesRead, length);

				if (totalBytesRead == bufferSize)
				{
					var nextBytes = new byte[1];
					var read = await stream.ReadAsync(nextBytes, 0, 1, ct);

					if (read != 1)
					{
						continue;
					}

					var temp = new byte[bufferSize * 2];
					SystemBuffer.BlockCopy(readBuffer, 0, temp, 0, bufferSize);
					SystemBuffer.SetByte(temp, totalBytesRead, (byte) nextBytes[0]);
					readBuffer = temp;
					totalBytesRead++;
				}
			}

			progressCallback?.Invoke((ulong) totalBytesRead, (ulong) totalBytesRead);

			var buffer = readBuffer;
			if (totalBytesRead != bufferSize)
			{
				buffer = new byte[totalBytesRead];
				SystemBuffer.BlockCopy(readBuffer, 0, buffer, 0, totalBytesRead);
			}

			return buffer;
		}
	}
}

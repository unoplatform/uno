using System.IO;
using Windows.Storage.Streams;
using System.Linq;
using UwpBuffer = Windows.Storage.Streams.Buffer;

namespace System.Runtime.InteropServices.WindowsRuntime
{
	public static partial class WindowsRuntimeBufferExtensions
	{
		public static IBuffer AsBuffer(this byte[] source)
			=> AsBuffer(source, 0, source.Length, source.Length);

		public static IBuffer AsBuffer(this byte[] source, int offset, int length)
			=> AsBuffer(source, offset, length, length);

		public static IBuffer AsBuffer(this byte[] source, int offset, int length, int capacity)
		{
			if (source is null)
			{
				throw new ArgumentNullException(nameof(source));
			}

			if (offset < 0) throw new ArgumentOutOfRangeException(nameof(offset), "offset is less than 0 (zero).");
			if (length < 0) throw new ArgumentOutOfRangeException(nameof(length), "length is less than 0 (zero).");
			if (capacity < 0) throw new ArgumentOutOfRangeException(nameof(capacity), "capacity is less than 0 (zero).");

			if (length > capacity)
			{
				throw new ArgumentException("length is greater than capacity");
			}

			if (source.Length - offset < Math.Max(length, capacity))
			{
				throw new ArgumentException("The array is not large enough to serve as a backing store for the IBuffer; that is, the number of bytes in source, beginning at offset, is less than length or capacity.");
			}

			return new UwpBuffer(new Memory<byte>(source, offset, capacity))
			{
				Length = (uint)length
			};
		}

		public static Stream AsStream(this IBuffer source)
			=> new StreamOverBuffer(UwpBuffer.Cast(source));

		public static void CopyTo(this byte[] source, IBuffer destination)
			=> UwpBuffer.Cast(destination).Write(0, source, 0, source.Length);

		public static void CopyTo(this byte[] source, int sourceIndex, IBuffer destination, uint destinationIndex, int count)
			=> UwpBuffer.Cast(destination).Write(destinationIndex, source, sourceIndex, count);

		public static void CopyTo(this IBuffer source, byte[] destination)
			=> UwpBuffer.Cast(source).CopyTo(0, destination, 0, destination.Length);

		public static void CopyTo(this IBuffer source, uint sourceIndex, byte[] destination, int destinationIndex, int count)
			=> UwpBuffer.Cast(source).CopyTo(sourceIndex, destination, destinationIndex, count);

		public static void CopyTo(this IBuffer source, IBuffer destination)
			=> UwpBuffer.Cast(source).CopyTo(0, UwpBuffer.Cast(destination), 0, destination.Capacity);

		public static void CopyTo(this IBuffer source, uint sourceIndex, IBuffer destination, uint destinationIndex, uint count)
			=> UwpBuffer.Cast(source).CopyTo(sourceIndex, UwpBuffer.Cast(destination), destinationIndex, count);

		public static byte GetByte(this IBuffer source, uint byteOffset)
			=> UwpBuffer.Cast(source).GetByte(byteOffset);

		public static IBuffer GetWindowsRuntimeBuffer(this MemoryStream underlyingStream)
			=> underlyingStream.GetBuffer().AsBuffer();

		public static IBuffer GetWindowsRuntimeBuffer(this MemoryStream underlyingStream, int positionInStream, int length)
			=> underlyingStream.GetBuffer().AsBuffer(positionInStream, length);

		public static bool IsSameData(this IBuffer buffer, IBuffer otherBuffer)
			=> UwpBuffer.Cast(buffer).Span.SequenceEqual(UwpBuffer.Cast(otherBuffer).Span);

		public static byte[] ToArray(this IBuffer source)
			=> UwpBuffer.Cast(source).ToArray();

		public static byte[] ToArray(this IBuffer source, uint sourceIndex, int count)
		{
			if (source is null)
			{
				throw new ArgumentNullException(nameof(source));
			}

			return UwpBuffer.Cast(source).ToArray(sourceIndex, count);
		}
	}
}

using System.IO;
using Windows.Storage.Streams;
using System.Linq;
using UwpBuffer = Windows.Storage.Streams.Buffer;

namespace System.Runtime.InteropServices.WindowsRuntime
{
	public static class WindowsRuntimeBufferExtensions
	{
		public static IBuffer AsBuffer(this byte[] source)
		{
			if (source is null)
			{
				throw new ArgumentNullException(nameof(source));
			}

			return source.AsBuffer(0, source.Length);
		}

		public static IBuffer AsBuffer(this byte[] source, int offset, int length) =>
			AsBuffer(source, offset, length, length);

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
		{
			if (source is null)
			{
				throw new ArgumentNullException(nameof(source));
			}

			var data = UwpBuffer.Cast(source).GetSegment();
			var stream = new MemoryStream(data.Array!, data.Offset, data.Count);

			return stream;
		}

		[Uno.NotImplemented]
		public static void CopyTo(this byte[] source, IBuffer destination) { throw new NotImplementedException(); }
		[Uno.NotImplemented]
		public static void CopyTo(this IBuffer source, byte[] destination) { throw new NotImplementedException(); }
		[Uno.NotImplemented]
		public static void CopyTo(this IBuffer source, IBuffer destination) { throw new NotImplementedException(); }
		[Uno.NotImplemented]
		public static void CopyTo(this byte[] source, int sourceIndex, IBuffer destination, uint destinationIndex, int count) { throw new NotImplementedException(); }
		[Uno.NotImplemented]
		public static void CopyTo(this IBuffer source, uint sourceIndex, byte[] destination, int destinationIndex, int count) { throw new NotImplementedException(); }
		[Uno.NotImplemented]
		public static void CopyTo(this IBuffer source, uint sourceIndex, IBuffer destination, uint destinationIndex, uint count) { throw new NotImplementedException(); }

		public static byte GetByte(this IBuffer source, uint byteOffset) => source.ToArray()[byteOffset];

		[Uno.NotImplemented]
		public static IBuffer GetWindowsRuntimeBuffer(this MemoryStream underlyingStream) { throw new NotImplementedException(); }
		[Uno.NotImplemented]
		public static IBuffer GetWindowsRuntimeBuffer(this MemoryStream underlyingStream, int positionInStream, int length) { throw new NotImplementedException(); }

		public static bool IsSameData(this IBuffer buffer, IBuffer otherBuffer) =>
			buffer.ToArray().SequenceEqual(otherBuffer.ToArray());

		public static byte[] ToArray(this IBuffer source)
		{
			if (source is null)
			{
				throw new ArgumentNullException(nameof(source));
			}

			return UwpBuffer.Cast(source).ToArray();
		}

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

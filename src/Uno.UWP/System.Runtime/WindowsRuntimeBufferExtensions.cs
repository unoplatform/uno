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
			return new UwpBuffer(source);
		}

		[Uno.NotImplemented]
		public static IBuffer AsBuffer(this byte[] source, int offset, int length) { throw new NotImplementedException(); }
		[Uno.NotImplemented]
		public static IBuffer AsBuffer(this byte[] source, int offset, int length, int capacity) { throw new NotImplementedException(); }

		public static Stream AsStream(this IBuffer source)
		{
			switch (source)
			{
				case UwpBuffer mb:
					return new MemoryStream(mb.Data);

				default:
					throw new NotSupportedException("This buffer is not supported");
			}
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
		[Uno.NotImplemented]
		public static byte GetByte(this IBuffer source, uint byteOffset) { throw new NotImplementedException(); }
		[Uno.NotImplemented]
		public static IBuffer GetWindowsRuntimeBuffer(this MemoryStream underlyingStream) { throw new NotImplementedException(); }
		[Uno.NotImplemented]
		public static IBuffer GetWindowsRuntimeBuffer(this MemoryStream underlyingStream, int positionInStream, int length) { throw new NotImplementedException(); }
		[Uno.NotImplemented]
		public static bool IsSameData(this IBuffer buffer, IBuffer otherBuffer) { throw new NotImplementedException(); }

		public static byte[] ToArray(this IBuffer source)
		{
			switch (source)
			{
				case UwpBuffer b:
					return b.Data;
				default:
					throw new NotSupportedException("This buffer is not supported");
			}
		}

		public static byte[] ToArray(this IBuffer source, uint sourceIndex, int count)
		{
			switch (source)
			{
				case UwpBuffer mb:
					return mb.Data.Skip((int)sourceIndex).Take(count).ToArray();

				default:
					throw new NotSupportedException("This buffer is not supported");
			}
		}
	}
}

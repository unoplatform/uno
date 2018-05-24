
using Windows.Storage.Streams;

namespace System.IO
{
	public static class WindowsRuntimeStreamExtensions
	{
		[Uno.NotImplemented]
		public static IInputStream AsInputStream(this Stream stream) { throw new NotImplementedException(); }
		[Uno.NotImplemented]
		public static IOutputStream AsOutputStream(this Stream stream) { throw new NotImplementedException(); }
		[Uno.NotImplemented]
		public static IRandomAccessStream AsRandomAccessStream(this Stream stream) { throw new NotImplementedException(); }
		[Uno.NotImplemented]
		public static Stream AsStream(this IRandomAccessStream windowsRuntimeStream) { throw new NotImplementedException(); }
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
	}
}
using System.IO;
using Android.Media;
using Windows.Storage.Streams;
using Stream = System.IO.Stream;

namespace Windows.Media.Playback;
internal class UnoMediaDataSource : MediaDataSource
{
	private readonly Stream _stream;

	public UnoMediaDataSource(IRandomAccessStream stream)
	{
		_stream = stream.AsStreamForRead();
	}

	public override long Size => _stream.Length;

	public override void Close() => _stream.Close();

	public override int ReadAt(long position, byte[] buffer, int offset, int size)
	{
		_stream.Seek(position, SeekOrigin.Begin);
		return _stream.Read(buffer, offset, size);
	}
}

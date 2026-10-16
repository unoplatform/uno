#if __SKIA__
#nullable enable

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using Uno.Disposables;
using Uno.UI.Composition.Drawing;
using Windows.Graphics.Imaging;

namespace Uno.UI.RuntimeTests.Helpers;

/// <summary>
/// Forwards to another codec and counts the decodes of one encoded image, told apart by its length so that another
/// test's image load doesn't add to the count, and the images created from pixels of <see cref="ImageSize"/>. It doesn't
/// implement <see cref="IImageEncoderDecoder.TryGetSize"/>, so sizes are read through the interface's default, like a
/// codec written before that member existed.
/// </summary>
internal class CountingImageCodec(IImageEncoderDecoder inner, long imageLength) : IImageEncoderDecoder
{
	private int _decodes;
	private int _createdImages;

	protected IImageEncoderDecoder Inner => inner;

	protected bool IsImage(Stream stream) => stream.CanSeek && stream.Length == imageLength;

	public int Decodes => Volatile.Read(ref _decodes);

	public (int Width, int Height) ImageSize { get; init; }

	public int CreatedImages => Volatile.Read(ref _createdImages);

	public bool TryDecode(Stream stream, int? targetWidth, int? targetHeight, [NotNullWhen(true)] out ImageFrames? frames)
	{
		if (IsImage(stream))
		{
			Interlocked.Increment(ref _decodes);
		}

		return inner.TryDecode(stream, targetWidth, targetHeight, out frames);
	}

	public IImage CreateImage(int pixelWidth, int pixelHeight, ReadOnlySpan<byte> bgraPremul)
	{
		if ((pixelWidth, pixelHeight) == ImageSize)
		{
			Interlocked.Increment(ref _createdImages);
		}

		return inner.CreateImage(pixelWidth, pixelHeight, bgraPremul);
	}

	public ImageFrames CreateFrames(IImage image) => inner.CreateFrames(image);

	public void Encode(Stream destination, byte[] pixels, int width, int height, BitmapPixelFormat pixelFormat, BitmapAlphaMode alphaMode, BitmapEncoderFormat format, int quality)
		=> inner.Encode(destination, pixels, width, height, pixelFormat, alphaMode, format, quality);

	/// <summary>Registers this codec as <see cref="ImageEncoderDecoder.Current"/> until the result is disposed.</summary>
	public IDisposable Register()
	{
		var previous = ImageEncoderDecoder.Current;
		ImageEncoderDecoder.Current = this;
		return Disposable.Create(() => ImageEncoderDecoder.Current = previous);
	}
}

/// <summary>
/// A <see cref="CountingImageCodec"/> that reads sizes through its inner codec's own TryGetSize, and counts the bytes
/// that reads from the image's stream: a decode inside the inner codec is out of <see cref="CountingImageCodec.Decodes"/>'s
/// sight, but it reads the whole stream.
/// </summary>
internal sealed class SizeReadingCountingImageCodec(IImageEncoderDecoder inner, long imageLength)
	: CountingImageCodec(inner, imageLength), IImageEncoderDecoder
{
	private long _sizeBytesRead;

	public long SizeBytesRead => Interlocked.Read(ref _sizeBytesRead);

	public bool TryGetSize(Stream stream, out int pixelWidth, out int pixelHeight)
	{
		var counting = new ReadCountingStream(stream);
		var result = Inner.TryGetSize(counting, out pixelWidth, out pixelHeight);
		if (IsImage(stream))
		{
			Interlocked.Add(ref _sizeBytesRead, counting.BytesRead);
		}

		return result;
	}

	private sealed class ReadCountingStream(Stream inner) : Stream
	{
		public long BytesRead { get; private set; }

		public override bool CanRead => inner.CanRead;

		public override bool CanSeek => inner.CanSeek;

		public override bool CanWrite => false;

		public override long Length => inner.Length;

		public override long Position
		{
			get => inner.Position;
			set => inner.Position = value;
		}

		public override int Read(byte[] buffer, int offset, int count) => Counted(inner.Read(buffer, offset, count));

		public override int Read(Span<byte> buffer) => Counted(inner.Read(buffer));

		public override long Seek(long offset, SeekOrigin origin) => inner.Seek(offset, origin);

		public override void Flush()
		{
		}

		public override void SetLength(long value) => throw new NotSupportedException();

		public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

		private int Counted(int read)
		{
			BytesRead += read;
			return read;
		}
	}
}
#endif

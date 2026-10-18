#nullable enable

using System;
using System.IO;

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// The bytes of an encoded image, for the header reads the managed decoder shares between decoding an image and reading
/// its size: the whole file in memory, or a seekable stream read in small blocks on demand, so that reading a header reads
/// little more than the header. Reading past the end throws <see cref="IndexOutOfRangeException"/>, as an array does.
/// </summary>
internal sealed class ImageBytes
{
	private const int BlockSize = 32;

	private readonly Stream? _stream;
	private readonly long _origin;
	private readonly byte[]? _block;
	private int _blockStart = -1;
	private int _blockLength;

	public ImageBytes(byte[] array)
	{
		Data = array;
		Length = array.Length;
	}

	/// <param name="origin">Where the image starts in <paramref name="stream"/>.</param>
	public ImageBytes(Stream stream, long origin)
	{
		var length = Math.Max(0, stream.Length - origin);
		if (length > int.MaxValue)
		{
			// A file the decoder can't hold in an array either.
			throw new NotSupportedException("The image is too large.");
		}

		_stream = stream;
		_origin = origin;
		_block = new byte[BlockSize];
		Length = (int)length;
	}

	/// <summary>The whole file, when it is in memory.</summary>
	public byte[]? Data { get; }

	public int Length { get; }

	public byte this[int index] => Data is { } data ? data[index] : ReadFromStream(index);

	private byte ReadFromStream(int index)
	{
		if ((uint)index >= (uint)Length)
		{
			throw new IndexOutOfRangeException();
		}

		if (index < _blockStart || index >= _blockStart + _blockLength)
		{
			_blockStart = index & ~(BlockSize - 1);
			_stream!.Position = _origin + _blockStart;
			_blockLength = 0;
			var wanted = Math.Min(BlockSize, Length - _blockStart);
			while (_blockLength < wanted)
			{
				var read = _stream.Read(_block!, _blockLength, wanted - _blockLength);
				if (read <= 0)
				{
					throw new EndOfStreamException();
				}

				_blockLength += read;
			}
		}

		return _block![index - _blockStart];
	}
}

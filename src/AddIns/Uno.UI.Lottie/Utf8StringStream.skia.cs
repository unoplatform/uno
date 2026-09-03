#nullable enable

#if HAS_SKOTTIE

using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Uno.UI.Lottie;

/// <summary>A forward-only read stream over a UTF-8 encoding of a string, so a Lottie JSON payload can be handed to
/// Skottie without materializing the whole byte array.</summary>
internal sealed class Utf8StringStream(string text) : Stream
{
	private readonly Encoder _encoder = Encoding.UTF8.GetEncoder();
	private readonly byte[] _overflow = new byte[4];
	private int _charOffset;
	private int _overflowOffset;
	private int _overflowCount;

	public override bool CanRead => true;

	public override bool CanSeek => false;

	public override bool CanWrite => false;

	public override long Length { get; } = Encoding.UTF8.GetByteCount(text);

	public override long Position { get; set; }

	public override void Flush()
	{
	}

	public override int Read(byte[] buffer, int offset, int count)
		=> Read(buffer.AsSpan(offset, count));

	public override int Read(Span<byte> buffer)
	{
		if (buffer.Length == 0)
		{
			return 0;
		}

		var bytesWritten = 0;
		if (_overflowCount > 0)
		{
			var copied = Math.Min(buffer.Length, _overflowCount);
			_overflow.AsSpan(_overflowOffset, copied).CopyTo(buffer);
			_overflowOffset += copied;
			_overflowCount -= copied;
			Position += copied;
			bytesWritten += copied;

			if (_overflowCount == 0)
			{
				_overflowOffset = 0;
			}

			if (bytesWritten == buffer.Length)
			{
				return bytesWritten;
			}

			buffer = buffer[bytesWritten..];
		}

		if (_charOffset >= text.Length)
		{
			return bytesWritten;
		}

		_encoder.Convert(text.AsSpan(_charOffset), buffer, flush: true, out var charsUsed, out var bytesUsed, out _);
		_charOffset += charsUsed;
		Position += bytesUsed;
		bytesWritten += bytesUsed;

		if (bytesWritten == 0 && _charOffset < text.Length)
		{
			var charCount = char.IsHighSurrogate(text[_charOffset]) && _charOffset + 1 < text.Length ? 2 : 1;
			var runeBytes = Encoding.UTF8.GetBytes(text.AsSpan(_charOffset, charCount), _overflow);
			var copied = Math.Min(buffer.Length, runeBytes);
			_overflow.AsSpan(0, copied).CopyTo(buffer);
			_overflowOffset = copied;
			_overflowCount = runeBytes - copied;
			_charOffset += charCount;
			Position += copied;
			bytesWritten += copied;
		}

		return bytesWritten;
	}

	public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
	{
		cancellationToken.ThrowIfCancellationRequested();
		return ValueTask.FromResult(Read(buffer.Span));
	}

	public override long Seek(long offset, SeekOrigin origin)
		=> throw new NotSupportedException();

	public override void SetLength(long value)
		=> throw new NotSupportedException();

	public override void Write(byte[] buffer, int offset, int count)
		=> throw new NotSupportedException();
}

#endif

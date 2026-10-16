#nullable enable

using System;
using System.Buffers.Binary;
using System.IO;
using Windows.Graphics.Imaging;
using Uno.UI.Composition.Drawing;
using Windows.Storage.Streams;

namespace Microsoft.UI.Text
{
	internal enum InlineImageEncoding
	{
		Unknown,
		Png,
		Jpeg,
		Gif,
		Webp,
		Bmp,
		Dib,
	}

	internal sealed class InlineImageState : IEquatable<InlineImageState>
	{
		internal const int MaxEncodedBytes = 4 * 1024 * 1024;
		internal const int MaxDimension = 8_192;
		internal const int MaxAlternateTextLength = 16_384;
		internal const long MaxDecodedPixels = 4L * 1024 * 1024;

		private byte[] _data = Array.Empty<byte>();
		private IImage? _decodedImage;
		private long _decodedPixelCount = -1;
		private InlineImageEncoding _encoding;

		public byte[] Data
		{
			get => _data;
			set
			{
				_decodedImage?.Dispose();
				_data = value ?? Array.Empty<byte>();
				_decodedImage = null;
				_decodedPixelCount = -1;
				_encoding = InlineImageEncoding.Unknown;
			}
		}
		public int Width;
		public int Height;
		public int Ascent;
		public global::Microsoft.UI.Text.VerticalCharacterAlignment VerticalAlignment;
		public string AlternateText = string.Empty;
		public bool IsObjectFallback;

		public InlineImageState Clone()
		{
			var clone = (InlineImageState)MemberwiseClone();
			clone._decodedImage = null;
			return clone;
		}

		internal int EncodedLength => _data.Length;

		internal bool HasDecodedImage => _decodedImage is not null;

		// Clones share the encoded bytes but each decodes its own image, so the bytes are the stable identity.
		internal object TextureKey => _data;

		internal long DecodedByteLength => GetDecodedPixelCount() * 4;

		internal static InlineImageState CreateFromStream(
			IRandomAccessStream value,
			int? width,
			int? height,
			int? ascent,
			global::Microsoft.UI.Text.VerticalCharacterAlignment verticalAlignment,
			string? alternateText)
		{
			ArgumentNullException.ThrowIfNull(value);
			if (value.Size > MaxEncodedBytes)
			{
				throw new ArgumentException("The image stream is too large.", nameof(value));
			}

			value.Seek(0);
			using var buffer = new MemoryStream();
			var source = value.AsStream();
			var chunk = new byte[8192];
			while (true)
			{
				var read = source.Read(chunk, 0, chunk.Length);
				if (read == 0)
				{
					break;
				}

				if (buffer.Length > MaxEncodedBytes - read)
				{
					throw new ArgumentException("The image stream is too large.", nameof(value));
				}
				buffer.Write(chunk, 0, read);
			}

			if (!TryCreate(
				buffer.ToArray(),
				width,
				height,
				ascent,
				verticalAlignment,
				alternateText,
				InlineImageEncoding.Unknown,
				out var image))
			{
				throw new ArgumentException("The image stream is invalid or unsupported.", nameof(value));
			}

			return image;
		}

		internal static bool TryCreate(
			byte[] data,
			int? width,
			int? height,
			int? ascent,
			global::Microsoft.UI.Text.VerticalCharacterAlignment verticalAlignment,
			string? alternateText,
			InlineImageEncoding encodingHint,
			out InlineImageState image)
		{
			image = new InlineImageState();
			if (data.Length is 0 or > MaxEncodedBytes
				|| width is < 0 or > MaxDimension
				|| height is < 0 or > MaxDimension
				|| ascent is < 0 or > MaxDimension
				|| !Enum.IsDefined(verticalAlignment)
				|| (alternateText?.Length ?? 0) > MaxAlternateTextLength)
			{
				return false;
			}

			var normalized = data;
			var encoding = DetectEncoding(data);
			if (!TryInspect(normalized, out var pixelWidth, out var pixelHeight)
				&& encodingHint == InlineImageEncoding.Dib
				&& TryWrapDib(data, out var bitmap)
				&& TryInspect(bitmap, out pixelWidth, out pixelHeight))
			{
				normalized = bitmap;
				encoding = InlineImageEncoding.Bmp;
			}

			// The decoder materializes every frame, so the budget covers all of them, not just the canvas.
			var pixelCount = (long)pixelWidth * pixelHeight * GetFrameCount(normalized);
			if (pixelWidth is <= 0 or > MaxDimension
				|| pixelHeight is <= 0 or > MaxDimension
				|| pixelCount > MaxDecodedPixels)
			{
				return false;
			}

			image._data = normalized;
			image._decodedPixelCount = pixelCount;
			image._encoding = encoding;
			image.Width = width ?? pixelWidth;
			image.Height = height ?? pixelHeight;
			image.Ascent = ascent ?? image.Height;
			image.VerticalAlignment = verticalAlignment;
			image.AlternateText = alternateText ?? string.Empty;
			return true;
		}

		internal static InlineImageState CreateObjectFallback(int? width, int? height, string? alternateText)
		{
			var resolvedWidth = Math.Clamp(width ?? 16, 1, MaxDimension);
			var resolvedHeight = Math.Clamp(height ?? 16, 1, MaxDimension);
			var resolvedAlternateText = alternateText ?? string.Empty;
			if (resolvedAlternateText.Length > MaxAlternateTextLength)
			{
				resolvedAlternateText = resolvedAlternateText[..MaxAlternateTextLength];
			}

			return new InlineImageState
			{
				Width = resolvedWidth,
				Height = resolvedHeight,
				Ascent = resolvedHeight,
				VerticalAlignment = global::Microsoft.UI.Text.VerticalCharacterAlignment.Baseline,
				AlternateText = resolvedAlternateText,
				IsObjectFallback = true,
			};
		}

		internal long GetDecodedPixelCount()
		{
			if (IsObjectFallback)
			{
				return (long)Width * Height;
			}

			if (_decodedPixelCount >= 0)
			{
				return _decodedPixelCount;
			}

			return _decodedPixelCount = TryInspect(_data, out var width, out var height) ? (long)width * height * GetFrameCount(_data) : long.MaxValue;
		}

		internal void Validate()
		{
			if (!IsObjectFallback && _data.Length == 0
				|| IsObjectFallback && _data.Length != 0
				|| _data.Length > MaxEncodedBytes
				|| Width is < 0 or > MaxDimension
				|| Height is < 0 or > MaxDimension
				|| Ascent is < 0 or > MaxDimension
				|| !Enum.IsDefined(VerticalAlignment)
				|| AlternateText.Length > MaxAlternateTextLength)
			{
				throw new ArgumentException("The inline image metadata is invalid.");
			}

			if (_data.Length > 0)
			{
				if (GetDecodedPixelCount() > MaxDecodedPixels)
				{
					throw new ArgumentException("The inline image data is invalid or too large.");
				}
			}
		}

		internal IImage? GetDecodedImage()
		{
			if (_decodedImage is null && _data.Length > 0)
			{
				Validate();
				using var stream = new MemoryStream(_data, writable: false);
				if (ImageEncoderDecoder.Current.TryDecode(stream, null, null, out var frames))
				{
					// Inline objects are static: keep the first frame and release any animation frames.
					_decodedImage = frames.Frames[0];
					for (var i = 1; i < frames.Frames.Count; i++)
					{
						frames.Frames[i].Dispose();
					}
				}
			}

			return _decodedImage;
		}

		internal byte[] GetRtfEncodedData(out string control)
		{
			Validate();
			if (IsObjectFallback)
			{
				throw new InvalidOperationException("An object fallback does not contain executable or image payload data.");
			}

			var encoding = _encoding == InlineImageEncoding.Unknown ? DetectEncoding(_data) : _encoding;
			if (encoding == InlineImageEncoding.Png)
			{
				control = "pngblip";
				return _data;
			}
			if (encoding == InlineImageEncoding.Jpeg)
			{
				control = "jpegblip";
				return _data;
			}

			var decoded = GetDecodedImage() ?? throw new ArgumentException("The inline image data is invalid.");
			var pixels = new byte[(long)decoded.PixelWidth * decoded.PixelHeight * 4];
			decoded.CopyPixels(pixels);
			using var encoded = new MemoryStream();
			ImageEncoderDecoder.Current.Encode(encoded, pixels, decoded.PixelWidth, decoded.PixelHeight, BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied, BitmapEncoderFormat.Png, 100);
			if (encoded.Length is 0 or > MaxEncodedBytes)
			{
				throw new ArgumentException("The inline image cannot be represented safely in RTF.");
			}

			control = "pngblip";
			return encoded.ToArray();
		}

		// Reads the pixel size from the container header only, so oversized images are rejected before decoding.
		private static bool TryInspect(ReadOnlySpan<byte> data, out int width, out int height)
		{
			width = height = 0;
			switch (DetectEncoding(data))
			{
				case InlineImageEncoding.Png when data.Length >= 24:
					width = (int)BinaryPrimitives.ReadUInt32BigEndian(data.Slice(16, 4));
					height = (int)BinaryPrimitives.ReadUInt32BigEndian(data.Slice(20, 4));
					break;
				case InlineImageEncoding.Gif when data.Length >= 10:
					width = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(6, 2));
					height = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(8, 2));
					break;
				case InlineImageEncoding.Bmp when data.Length >= 26:
					if (BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(14, 4)) == 12)
					{
						width = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(18, 2));
						height = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(20, 2));
					}
					else
					{
						width = BinaryPrimitives.ReadInt32LittleEndian(data.Slice(18, 4));
						var signedHeight = BinaryPrimitives.ReadInt32LittleEndian(data.Slice(22, 4));
						height = signedHeight == int.MinValue ? 0 : Math.Abs(signedHeight);
					}
					break;
				case InlineImageEncoding.Jpeg:
					TryInspectJpeg(data, out width, out height);
					break;
				case InlineImageEncoding.Webp:
					TryInspectWebp(data, out width, out height);
					break;
			}

			return width > 0 && height > 0;
		}

		// Frames an animated container declares. Malformed or truncated data counts what it could read (at least 1).
		private static long GetFrameCount(ReadOnlySpan<byte> data)
			=> Math.Max(1, DetectEncoding(data) switch
			{
				InlineImageEncoding.Gif => CountGifFrames(data),
				InlineImageEncoding.Png => CountApngFrames(data),
				InlineImageEncoding.Webp => CountWebpFrames(data),
				_ => 1,
			});

		private static long CountGifFrames(ReadOnlySpan<byte> data)
		{
			if (data.Length < 13)
			{
				return 1;
			}

			var offset = 13;
			if ((data[10] & 0x80) != 0)
			{
				offset += 3 << ((data[10] & 0x07) + 1);
			}

			long frames = 0;
			while (offset < data.Length)
			{
				switch (data[offset])
				{
					case 0x2C: // image descriptor
						frames++;
						if (offset + 10 > data.Length)
						{
							return frames;
						}
						var packed = data[offset + 9];
						offset += 10;
						if ((packed & 0x80) != 0)
						{
							offset += 3 << ((packed & 0x07) + 1);
						}
						offset++; // LZW minimum code size
						offset = SkipGifSubBlocks(data, offset);
						break;
					case 0x21: // extension
						offset = SkipGifSubBlocks(data, offset + 2);
						break;
					default: // trailer (0x3B) or garbage
						return frames;
				}
			}

			return frames;
		}

		private static int SkipGifSubBlocks(ReadOnlySpan<byte> data, int offset)
		{
			while (offset < data.Length && data[offset] != 0)
			{
				offset += data[offset] + 1;
			}

			return offset + 1;
		}

		private static long CountApngFrames(ReadOnlySpan<byte> data)
		{
			var offset = 8;
			while (offset + 12 <= data.Length)
			{
				var length = BinaryPrimitives.ReadUInt32BigEndian(data.Slice(offset, 4));
				var type = data.Slice(offset + 4, 4);
				if (type.SequenceEqual("acTL"u8))
				{
					return offset + 12 <= data.Length ? BinaryPrimitives.ReadUInt32BigEndian(data.Slice(offset + 8, 4)) : 1;
				}
				if (type.SequenceEqual("IDAT"u8) || length > int.MaxValue - 12)
				{
					return 1;
				}
				offset += 12 + (int)length;
			}

			return 1;
		}

		private static long CountWebpFrames(ReadOnlySpan<byte> data)
		{
			long frames = 0;
			var offset = 12;
			while (offset + 8 <= data.Length)
			{
				if (data.Slice(offset, 4).SequenceEqual("ANMF"u8))
				{
					frames++;
				}
				var size = BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(offset + 4, 4));
				if (size > int.MaxValue - 9)
				{
					break;
				}
				offset += 8 + (int)size + (int)(size & 1);
			}

			return frames;
		}

		private static void TryInspectJpeg(ReadOnlySpan<byte> data, out int width, out int height)
		{
			width = height = 0;
			var offset = 2;
			while (offset + 4 <= data.Length)
			{
				if (data[offset] != 0xff)
				{
					return;
				}

				var marker = data[offset + 1];
				if (marker == 0xff)
				{
					offset++;
					continue;
				}

				var length = BinaryPrimitives.ReadUInt16BigEndian(data.Slice(offset + 2, 2));
				// SOF0-SOF15, except DHT (C4), JPG (C8) and DAC (CC), carry the frame size.
				if (marker is >= 0xc0 and <= 0xcf and not 0xc4 and not 0xc8 and not 0xcc)
				{
					if (offset + 9 <= data.Length)
					{
						height = BinaryPrimitives.ReadUInt16BigEndian(data.Slice(offset + 5, 2));
						width = BinaryPrimitives.ReadUInt16BigEndian(data.Slice(offset + 7, 2));
					}
					return;
				}

				if (length < 2)
				{
					return;
				}
				offset += 2 + length;
			}
		}

		private static void TryInspectWebp(ReadOnlySpan<byte> data, out int width, out int height)
		{
			width = height = 0;
			if (data.Length < 30)
			{
				return;
			}

			var chunk = data.Slice(12, 4);
			if (chunk.SequenceEqual("VP8X"u8))
			{
				width = 1 + (data[24] | data[25] << 8 | data[26] << 16);
				height = 1 + (data[27] | data[28] << 8 | data[29] << 16);
			}
			else if (chunk.SequenceEqual("VP8L"u8) && data[20] == 0x2f)
			{
				var bits = BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(21, 4));
				width = (int)(bits & 0x3fff) + 1;
				height = (int)((bits >> 14) & 0x3fff) + 1;
			}
			else if (chunk.SequenceEqual("VP8 "u8))
			{
				width = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(26, 2)) & 0x3fff;
				height = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(28, 2)) & 0x3fff;
			}
		}

		private static InlineImageEncoding DetectEncoding(ReadOnlySpan<byte> data)
		{
			if (data.StartsWith(new byte[] { 0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a }))
			{
				return InlineImageEncoding.Png;
			}
			if (data.Length >= 3 && data[0] == 0xff && data[1] == 0xd8 && data[2] == 0xff)
			{
				return InlineImageEncoding.Jpeg;
			}
			if (data.StartsWith("GIF87a"u8) || data.StartsWith("GIF89a"u8))
			{
				return InlineImageEncoding.Gif;
			}
			if (data.Length >= 12 && data[..4].SequenceEqual("RIFF"u8) && data.Slice(8, 4).SequenceEqual("WEBP"u8))
			{
				return InlineImageEncoding.Webp;
			}
			if (data.Length >= 2 && data[0] == (byte)'B' && data[1] == (byte)'M')
			{
				return InlineImageEncoding.Bmp;
			}

			return InlineImageEncoding.Unknown;
		}

		private static bool TryWrapDib(ReadOnlySpan<byte> dib, out byte[] bitmap)
		{
			bitmap = Array.Empty<byte>();
			if (dib.Length < 12)
			{
				return false;
			}

			var headerSize = BinaryPrimitives.ReadUInt32LittleEndian(dib);
			if (headerSize < 12 || headerSize > (uint)dib.Length)
			{
				return false;
			}

			int colorTableBytes;
			int maskBytes;
			if (headerSize == 12)
			{
				var bitCount = BinaryPrimitives.ReadUInt16LittleEndian(dib.Slice(10, 2));
				colorTableBytes = bitCount <= 8 ? checked((1 << bitCount) * 3) : 0;
				maskBytes = 0;
			}
			else if (headerSize >= 40 && dib.Length >= 40)
			{
				var bitCount = BinaryPrimitives.ReadUInt16LittleEndian(dib.Slice(14, 2));
				var compression = BinaryPrimitives.ReadUInt32LittleEndian(dib.Slice(16, 4));
				var colorsUsed = BinaryPrimitives.ReadUInt32LittleEndian(dib.Slice(32, 4));
				var colorCount = colorsUsed != 0
					? colorsUsed
					: bitCount <= 8 ? 1u << bitCount : 0;
				if (colorCount > 256)
				{
					return false;
				}

				colorTableBytes = checked((int)colorCount * 4);
				maskBytes = headerSize == 40
					? compression switch
					{
						3 => 12,
						6 => 16,
						_ => 0,
					}
					: 0;
			}
			else
			{
				return false;
			}

			var pixelOffset = checked(14 + (int)headerSize + maskBytes + colorTableBytes);
			var fileSize = checked(14 + dib.Length);
			if (pixelOffset > fileSize || fileSize > MaxEncodedBytes)
			{
				return false;
			}

			bitmap = GC.AllocateUninitializedArray<byte>(fileSize);
			bitmap[0] = (byte)'B';
			bitmap[1] = (byte)'M';
			BinaryPrimitives.WriteUInt32LittleEndian(bitmap.AsSpan(2, 4), (uint)fileSize);
			bitmap.AsSpan(6, 4).Clear();
			BinaryPrimitives.WriteUInt32LittleEndian(bitmap.AsSpan(10, 4), (uint)pixelOffset);
			dib.CopyTo(bitmap.AsSpan(14));
			return true;
		}

		public bool Equals(InlineImageState? other)
			=> other is not null
				&& Width == other.Width
				&& Height == other.Height
				&& Ascent == other.Ascent
				&& VerticalAlignment == other.VerticalAlignment
				&& string.Equals(AlternateText, other.AlternateText, StringComparison.Ordinal)
				&& IsObjectFallback == other.IsObjectFallback
				&& (ReferenceEquals(_data, other._data) || _data.AsSpan().SequenceEqual(other._data));

		public override bool Equals(object? obj) => Equals(obj as InlineImageState);

		public override int GetHashCode()
			=> HashCode.Combine(Width, Height, Ascent, VerticalAlignment, AlternateText, IsObjectFallback, Data.Length);
	}
}

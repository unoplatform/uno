#nullable enable

using System;
using System.Buffers.Binary;
using System.IO;
using SkiaSharp;
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
		private SKImage? _decodedImage;
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

			var pixelCount = (long)pixelWidth * pixelHeight;
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

			using var data = SKData.CreateCopy(_data);
			using var codec = SKCodec.Create(data);
			return _decodedPixelCount = codec is null ? long.MaxValue : (long)codec.Info.Width * codec.Info.Height;
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

		internal SKImage? GetDecodedImage()
		{
			if (_decodedImage is null && _data.Length > 0)
			{
				Validate();
				using var data = SKData.CreateCopy(_data);
				_decodedImage = SKImage.FromEncodedData(data);
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
			using var encoded = decoded.Encode(SKEncodedImageFormat.Png, 100);
			if (encoded is null || encoded.Size is 0 or > MaxEncodedBytes)
			{
				throw new ArgumentException("The inline image cannot be represented safely in RTF.");
			}

			control = "pngblip";
			return encoded.ToArray();
		}

		private static bool TryInspect(byte[] data, out int width, out int height)
		{
			using var encoded = SKData.CreateCopy(data);
			using var codec = SKCodec.Create(encoded);
			width = codec?.Info.Width ?? 0;
			height = codec?.Info.Height ?? 0;
			return codec is not null;
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

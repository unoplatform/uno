#nullable enable

using System;
using SkiaSharp;

namespace Microsoft.UI.Text
{
	internal sealed class InlineImageState : IEquatable<InlineImageState>
	{
		internal const int MaxEncodedBytes = 4 * 1024 * 1024;
		internal const int MaxDimension = 8_192;
		internal const int MaxAlternateTextLength = 16_384;
		internal const long MaxDecodedPixels = 4L * 1024 * 1024;

		private byte[] _data = Array.Empty<byte>();
		private SKImage? _decodedImage;
		private long _decodedPixelCount = -1;

		public byte[] Data
		{
			get => _data;
			set
			{
				_data = value ?? Array.Empty<byte>();
				_decodedImage = null;
				_decodedPixelCount = -1;
			}
		}
		public int Width;
		public int Height;
		public int Ascent;
		public global::Microsoft.UI.Text.VerticalCharacterAlignment VerticalAlignment;
		public string AlternateText = string.Empty;

		public InlineImageState Clone() => (InlineImageState)MemberwiseClone();

		internal int EncodedLength => _data.Length;

		internal long GetDecodedPixelCount()
		{
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
			if (_data.Length is 0 or > MaxEncodedBytes
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

		public bool Equals(InlineImageState? other)
			=> other is not null
				&& Width == other.Width
				&& Height == other.Height
				&& Ascent == other.Ascent
				&& VerticalAlignment == other.VerticalAlignment
				&& string.Equals(AlternateText, other.AlternateText, StringComparison.Ordinal)
				&& (ReferenceEquals(_data, other._data) || _data.AsSpan().SequenceEqual(other._data));

		public override bool Equals(object? obj) => Equals(obj as InlineImageState);

		public override int GetHashCode()
			=> HashCode.Combine(Width, Height, Ascent, VerticalAlignment, AlternateText, Data.Length);
	}
}

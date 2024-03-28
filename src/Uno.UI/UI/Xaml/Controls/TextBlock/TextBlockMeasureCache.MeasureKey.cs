#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Windows.UI.Text;
using Windows.UI.Xaml.Media;
using Uno.Extensions;

namespace Windows.UI.Xaml.Controls
{
	[DebuggerDisplay("\"{_text}\"")]
	internal partial class TextBlockMeasureCache
	{
		/// <summary>
		/// Defines text characteristics
		/// </summary>
		private class MeasureKey
		{
			public class Comparer : IEqualityComparer<MeasureKey>
			{
				public static Comparer Instance { get; } = new Comparer();

				public bool Equals(MeasureKey? x, MeasureKey? y)
				{
					var v = x is not null
						&& y is not null
						&& x._text == y._text
						&& Math.Abs(x._fontSize - y._fontSize) < 0.1e-8
						&& x._fontFamily == y._fontFamily
						&& x._fontStyle == y._fontStyle
						&& x._textWrapping == y._textWrapping
						&& x._fontWeight == y._fontWeight
						&& x._maxLines == y._maxLines
						&& x._textTrimming == y._textTrimming
						&& x._textAlignment == y._textAlignment
						&& Math.Abs(x._lineHeight - y._lineHeight) < 0.1e-8
						&& x._padding == y._padding
						&& x._lineStackingStrategy == y._lineStackingStrategy
						&& x._textDecorations == y._textDecorations
						&& x._characterSpacing == y._characterSpacing;

					return v;
				}

				public int GetHashCode(MeasureKey obj)
					=> obj._hashCode;
			}

			private readonly int _hashCode;

			public MeasureKey(
				TextBlock source
			)
			{
				_fontStyle = source.FontStyle;
				_textWrapping = source.TextWrapping;
				_fontWeight = source.FontWeight;
				_text = source.Text;
				_fontFamily = source.FontFamily;
				_fontSize = source.FontSize;
				_maxLines = source.MaxLines;
				_textTrimming = source.TextTrimming;
				_textAlignment = source.TextAlignment;
				_lineHeight = source.LineHeight;
				_padding = source.Padding;
				_lineStackingStrategy = source.LineStackingStrategy;
				_characterSpacing = source.CharacterSpacing;
				_textDecorations = source.TextDecorations;

				_hashCode = _text?.GetHashCode() ?? 0
					^ _fontFamily.GetHashCode()
					^ _fontSize.GetHashCode();
			}

			public override int GetHashCode() => _hashCode;

			public override bool Equals(object? obj)
				=> obj is MeasureKey key && Comparer.Instance.Equals(this, key);

			private readonly FontStyle _fontStyle;
			private readonly TextWrapping _textWrapping;
			private readonly FontWeight _fontWeight;
			private readonly string? _text;
			private readonly FontFamily? _fontFamily;
			private readonly double _fontSize;
			private readonly int _maxLines;
			private readonly TextTrimming _textTrimming;
			private readonly TextAlignment _textAlignment;
			private readonly double _lineHeight;
			private readonly Thickness _padding;
			private readonly LineStackingStrategy _lineStackingStrategy;
			private readonly int _characterSpacing;
			private readonly TextDecorations _textDecorations;

			public FontFamily? FontFamily => _fontFamily;

			internal bool IsWrapping => _textWrapping != TextWrapping.NoWrap;
			internal bool IsClipping => _textTrimming != TextTrimming.None;
		}
	}
}

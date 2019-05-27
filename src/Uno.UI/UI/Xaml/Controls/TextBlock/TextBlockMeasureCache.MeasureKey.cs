using System.Collections.Generic;
using Windows.UI.Text;
using Windows.UI.Xaml.Media;
using Uno.Extensions;

namespace Windows.UI.Xaml.Controls
{
	public partial class TextBlockMeasureCache
	{
		class MeasureKey
		{
			public class Comparer : IEqualityComparer<MeasureKey>
			{
				public static Comparer Instance { get; } = new Comparer();

				public bool Equals(MeasureKey x, MeasureKey y) =>
					x.Text == y.Text
					&& x.FontSize == y.FontSize
					&& x.FontFamily == y.FontFamily
					&& x.FontStyle == y.FontStyle
					&& x.TextWrapping == y.TextWrapping
					&& x.FontWeight == y.FontWeight
					&& x.MaxLines == y.MaxLines
					&& x.TextTrimming == y.TextTrimming
					&& x.TextAlignment == y.TextAlignment
					&& x.LineHeight == y.LineHeight
					&& x.LineStackingStrategy == y.LineStackingStrategy
					&& x.TextDecorations == y.TextDecorations
					&& x.CharacterSpacing == y.CharacterSpacing;

				public int GetHashCode(MeasureKey obj)
					=> obj._hashCode;
			}

			private readonly int _hashCode;

			public MeasureKey(
				TextBlock source
			)
			{
				FontStyle = source.FontStyle;
				TextWrapping = source.TextWrapping;
				FontWeight = source.FontWeight;
				Text = source.Text;
				FontFamily = source.FontFamily;
				FontSize = source.FontSize;
				MaxLines = source.MaxLines;
				TextTrimming = source.TextTrimming;
				TextAlignment = source.TextAlignment;
				LineHeight = source.LineHeight;
				LineStackingStrategy = source.LineStackingStrategy;
				CharacterSpacing = source.CharacterSpacing;
				TextDecorations = source.TextDecorations;

				_hashCode = Text?.GetHashCode() ?? 0
					^ FontFamily.GetHashCode()
					^ FontSize.GetHashCode();
			}

			public override int GetHashCode() => _hashCode;

			public override bool Equals(object obj)
				=> obj is MeasureKey key ? Comparer.Instance.Equals(this, key) : false;

			public FontStyle FontStyle { get; }
			public TextWrapping TextWrapping { get; }
			public FontWeight FontWeight { get; }
			public string Text { get; }
			public FontFamily FontFamily { get; }
			public double FontSize { get; }
			public int MaxLines { get; }
			public TextTrimming TextTrimming { get; }
			public TextAlignment TextAlignment { get; }
			public double LineHeight { get; }
			public LineStackingStrategy LineStackingStrategy { get; }
			public int CharacterSpacing { get; }
			public TextDecorations TextDecorations { get; }
		}

	}
}

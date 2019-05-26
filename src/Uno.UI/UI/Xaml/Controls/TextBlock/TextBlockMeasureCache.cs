using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Windows.Foundation;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
using Microsoft.Extensions.Logging;
using Uno.Extensions;
using Uno;

namespace Windows.UI.Xaml.Controls
{
	public partial class TextBlockMeasureCache
	{
		private Dictionary<MeasureKey, MeasureEntry> _entries = new Dictionary<MeasureKey, MeasureEntry>(new MeasureKey.Comparer());

		public void CacheMeasure(TextBlock source, Size availableSize, Size measuredSize)
		{
			var key = new MeasureKey(source);

			if (!_entries.TryGetValue(key, out var entry))
			{
				_entries[key] = entry = new MeasureEntry();
			}

			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				// {0} is used to avoid parsing errors caused by formatting a "{}" in the text
				this.Log().LogDebug("{0}", $"TextMeasure-new [{source.Text} / {source.TextWrapping} / {source.MaxLines}]: {availableSize} -> {measuredSize}");
			}

			entry.CacheMeasure(availableSize, measuredSize);
		}

		public Size? FindMeasuredSize(TextBlock source, Size availableSize)
		{
			var key = new MeasureKey(source);

			if (_entries.TryGetValue(key, out var entry))
			{
				var measuredSize = entry.FindMeasuredSize(key, availableSize);

				if (this.Log().IsEnabled(LogLevel.Debug))
				{
					// {0} is used to avoid parsing errors caused by formatting a "{}" in the text
					this.Log().LogDebug("{0}", $"TextMeasure-cached [{source.Text} / {source.TextWrapping} / {source.MaxLines}]: {availableSize} -> {measuredSize}");
				}

				return measuredSize;

			}

			return null;
		}

		class MeasureEntry
		{
			public Dictionary<CachedTuple<double, double>, MeasureSizeEntry> _sizes
				= new Dictionary<CachedTuple<double, double>, MeasureSizeEntry>();

			public Size? FindMeasuredSize(MeasureKey key, Size availableSize)
			{
				if(_sizes.TryGetValue(CachedTuple.Create(availableSize.Width, availableSize.Height), out var sizeEntry))
				{
					return sizeEntry.MeasuredSize;
				}
				else
				{
					if(key.TextWrapping == TextWrapping.NoWrap)
					{
						// No wrap, assume any width below the asked available size
						// is valid.
						foreach(var keySize in _sizes)
						{
							if(keySize.Value.MeasuredSize.Width <= availableSize.Width)
							{
								return keySize.Value.MeasuredSize;
							}
						}
					}
					else
					{
						foreach (var keySize in _sizes)
						{
							// If text wraps and the available width is the same, any height below the
							// available size is valid.
							if (
								keySize.Key.Item1 == availableSize.Width
								&& keySize.Value.MeasuredSize.Height <= availableSize.Height
							)
							{
								return keySize.Value.MeasuredSize;
							}
						}
					}
				}
				return null;
			}

			internal void CacheMeasure(Size desiredSize, Size measuredSize)
			{
				_sizes[CachedTuple.Create(desiredSize.Width, desiredSize.Height)] = new MeasureSizeEntry(measuredSize);
			}
		}

		class MeasureSizeEntry
		{
			public MeasureSizeEntry(Size measuredSize) => MeasuredSize = measuredSize;

			public Size MeasuredSize { get; }
		}

		class MeasureKey
		{
			public class Comparer : IEqualityComparer<MeasureKey>
			{
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

				_hashCode = Text?.GetHashCode() ?? 0
					^ FontFamily.GetHashCode()
					^ FontSize.GetHashCode();
			}

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
		}

	}
}

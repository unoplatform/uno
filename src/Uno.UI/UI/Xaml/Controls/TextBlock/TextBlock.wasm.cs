using System;
using System.Collections.Generic;
using System.Globalization;
using Windows.Foundation;
using Windows.UI.Xaml.Documents;
using Uno.Extensions;
using Uno.Foundation;
using System.Linq;
using Uno.UI.UI.Xaml.Documents;
using Microsoft.Extensions.Logging;

namespace Windows.UI.Xaml.Controls
{
	partial class TextBlock : FrameworkElement
	{
		private const int MaxMeasureCache = 50;

		private int _cacheIndex = 0;
		private Dictionary<Size, (Size measuredSize, int age)> _measureCache = new Dictionary<Size, (Size, int)>();

		public TextBlock() : base("p")
		{
			OnFontStyleChangedPartial();
			OnFontWeightChangedPartial();
			OnTextChangedPartial();
			OnFontFamilyChangedPartial();
			OnCharacterSpacingChangedPartial();
			OnLineHeightChangedPartial();
			OnTextAlignmentChangedPartial();
			OnTextWrappingChangedPartial();
		}

		partial void InvalidateTextBlockPartial()
		{
			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().LogDebug($"Clearing measure cache");
			}
			_measureCache.Clear();
		}

		protected override Size MeasureOverride(Size availableSize)
		{
			if (_measureCache.TryGetValue(availableSize, out var result))
			{
				if (this.Log().IsEnabled(LogLevel.Debug))
				{
					// {0} is used to avoid parsing errors caused by formatting a "{}" in the text
					this.Log().LogDebug("{0}", $"Measure-cached [{Text}]: {availableSize} -> {result}");
				}

				return result.measuredSize;
			}

			var size = MeasureView(availableSize);

			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().LogDebug("{0}", $"Measure-new ({_measureCache.Count}) [{Text}]: {availableSize} -> {size}");
			}

			AddToMeasureCache(availableSize, size);
			return size;
		}

		private void AddToMeasureCache(Size availableSize, Size size)
		{
			_measureCache[availableSize] = (size, _cacheIndex++);

			if(_measureCache.Count > MaxMeasureCache)
			{
				var minValue = _measureCache.Min(p => p.Value.age) + MaxMeasureCache / 2;

				if (this.Log().IsEnabled(LogLevel.Debug))
				{
					this.Log().LogDebug($"Scavenging measure cache (Above {MaxMeasureCache}, {minValue})");
				}

				_measureCache.Remove(r => r.Value.age < minValue);
			}
		}

		private int GetCharacterIndexAtPoint(Point point)
		{
			throw new NotSupportedException();
		}

		partial void OnFontStyleChangedPartial()
		{
			this.SetFontStyle(FontStyle);
		}

		partial void OnFontWeightChangedPartial()
		{
			this.SetFontWeight(FontWeight);
		}

		partial void OnTextChangedPartial()
		{
			if (UseInlinesFastPath)
			{
				this.SetText(Text);
			}

			UpdateHitTest();
		}

		partial void ClearTextPartial()
		{
			SetHtmlContent(""); // Remove any child element
		}

		partial void OnFontFamilyChangedPartial()
		{
			this.SetFontFamily(FontFamily);
		}

		partial void OnFontSizeChangedPartial()
		{
			this.SetFontSize(FontSize);
		}

		partial void OnMaxLinesChangedPartial()
		{
			this.SetMaxLines(MaxLines);
		}

		partial void OnTextTrimmingChangedPartial()
		{
			this.SetTextTrimming(TextTrimming);
		}

		partial void OnForegroundChangedPartial()
		{
			this.SetForeground(Foreground);
		}

		partial void OnTextAlignmentChangedPartial()
		{
			this.SetTextAlignment(TextAlignment);
		}

		partial void OnLineHeightChangedPartial()
		{
			this.SetLineHeight(LineHeight);
		}

		partial void OnCharacterSpacingChangedPartial()
		{
			this.SetCharacterSpacing(CharacterSpacing);
		}

		partial void OnTextDecorationsChangedPartial()
		{
			this.SetTextDecorations(TextDecorations);
		}

		partial void OnTextWrappingChangedPartial()
		{
			this.SetTextWrapping(TextWrapping);
		}

		partial void OnPaddingChangedPartial()
		{
			this.SetTextPadding(Padding);
		}

		internal override bool IsViewHit()
		{
			return Text != null || base.IsViewHit();
		}
	}
}

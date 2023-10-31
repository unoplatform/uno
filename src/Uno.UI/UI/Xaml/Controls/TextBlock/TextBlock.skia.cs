using System;
using System.Collections.Generic;
using System.Globalization;
using Windows.Foundation;
using Windows.UI.Xaml.Documents;
using Uno.Extensions;
using System.Linq;
using Windows.UI.Xaml.Hosting;
using SkiaSharp;
using Windows.UI.Composition;
using System.Numerics;
using Windows.UI.Composition.Interactions;
using Uno.Disposables;
using Windows.UI.Xaml.Media;
using Uno.UI;
using Windows.UI.Xaml.Documents.TextFormatting;

#nullable enable

namespace Windows.UI.Xaml.Controls
{
	partial class TextBlock : FrameworkElement, IBlock
	{
		private readonly TextVisual _textVisual;

		public TextBlock()
		{
			SetDefaultForeground(ForegroundProperty);
			_textVisual = new TextVisual(Visual.Compositor, this);

			Visual.Children.InsertAtBottom(_textVisual);
		}

#if DEBUG
		private protected override void OnLoaded()
		{
			base.OnLoaded();
			_textVisual.Comment = $"{Visual.Comment}#text";
		}
#endif

		protected override Size MeasureOverride(Size availableSize)
		{
			var padding = Padding;
			var availableSizeWithoutPadding = availableSize.Subtract(padding);
			var defaultLineHeight = GetComputedLineHeight();
			var desiredSize = Inlines.Measure(availableSizeWithoutPadding, defaultLineHeight);

			return desiredSize.Add(padding);
		}

		private void ApplyFlowDirection(float width)
		{
			if (this.FlowDirection == FlowDirection.RightToLeft)
			{
				_textVisual.TransformMatrix = new Matrix4x4(new Matrix3x2(-1.0f, 0.0f, 0.0f, 1.0f, width, 0.0f));
			}
			else
			{
				_textVisual.TransformMatrix = Matrix4x4.Identity;
			}
		}

		protected override Size ArrangeOverride(Size finalSize)
		{
			var padding = Padding;
			var availableSizeWithoutPadding = finalSize.Subtract(padding);
			var arrangedSizeWithoutPadding = Inlines.Arrange(availableSizeWithoutPadding);
			_textVisual.Size = new Vector2((float)arrangedSizeWithoutPadding.Width, (float)arrangedSizeWithoutPadding.Height);
			_textVisual.Offset = new Vector3((float)padding.Left, (float)padding.Top, 0);
			ApplyFlowDirection((float)finalSize.Width);
			return base.ArrangeOverride(finalSize);
		}

		/// <summary>
		/// Gets the line height of the TextBlock either 
		/// based on the LineHeight property or the default 
		/// font line height.
		/// </summary>
		/// <returns>Computed line height</returns>
		internal float GetComputedLineHeight()
		{
			var lineHeight = LineHeight;
			if (!lineHeight.IsNaN() && lineHeight > 0)
			{
				return (float)lineHeight;
			}
			else
			{
				var font = FontDetailsCache.GetFont(FontFamily?.Source, (float)FontSize, FontWeight, FontStyle);
				return font.LineHeight;
			}
		}

		partial void InvalidateTextBlockPartial()
		{
			_cachedFontLineHeight = null;
		}

		internal override void OnPropertyChanged2(DependencyPropertyChangedEventArgs args)
		{
			base.OnPropertyChanged2(args);
			if (args.Property == FlowDirectionProperty)
			{
				ApplyFlowDirection((float)this.RenderSize.Width);
			}
		}

		private Hyperlink? FindHyperlinkAt(Point point)
		{
			var padding = Padding;
			var span = Inlines.GetRenderSegmentSpanAt(point - new Point(padding.Left, padding.Top), false);

			if (span == null)
			{
				return null;
			}

			var inline = span.Segment.Inline;

			while ((inline = inline.GetParent() as Inline) != null)
			{
				if (inline is Hyperlink hyperlink)
				{
					return hyperlink;
				}
			}

			return null;
		}

		partial void OnInlinesChangedPartial()
		{
			Inlines.InvalidateMeasure();
		}

		// Invalidate Inlines measure when any IBlock properties used during measuring change:

		partial void OnMaxLinesChangedPartial()
		{
			Inlines.InvalidateMeasure();
		}

		partial void OnTextWrappingChangedPartial()
		{
			Inlines.InvalidateMeasure();
		}

		partial void OnLineHeightChangedPartial()
		{
			Inlines.InvalidateMeasure();
		}

		partial void OnLineStackingStrategyChangedPartial()
		{
			Inlines.InvalidateMeasure();
		}
	}
}

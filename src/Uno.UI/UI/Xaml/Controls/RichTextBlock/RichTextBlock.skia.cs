using System;
using System.Collections.Generic;
using Windows.Foundation;
using Windows.UI.Composition;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Documents.TextFormatting;
using Uno.UI;
using Uno.UI.Xaml.Core;
using System.Numerics;

#nullable enable

namespace Windows.UI.Xaml.Controls;

partial class RichTextBlock
{
	private readonly BlocksVisual _textVisual;

	public RichTextBlock()
	{
		SetDefaultForeground(ForegroundProperty);

		_textVisual = new BlocksVisual(Visual.Compositor, this);
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
		var availableSizeWithoutPadding = availableSize.Subtract(padding).AtLeastZero();
		var defaultLineHeight = GetComputedLineHeight();
		var desiredSize = Blocks.Measure(availableSizeWithoutPadding, defaultLineHeight);

		desiredSize = desiredSize.Add(padding);

		if (GetUseLayoutRounding())
		{
			// In order to prevent text clipping as a result of layout rounding at
			// scales other than 1.0x, the ceiling of the rescaled size is used.
			var plateauScale = RootScale.GetRasterizationScaleForElement(this);
			Size pageNodeSize = desiredSize;
			desiredSize.Width = ((int)Math.Ceiling(pageNodeSize.Width * plateauScale)) / plateauScale;
			desiredSize.Height = ((int)Math.Ceiling(pageNodeSize.Height * plateauScale)) / plateauScale;

			// LsTextLine is not aware of layoutround and uses baseline height to place the rendered text.
			// However, because the height of the *block is potentionally layoutround-ed up, we should adjust the
			// placement of text by the difference.  Horizontal adjustment is not of concern since
			// LsTextLine uses arranged size which is already layoutround-ed.
			//_layoutRoundingHeightAdjustment = desiredSize.Height - pageNodeSize.Height;
		}

		return desiredSize;
	}

	protected override Size ArrangeOverride(Size finalSize)
	{
		var padding = Padding;
		var availableSizeWithoutPadding = finalSize.Subtract(padding);
		var arrangedSizeWithoutPadding = Blocks.Arrange(availableSizeWithoutPadding);
		_textVisual.Size = new Vector2((float)arrangedSizeWithoutPadding.Width, (float)arrangedSizeWithoutPadding.Height);
		_textVisual.Offset = new Vector3((float)padding.Left, (float)padding.Top, 0);
		ApplyFlowDirection((float)finalSize.Width);
		return base.ArrangeOverride(finalSize);
	}

	/// <summary>
	/// Gets the line height of the RichTextBlock either 
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

	internal override void OnPropertyChanged2(DependencyPropertyChangedEventArgs args)
	{
		base.OnPropertyChanged2(args);
		if (args.Property == FlowDirectionProperty)
		{
			ApplyFlowDirection((float)this.RenderSize.Width);
		}
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

	private Hyperlink? FindHyperlinkAt(Point point)
	{
		var padding = Padding;
		var span = Blocks.GetRenderSegmentSpanAt(point - new Point(padding.Left, padding.Top), false)?.span;

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

	partial void OnBlocksChangedPartial()
	{
		Blocks.InvalidateMeasure();
	}

	// Invalidate Blocks measure when any ITextVisualElement properties used during measuring change:

	partial void OnMaxLinesChangedPartial()
	{
		Blocks.InvalidateMeasure();
	}

	partial void OnTextWrappingChangedPartial()
	{
		Blocks.InvalidateMeasure();
	}

	partial void OnLineHeightChangedPartial()
	{
		Blocks.InvalidateMeasure();
	}

	partial void OnLineStackingStrategyChangedPartial()
	{
		Blocks.InvalidateMeasure();
	}
}

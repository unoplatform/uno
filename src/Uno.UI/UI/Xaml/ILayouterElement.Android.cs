#nullable enable
using System;
using Uno.UI;

namespace Windows.UI.Xaml;

internal partial interface ILayouterElement
{
	bool StretchAffectsMeasure { get; }

	HorizontalAlignment HorizontalAlignment { get; }

	VerticalAlignment VerticalAlignment { get; }

	internal void OnMeasureInternal(int widthMeasureSpec, int heightMeasureSpec)
	{
		var availableSize = ViewHelper.LogicalSizeFromSpec(widthMeasureSpec, heightMeasureSpec);

		this.DoMeasure(availableSize, out var measuredSizeLogical);

		var measuredSize = measuredSizeLogical.LogicalToPhysicalPixels();

		if (StretchAffectsMeasure)
		{
			if (HorizontalAlignment == HorizontalAlignment.Stretch &&
			    !double.IsPositiveInfinity(availableSize.Width))
			{
				measuredSize.Width = ViewHelper.MeasureSpecGetSize(widthMeasureSpec);
			}

			if (VerticalAlignment == VerticalAlignment.Stretch && !double.IsPositiveInfinity(availableSize.Height))
			{
				measuredSize.Height = ViewHelper.MeasureSpecGetSize(heightMeasureSpec);
			}
		}

		// Report our final dimensions.
		SetMeasuredDimensionInternal(
			(int)measuredSize.Width,
			(int)measuredSize.Height
		);
	}

	internal void SetMeasuredDimensionInternal(int width, int height);
}

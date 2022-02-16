#nullable enable
using System;
using System.Runtime.CompilerServices;
using Uno.UI;
using Windows.Foundation;

namespace Windows.UI.Xaml;

internal partial interface ILayouterElement
{
	bool StretchAffectsMeasure { get; }

	HorizontalAlignment HorizontalAlignment { get; }

	VerticalAlignment VerticalAlignment { get; }

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal Size OnMeasureInternal(int widthMeasureSpec, int heightMeasureSpec)
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

		return measuredSize;
	}

	internal void SetMeasuredDimensionInternal(int width, int height);
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX reference TickBar_Partial.cpp, TickBar_Partial.h

using System;
using System.Collections.Generic;
using System.Linq;
using DirectUI;
using Uno.UI.Extensions;
using Uno.UI.Xaml.Core;
using Windows.Foundation;
using Windows.UI.Xaml.Shapes;
using static Microsoft/* UWP don't rename */.UI.Xaml.Controls._Tracing;
using Uno.UI.Xaml.Core.Scaling;

namespace Windows.UI.Xaml.Controls.Primitives;

public sealed partial class TickBar
{
	private const int MIN_TICKMARK_GAP = 20;
	private const float DEBUG_FLOAT_EPSILON = 0.0001f;

	/// <summary>
	/// Initializes a new instance of the TickBar class.
	/// </summary>
	public TickBar()
	{
	}

	protected override Size ArrangeOverride(Size finalSize)
	{
		base.ArrangeOverride(finalSize);

		bool bVerticalMode = false;
		double finalLength = 0;
		double thumbOffset = 0;
		double visualRange = 0;
		uint tickMarkNumber = 0;
		double tickMarkInterval = 0;
		double numIntervals = 0;
		uint ratioOfLogicalToVisibleTickMarks = 0;
		int tickMarkDelta = 0;
		int i = 0;
		bool bIsDirectionReversed = false;
		Rect rcTick = new Rect();
		int j = 0;
		double zoomScale = 0.0;
		double singlePixelWidthScaled = 0.0;
		double currentX = 0;
		double currentY = 0;

#if TICKBAR_DBG
		Trace(L"BEGIN TickBar::ArrangeOverride()");
#endif // TICKBAR_DBG

		if (TemplatedParent == null)
		{
			throw new InvalidOperationException("Templated parent must be set");
		}
		var spParentSlider = TemplatedParent as Slider;
		if (spParentSlider != null)
		{
			// If tickFrequency <= 0, do nothing.
			var tickFrequency = spParentSlider.TickFrequency;
			if (DoubleUtil.LessThanOrClose(tickFrequency, 0))
			{
				return default;
			}

			zoomScale = RootScale.GetRasterizationScaleForElement(this);
			singlePixelWidthScaled = 1 / zoomScale;

#if TICKBAR_DBG
			swprintf_s(g_szTickBarDbg, g_szTickBarDbgLen,
				L"zoomScale=%.2f, singlePixelWidthScaled=%.2f", zoomScale, singlePixelWidthScaled);
			Trace(g_szTickBarDbg);
#endif // TICKBAR_DBG

			var orientation = spParentSlider.Orientation;
			if (orientation == Orientation.Horizontal)
			{
				finalLength = finalSize.Width;
				rcTick.Width = (float)singlePixelWidthScaled;
				rcTick.Height = finalSize.Height;
			}
			else
			{
				bVerticalMode = true;
				finalLength = finalSize.Height;
				rcTick.Width = finalSize.Width;
				rcTick.Height = (float)singlePixelWidthScaled;
			}

			// Determine visualRange, the range in which we lay out the tick marks.  This distance
			// is equal to the track length of the Slider minus the Thumb length in the direction of orientation.
			var thumbLength = spParentSlider.GetThumbLength();
			visualRange = finalLength - thumbLength;

			// Determine tickInterval, the amount of space that goes between visual tick marks.
			var min = spParentSlider.Minimum;
			var max = spParentSlider.Maximum;

			// Determine the number of intervals between tick marks, and then the number of tick marks.
			// There is one tick mark at the end of each full interval, and an additional tick mark at the start.
			numIntervals = DoubleUtil.Max(1, (max - min) / tickFrequency);
			tickMarkNumber = (uint)(DoubleUtil.Floor(numIntervals));

			tickMarkInterval = DoubleUtil.Max(1, visualRange / numIntervals);

#if TICKBAR_DBG
			swprintf_s(g_szTickBarDbg, g_szTickBarDbgLen,
				L"finalLength=%.2f, thumbLength=%.2f", finalLength, thumbLength);
			Trace(g_szTickBarDbg);
			swprintf_s(g_szTickBarDbg, g_szTickBarDbgLen,
				L"visualRange=%.2f, numIntervals=%.2f", visualRange, numIntervals);
			Trace(g_szTickBarDbg);
			swprintf_s(g_szTickBarDbg, g_szTickBarDbgLen,
				L"tickMarkNumber=%d, tickMarkInterval=%.2f", tickMarkNumber, tickMarkInterval);
			Trace(g_szTickBarDbg);
#endif // TICKBAR_DBG

			if (DoubleUtil.LessThan(tickMarkInterval, MIN_TICKMARK_GAP))
			{
				// Windows has a requirement we must honor that visual tick marks are not closer than MIN_TICKMARK_GAP.
				// If our calculated tickMarkInterval does not meet this requirement, we find the smallest multiple
				// of tickMarkInterval > MIN_TICKMARK_GAP, and omit drawing tick marks whose values are not multiples
				// of that multiple.
				ratioOfLogicalToVisibleTickMarks = (uint)DoubleUtil.Ceil(MIN_TICKMARK_GAP / tickMarkInterval);
				tickMarkInterval *= ratioOfLogicalToVisibleTickMarks;
				tickMarkNumber /= ratioOfLogicalToVisibleTickMarks;

#if TICKBAR_DBG
				Trace(L"tickMarkInterval < MIN_TICKMARK_GAP... will use smallest multiple of tickMarkInterval that is > MIN_TICKMARK_GAP");
				swprintf_s(g_szTickBarDbg, g_szTickBarDbgLen,
					L"ratioOfLogicalToVisibleTickMarks=%d", ratioOfLogicalToVisibleTickMarks);
				Trace(g_szTickBarDbg);
				swprintf_s(g_szTickBarDbg, g_szTickBarDbgLen,
					L"tickMarkNumber=%d, tickMarkInterval=%.2f", tickMarkNumber, tickMarkInterval);
				Trace(g_szTickBarDbg);
#endif // TICKBAR_DBG
			}

			// Draw the first tick mark. We need to account for this after doing the division by ratioOfLogicalToVisibleTickMarks,
			// so that it's not lost in that division.
			++tickMarkNumber;
			//MUX_TRACE("++tickMarkNumber, to draw the first tick mark");

			// Create the tick marks in the Children collection.
			var childrenLocal = this.GetChildren().OfType<UIElement>().ToList();
			var childrenSize = childrenLocal.Count;

			// tickMarkDelta is the number of tick marks we need to add or remove to reach tickMarkNumber, the
			// desired number of tick marks in the children collection.
			tickMarkDelta = (int)tickMarkNumber - childrenSize;

			if (tickMarkDelta < 0)
			{
				// If we have more tick marks than we need, slough off the extra ones.
				for (i = tickMarkDelta; i < 0; ++i)
				{
					RemoveChild(childrenLocal[childrenLocal.Count - 1]);
					childrenLocal.RemoveAt(childrenLocal.Count - 1);
				}
			}
			else
			{
				// Create additional tick marks until we have enough.
				var spTickBarFillBrush = this.Fill;

				for (; i < tickMarkDelta; ++i)
				{
					var spTickMark = new Rectangle();
					if (bVerticalMode)
					{
						spTickMark.Width = finalSize.Width;
						spTickMark.Height = singlePixelWidthScaled;
					}
					else
					{
						spTickMark.Width = singlePixelWidthScaled;
						spTickMark.Height = finalSize.Height;
					}
					spTickMark.Fill = spTickBarFillBrush;

					AddChild(spTickMark);
					childrenLocal.Add(spTickMark);
				}
			}

			// Actually arrange the tick marks.
			//
			// Note that the first tick mark should appear in the middle of the thumb when the thumb is
			// at its starting position.  If the thumb width is even, round up.
			//
			// When IsDirectionReversed is TRUE, we modify the value by 1px to account for the width of the tick mark.

			thumbOffset = DoubleUtil.Max(0, (thumbLength - singlePixelWidthScaled) / 2);
			bIsDirectionReversed = spParentSlider.IsDirectionReversed;

#if TICKBAR_DBG
			swprintf_s(g_szTickBarDbg, g_szTickBarDbgLen,
				L"finalSize.Width=%.2f, finalSize.Height=%.2f", finalSize.Width, finalSize.Height);
			Trace(g_szTickBarDbg);
#endif // TICKBAR_DBG

			for (; j < tickMarkNumber; ++j)
			{
				UIElement spChild = childrenLocal[j];

				double tickOffset;
				if (bVerticalMode == bIsDirectionReversed)
				{
					// For horizontal Slider or vertical Slider with IsDirectionReversed, thumb and tick marks
					// start at the beginning of the track rect.
					// Distribution is important because the final gap between ticks may be only a partial interval.
					tickOffset = thumbOffset + j * tickMarkInterval;
				}
				else
				{
					// For vertical Slider or horizontal Slider with IsDirectionReversed, thumb and tick marks
					// start at the end of the track rect.
					// Distribution is important because the final gap between ticks may be only a partial interval.
					tickOffset = finalLength - (thumbOffset + singlePixelWidthScaled) - j * tickMarkInterval;
				}

				if (bVerticalMode)
				{
					currentY = tickOffset;
				}
				else
				{
					currentX = tickOffset;
				}

				// We don't want to use fractional coordinates since tick marks should only occupy 1 screen pixel.
				// Since rounding errors accumulate, we round to the closest position right before arranging the
				// tick mark, and do not pass the rounded value to our next calculation.

#if TICKBAR_DBG
				swprintf_s(g_szTickBarDbg, g_szTickBarDbgLen,
					L"  TM:%d, currentX=%.2f, currentY=%.2f", j, currentX, currentY);
				Trace(g_szTickBarDbg);
#endif // TICKBAR_DBG

				// account for the 1px tick mark thickness
				MUX_ASSERT(DoubleUtil.GreaterThan(currentX, -singlePixelWidthScaled - DEBUG_FLOAT_EPSILON));
				MUX_ASSERT(DoubleUtil.LessThan(currentX, finalSize.Width + singlePixelWidthScaled + DEBUG_FLOAT_EPSILON));
				MUX_ASSERT(DoubleUtil.GreaterThan(currentY, -singlePixelWidthScaled - DEBUG_FLOAT_EPSILON));
				MUX_ASSERT(DoubleUtil.LessThan(currentY, finalSize.Height + singlePixelWidthScaled + DEBUG_FLOAT_EPSILON));

				rcTick.X = (float)currentX;
				rcTick.Y = (float)currentY;
				spChild.Arrange(rcTick);
			}

#if TICKBAR_DBG
			Trace(L"  END TickBar::ArrangeOverride()");
#endif // TICKBAR_DBG
		}

		return finalSize;
	}

	internal override void OnPropertyChanged2(DependencyPropertyChangedEventArgs args)
	{
		base.OnPropertyChanged2(args);

		if (args.Property == FillProperty)
		{
			PropagateFill();
		}
	}

	private void PropagateFill()
	{
		var tickBarFillBrush = Fill;
		var children = this.GetChildren();

		foreach (var child in children.OfType<Rectangle>())
		{
			child.Fill = tickBarFillBrush;
		}
	}
}

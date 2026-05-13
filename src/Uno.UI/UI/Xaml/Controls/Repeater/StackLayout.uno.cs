// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls;

// Uno-specific scaffolding for the C++ OrientationBasedMeasures multiple
// inheritance plus the IsSignificantViewportChange optimization for StackLayout
// on native targets (Skia uses the unmodified WinUI flow).
partial class StackLayout
{
	private ScrollOrientation _scrollOrientation;

	ScrollOrientation OrientationBasedMeasures.ScrollOrientation
	{
		get => _scrollOrientation;
		set => _scrollOrientation = value;
	}

	private ScrollOrientation GetScrollOrientation() => _scrollOrientation;

	private void SetScrollOrientation(ScrollOrientation value) => _scrollOrientation = value;

	private double Major(Size size) => ((OrientationBasedMeasures)this).Major(size);
	private double Minor(Size size) => ((OrientationBasedMeasures)this).Minor(size);
	private double Major(Point point) => ((OrientationBasedMeasures)this).Major(point);
	private double Minor(Point point) => ((OrientationBasedMeasures)this).Minor(point);

	private double MajorSize(Rect rect) => ((OrientationBasedMeasures)this).MajorSize(rect);
	private double MinorSize(Rect rect) => ((OrientationBasedMeasures)this).MinorSize(rect);
	private double MajorStart(Rect rect) => ((OrientationBasedMeasures)this).MajorStart(rect);
	private double MajorEnd(Rect rect) => ((OrientationBasedMeasures)this).MajorEnd(rect);
	private double MinorStart(Rect rect) => ((OrientationBasedMeasures)this).MinorStart(rect);
	private double MinorEnd(Rect rect) => ((OrientationBasedMeasures)this).MinorEnd(rect);

	private void SetMajorSize(ref Rect rect, double value) => ((OrientationBasedMeasures)this).SetMajorSize(ref rect, value);
	private void SetMajorStart(ref Rect rect, double value) => ((OrientationBasedMeasures)this).SetMajorStart(ref rect, value);
	private void SetMinorSize(ref Rect rect, double value) => ((OrientationBasedMeasures)this).SetMinorSize(ref rect, value);
	private void SetMinorStart(ref Rect rect, double value) => ((OrientationBasedMeasures)this).SetMinorStart(ref rect, value);

	private Rect MinorMajorRect(float minor, float major, float minorSize, float majorSize)
		=> ((OrientationBasedMeasures)this).MinorMajorRect(minor, major, minorSize, majorSize);

	private Point MinorMajorPoint(float minor, float major)
		=> ((OrientationBasedMeasures)this).MinorMajorPoint(minor, major);

	private Size MinorMajorSize(float minor, float major)
		=> ((OrientationBasedMeasures)this).MinorMajorSize(minor, major);

#if !__SKIA__
	/// <inheritdoc />
	protected internal override bool IsSignificantViewportChange(object state, Rect oldViewport, Rect newViewport)
	{
		if (state is StackLayoutState { Uno_LastKnownAverageElementSize: > 0 } stackState)
		{
			var neededElementsCountToFillNewViewport = Math.Min(stackState.Uno_LastKnownItemsCount, MajorSize(newViewport) / stackState.Uno_LastKnownAverageElementSize);
			if (stackState.Uno_LastKnownRealizedElementsCount < neededElementsCountToFillNewViewport)
			{
				// Only a few first items have been measured so far (usually 1 or 2), this might be because the IR is within a SV and not visible yet.
				// Note: In that case we have to make sure to not only validate the Major axis since the parent SV could be vertical while local layout itself is horizontal.
				// Note2: Depending of the platform (Android), we might be invoked with empty viewport, make sure to consider out-of-bound in such case.
				// Test case: When_NestedInSVAndOutOfViewportOnInitialLoad_Then_MaterializedEvenWhenScrollingOnMinorAxis
				const int threshold = 100; // Allows 100px above and after to trigger loading even before IR is visible.
				var isOutOfBounds = newViewport is { Width: 0 } or { Height: 0 }
					|| MajorEnd(newViewport) < -threshold
					|| MajorStart(newViewport) > Major(stackState.Uno_LastKnownDesiredSize) + threshold
					|| MinorEnd(newViewport) < -threshold
					|| MinorStart(newViewport) > Minor(stackState.Uno_LastKnownDesiredSize) + threshold;

				return !isOutOfBounds;
			}

			var size = Math.Max(MajorSize(oldViewport), MajorSize(newViewport));
			var minDelta = Math.Min(stackState.Uno_LastKnownAverageElementSize * 5, size);

			return Math.Abs(MajorStart(oldViewport) - MajorStart(newViewport)) > minDelta
				|| Math.Abs(MajorEnd(oldViewport) - MajorEnd(newViewport)) > minDelta;
		}
		else
		{
			return base.IsSignificantViewportChange(state, oldViewport, newViewport);
		}
	}
#endif
}

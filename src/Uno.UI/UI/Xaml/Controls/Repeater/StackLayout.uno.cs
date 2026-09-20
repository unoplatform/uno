// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls;

// Uno-specific scaffolding for the C++ OrientationBasedMeasures multiple
// inheritance, plus the stable layout origin GetExtent needs on Uno.
partial class StackLayout
{
	/// <summary>
	/// Holds the layout origin WinUI's GetExtent formula produces steady across measure passes.
	/// </summary>
	/// <remarks>
	/// WinUI computes the origin as firstRealizedLayoutBounds.MajorStart - firstRealizedItemIndex *
	/// averageElementSize. averageElementSize is a running mean over the realized items, so the origin
	/// drifts by a few pixels whenever a tall item enters or leaves the realization buffer. WinUI
	/// absorbs that drift through the viewport shift the repeater hands its scroller; Uno's
	/// ScrollViewer does not consume ItemsRepeater's pending viewport shift, so the drift lands
	/// straight on screen: items change their repeater-local Y between two wheel ticks, and a
	/// shrinking estimate drags the scroll offset backward mid-gesture (uno#23041, uno#23042).
	///
	/// So we keep the origin we last reported, and let MajorSize grow to cover the realized range
	/// instead. Two cases release it: the first item being realized, where the natural origin is 0
	/// and holding a stale one would offset the whole list, and realized items sitting above the held
	/// origin, which happens when scrolling back up puts items at negative algorithm coordinates --
	/// holding then would leave them above the repeater's frame, unreachable.
	///
	/// Remove once Uno's ScrollViewer honours the pending viewport shift.
	/// </remarks>
	private void StabilizeExtentOrigin(
		ref Rect extent,
		StackLayoutState stackState,
		int firstRealizedItemIndex,
		Rect firstRealizedLayoutBounds)
	{
		var held = stackState.Uno_LastReportedExtentMajorStart;
		var hasHeld = !float.IsNaN(held);
		var itemsAboveHeld = hasHeld && MajorStart(firstRealizedLayoutBounds) < held;

		if (hasHeld && firstRealizedItemIndex != 0 && !itemsAboveHeld)
		{
			SetMajorStart(ref extent, held);
		}

		stackState.Uno_LastReportedExtentMajorStart = (float)MajorStart(extent);
	}

	private ScrollOrientation _scrollOrientation;

	ScrollOrientation OrientationBasedMeasures.ScrollOrientation
	{
		get => _scrollOrientation;
		set => _scrollOrientation = value;
	}

#pragma warning disable IDE0051 // Scaffolding kept for 1:1 parity with WinUI OrientationBasedMeasures.h:12-38 — some overloads have no live caller in this port yet.
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
#pragma warning restore IDE0051
}

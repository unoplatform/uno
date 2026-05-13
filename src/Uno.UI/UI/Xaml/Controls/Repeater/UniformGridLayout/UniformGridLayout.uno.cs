// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls;

// Uno-specific scaffolding for the C++ OrientationBasedMeasures multiple inheritance.
partial class UniformGridLayout
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
}

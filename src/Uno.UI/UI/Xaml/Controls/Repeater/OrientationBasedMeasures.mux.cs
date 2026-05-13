// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference OrientationBasedMeasures.cpp, commit 4b206bce3

using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls;

// In the C++ source these are member functions on OrientationBasedMeasures
// that return float& references so the caller can both read and mutate the
// underlying field of the Size/Point/Rect. C# cannot return an in-struct
// reference to a value type field, so the port surfaces paired getter and
// setter helpers (Major/Minor + SetMajorSize/SetMinorStart, etc.).
internal static class OrientationBasedMeasuresExtensions
{
	// Major - Scrolling/virtualizing direction
	// Minor - Opposite direction
	public static double Major(this OrientationBasedMeasures obm, Size size)
		=> obm.ScrollOrientation == ScrollOrientation.Vertical ? size.Height : size.Width;

	public static double Minor(this OrientationBasedMeasures obm, Size size)
		=> obm.ScrollOrientation == ScrollOrientation.Vertical ? size.Width : size.Height;

	public static double Major(this OrientationBasedMeasures obm, Point point)
		=> obm.ScrollOrientation == ScrollOrientation.Vertical ? point.Y : point.X;

	public static double Minor(this OrientationBasedMeasures obm, Point point)
		=> obm.ScrollOrientation == ScrollOrientation.Vertical ? point.X : point.Y;

	public static double MajorSize(this OrientationBasedMeasures obm, Rect rect)
		=> obm.ScrollOrientation == ScrollOrientation.Vertical ? rect.Height : rect.Width;

	public static void SetMajorSize(this OrientationBasedMeasures obm, ref Rect rect, double value)
	{
		if (obm.ScrollOrientation == ScrollOrientation.Vertical)
		{
			rect.Height = value;
		}
		else
		{
			rect.Width = value;
		}
	}

	public static double MinorSize(this OrientationBasedMeasures obm, Rect rect)
		=> obm.ScrollOrientation == ScrollOrientation.Vertical ? rect.Width : rect.Height;

	public static void SetMinorSize(this OrientationBasedMeasures obm, ref Rect rect, double value)
	{
		if (obm.ScrollOrientation == ScrollOrientation.Vertical)
		{
			rect.Width = value;
		}
		else
		{
			rect.Height = value;
		}
	}

	public static double MajorStart(this OrientationBasedMeasures obm, Rect rect)
		=> obm.ScrollOrientation == ScrollOrientation.Vertical ? rect.Y : rect.X;

	public static void SetMajorStart(this OrientationBasedMeasures obm, ref Rect rect, double value)
	{
		if (obm.ScrollOrientation == ScrollOrientation.Vertical)
		{
			rect.Y = value;
		}
		else
		{
			rect.X = value;
		}
	}

	public static double MajorEnd(this OrientationBasedMeasures obm, Rect rect)
		=> obm.ScrollOrientation == ScrollOrientation.Vertical ? rect.Y + rect.Height : rect.X + rect.Width;

	public static double MinorStart(this OrientationBasedMeasures obm, Rect rect)
		=> obm.ScrollOrientation == ScrollOrientation.Vertical ? rect.X : rect.Y;

	public static void SetMinorStart(this OrientationBasedMeasures obm, ref Rect rect, double value)
	{
		if (obm.ScrollOrientation == ScrollOrientation.Vertical)
		{
			rect.X = value;
		}
		else
		{
			rect.Y = value;
		}
	}

	// Convenience setter that mirrors the C++ `MinorStart(rect) += increment` mutation.
	public static void AddMinorStart(this OrientationBasedMeasures obm, ref Rect rect, double increment)
	{
		if (obm.ScrollOrientation == ScrollOrientation.Vertical)
		{
			rect.X += increment;
		}
		else
		{
			rect.Y += increment;
		}
	}

	public static double MinorEnd(this OrientationBasedMeasures obm, Rect rect)
		=> obm.ScrollOrientation == ScrollOrientation.Vertical ? rect.X + rect.Width : rect.Y + rect.Height;

	public static Rect MinorMajorRect(this OrientationBasedMeasures obm, float minor, float major, float minorSize, float majorSize)
		=> obm.ScrollOrientation == ScrollOrientation.Vertical
			? new Rect(minor, major, minorSize, majorSize)
			: new Rect(major, minor, majorSize, minorSize);

	public static Point MinorMajorPoint(this OrientationBasedMeasures obm, float minor, float major)
		=> obm.ScrollOrientation == ScrollOrientation.Vertical ? new Point(minor, major) : new Point(major, minor);

	public static Size MinorMajorSize(this OrientationBasedMeasures obm, float minor, float major)
		=> obm.ScrollOrientation == ScrollOrientation.Vertical ? new Size(minor, major) : new Size(major, minor);
}

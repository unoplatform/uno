// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls
{
	// Note: WinUI uses this as base class for multi-parents inheritance.
	//		 for C# we use the "default interface method" to achieve this,
	//		 but it's actually **NOT** an interface!
	internal interface OrientationBasedMeasures
	{
		ScrollOrientation ScrollOrientation { get; set; } // = ScrollOrientation.Vertical; we cannot init properties of interfaces, however Vertical is already the default value!
	}

	internal static class OrientationBasedMeasuresExtensions
	{
		public static double Major(this OrientationBasedMeasures obm, Size size)
			=> obm.ScrollOrientation == ScrollOrientation.Vertical ? size.Height : size.Width;

		public static double Minor(this OrientationBasedMeasures obm, Size size)
			=> obm.ScrollOrientation == ScrollOrientation.Vertical ? size.Width : size.Height;

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

		public static void AddMinorStart(this OrientationBasedMeasures obm, ref Rect rect, double increment)
		{
			if (obm.ScrollOrientation == ScrollOrientation.Vertical)
			{
				rect.X += increment;
			}
			else
			{
				rect.Y -= increment;
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
}

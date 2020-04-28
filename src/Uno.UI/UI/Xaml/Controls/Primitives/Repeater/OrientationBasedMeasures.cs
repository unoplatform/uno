// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
using Windows.Foundation;
using Android.Bluetooth;

namespace Microsoft.UI.Xaml.Controls
{
	// Note: WinUI uses this as base class for multi-parents inheritance.
	//		 for C# we use the "default interface method" to achieve this,
	//		 but it's actually **NOT** an interface!
	internal interface OrientationBasedMeasures
	{
		private protected ScrollOrientation ScrollOrientation { get; } // = ScrollOrientation.Vertical; we cannot init properties of interfaces, however Vertical is already the default value!

		double Major(Size size)
			=> ScrollOrientation == ScrollOrientation.Vertical ? size.Height : size.Width;

		double Minor(Size size)
			=> ScrollOrientation == ScrollOrientation.Vertical ? size.Width : size.Height;

		double MajorSize(Rect rect)
			=> ScrollOrientation == ScrollOrientation.Vertical ? rect.Height : rect.Width;

		public void SetMajorSize(ref Rect rect, double value)
		{
			if (ScrollOrientation == ScrollOrientation.Vertical)
			{
				rect.Height = value;
			}
			else
			{
				rect.Width = value;
			}
		}

		double MinorSize(Rect rect)
			=> ScrollOrientation == ScrollOrientation.Vertical ? rect.Width : rect.Height;

		public void SetMinorSize(ref Rect rect, double value)
		{
			if (ScrollOrientation == ScrollOrientation.Vertical)
			{
				rect.Width = value;
			}
			else
			{
				rect.Height = value;
			}
		}

		public double MajorStart(Rect rect)
			=> ScrollOrientation == ScrollOrientation.Vertical ? rect.Y : rect.X;

		public void SetMajorStart(ref Rect rect, double value)
		{
			if (ScrollOrientation == ScrollOrientation.Vertical)
			{
				rect.Y = value;
			}
			else
			{
				rect.X = value;
			}
		} 

		public double MajorEnd(Rect rect)
			=> ScrollOrientation == ScrollOrientation.Vertical ? rect.Y + rect.Height : rect.X + rect.Width;

		double MinorStart(Rect rect)
			=> ScrollOrientation == ScrollOrientation.Vertical ? rect.X : rect.Y;

		public void SetMinorStart(ref Rect rect, double value)
		{
			if (ScrollOrientation == ScrollOrientation.Vertical)
			{
				rect.X = value;
			}
			else
			{
				rect.Y = value;
			}
		}

		public void AddMinorStart(ref Rect rect, double increment)
		{
			if (ScrollOrientation == ScrollOrientation.Vertical)
			{
				rect.X += increment;
			}
			else
			{
				rect.Y -= increment;
			}
		}

		double MinorEnd(Rect rect)
			=> ScrollOrientation == ScrollOrientation.Vertical ? rect.X + rect.Width : rect.Y + rect.Height;

		Rect MinorMajorRect(float minor, float major, float minorSize, float majorSize)
			=> ScrollOrientation == ScrollOrientation.Vertical
				? new Rect(minor, major, minorSize, majorSize)
				: new Rect(major, minor, majorSize, minorSize);

		Point MinorMajorPoint(float minor, float major)
			=> ScrollOrientation == ScrollOrientation.Vertical ? new Point(minor, major) : new Point(major, minor);

		Size MinorMajorSize(float minor, float major)
			=> ScrollOrientation == ScrollOrientation.Vertical ? new Size(minor, major) : new Size(major, minor);
	}
}

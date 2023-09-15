// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using MUXControlsTestApp.Utilities;
using Windows.Foundation;
using Microsoft.UI.Xaml.Controls;
using DisplayInformation = Windows.Graphics.Display.DisplayInformation;

namespace Microsoft.UI.Xaml.Tests.MUXControls.ApiTests.RepeaterTests.Common
{
	public class OrientationBasedMeasures
	{
		public ScrollOrientation ScrollOrientation { get; set; }
		private double m_rawPixelsPerViewPixel = 1.0;
		private bool m_useLayoutRounding;

		public bool IsVerical
		{
			get { return ScrollOrientation == ScrollOrientation.Vertical; }
		}

		public OrientationBasedMeasures(ScrollOrientation o, bool useLayoutRounding = true)
		{
			ScrollOrientation = o;
			m_useLayoutRounding = useLayoutRounding;

			bool? hasThreadAccess = Window.Current.DispatcherQueue.HasThreadAccess;
			if (useLayoutRounding && hasThreadAccess.HasValue && hasThreadAccess.Value)
				m_rawPixelsPerViewPixel = DisplayInformation.GetForCurrentView().RawPixelsPerViewPixel;
		}

		public double Major(Size size)
		{
			return RoundForLayout(IsVerical ? size.Height : size.Width);
		}

		public double Minor(Size size)
		{
			return RoundForLayout(IsVerical ? size.Width : size.Height);
		}

		public double MajorSize(Rect rect)
		{
			return RoundForLayout(IsVerical ? rect.Height : rect.Width);
		}

		public double MinorSize(Rect rect)
		{
			return RoundForLayout(IsVerical ? rect.Width : rect.Height);
		}

		public double MajorStart(Rect rect)
		{
			return RoundForLayout(IsVerical ? rect.Top : rect.Left);
		}

		public double MajorEnd(Rect rect)
		{
			return RoundForLayout(IsVerical ? rect.Bottom : rect.Right);
		}

		public double MinorStart(Rect rect)
		{
			return RoundForLayout(IsVerical ? rect.Left : rect.Top);
		}

		public void SetMajorSize(ref Rect rect, double value)
		{
			if (IsVerical)
			{
				rect.Height = RoundForLayout(value);
			}
			else
			{
				rect.Width = RoundForLayout(value);
			}
		}

		public void SetMajorStart(ref Rect rect, double value)
		{
			if (IsVerical)
			{
				rect.Y = RoundForLayout(value);
			}
			else
			{
				rect.X = RoundForLayout(value);
			}
		}

		public void SetMinorStart(ref Rect rect, double value)
		{
			if (IsVerical)
			{
				rect.X = RoundForLayout(value);
			}
			else
			{
				rect.Y = RoundForLayout(value);
			}
		}

		public Rect MinorMajorRect(double minor, double major, double minorSize, double majorSize)
		{
			var min = RoundForLayout(minor);
			var maj = RoundForLayout(major);
			var minSize = RoundForLayout(minorSize);
			var majSize = RoundForLayout(majorSize);
			return
				IsVerical ?
				new Rect(min, maj, minSize, majSize) :
				new Rect(maj, min, majSize, minSize);
		}

		public Point MinorMajorPoint(double minor, double major)
		{
			var min = RoundForLayout(minor);
			var maj = RoundForLayout(major);
			return
				IsVerical ?
				new Point(min, maj) :
				new Point(maj, min);
		}

		public Size MinorMajorSize(double minor, double major)
		{
			var min = RoundForLayout(minor);
			var maj = RoundForLayout(major);
			return
				IsVerical ?
				new Size(min, maj) :
				new Size(maj, min);
		}

		private double RoundForLayout(double value)
		{
			if (m_useLayoutRounding)
				return global::System.Math.Floor((m_rawPixelsPerViewPixel * value) + 0.5) / m_rawPixelsPerViewPixel;
			else
				return value;
		}
	}
}

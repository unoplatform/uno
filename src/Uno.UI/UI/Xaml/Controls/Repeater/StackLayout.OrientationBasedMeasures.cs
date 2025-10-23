using System;
using System.Linq;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls
{
	partial class StackLayout : OrientationBasedMeasures
	{
		private ScrollOrientation ScrollOrientation { get; set; }
		ScrollOrientation OrientationBasedMeasures.ScrollOrientation
		{
			get => ScrollOrientation;
			set => ScrollOrientation = value;
		}

		private protected double Major(Size size) => ((OrientationBasedMeasures)this).Major(size);
		private protected double Minor(Size size) => ((OrientationBasedMeasures)this).Minor(size);
		private protected double MajorSize(Rect rect) => ((OrientationBasedMeasures)this).MajorSize(rect);
		private protected double MinorSize(Rect rect) => ((OrientationBasedMeasures)this).MinorSize(rect);
		private protected double MajorStart(Rect rect) => ((OrientationBasedMeasures)this).MajorStart(rect);
		private protected double MajorEnd(Rect rect) => ((OrientationBasedMeasures)this).MajorEnd(rect);
		private protected double MinorStart(Rect rect) => ((OrientationBasedMeasures)this).MinorStart(rect);
		private protected double MinorEnd(Rect rect) => ((OrientationBasedMeasures)this).MinorEnd(rect);
		private protected Rect MinorMajorRect(float minor, float major, float minorSize, float majorSize) => ((OrientationBasedMeasures)this).MinorMajorRect(minor, major, minorSize, majorSize);
		private protected Point MinorMajorPoint(float minor, float major) => ((OrientationBasedMeasures)this).MinorMajorPoint(minor, major);
		private protected Size MinorMajorSize(float minor, float major) => ((OrientationBasedMeasures)this).MinorMajorSize(minor, major);

		private protected void SetMajorSize(ref Rect rect, double value) => ((OrientationBasedMeasures)this).SetMajorSize(ref rect, value);
		private protected void SetMajorStart(ref Rect rect, double value) => ((OrientationBasedMeasures)this).SetMajorStart(ref rect, value);
		private protected void SetMinorSize(ref Rect rect, double value) => ((OrientationBasedMeasures)this).SetMinorSize(ref rect, value);
		private protected void SetMinorStart(ref Rect rect, double value) => ((OrientationBasedMeasures)this).SetMinorStart(ref rect, value);

		private protected void AddMinorStart(ref Rect rect, double increment) => ((OrientationBasedMeasures)this).AddMinorStart(ref rect, increment);
	}
}

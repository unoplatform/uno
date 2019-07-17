using System;
using Android.Animation;

namespace Windows.UI.Xaml.Media.Animation
{
	partial class CircleEase
	{
		internal override ITimeInterpolator CreateTimeInterpolator()
		{
			throw new NotSupportedException();
		}
	}
}

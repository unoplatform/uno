using System;
using System.Collections.Generic;
using System.Text;
using Android.Animation;

namespace Microsoft.UI.Xaml.Media.Animation
{
	public partial class CubicEase
	{
		internal override ITimeInterpolator CreateTimeInterpolator()
		{
			return EasingFunctionHelpers.GetPowerTimeInterpolator(3, this.EasingMode);
		}
	}
}

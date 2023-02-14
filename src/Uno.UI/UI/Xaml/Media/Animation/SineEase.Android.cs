using System;
using System.Collections.Generic;
using System.Text;
using Android.Animation;

namespace Microsoft.UI.Xaml.Media.Animation
{
	public partial class SineEase
	{
		internal override ITimeInterpolator CreateTimeInterpolator()
		{
			//There are no particular sine ease function in android therefore we replicate it with a regular quadratic function
			return EasingFunctionHelpers.GetPowerTimeInterpolator(2, this.EasingMode);
		}
	}
}

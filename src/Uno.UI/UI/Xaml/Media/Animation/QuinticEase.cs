using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.UI.Xaml.Media.Animation
{
	public partial class QuinticEase : EasingFunctionBase
	{
		private protected override double EaseInCore(double normalizedTime)
		{
			return normalizedTime * normalizedTime * normalizedTime * normalizedTime * normalizedTime;
		}
	}
}

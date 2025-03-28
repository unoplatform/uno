
using System;

namespace Windows.UI.Xaml.Media.Animation
{
	public partial class CircleEase : EasingFunctionBase
	{
		private protected override double EaseInCore(double normalizedTime)
		{
			return 1 - Math.Sqrt(1 - normalizedTime * normalizedTime);
		}
	}
}

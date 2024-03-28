using System;

namespace Windows.UI.Xaml.Media.Animation
{
	public partial class SineEase : EasingFunctionBase
	{
		private protected override double EaseInCore(double normalizedTime)
		{
			return 1.0 - EaseOutSine(1 - normalizedTime);
		}

		private static double EaseOutSine(double normalizedTime)
		{
			return Math.Sin(normalizedTime * (Math.PI / 2));
		}
	}
}

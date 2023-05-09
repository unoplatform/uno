using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.UI.Xaml.Media.Animation
{
	public partial class BounceEase : EasingFunctionBase
	{
		// Source: https://easings.net/

		private protected override double EaseInCore(double normalizedTime)
		{
			return 1.0 - BounceEaseOut(1.0 - normalizedTime);
		}

		private static double BounceEaseOut(double progress)
		{
			const double n1 = 7.5625;
			const double d1 = 2.75;

			if (progress < 1 / d1)
			{
				return n1 * progress * progress;
			}
			else if (progress < 2 / d1)
			{
				return n1 * (progress -= 1.5 / d1) * progress + 0.75;
			}
			else if (progress < 2.5 / d1)
			{
				return n1 * (progress -= 2.25 / d1) * progress + 0.9375;
			}
			else
			{
				return n1 * (progress -= 2.625 / d1) * progress + 0.984375;
			}
		}

	}
}

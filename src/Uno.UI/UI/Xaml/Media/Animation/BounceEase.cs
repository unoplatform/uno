using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.UI.Xaml.Media.Animation
{
	public partial class BounceEase : EasingFunctionBase
	{
		// Source: https://easings.net/

		public override double Ease(double currentTime, double startValue, double finalValue, double duration)
		{
			var delta = finalValue - startValue;
			var progress = currentTime / duration;

			var ratio = EaseCore(progress);

			return startValue + ratio * delta;
		}

		internal double EaseCore(double progress)
		{
			switch (EasingMode)
			{
				case EasingMode.EaseIn:
					return BounceEaseIn(progress);

				case EasingMode.EaseOut:
					return BounceEaseOut(progress);

				case EasingMode.EaseInOut:
				default:
					return BounceEaseInOut(progress);
			}
		}

		private static double BounceEaseIn(double progress)
		{
			return 1 - BounceEaseOut(1 - progress);
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

		private static double BounceEaseInOut(double progress)
		{
			return progress < 0.5
				? (1 - BounceEaseOut(1 - 2 * progress)) / 2
				: (1 + BounceEaseOut(2 * progress - 1)) / 2;
		}
}
}

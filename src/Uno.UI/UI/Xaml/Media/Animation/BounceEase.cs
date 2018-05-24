using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.UI.Xaml.Media.Animation
{
	public partial class BounceEase : EasingFunctionBase
	{
		public override double Ease(double currentTime, double startValue, double finalValue, double duration)
		{
			//Depending on the mode we have different functions for the return value.
			switch (this.EasingMode)
			{
				case EasingMode.EaseIn:
					return BounceEaseIn(currentTime, startValue,finalValue, duration);

				case EasingMode.EaseOut:
					return BounceEaseOut(currentTime, startValue, finalValue, duration);

				case EasingMode.EaseInOut:

					if (currentTime < duration / 2)
						return BounceEaseIn(currentTime * 2, 0, finalValue, duration) * .5 + startValue;
					else
						return BounceEaseOut(currentTime * 2 - duration, 0, finalValue, duration) * .5 + finalValue * .5 + startValue;

				default:
					return finalValue * currentTime / duration + startValue;
			}
		}

		public static double BounceEaseIn(double currentTime, double startValue, double finalValue, double duration)
		{
			return finalValue - BounceEaseOut(duration - currentTime, 0, finalValue, duration) + startValue;
		}

		public static double BounceEaseOut(double currentTime, double startValue, double finalValue, double duration)
		{
			if ((currentTime /= duration) < (1 / 2.75))
				return finalValue * (7.5625 * currentTime * currentTime) + startValue;
			else if (currentTime < (2 / 2.75))
				return finalValue * (7.5625 * (currentTime -= (1.5 / 2.75)) * currentTime + .75) + startValue;
			else if (currentTime < (2.5 / 2.75))
				return finalValue * (7.5625 * (currentTime -= (2.25 / 2.75)) * currentTime + .9375) + startValue;
			else
				return finalValue * (7.5625 * (currentTime -= (2.625 / 2.75)) * currentTime + .984375) + startValue;
		}
	}
}

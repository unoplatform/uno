
using System;

namespace Microsoft.UI.Xaml.Media.Animation
{
	public partial class CircleEase : EasingFunctionBase
	{
		public override double Ease(double currentTime, double startValue, double finalValue, double duration)
		{
			var c = finalValue - startValue;

			var t = currentTime;
			var b = startValue;
			var d = duration;

			//Depending on the mode we have different functions for the return value.
			switch (this.EasingMode)
			{
				case EasingMode.EaseIn:
					// http://gizma.com/easing/#circ1
					t /= d;
					return -c * (Math.Sqrt(1 - t * t) - 1) + b;
				case EasingMode.EaseOut:
					// http://gizma.com/easing/#circ2
					t /= d;
					t--;
					return c * Math.Sqrt(1 - t * t) + b;
				case EasingMode.EaseInOut:
					// http://gizma.com/easing/#circ3
					t /= d / 2;
					if (t < 1) return -c / 2 * (Math.Sqrt(1 - t * t) - 1) + b;
					t -= 2;
					return c / 2 * (Math.Sqrt(1 - t * t) + 1) + b;

				default:
					return finalValue * currentTime / duration + startValue;
			}
		}
	}
}

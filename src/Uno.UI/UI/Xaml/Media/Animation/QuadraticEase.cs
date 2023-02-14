using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.UI.Xaml.Media.Animation
{
	public partial class QuadraticEase : EasingFunctionBase
	{
		public override double Ease(double currentTime, double startValue, double finalValue, double duration)
		{
			var changeInValue = finalValue - startValue;

			//Depending on the mode we have different functions for the return value.
			switch (this.EasingMode)
			{
				case EasingMode.EaseIn:
					currentTime /= duration;
					return changeInValue * currentTime * currentTime + startValue;

				case EasingMode.EaseOut:
					currentTime /= duration;
					return -changeInValue * currentTime * (currentTime - 2) + startValue;

				case EasingMode.EaseInOut:

					currentTime /= duration / 2;
					if (currentTime < 1)
					{
						return changeInValue / 2 * currentTime * currentTime + startValue;
					}
					currentTime--;
					return -changeInValue / 2 * (currentTime * (currentTime - 2) - 1) + startValue;

				default:
					return finalValue * currentTime / duration + startValue;
			}
		}
	}
}

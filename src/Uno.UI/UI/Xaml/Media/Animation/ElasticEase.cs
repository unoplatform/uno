using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.UI.Xaml.Media.Animation
{
	public partial class ElasticEase : EasingFunctionBase
	{
		public override double Ease(double currentTime, double startValue, double finalValue, double duration)
		{
			double p = duration * .3;
			double s = p / 4;

			//Depending on the mode we have different functions for the return value.
			switch (this.EasingMode)
			{
				case EasingMode.EaseIn:
					if ((currentTime /= duration) == 1)
					{
						return startValue + finalValue;
					}

					return -(finalValue * Math.Pow(2, 10 * (currentTime -= 1)) * Math.Sin((currentTime * duration - s) * (2 * Math.PI) / p)) + startValue;

				case EasingMode.EaseOut:
					if ((currentTime /= duration) == 1)
					{
						return startValue + finalValue;
					}

					return (finalValue * Math.Pow(2, -10 * currentTime) * Math.Sin((currentTime * duration - s) * (2 * Math.PI) / p) + finalValue + startValue);

				case EasingMode.EaseInOut:
					if ((currentTime /= duration / 2) == 2)
					{
						return startValue + finalValue;
					}

					p = duration * (.3 * 1.5);

					if (currentTime < 1)
					{
						return -.5 * (finalValue * Math.Pow(2, 10 * (currentTime -= 1)) * Math.Sin((currentTime * duration - s) * (2 * Math.PI) / p)) + startValue;
					}

					return finalValue * Math.Pow(2, -10 * (currentTime -= 1)) * Math.Sin((currentTime * duration - s) * (2 * Math.PI) / p) * .5 + finalValue + startValue;

				default:
					//Linear
					return finalValue * currentTime / duration + startValue;
			}
		}
	}
}

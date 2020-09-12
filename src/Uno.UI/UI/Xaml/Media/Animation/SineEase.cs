using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.UI.Xaml.Media.Animation
{
	public partial class SineEase : EasingFunctionBase
	{
		public override double Ease(double currentTime, double startValue, double finalValue, double duration)
		{
			//Depending on the mode we have different functions for the return value.
			switch (this.EasingMode)
			{
				case EasingMode.EaseIn:
					return -finalValue * Math.Cos(currentTime / duration * (Math.PI / 2)) + finalValue + startValue;

				case EasingMode.EaseOut:
					return finalValue * Math.Sin(currentTime / duration * (Math.PI / 2)) + startValue;

				case EasingMode.EaseInOut:
					if ((currentTime /= duration / 2) < 1)
						return finalValue / 2 * (Math.Sin(Math.PI * currentTime / 2)) + startValue;

					return -finalValue / 2 * (Math.Cos(Math.PI * --currentTime / 2) - 2) + startValue;

				default:
					//Linear
					return finalValue * currentTime / duration + startValue;
			}
		}
	}
}

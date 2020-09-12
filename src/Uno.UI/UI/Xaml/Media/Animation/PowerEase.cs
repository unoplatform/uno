using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.UI.Xaml.Media.Animation
{
	public partial class PowerEase : EasingFunctionBase
	{
		public int Power
		{
			get { return (int)this.GetValue(PowerProperty); }
			set { this.SetValue(PowerProperty, value); }
		}

		public static DependencyProperty PowerProperty { get ; } =
			DependencyProperty.Register("Power", typeof(int), typeof(PowerEase), new FrameworkPropertyMetadata(2));

		public override double Ease(double currentTime, double startValue, double finalValue, double duration)
		{
			var changeInValue = finalValue - startValue;
			double timePower = 1;

			//Use linear if Power of 1
			if (Power == 1)
			{
				return changeInValue * currentTime / duration + startValue;
			}

			//Use Quadratic if Power of 2
			if (Power == 2)
			{
				var quad = new QuadraticEase();
				return quad.Ease(currentTime, startValue, finalValue, duration);
			}

			//Depending on the mode we have different functions for the return value.
			switch (this.EasingMode)
			{
				case EasingMode.EaseIn:
					currentTime /= duration;

					for (int i = 0; i < Power; i++)
					{
						timePower *= currentTime;
                    }
					return changeInValue * timePower + startValue;

				case EasingMode.EaseOut:
					currentTime /= duration;

					for (int i = 0; i < Power; i++)
					{
						timePower *= currentTime;
                    }

					if (Power % 2 == 0)
					{
						return -changeInValue * (timePower - 1) + startValue;
					}
					return changeInValue * (timePower + 1) + startValue;

				case EasingMode.EaseInOut:

					currentTime /= duration / 2;

					for (int i = 0; i <= Power; i++)
					{
						timePower *= currentTime;
                    }

					if (currentTime < 1)
					{
						return changeInValue / 2 * timePower + startValue;
					}
					currentTime -= 2;

					if (Power % 2 == 0)
					{
						return changeInValue / 2 * (timePower + 2) + startValue;
					}

					return -changeInValue / 2 * (timePower - 2) + startValue;

				default:
					return finalValue * currentTime / duration + startValue;
			}
		}
	}
}
using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.UI.Xaml.Media.Animation
{
	public partial class BackEase : EasingFunctionBase
	{
		public double Amplitude
		{
			get { return (double)this.GetValue(AmplitudeProperty); }
			set { this.SetValue(AmplitudeProperty, value); }
		}
		
		public static DependencyProperty AmplitudeProperty { get ; } =
			DependencyProperty.Register("Amplitude", typeof(double), typeof(BackEase), new FrameworkPropertyMetadata(1d));

		public override double Ease(double currentTime, double startValue, double finalValue, double duration)
		{
			//BackTensionValue
			double s = 1.70158 * Amplitude;

			//Depending on the mode we have different functions for the return value.
			switch (this.EasingMode)
			{
				case EasingMode.EaseIn:
					return finalValue * (currentTime /= duration) * currentTime * ((s + 1) * currentTime - s) + startValue;

				case EasingMode.EaseOut:
					return finalValue * ((currentTime = currentTime / duration - 1) * currentTime * ((s + 1) * currentTime + s) + 1) + startValue;

				case EasingMode.EaseInOut:
					if ((currentTime /= duration / 2) < 1)
						return finalValue / 2 * (currentTime * currentTime * (((s *= (1.525)) + 1) * currentTime - s)) + startValue;
					return finalValue / 2 * ((currentTime -= 2) * currentTime * (((s *= (1.525)) + 1) * currentTime + s) + 2) + startValue;

				default:
					//Linear
					return finalValue * currentTime / duration + startValue;
			}
		}
	}
}

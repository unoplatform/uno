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

		public static DependencyProperty AmplitudeProperty { get; } =
			DependencyProperty.Register("Amplitude", typeof(double), typeof(BackEase), new FrameworkPropertyMetadata(1d));

		private protected override double EaseInCore(double normalizedTime)
		{
			//BackTensionValue
			double s = 1.70158 * Amplitude;
			return normalizedTime * normalizedTime * ((s + 1) * normalizedTime - s);
		}
	}
}

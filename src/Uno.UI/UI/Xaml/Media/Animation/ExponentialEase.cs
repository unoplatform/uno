using System;

namespace Windows.UI.Xaml.Media.Animation
{
	public partial class ExponentialEase : EasingFunctionBase
	{
		public double Exponent
		{
			get => (double)this.GetValue(ExponentProperty);
			set => this.SetValue(ExponentProperty, value);
		}

		public static DependencyProperty ExponentProperty { get; } =
			DependencyProperty.Register(
				nameof(Exponent),
				typeof(double),
				typeof(ExponentialEase),
				new FrameworkPropertyMetadata(defaultValue: 2.0)
			);

		private protected override double EaseInCore(double normalizedTime)
		{
			var exponent = Exponent;
			var normalizedValue = (Math.Exp(exponent * normalizedTime) - 1) / (Math.Exp(exponent) - 1);

			return normalizedValue;
		}
	}
}

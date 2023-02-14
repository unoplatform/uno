#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
using System;

namespace Microsoft.UI.Xaml.Media.Animation
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
			"Exponent", typeof(double),
			typeof(global::Microsoft.UI.Xaml.Media.Animation.ExponentialEase),
			new FrameworkPropertyMetadata(
				defaultValue: 1.0,
				propertyChangedCallback: (s, e) => (s as ExponentialEase)?.OnExponentChanged((double)e.OldValue, (double)e.NewValue)
			)
		);

		internal virtual void OnExponentChanged(double oldValue, double newValue)
		{
			OnExponentChangedPartial(oldValue, newValue);
		}

		partial void OnExponentChangedPartial(double oldValue, double newValue);

		public ExponentialEase()
		{
		}

		public override double Ease(double currentTime, double startValue, double finalValue, double duration)
		{
			var t = currentTime / duration;
			var exponent = Exponent;
			var normalizedValue = (Math.Exp(exponent * t) - 1) / (Math.Exp(exponent) - 1);

			return startValue + (finalValue - startValue) * normalizedValue;
		}
	}
}

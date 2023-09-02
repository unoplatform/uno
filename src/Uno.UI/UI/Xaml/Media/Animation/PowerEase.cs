using System;

namespace Windows.UI.Xaml.Media.Animation
{
	public partial class PowerEase : EasingFunctionBase
	{
		public double Power
		{
			get => (double)this.GetValue(PowerProperty);
			set => this.SetValue(PowerProperty, value);
		}

		public static DependencyProperty PowerProperty { get; } =
			DependencyProperty.Register(nameof(Power), typeof(double), typeof(PowerEase), new FrameworkPropertyMetadata(2.0));

		private protected override double EaseInCore(double normalizedTime)
		{
			var power = Math.Max(Power, 0.0);
			return Math.Pow(normalizedTime, power);
		}
	}
}

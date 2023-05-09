using System;

namespace Windows.UI.Xaml.Media.Animation
{
	public partial class PowerEase : EasingFunctionBase
	{
		public double Power
		{
			get { return (int)this.GetValue(PowerProperty); }
			set { this.SetValue(PowerProperty, value); }
		}

		public static DependencyProperty PowerProperty { get; } =
			DependencyProperty.Register(nameof(Power), typeof(int), typeof(PowerEase), new FrameworkPropertyMetadata(2));

		private protected override double EaseInCore(double normalizedTime)
		{
			return Math.Pow(normalizedTime, Power);
		}
	}
}

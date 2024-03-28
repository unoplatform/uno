namespace Windows.UI.Xaml.Media.Animation
{
	public partial class EasingFunctionBase : DependencyObject, IEasingFunction
	{
		private protected EasingFunctionBase()
		{
			InitializeBinder();
		}

		public EasingMode EasingMode
		{
			get => (EasingMode)this.GetValue(EasingModeProperty);
			set => this.SetValue(EasingModeProperty, value);
		}

		public static DependencyProperty EasingModeProperty { get; } =
			DependencyProperty.Register("EasingMode", typeof(EasingMode), typeof(EasingFunctionBase), new FrameworkPropertyMetadata(EasingMode.EaseOut));

		// https://github.com/dotnet/wpf/blob/ebe5937b557c3fa9cb29fb3a417c71652573c7e4/src/Microsoft.DotNet.Wpf/src/PresentationCore/System/Windows/Media/Animation/EasingFunctionBase.cs#L45-L61
		public double Ease(double normalizedTime)
		{
			switch (EasingMode)
			{
				case EasingMode.EaseIn:
					return EaseInCore(normalizedTime);
				case EasingMode.EaseOut:
					// EaseOut is the same as EaseIn, except time is reversed & the result is flipped.
					return 1.0 - EaseInCore(1.0 - normalizedTime);
				case EasingMode.EaseInOut:
				default:
					// EaseInOut is a combination of EaseIn & EaseOut fit to the 0-1, 0-1 range.
					return (normalizedTime < 0.5)
						? EaseInCore(normalizedTime * 2.0) * 0.5
						: (1.0 - EaseInCore((1.0 - normalizedTime) * 2.0)) * 0.5 + 0.5;
			}
		}

		private protected virtual double EaseInCore(double normalizedTime)
		{
			return normalizedTime;
		}

		double IEasingFunction.Ease(double currentTime, double startValue, double finalValue, double duration)
			=> startValue + Ease(currentTime / duration) * (finalValue - startValue);
	}
}

namespace Windows.UI.Xaml.Media.Animation
{
	partial class EasingColorKeyFrame : ColorKeyFrame
	{
		public static DependencyProperty EasingFunctionProperty = DependencyProperty.Register(
			"EasingFunction", typeof(EasingFunctionBase),
			typeof(EasingColorKeyFrame),
			new FrameworkPropertyMetadata(default(EasingFunctionBase)));

		public EasingFunctionBase EasingFunction
		{
			get => (EasingFunctionBase)GetValue(EasingFunctionProperty);
			set => SetValue(EasingFunctionProperty, value);
		}

		internal override IEasingFunction GetEasingFunction() => EasingFunction;
	}
}

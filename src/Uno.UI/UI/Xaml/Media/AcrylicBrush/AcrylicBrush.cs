namespace Windows.UI.Xaml.Media
{
	public partial class AcrylicBrush : XamlCompositionBrushBase
	{
		public AcrylicBrush() : base()
		{
		}

		public double TintOpacity
		{
			get => (double)GetValue(TintOpacityProperty);
			set => SetValue(TintOpacityProperty, value);
		}

		public Color TintColor
		{
			get => (Color)GetValue(TintColorProperty);
			set => SetValue(TintColorProperty, value);
		}

		public AcrylicBackgroundSource BackgroundSource
		{
			get => (AcrylicBackgroundSource)GetValue(BackgroundSourceProperty);
			set => SetValue(BackgroundSourceProperty, value);
		}

		public bool AlwaysUseFallback
		{
			get => (bool)GetValue(AlwaysUseFallbackProperty);
			set => SetValue(AlwaysUseFallbackProperty, value);
		}

		public  double? TintLuminosityOpacity
		{
			get => (double?)GetValue(TintLuminosityOpacityProperty);
			set => SetValue(TintLuminosityOpacityProperty, value);
		}

		public static DependencyProperty AlwaysUseFallbackProperty { get; } =
			DependencyProperty.Register(
				nameof(AlwaysUseFallback),
				typeof(bool),
				typeof(AcrylicBrush),
				new FrameworkPropertyMetadata(default(bool)));

		public static DependencyProperty BackgroundSourceProperty { get; } =
			DependencyProperty.Register(
				nameof(BackgroundSource),
				typeof(AcrylicBackgroundSource),
				typeof(AcrylicBrush),
				new FrameworkPropertyMetadata(AcrylicBackgroundSource.Backdrop));

		public static DependencyProperty TintColorProperty { get; } =
			DependencyProperty.Register(
				nameof(TintColor),
				typeof(Color),
				typeof(AcrylicBrush),
				new FrameworkPropertyMetadata(default(Color)));

		public static DependencyProperty TintOpacityProperty { get; } =
			DependencyProperty.Register(
				nameof(TintOpacity),
				typeof(double),
				typeof(AcrylicBrush),
				new FrameworkPropertyMetadata(0.5));

		public static DependencyProperty TintLuminosityOpacityProperty { get; } =
			DependencyProperty.Register(
				nameof(TintLuminosityOpacity),
				typeof(double?),
				typeof(AcrylicBrush),
				new FrameworkPropertyMetadata(default(double?)));

		internal Color FallbackColorWithOpacity => GetColorWithOpacity(FallbackColor);
	}
}

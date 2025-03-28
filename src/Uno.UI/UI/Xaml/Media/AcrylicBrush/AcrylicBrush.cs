using Color = Windows.UI.Color;

namespace Windows.UI.Xaml.Media
{
	/// <summary>
	/// Paints an area with a semi-transparent material that uses multiple
	/// effects including blur and a noise texture.
	/// </summary>
	/// <remarks>Currently uses blurring only in non-Skia Uno heads.</remarks>
	public partial class AcrylicBrush : XamlCompositionBrushBase
	{
		/// <summary>
		/// Initializes a new instance of the AcrylicBrush class.
		/// </summary>
		public AcrylicBrush() : base()
		{
		}

		/// <summary>
		/// Gets or sets the color tint for the semi-transparent acrylic material.
		/// </summary>
		public Color TintColor
		{
			get => (Color)GetValue(TintColorProperty);
			set => SetValue(TintColorProperty, value);
		}

		/// <summary>
		/// Gets or sets the degree of opacity of the color tint.
		/// </summary>
		public double TintOpacity
		{
			get => (double)GetValue(TintOpacityProperty);
			set => SetValue(TintOpacityProperty, value);
		}

		/// <summary>
		/// Gets or sets the brightness amount between the TintColor
		/// and the underlying pixels behind the Acrylic surface.
		/// </summary>
		/// <remarks>Currently does not have effect in Uno.</remarks>
		public double? TintLuminosityOpacity
		{
			get => (double?)GetValue(TintLuminosityOpacityProperty);
			set => SetValue(TintLuminosityOpacityProperty, value);
		}

		/// <summary>
		/// Gets or sets a value that specifies whether the brush samples
		/// from the app content or from the content behind the app window.
		/// </summary>
		/// <remarks>HostBackdrop currently supported on macOS only.</remarks>
		public AcrylicBackgroundSource BackgroundSource
		{
			get => (AcrylicBackgroundSource)GetValue(BackgroundSourceProperty);
			set => SetValue(BackgroundSourceProperty, value);
		}

		/// <summary>
		/// Gets or sets a value that specifies whether the brush
		/// is forced to the solid fallback color.
		/// </summary>
		public bool AlwaysUseFallback
		{
			get => (bool)GetValue(AlwaysUseFallbackProperty);
			set => SetValue(AlwaysUseFallbackProperty, value);
		}

		/// <summary>
		/// Identifies the TintColor dependency property.
		/// </summary>
		public static DependencyProperty TintColorProperty { get; } =
			DependencyProperty.Register(
				nameof(TintColor),
				typeof(Color),
				typeof(AcrylicBrush),
				new FrameworkPropertyMetadata(default(Color)));

		/// <summary>
		/// Identifies the TintOpacity dependency property.
		/// </summary>
		public static DependencyProperty TintOpacityProperty { get; } =
			DependencyProperty.Register(
				nameof(TintOpacity),
				typeof(double),
				typeof(AcrylicBrush),
				new FrameworkPropertyMetadata(0.5));

		/// <summary>
		/// Identifies the TintLuminosityOpacity dependency property.
		/// </summary>
		public static DependencyProperty TintLuminosityOpacityProperty { get; } =
			DependencyProperty.Register(
				nameof(TintLuminosityOpacity),
				typeof(double?),
				typeof(AcrylicBrush),
				new FrameworkPropertyMetadata(default(double?)));

		/// <summary>
		/// Identifies the BackgroundSource dependency property.
		/// </summary>
		public static DependencyProperty BackgroundSourceProperty { get; } =
			DependencyProperty.Register(
				nameof(BackgroundSource),
				typeof(AcrylicBackgroundSource),
				typeof(AcrylicBrush),
				new FrameworkPropertyMetadata(AcrylicBackgroundSource.Backdrop));

		/// <summary>
		/// Identifies the AlwaysUseFallback dependency property.
		/// </summary>
		public static DependencyProperty AlwaysUseFallbackProperty { get; } =
			DependencyProperty.Register(
				nameof(AlwaysUseFallback),
				typeof(bool),
				typeof(AcrylicBrush),
				new FrameworkPropertyMetadata(
					// Due to the fact that additional subviews are added to acrylic owner views
					// on platforms other than WASM and Skia, we default to using fallback where not completely safe
					// When this is explicitly set to false, Acrylic will be displayed
#if __WASM__ || __SKIA__
					false
#else
					true
#endif
					));

		/// <summary>
		/// Returns the tint color mixed with tint opacity value.
		/// </summary>
		internal Color TintColorWithTintOpacity => TintColor.WithOpacity(TintOpacity);
	}
}

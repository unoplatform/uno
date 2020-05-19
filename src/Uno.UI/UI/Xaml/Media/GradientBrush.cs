using Windows.UI.Xaml.Markup;
using Windows.UI;

namespace Windows.UI.Xaml.Media
{
	[ContentProperty(Name = "GradientStops")]
	public abstract partial class GradientBrush : Brush
	{
		protected GradientBrush()
		{
			GradientStops = new GradientStopCollection();
		}

		public static readonly DependencyProperty FallbackColorProperty = DependencyProperty.Register(
			"FallbackColor", typeof(Color), typeof(GradientBrush), new PropertyMetadata(default(Color)));
		public Color FallbackColor
		{
			get => (Color)GetValue(FallbackColorProperty);
			set => SetValue(FallbackColorProperty, value);
		}

		public static readonly DependencyProperty GradientStopsProperty = DependencyProperty.Register(
			"GradientStops",
			typeof(GradientStopCollection),
			typeof(GradientBrush),
			new PropertyMetadata(null)
		);

		public GradientStopCollection GradientStops
		{
			get => (GradientStopCollection)GetValue(GradientStopsProperty);
			set => SetValue(GradientStopsProperty, value);
		}

		public static readonly DependencyProperty MappingModeProperty =
			DependencyProperty.Register(
				"MappingMode",
				typeof(BrushMappingMode),
				typeof(GradientBrush),
				new FrameworkPropertyMetadata(BrushMappingMode.RelativeToBoundingBox));

		public BrushMappingMode MappingMode
		{
			get => (BrushMappingMode)GetValue(MappingModeProperty);
			set => SetValue(MappingModeProperty, value);
		}

		internal Color FallbackColorWithOpacity => GetColorWithOpacity(FallbackColor);
	}
}

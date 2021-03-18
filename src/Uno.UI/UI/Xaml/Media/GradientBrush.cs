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

		public static DependencyProperty FallbackColorProperty { get ; } = DependencyProperty.Register(
			"FallbackColor", typeof(Color), typeof(GradientBrush), new FrameworkPropertyMetadata(default(Color)));

		public Color FallbackColor
		{
			get => (Color)GetValue(FallbackColorProperty);
			set => SetValue(FallbackColorProperty, value);
		}

		public static DependencyProperty GradientStopsProperty { get ; } = DependencyProperty.Register(
			"GradientStops",
			typeof(GradientStopCollection),
			typeof(GradientBrush),
				new FrameworkPropertyMetadata(
					defaultValue: null,
					options: FrameworkPropertyMetadataOptions.ValueInheritsDataContext | FrameworkPropertyMetadataOptions.LogicalChild | FrameworkPropertyMetadataOptions.AffectsArrange
				)
		);

		public GradientStopCollection GradientStops
		{
			get => (GradientStopCollection)GetValue(GradientStopsProperty);
			set => SetValue(GradientStopsProperty, value);
		}

		public static DependencyProperty MappingModeProperty { get ; } =
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

using Windows.Foundation;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Uno;
using Windows.Foundation.Collections;

namespace Microsoft.UI.Xaml.Media
{
	[ContentProperty(Name = nameof(GradientStops))]
	public sealed partial class RadialGradientBrush : XamlCompositionBrushBase
	{
		public static DependencyProperty SpreadMethodProperty { get; } =
			DependencyProperty.Register(
			nameof(SpreadMethod),
			typeof(GradientSpreadMethod),
			typeof(RadialGradientBrush),
			new FrameworkPropertyMetadata(
				defaultValue: GradientSpreadMethod.Pad,
				options: FrameworkPropertyMetadataOptions.AffectsRender));

		public GradientSpreadMethod SpreadMethod
		{
			get => (GradientSpreadMethod)GetValue(SpreadMethodProperty);
			set => SetValue(SpreadMethodProperty, value);
		}

		public static DependencyProperty MappingModeProperty { get; } =
			DependencyProperty.Register(
				"MappingMode",
				typeof(BrushMappingMode),
				typeof(RadialGradientBrush),
				new FrameworkPropertyMetadata(
					defaultValue: BrushMappingMode.RelativeToBoundingBox,
					options: FrameworkPropertyMetadataOptions.AffectsRender));

		public BrushMappingMode MappingMode
		{
			get => (BrushMappingMode)GetValue(MappingModeProperty);
			set => SetValue(MappingModeProperty, value);
		}

		public IObservableVector<GradientStop> GradientStops { get; } = new ObservableVector<GradientStop>();

		public static DependencyProperty CenterProperty { get; } = DependencyProperty.Register(
			nameof(Center), typeof(Point), typeof(RadialGradientBrush), new FrameworkPropertyMetadata(new Point(0.5d, 0.5d)));

		public Point Center
		{
			get => (Point)GetValue(CenterProperty);
			set => SetValue(CenterProperty, value);
		}

		public static DependencyProperty RadiusXProperty { get; } = DependencyProperty.Register(
			nameof(RadiusX), typeof(double), typeof(RadialGradientBrush), new FrameworkPropertyMetadata(0.5d));

		public double RadiusX
		{
			get => (double)GetValue(RadiusXProperty);
			set => SetValue(RadiusXProperty, value);
		}

		public static DependencyProperty RadiusYProperty { get; } = DependencyProperty.Register(
			nameof(RadiusY), typeof(double), typeof(RadialGradientBrush), new FrameworkPropertyMetadata(0.5d));

		public double RadiusY
		{
			get => (double)GetValue(RadiusYProperty);
			set => SetValue(RadiusYProperty, value);
		}

		public static DependencyProperty GradientOriginProperty { get; } = DependencyProperty.Register(
			nameof(GradientOrigin), typeof(Point), typeof(RadialGradientBrush), new FrameworkPropertyMetadata(new Point(0.5d, 0.5d)));

		[NotImplemented]
		public Point GradientOrigin
		{
			get => (Point)GetValue(GradientOriginProperty);
			set => SetValue(GradientOriginProperty, value);
		}

		public static DependencyProperty InterpolationSpaceProperty { get; } = DependencyProperty.Register(
			nameof(InterpolationSpace), typeof(CompositionColorSpace), typeof(RadialGradientBrush), new FrameworkPropertyMetadata(default(CompositionColorSpace)));

		[NotImplemented]
		public CompositionColorSpace InterpolationSpace
		{
			get => (CompositionColorSpace)GetValue(InterpolationSpaceProperty);
			set => SetValue(InterpolationSpaceProperty, value);
		}
	}
}

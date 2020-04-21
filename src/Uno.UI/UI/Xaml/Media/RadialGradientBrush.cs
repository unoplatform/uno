using Windows.Foundation;
using Windows.UI.Composition;
using Windows.UI.Xaml.Markup;

namespace Windows.UI.Xaml.Media
{
	public sealed partial class RadialGradientBrush : GradientBrush
	{
		public static readonly DependencyProperty CenterProperty = DependencyProperty.Register(
			"Center", typeof(Point), typeof(RadialGradientBrush), new PropertyMetadata(new Point(0.5d, 0.5d)));

		public Point Center
		{
			get => (Point)GetValue(CenterProperty);
			set => SetValue(CenterProperty, value);
		}

		public static readonly DependencyProperty RadiusXProperty = DependencyProperty.Register(
			"RadiusX", typeof(double), typeof(RadialGradientBrush), new PropertyMetadata(1.0d));

		public double RadiusX
		{
			get => (double)GetValue(RadiusXProperty);
			set => SetValue(RadiusXProperty, value);
		}

		public static readonly DependencyProperty RadiusYProperty = DependencyProperty.Register(
			"RadiusY", typeof(double), typeof(RadialGradientBrush), new PropertyMetadata(1.0d));

		public double RadiusY
		{
			get => (double)GetValue(RadiusYProperty);
			set => SetValue(RadiusYProperty, value);
		}

		public static readonly DependencyProperty GradientOriginProperty = DependencyProperty.Register(
			"GradientOrigin", typeof(Point), typeof(RadialGradientBrush), new PropertyMetadata(default(Point)));

		public Point GradientOrigin
		{
			get => (Point)GetValue(GradientOriginProperty);
			set => SetValue(GradientOriginProperty, value);
		}

		public static readonly DependencyProperty InterpolationSpaceProperty = DependencyProperty.Register(
			"InterpolationSpace", typeof(CompositionColorSpace), typeof(RadialGradientBrush), new PropertyMetadata(default(CompositionColorSpace)));

		public CompositionColorSpace InterpolationSpace
		{
			get => (CompositionColorSpace)GetValue(InterpolationSpaceProperty);
			set => SetValue(InterpolationSpaceProperty, value);
		}
	}
}

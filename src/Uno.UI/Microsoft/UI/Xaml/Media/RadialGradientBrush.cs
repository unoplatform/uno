using Windows.Foundation;
using Windows.UI.Composition;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Markup;
using Windows.UI.Xaml.Media;
using Uno;

namespace Microsoft.UI.Xaml.Media
{
	public sealed partial class RadialGradientBrush : GradientBrush
	{
		public static DependencyProperty CenterProperty { get ; } = DependencyProperty.Register(
			"Center", typeof(Point), typeof(RadialGradientBrush), new FrameworkPropertyMetadata(new Point(0.5d, 0.5d)));

		public Point Center
		{
			get => (Point)GetValue(CenterProperty);
			set => SetValue(CenterProperty, value);
		}

		public static DependencyProperty RadiusXProperty { get ; } = DependencyProperty.Register(
			"RadiusX", typeof(double), typeof(RadialGradientBrush), new FrameworkPropertyMetadata(0.5d));

		public double RadiusX
		{
			get => (double)GetValue(RadiusXProperty);
			set => SetValue(RadiusXProperty, value);
		}

		public static DependencyProperty RadiusYProperty { get ; } = DependencyProperty.Register(
			"RadiusY", typeof(double), typeof(RadialGradientBrush), new FrameworkPropertyMetadata(0.5d));

		public double RadiusY
		{
			get => (double)GetValue(RadiusYProperty);
			set => SetValue(RadiusYProperty, value);
		}

		public static DependencyProperty GradientOriginProperty { get ; } = DependencyProperty.Register(
			"GradientOrigin", typeof(Point), typeof(RadialGradientBrush), new FrameworkPropertyMetadata(new Point(0.5d, 0.5d)));

		[NotImplemented]
		public Point GradientOrigin
		{
			get => (Point)GetValue(GradientOriginProperty);
			set => SetValue(GradientOriginProperty, value);
		}

		public static DependencyProperty InterpolationSpaceProperty { get ; } = DependencyProperty.Register(
			"InterpolationSpace", typeof(CompositionColorSpace), typeof(RadialGradientBrush), new FrameworkPropertyMetadata(default(CompositionColorSpace)));

		[NotImplemented]
		public CompositionColorSpace InterpolationSpace
		{
			get => (CompositionColorSpace)GetValue(InterpolationSpaceProperty);
			set => SetValue(InterpolationSpaceProperty, value);
		}
	}
}

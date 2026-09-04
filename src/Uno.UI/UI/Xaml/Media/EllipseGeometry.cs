using System;
using Windows.Foundation;
using System.Numerics;
using SkiaSharp;
using Microsoft.UI.Composition;

namespace Microsoft.UI.Xaml.Media
{
	public partial class EllipseGeometry : Geometry
	{
		public static DependencyProperty CenterProperty { get; } =
			DependencyProperty.Register(
				nameof(Center), typeof(Point),
				typeof(EllipseGeometry),
				new FrameworkPropertyMetadata(
					default(Point),
					options: FrameworkPropertyMetadataOptions.AffectsMeasure));

		public static DependencyProperty RadiusXProperty { get; } =
			DependencyProperty.Register(
				nameof(RadiusX), typeof(double),
				typeof(EllipseGeometry),
				new FrameworkPropertyMetadata(
					default(double),
					options: FrameworkPropertyMetadataOptions.AffectsMeasure));
		public static DependencyProperty RadiusYProperty { get; } =
			DependencyProperty.Register(
				nameof(RadiusY), typeof(double),
				typeof(EllipseGeometry),
				new FrameworkPropertyMetadata(
					default(double),
					options: FrameworkPropertyMetadataOptions.AffectsMeasure));

		public Point Center
		{
			get => (Point)GetValue(CenterProperty);
			set => SetValue(CenterProperty, value);
		}
		public double RadiusX
		{
			get => (double)GetValue(RadiusXProperty);
			set => SetValue(RadiusXProperty, value);
		}
		public double RadiusY
		{
			get => (double)GetValue(RadiusYProperty);
			set => SetValue(RadiusYProperty, value);
		}

		public EllipseGeometry()
		{
			InitPartials();
		}

		partial void InitPartials();

		internal override SKPath GetSKPath() => CompositionGeometry.BuildEllipseGeometry(Center.ToVector2(), new Vector2((float)RadiusX, (float)RadiusY));
	}
}

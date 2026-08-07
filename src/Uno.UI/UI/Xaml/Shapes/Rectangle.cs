using Windows.Foundation;
using System;
using Microsoft.UI.Composition;
using System.Numerics;

namespace Microsoft.UI.Xaml.Shapes
{
	public partial class Rectangle : Shape
	{
#if __SKIA__
		/// <inheritdoc />
		protected override Size MeasureOverride(Size availableSize)
			=> MeasureRelativeShape(availableSize);
#endif

		#region RadiusY (DP)
		public static DependencyProperty RadiusYProperty { get; } = DependencyProperty.Register(
			"RadiusY",
			typeof(double),
			typeof(Rectangle),
			new FrameworkPropertyMetadata(
				defaultValue: 0.0,
				options: FrameworkPropertyMetadataOptions.AffectsMeasure
			)
		);

		public double RadiusY
		{
			get => (double)this.GetValue(RadiusYProperty);
			set => this.SetValue(RadiusYProperty, value);
		}
		#endregion

		#region RadiusX (DP)
		public static DependencyProperty RadiusXProperty { get; } = DependencyProperty.Register(
			"RadiusX",
			typeof(double),
			typeof(Rectangle),
			new FrameworkPropertyMetadata(
				defaultValue: 0.0,
				options: FrameworkPropertyMetadataOptions.AffectsMeasure
			)
		);

		public double RadiusX
		{
			get => (double)this.GetValue(RadiusXProperty);
			set => this.SetValue(RadiusXProperty, value);
		}

#nullable enable
		public Rectangle()
		{
		}

		/// <inheritdoc />
		protected override Size ArrangeOverride(Size finalSize)
		{
			var (_, renderingArea) = ArrangeRelativeShape(finalSize);
			var path = renderingArea.Width > 0 && renderingArea.Height > 0
				? GetGeometry(renderingArea)
				: null;

			Render(path);

			return finalSize;
		}

		private SkiaGeometrySource2D GetGeometry(Rect finalRect)
		{
			var radiusX = RadiusX;
			var radiusY = RadiusY;

			var offset = new Vector2((float)finalRect.Left, (float)finalRect.Top);
			var size = new Vector2((float)finalRect.Width, (float)finalRect.Height);

			var geometry = radiusX is 0 || radiusY is 0
				? CompositionGeometry.BuildRectangleGeometry(offset, size)
				: CompositionGeometry.BuildRoundedRectangleGeometry(offset, size, new Vector2((float)radiusX, (float)radiusY));

			return new SkiaGeometrySource2D(geometry);
		}
#nullable disable
		#endregion

#if __NETSTD_REFERENCE__
		protected override Size MeasureOverride(Size availableSize) => base.MeasureOverride(availableSize);
		protected override Size ArrangeOverride(Size finalSize) => base.ArrangeOverride(finalSize);
#endif
	}
}

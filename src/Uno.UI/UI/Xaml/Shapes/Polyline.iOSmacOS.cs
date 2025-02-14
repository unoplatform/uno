using System;
using CoreGraphics;
using Uno.Media;
using Windows.Foundation;

namespace Windows.UI.Xaml.Shapes
{
	public partial class Polyline : Shape
	{
		/// <inheritdoc />
		protected override Size MeasureOverride(Size availableSize)
			=> MeasureAbsoluteShape(availableSize, GetPath());

		/// <inheritdoc />
		protected override Size ArrangeOverride(Size finalSize)
			=> ArrangeAbsoluteShape(finalSize, GetPath());

		private CGPath GetPath()
		{
			var points = Points;
			if (points == null || points.Count <= 1)
			{
				return null;
			}

			var streamGeometry = GeometryHelper.Build(c =>
			{
				c.BeginFigure(points[0], true);
				for (var i = 1; i < points.Count; i++)
				{
					c.LineTo(points[i], true, false);
				}
			});

			return streamGeometry.ToCGPath();
		}
	}
}

using CoreGraphics;
using Uno.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Windows.Foundation;

namespace Windows.UI.Xaml.Shapes
{
	public partial class Polygon : Shape
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
				c.LineTo(points[0], true, false);
			});

			return streamGeometry.ToCGPath();
		}
	}
}

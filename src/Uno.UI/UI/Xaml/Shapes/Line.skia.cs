using System;
using Uno.Media;
using Windows.Foundation;
using Windows.UI.Composition;

namespace Windows.UI.Xaml.Shapes
{
	public partial class Line : Shape
	{
		/// <inheritdoc />
		protected override Size MeasureOverride(Size availableSize)
			=> MeasureAbsoluteShape(availableSize, GetPath());

		/// <inheritdoc />
		protected override Size ArrangeOverride(Size finalSize)
			=> ArrangeAbsoluteShape(finalSize, GetPath());

		private SkiaGeometrySource2D GetPath()
		{
			if (Math.Abs(X1 - X2) > double.Epsilon || Math.Abs(Y1 - Y2) > double.Epsilon)
			{
				var streamGeometry = GeometryHelper.Build(c =>
				{
					c.BeginFigure(new Point(X1, Y1), false);
					c.LineTo(new Point(X2, Y2), false, false);
				});

				return streamGeometry.GetGeometrySource2D();
			}

			return null;
		}
	}
}

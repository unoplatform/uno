using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.UI.Xaml.Media;
using Uno.Extensions;
using System.Linq;
using Uno.Disposables;
using Uno.UI.Extensions;
using Uno.UI;
using Microsoft.UI.Composition;
using Windows.Foundation;
using Windows.Graphics;
using System.Numerics;

namespace Microsoft.UI.Xaml.Shapes
{
	public partial class Rectangle : Shape
	{
		static Rectangle()
		{
			StretchProperty.OverrideMetadata(typeof(Rectangle), new FrameworkPropertyMetadata(defaultValue: Media.Stretch.Fill));
		}

		public Rectangle()
		{
		}

		/// <inheritdoc />
		protected override Size MeasureOverride(Size availableSize)
			=> base.MeasureRelativeShape(availableSize);

		/// <inheritdoc />
		protected override Size ArrangeOverride(Size finalSize)
		{
			var (shapeSize, renderingArea) = ArrangeRelativeShape(finalSize);

			SkiaGeometrySource2D path;

			if (renderingArea.Width > 0 && renderingArea.Height > 0)
			{
				path = GetGeometry(renderingArea);
			}
			else
			{
				path = null;
			}

			Render(path);

			return shapeSize;
		}

		private SkiaGeometrySource2D GetGeometry(Rect finalRect)
		{
			var strokeThickness = StrokeThickness;
			var radiusX = RadiusX;
			var radiusY = RadiusY;

			var offset = new Vector2((float)(finalRect.Left), (float)(finalRect.Top));
			var size = new Vector2((float)finalRect.Width, (float)finalRect.Height);

			SkiaGeometrySource2D geometry;
			if (radiusX == 0 || radiusY == 0)
			{
				// Simple rectangle
				geometry = new SkiaGeometrySource2D(
					CompositionGeometry.BuildRectangleGeometry(
						offset,
						size));
			}
			else
			{
				// Complex rectangle
				geometry = new SkiaGeometrySource2D(
					CompositionGeometry.BuildRoundedRectangleGeometry(
						offset,
						size,
						new Vector2((float)radiusX, (float)radiusY)));
			}

			return geometry;
		}
	}
}

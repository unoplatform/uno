using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml.Media;
using Uno.Extensions;
using System.Linq;
using Uno.Disposables;
using Uno.UI.Extensions;
using Uno.UI;
using Windows.UI.Composition;
using Windows.Foundation;
using Windows.Graphics;

namespace Windows.UI.Xaml.Shapes
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
				path = GetGeometry(renderingArea.Size);
			}
			else
			{
				path = null;
			}

			Render(path);

			return shapeSize;
		}

		private SkiaGeometrySource2D GetGeometry(Size finalSize)
		{
			var area = new Rect(0, 0, finalSize.Width, finalSize.Height);

			var geometry = new SkiaGeometrySource2D();

			if (Math.Max(RadiusX, RadiusY) > 0)
			{
				geometry.Geometry.AddRoundRect(area.ToSKRect(), (float)RadiusX, (float)RadiusY);
			}
			else
			{
				geometry.Geometry.AddRect(area.ToSKRect());
			}

			return geometry;
		}
	}
}

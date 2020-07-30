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
	public partial class Rectangle
	{

		public Rectangle()
		{
		}

		internal override SkiaGeometrySource2D GetGeometry(Size finalSize)
		{
			var area = new Rect(0, 0, finalSize.Width, finalSize.Height);

			switch (Stretch)
			{
				default:
				case Stretch.None:
					break;
				case Stretch.Fill:
					area = new Rect(0, 0, finalSize.Width, finalSize.Height);
					break;
				case Stretch.Uniform:
					area = (area.Height > area.Width)
						? (new Rect((float)area.X, (float)area.Y, (float)area.Width, (float)area.Width))
						: (new Rect((float)area.X, (float)area.Y, (float)area.Height, (float)area.Height));
					break;
				case Stretch.UniformToFill:
					area = (area.Height > area.Width)
						? (new Rect((float)area.X, (float)area.Y, (float)area.Height, (float)area.Height))
						: (new Rect((float)area.X, (float)area.Y, (float)area.Width, (float)area.Width));
					break;
			}

			var shrinkValue = -ActualStrokeThickness / 2;
			if (area != Rect.Empty)
			{
				area.Inflate(shrinkValue, shrinkValue);
			}

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


		partial void OnRadiusXChangedPartial()
		{
			InvalidateMeasure();
		}

		partial void OnRadiusYChangedPartial()
		{
			InvalidateMeasure();
		}

	}
}

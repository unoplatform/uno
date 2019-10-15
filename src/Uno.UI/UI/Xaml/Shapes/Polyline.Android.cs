using Uno.Media;
using System;
using System.Collections.Generic;
using System.Text;
using Uno.UI;
using Uno.Extensions;
using Windows.Foundation;

namespace Windows.UI.Xaml.Shapes
{
	public partial class Polyline
	{
		protected override Android.Graphics.Path GetPath()
		{
			var coords = Points;

			if (coords != null)
			{
				var streamGeometry = GeometryHelper.Build(c =>
				{
					c.BeginFigure(new Point((double)coords[0].X, (double)coords[0].Y), true, false);
					for (int i = 1; i < coords.Count; i++)
					{
						c.LineTo(new Point((double)coords[i].X, (double)coords[i].Y), true, false);
					}
				});

				return streamGeometry.ToPath();
			}

			return null;
		}
	}
}

using System;
using System.Collections.Generic;
using System.Text;
using CoreAnimation;
using CoreGraphics;
using Uno.Disposables;
using Uno.Media;
using Windows.Foundation;

namespace Windows.UI.Xaml.Shapes
{
	public partial class Line
	{
		public Line()
		{
#if __IOS__
			ClipsToBounds = false;
#endif
		}

		protected override CGPath GetPath()
		{
			if (Math.Abs(X1 - X2) > double.Epsilon || Math.Abs(Y1 - Y2) > double.Epsilon)
			{
				var streamGeometry = GeometryHelper.Build(c =>
				{
					c.BeginFigure(new Point(X1, Y1), false, false);
					c.LineTo(new Point(X2, Y2), false, false);
				});

				return streamGeometry.ToCGPath();
			}

			return null;
		}
	}
}

using System;
using System.Linq;
using Uno.UI.Extensions;
using Windows.Foundation;
using Microsoft.UI.Xaml.Media;
using CoreGraphics;
using UIKit;
using _BezierPath = UIKit.UIBezierPath;

using ObjCRuntime;


namespace Microsoft.UI.Xaml.Shapes
{
	public partial class Rectangle
	{
		public Rectangle()
		{
			ClipsToBounds = true;
		}

		/// <inheritdoc />
		protected override Size ArrangeOverride(Size finalSize)
		{
			var (shapeSize, renderingArea) = ArrangeRelativeShape(finalSize);

			CGPath path;
			if (renderingArea.Width > 0 && renderingArea.Height > 0)
			{
				path = Math.Max(RadiusX, RadiusY) > 0
					? _BezierPath.FromRoundedRect(renderingArea, UIRectCorner.AllCorners, new CGSize(RadiusX, RadiusY)).CGPath
					: _BezierPath.FromRect(renderingArea).ToCGPath();
			}
			else
			{
				path = null;
			}
			Render(path);

			return shapeSize;
		}
	}
}

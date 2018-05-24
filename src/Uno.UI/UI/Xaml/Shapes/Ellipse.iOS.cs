using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

#if XAMARIN_IOS_UNIFIED
using Foundation;
using UIKit;
using CoreAnimation;
using CoreGraphics;
#elif XAMARIN_IOS
using MonoTouch.Foundation;
using MonoTouch.UIKit;
using MonoTouch.CoreGraphics;
using MonoTouch.CoreAnimation;
using CGRect = System.Drawing.RectangleF;
using nfloat = System.Single;
using CGPoint = System.Drawing.PointF;
using nint = System.Int32;
using CGSize = System.Drawing.SizeF;
#endif

namespace Windows.UI.Xaml.Shapes
{
	public partial class Ellipse : ArbitraryShapeBase
    {
		public Ellipse()
		{
		}

		protected override CGPath GetPath()
		{
			var bounds = Bounds;

			var width = double.IsNaN(Width) ? bounds.Width : Width;
			var height = double.IsNaN(Height) ? bounds.Height : Height;

			var area = new CGRect(
				x: 0, 
				y: 0,

				//In ios we need to inflate the bounds because the stroke thickness is not taken into account when
				//forming the ellipse from rect.
				width: width - (nfloat)this.ActualStrokeThickness, 
				height: height - (nfloat)this.ActualStrokeThickness
			);

			return CGPath.EllipseFromRect(area);
		}
	}
}

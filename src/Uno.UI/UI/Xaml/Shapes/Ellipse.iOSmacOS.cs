using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

using Foundation;
using CoreAnimation;
using CoreGraphics;
using Size = Windows.Foundation.Size;

namespace Windows.UI.Xaml.Shapes
{
	public partial class Ellipse : ArbitraryShapeBase
    {
	    public Ellipse()
		{
		}

		protected override CGPath GetPath()
		{
			var calculatedBounds = SizeFromUISize(Bounds.Size); ;

			var width = double.IsNaN(Width) ? calculatedBounds.Width : Width;
			var height = double.IsNaN(Height) ? calculatedBounds.Height : Height;

			var strokeThickness = (nfloat)this.ActualStrokeThickness;

			var area = new CGRect(
				x: 0, 
				y: 0,

				//In ios we need to inflate the bounds because the stroke thickness is not taken into account when
				//forming the ellipse from rect.
				width: width - strokeThickness, 
				height: height - strokeThickness
            );

			return CGPath.EllipseFromRect(area);
		}
	}
}

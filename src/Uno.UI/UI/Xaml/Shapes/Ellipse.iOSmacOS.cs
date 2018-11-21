using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

using Foundation;
using CoreAnimation;
using CoreGraphics;

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

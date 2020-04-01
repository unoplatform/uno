using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using Windows.UI.Xaml.Media;
using Foundation;
using CoreAnimation;
using CoreGraphics;
using Uno.UI;
using Size = Windows.Foundation.Size;

namespace Windows.UI.Xaml.Shapes
{
	public partial class Ellipse : Shape
	{
		public Ellipse()
		{
			ClipsToBounds = true;

			//Set default stretch value
			Stretch = Stretch.Fill;
		}

		/// <inheritdoc />
		protected override Size MeasureOverride(Size availableSize)
			=> base.MeasureRelativeShape(availableSize);

		/// <inheritdoc />
		protected override Size ArrangeOverride(Size finalSize)
		{
			var (size, pathArea) = ArrangeRelativeShape(finalSize);

			if (pathArea.Width > 0 && pathArea.Height > 0)
			{
				Render(CGPath.EllipseFromRect(pathArea));
			}

			return size;
		}

		//protected override CGPath GetPath(Size availableSize)
		//{
		//	//var calculatedBounds = this.ApplySizeConstraints(availableSize);

		//	//var strokeThickness = (nfloat)this.ActualStrokeThickness;

		//	//var area = new CGRect(
		//	//	x: 0,
		//	//	y: 0,

		//	//	//In ios we need to inflate the bounds because the stroke thickness is not taken into account when
		//	//	//forming the ellipse from rect.
		//	//	width: calculatedBounds.Width - strokeThickness,
		//	//	height: calculatedBounds.Height - strokeThickness
		//	//);

		//	var strokeThickness = ActualStrokeThickness;
		//	var halfStrokeThickness = (nfloat)GetHalfStrokeThickness();

		//	var area = new CGRect(
		//		x: 0,
		//		y: 0,

		//		// The CGRect does not have concept of stroke thickness, so we have to manually exclude it from the path itself.
		//		// Note: we actually remove half of the thickness for the top/left, and the other half on the bottom/right.
		//		width: availableSize.Width - strokeThickness,
		//		height: availableSize.Height - strokeThickness
		//	);
		//	var origin = CGAffineTransform.MakeTranslation(halfStrokeThickness, halfStrokeThickness);

		//	return CGPath.EllipseFromRect(area, origin);
		//}
	}
}

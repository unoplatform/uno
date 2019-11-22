using Android.Graphics;
using Uno.UI;
using System;
using System.Drawing;
using Size = Windows.Foundation.Size;
using Rect = Windows.Foundation.Rect;

namespace Windows.UI.Xaml.Shapes
{
	public partial class Ellipse : ArbitraryShapeBase
	{
		public Ellipse()
		{
			//Set default stretch value
			this.Stretch = Windows.UI.Xaml.Media.Stretch.Fill;
		}

		protected override Android.Graphics.Path GetPath()
		{
			var minMax = this.GetMinMax();

			var viewGroup = this as UnoViewGroup;
			var finalSize =
				new Size(viewGroup.Width, viewGroup.Height)
					.AtMost(minMax.max)
					.AtLeast(minMax.min);

			var bounds = new RectF(0, 0, (float)finalSize.Width, (float)finalSize.Height);

			var output = new Android.Graphics.Path();
			output.AddOval(bounds, Android.Graphics.Path.Direction.Cw);
			return output;
		}
	}
}

using Android.Graphics;
using Uno.UI;
using System;
using System.Drawing;
using Size = Windows.Foundation.Size;

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
			var minMaxInLogicalPixels = this.GetMinMax();

			var viewGroup = this as UnoViewGroup;
			var finalSizeInPhysicalPixels =
				new Size(viewGroup.Width, viewGroup.Height)
					.AtMost(minMaxInLogicalPixels.max.LogicalToPhysicalPixels())
					.AtLeast(minMaxInLogicalPixels.min.LogicalToPhysicalPixels());

			var bounds = new RectF(0, 0, (float)finalSizeInPhysicalPixels.Width, (float)finalSizeInPhysicalPixels.Height);

			var output = new Android.Graphics.Path();
			output.AddOval(bounds, Android.Graphics.Path.Direction.Cw);
			return output;
		}
	}
}

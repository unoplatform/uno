using Android.Graphics;
using Uno.UI;
using System;
using System.Drawing;
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
			var viewGroup = this as UnoViewGroup;
			var bounds = new RectF(0, 0, viewGroup.Width, viewGroup.Height);

			var output = new Android.Graphics.Path();
			output.AddOval(bounds, Android.Graphics.Path.Direction.Cw);
			return output;
		}
	}
}

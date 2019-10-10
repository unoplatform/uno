using Android.Graphics;
using Uno.UI;
using System;
using System.Drawing;

namespace Windows.UI.Xaml.Shapes
{
    public partial class Ellipse : ArbitraryShapeBase
	{
        public Ellipse()
        {
			//Set default stretch value
			this.Stretch = Windows.UI.Xaml.Media.Stretch.Fill;
        }

		private Android.Graphics.Path MakeOval(Windows.Foundation.Rect bounds)
        {
            var output = new Android.Graphics.Path();
            output.AddOval(bounds.ToRectF(), Android.Graphics.Path.Direction.Cw);
            return output;
        }

		protected override Android.Graphics.Path GetPath()
		{
			//If no height is explicitly set we need to force the shape to be drawn.
			var ovalHeight = double.IsNaN(Height) ? 100 : Height;
			var ovalWidth = double.IsNaN(Width) ? 100 : Width;
			return MakeOval(new Windows.Foundation.Rect(0, 0, ovalWidth, ovalHeight));
		}
	}
}

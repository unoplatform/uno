using Android.Graphics;
using Uno.UI;
using System;

namespace Windows.UI.Xaml.Shapes
{
	public partial class Rectangle : Shape
	{
		public Rectangle()
		{
			//Set default stretch value
			this.Stretch = Media.Stretch.Fill;
			SetWillNotDraw(false);
		}

		public override void Draw(Android.Graphics.Canvas canvas)
		{
			base.Draw(canvas);

			var drawArea = GetDrawArea(canvas);
			var rx = ViewHelper.LogicalToPhysicalPixels(RadiusX);
			var ry = ViewHelper.LogicalToPhysicalPixels(RadiusY);
			
			var fillRect = new Android.Graphics.Path();
			fillRect.AddRoundRect(drawArea.ToRectF(), rx, ry, Android.Graphics.Path.Direction.Cw);

			DrawFill(canvas, drawArea, fillRect);
			DrawStroke(canvas, drawArea, (c, r, p) => c.DrawRoundRect(r.ToRectF(), rx, ry, p));
		}

		partial void OnRadiusXChangedPartial()
		{
			Invalidate();
		}

		partial void OnRadiusYChangedPartial()
		{
			Invalidate();
		}

		protected override void RefreshShape(bool forceRefresh = false)
		{
			Invalidate();
		}
	}
}

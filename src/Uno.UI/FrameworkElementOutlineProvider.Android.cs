using Android.Graphics;
using Android.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Uno.UI
{
    public class FrameworkElementOutlineProvider : ViewOutlineProvider
    {
		public override void GetOutline(View view, Outline outline)
		{
			var rect = new RectF(0, 0, view.Width, view.Height);

			var cornerRadius = GetCornerRadius(view);

			var radii = new[]
			{
				cornerRadius.TopLeft,
				cornerRadius.TopLeft,
				cornerRadius.TopRight,
				cornerRadius.TopRight,
				cornerRadius.BottomRight,
				cornerRadius.BottomRight,
				cornerRadius.BottomLeft,
				cornerRadius.BottomLeft,
			}
				.Select(radius => (float)ViewHelper.LogicalToPhysicalPixels(radius))
				.ToArray();

			var path = new Path();
			path.AddRoundRect(rect, radii, Path.Direction.Cw);

			outline.SetConvexPath(path);
		}

		private static CornerRadius GetCornerRadius(View view)
		{
			switch (view)
			{
				case Border border:
					return border.CornerRadius;
				case Panel panel:
					return panel.CornerRadius;
				default:
					return CornerRadius.None;
			}
		}
	}
}

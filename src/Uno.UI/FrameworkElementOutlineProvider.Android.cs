using Android.Graphics;
using Android.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Shapes;

namespace Uno.UI
{
    public class FrameworkElementOutlineProvider : ViewOutlineProvider
    {
		public override void GetOutline(View view, Outline outline)
		{
			var rect = new RectF(0, 0, view.Width, view.Height);

			var cornerRadius = GetCornerRadius(view);

			var path = cornerRadius.GetOutlinePath(rect);

#pragma warning disable 618
			outline.SetConvexPath(path);
#pragma warning restore 618
		}

		private static CornerRadius GetCornerRadius(View view)
		{
			switch (view)
			{
				case Border border:
					return border.CornerRadius;
				case Panel panel:
					return panel.CornerRadius;
				case Control ctl:
					return ctl.CornerRadius;
				default:
					return CornerRadius.None;
			}
		}
	}
}

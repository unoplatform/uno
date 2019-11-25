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

			var path = cornerRadius.GetOutlinePath(rect);

			outline.SetConvexPath(path);
		}

		private static CornerRadius GetCornerRadius(View view)
		{
			if (view is IRoundedCornersElement rce)
			{
				return rce.CornerRadius;
			}

			return CornerRadius.None;
		}
	}
}

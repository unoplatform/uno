using System;
using System.Collections.Generic;
using System.Text;
using Android.Graphics;

namespace Uno.UI
{
	public static class RectangleExtensions
	{
		internal static Android.Graphics.RectF ToRectF(this Windows.Foundation.Rect rect)
		{
			return new Android.Graphics.RectF((float)rect.X, (float)rect.Y, (float)(rect.X + rect.Width), (float)(rect.Y + rect.Height));
        }

		internal static Android.Graphics.Path ToPath(this Windows.Foundation.Rect rect)
		{
			var path = new Android.Graphics.Path();

			path.AddRect(rect.ToRectF(), Path.Direction.Cw);

			return path;
		}

		//Need this because the current implementation of Foundation.Rect's "IsEmpty"
		// is not intended to convey information about the Rect's area.
		internal static bool HasZeroArea(this Windows.Foundation.Rect rect)
		{
			return rect.Width * rect.Height == 0;
		}
	}
}

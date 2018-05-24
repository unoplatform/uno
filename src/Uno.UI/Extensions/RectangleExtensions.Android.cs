using System;
using System.Collections.Generic;
using System.Text;

namespace Uno.UI
{
	public static class RectangleExtensions
	{
		internal static Android.Graphics.RectF ToRectF(this Windows.Foundation.Rect rect)
		{
			return new Android.Graphics.RectF((float)rect.X, (float)rect.Y, (float)(rect.X + rect.Width), (float)(rect.Y + rect.Height));
        }

		//Need this because the current implementation of Foundation.Rect's "IsEmpty"
		// is not intended to convey information about the Rect's area.
		internal static bool HasZeroArea(this Windows.Foundation.Rect rect)
		{
			return rect.Width * rect.Height == 0;
		}
	}
}

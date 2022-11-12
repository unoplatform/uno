using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace Windows.Foundation;

public partial struct Rect
{
	public static implicit operator Rect(Android.Graphics.Rect rect) => new Rect(rect.Left, rect.Top, rect.Width(), rect.Height());

	public static implicit operator Android.Graphics.Rect(Rect rect) => new Android.Graphics.Rect((int)rect.X, (int)rect.Y, (int)(rect.X + rect.Width), (int)(rect.Y + rect.Height));

	public static implicit operator Rect(Android.Graphics.RectF rect) => new Rect(rect.Left, rect.Top, rect.Width(), rect.Height());

	public static implicit operator Android.Graphics.RectF(Rect rect) => new Android.Graphics.RectF((int)rect.X, (int)rect.Y, (int)(rect.X + rect.Width), (int)(rect.Y + rect.Height));
}

#if __ANDROID__

namespace Windows.Foundation
{
	public partial struct Rect
	{
		public static implicit operator Rect(Android.Graphics.Rect rect) => new Rect(rect.Left, rect.Top, rect.Width(), rect.Height());

		public static implicit operator Android.Graphics.Rect(Rect rect) => new Android.Graphics.Rect((int)rect.X, (int)rect.Y, (int)(rect.X + rect.Width), (int)(rect.Y + rect.Height));

		public static implicit operator Rect(Android.Graphics.RectF rect) => new Rect(rect.Left, rect.Top, rect.Width(), rect.Height());

		public static explicit operator Android.Graphics.RectF(Rect rect) => new Android.Graphics.RectF((float)rect.Left, (float)rect.Top, (float)rect.Right, (float)rect.Bottom);
	}
}
#endif

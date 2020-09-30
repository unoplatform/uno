using Android.Graphics;
using Android.Views;
using Windows.UI.Xaml;
using Uno.UI.Extensions;

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
			return view is UIElement ue ? ue.GetCornerRadius() : CornerRadius.None;
		}
	}
}

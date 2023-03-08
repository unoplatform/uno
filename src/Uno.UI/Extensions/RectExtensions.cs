using Windows.Foundation;

namespace Uno.UI.Extensions;

internal static class RectExtensions
{
	internal static Rect Shrink(this Rect rect, Windows.UI.Xaml.Thickness thickness) =>
		new(
			rect.X + thickness.Left,
			rect.Y + thickness.Top,
			rect.Width - (thickness.Left + thickness.Right),
			rect.Height - (thickness.Top + thickness.Bottom));
}

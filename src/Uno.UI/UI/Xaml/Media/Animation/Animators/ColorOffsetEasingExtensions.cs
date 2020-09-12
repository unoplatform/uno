using Windows.UI;
using Windows.UI.Xaml.Media.Animation;

namespace Windows.UI.Xaml.Media.Animation
{
	internal static class ColorOffsetEasingExtensions
	{
		/// <summary>
		/// Apply easing function on each component of a ColorOffset
		/// </summary>
		public static ColorOffset Ease(this IEasingFunction easing, long frame, ColorOffset from, ColorOffset to, long duration)
		{
			var a = (int)easing.Ease(frame, from.A, to.A, duration);
			var r = (int)easing.Ease(frame, from.R, to.R, duration);
			var g = (int)easing.Ease(frame, from.G, to.G, duration);
			var b = (int)easing.Ease(frame, from.B, to.B, duration);

			return ColorOffset.FromArgb(a, r, g, b);
		}
	}
}

using Windows.Foundation;

namespace Microsoft.UI.Xaml.Media.Animation
{
	internal sealed class DispatcherPointAnimator(Point from, Point to, int frameRate = DispatcherAnimator<Point>.DefaultFrameRate)
		: DispatcherAnimator<Point>(from, to, frameRate)
	{
		protected override Point GetUpdatedValue(long frame, Point from, Point to) =>
			new Point(
				_easing.Ease(frame, from.X, to.X, Duration),
				_easing.Ease(frame, from.Y, to.Y, Duration));
	}
}

namespace Microsoft.UI.Xaml.Media.Animation
{
	internal sealed class DispatcherDoubleAnimator(double from, double to, int frameRate = DispatcherAnimator<double>.DefaultFrameRate)
		: DispatcherAnimator<double>(from, to, frameRate)
	{
		protected override double GetUpdatedValue(long frame, double from, double to) => (float)_easing.Ease(frame, from, to, Duration);
	}
}

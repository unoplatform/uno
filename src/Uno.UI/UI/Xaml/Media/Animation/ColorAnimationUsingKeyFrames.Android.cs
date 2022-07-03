using Windows.UI;

namespace Windows.UI.Xaml.Media.Animation
{
	partial class ColorAnimationUsingKeyFrames
	{
		// For performance consideration, do not report each frame if we are GPU bound
		// Frame will be reported on Pause or End (cf. InitializeAnimator)
		private bool ReportEachFrame() => this.GetIsDependantAnimation() || this.GetIsDurationZero();

		partial void OnFrame(IValueAnimator currentAnimator)
		{
			if (currentAnimator is CPUBoundAnimator<ColorOffset> cpuAnimator)
			{
				var easing = cpuAnimator.GetEasingFunction();
				var newValue = easing.Ease(
					currentAnimator.CurrentPlayTime,
					_startingValue ?? default(ColorOffset),
					_finalValue,
					currentAnimator.Duration);

				SetValue(newValue);
				return;
			}
			// TODO: https://github.com/unoplatform/uno/issues/2947
			else
			{
				// since there is no gpu-bound implementation,
				// make sure at least a value is set when the animation is ended/paused.
				if (!currentAnimator.IsRunning)
				{
					SetValue(_finalValue);
				}
			}
		}
	}
}

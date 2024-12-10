namespace Windows.UI.Xaml.Media.Animation
{
	public partial class DoubleAnimationUsingKeyFrames
	{
		partial void OnFrame(IValueAnimator currentAnimator)
		{
			if (currentAnimator.AnimatedValue is Java.Lang.Float javaFloat)
			{
				SetValue(javaFloat.DoubleValue());
			}
			else
			{
				SetValue(currentAnimator.AnimatedValue);
			}
		}

		// For performance consideration, do not report each frame if we are GPU bound
		// Frame will be repported on Pause or End (cf. InitializeAnimator)
		private bool ReportEachFrame() => this.GetIsDependantAnimation() || this.GetIsDurationZero();
	}
}

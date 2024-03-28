namespace Windows.UI.Xaml.Media.Animation
{
	partial class DiscreteColorKeyFrame : ColorKeyFrame
	{
		internal override IEasingFunction GetEasingFunction() => _easingFunction;

		private static readonly IEasingFunction _easingFunction = new DiscreteDoubleKeyFrame.DiscreteDoubleKeyFrameEasingFunction();

		internal class DiscreteDoubleKeyFrameEasingFunction : IEasingFunction
		{
			public double Ease(double currentTime, double startValue, double finalValue, double duration)
				=> currentTime < duration ? startValue : finalValue;
		}
	}
}

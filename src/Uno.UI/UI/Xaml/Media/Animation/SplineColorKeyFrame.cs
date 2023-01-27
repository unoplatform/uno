namespace Windows.UI.Xaml.Media.Animation
{
	partial class SplineColorKeyFrame : ColorKeyFrame
	{
		internal override IEasingFunction GetEasingFunction() => new SplineEasingFunction(KeySpline);
	}
}

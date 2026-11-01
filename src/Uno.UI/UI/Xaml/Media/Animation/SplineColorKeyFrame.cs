namespace Microsoft.UI.Xaml.Media.Animation
{
	partial class SplineColorKeyFrame : ColorKeyFrame
	{
		internal override float GetEffectiveProgress(float progress) => KeySpline is { } keySpline ? keySpline.GetSplineProgress(progress) : 0;
	}
}

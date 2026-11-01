namespace Microsoft.UI.Xaml.Media.Animation
{
	partial class DiscreteColorKeyFrame : ColorKeyFrame
	{
		internal override bool IsDiscrete => true;

		internal override float GetEffectiveProgress(float progress) => 0;
	}
}

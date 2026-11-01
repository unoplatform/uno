#nullable enable

namespace Microsoft.UI.Xaml.Media.Animation;

/// <summary>
/// Animates from the Point value of the previous key frame to its own Value using discrete interpolation.
/// </summary>
public partial class DiscretePointKeyFrame : PointKeyFrame
{
	public DiscretePointKeyFrame()
	{
	}

	internal override bool IsDiscrete => true;

	// Discrete keyframes clamp to the starting value of the time segment
	internal override float GetEffectiveProgress(float progress) => 0;
}

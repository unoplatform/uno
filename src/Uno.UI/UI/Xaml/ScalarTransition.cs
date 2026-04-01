using System;
using System.Numerics;
using Microsoft.UI.Composition;

namespace Microsoft.UI.Xaml;

public partial class ScalarTransition
{
	private static readonly TimeSpan DefaultDuration = TimeSpan.FromTicks(3_000_000); // 300ms, matches WinUI

	public ScalarTransition()
	{
		Duration = DefaultDuration;
	}

	public TimeSpan Duration { get; set; }

#if __SKIA__
	/// <summary>
	/// Creates a composition animation for this transition.
	/// Mirrors CScalarTransition::GetWUCAnimationNoRef in WinUI.
	/// </summary>
	internal ScalarKeyFrameAnimation CreateAnimation(Compositor compositor, float oldValue, float newValue)
	{
		// WinUI uses CubicBezier(0.8, 0.0, 0.2, 1.0) for scalar/vector3 transitions
		var easing = compositor.CreateCubicBezierEasingFunction(new Vector2(0.8f, 0f), new Vector2(0.2f, 1f));
		var animation = compositor.CreateScalarKeyFrameAnimation();
		animation.Duration = Duration;
		animation.InsertKeyFrame(0f, oldValue, easing);
		animation.InsertKeyFrame(1f, newValue, easing);
		return animation;
	}
#endif
}

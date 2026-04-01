using System;
#if __SKIA__
using Microsoft.UI.Composition;
using Windows.UI;
#endif

namespace Microsoft.UI.Xaml;

public partial class BrushTransition
{
	private static readonly TimeSpan DefaultDuration = TimeSpan.FromTicks(1_500_000); // 150ms, matches WinUI

	public BrushTransition()
	{
		Duration = DefaultDuration;
	}

	public TimeSpan Duration { get; set; }

#if __SKIA__
	/// <summary>
	/// Creates a composition color animation for this transition.
	/// Mirrors SharedTransitionAnimations::GetBrushAnimationNoRef in WinUI.
	/// </summary>
	internal ColorKeyFrameAnimation CreateAnimation(Compositor compositor, Color fromColor, Color toColor)
	{
		// WinUI uses linear easing for brush transitions
		var easing = compositor.CreateLinearEasingFunction();
		var animation = compositor.CreateColorKeyFrameAnimation();
		animation.Duration = Duration;
		animation.InterpolationColorSpace = CompositionColorSpace.Rgb;
		animation.InsertKeyFrame(0f, fromColor, easing);
		animation.InsertKeyFrame(1f, toColor, easing);
		return animation;
	}
#endif
}

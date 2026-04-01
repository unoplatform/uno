#nullable enable

using System;
using System.Numerics;
using Microsoft.UI.Composition;

namespace Microsoft.UI.Xaml;

public partial class Vector3Transition
{
	private static readonly TimeSpan DefaultDuration = TimeSpan.FromTicks(3_000_000); // 300ms, matches WinUI

	public Vector3Transition()
	{
		Duration = DefaultDuration;
		Components = Vector3TransitionComponents.X | Vector3TransitionComponents.Y | Vector3TransitionComponents.Z;
	}

	public TimeSpan Duration { get; set; }

	public Vector3TransitionComponents Components { get; set; }

#if __SKIA__
	/// <summary>
	/// Creates a composition animation for this transition.
	/// Mirrors CVector3Transition::GetWUCAnimationNoRef in WinUI.
	/// </summary>
	/// <returns>A Vector3KeyFrameAnimation, or null if Components is 0.</returns>
	internal Vector3KeyFrameAnimation? CreateAnimation(Compositor compositor, Vector3 oldValue, Vector3 newValue)
	{
		if (Components == 0)
		{
			return null;
		}

		// WinUI uses CubicBezier(0.8, 0.0, 0.2, 1.0) for scalar/vector3 transitions
		var easing = compositor.CreateCubicBezierEasingFunction(new Vector2(0.8f, 0f), new Vector2(0.2f, 1f));
		var animation = compositor.CreateVector3KeyFrameAnimation();
		animation.Duration = Duration;

		var allComponents = Vector3TransitionComponents.X | Vector3TransitionComponents.Y | Vector3TransitionComponents.Z;
		if (Components == allComponents)
		{
			// All subchannels are animated
			animation.InsertKeyFrame(0f, oldValue, easing);
		}
		else
		{
			// Some subchannels aren't animated. Snap them to their final value at frame 0.
			// This mirrors WinUI's expression-based approach for partial component animation.
			var startValue = new Vector3(
				(Components & Vector3TransitionComponents.X) != 0 ? oldValue.X : newValue.X,
				(Components & Vector3TransitionComponents.Y) != 0 ? oldValue.Y : newValue.Y,
				(Components & Vector3TransitionComponents.Z) != 0 ? oldValue.Z : newValue.Z);
			animation.InsertKeyFrame(0f, startValue, easing);
		}

		animation.InsertKeyFrame(1f, newValue, easing);
		return animation;
	}
#endif
}

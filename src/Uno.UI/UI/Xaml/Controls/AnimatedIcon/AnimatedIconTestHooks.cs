// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference AnimatedIconTestHooks.cpp, commit 2eba45b

using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls
{
	internal class AnimatedIconTestHooks
	{
		internal static void SetAnimationQueueBehavior(AnimatedIcon animatedIcon, AnimatedIconAnimationQueueBehavior behavior)
		{
			if (animatedIcon != null)
			{
				animatedIcon.SetAnimationQueueBehavior(behavior);
			}
		}

		internal static void SetDurationMultiplier(AnimatedIcon animatedIcon, float multiplier)
		{
			if (animatedIcon != null)
			{
				animatedIcon.SetDurationMultiplier(multiplier);
			}
		}

		internal static void SetSpeedUpMultiplier(AnimatedIcon animatedIcon, float multiplier)
		{
			if (animatedIcon != null)
			{
				animatedIcon.SetSpeedUpMultiplier(multiplier);
			}
		}

		internal static void SetQueueLength(AnimatedIcon animatedIcon, int length)
		{
			if (animatedIcon != null)
			{
				animatedIcon.SetQueueLength(length);
			}
		}

		internal static string GetLastAnimationSegment(AnimatedIcon animatedIcon)
		{
			if (animatedIcon != null)
			{
				return animatedIcon.GetLastAnimationSegment();
			}
			return "";
		}

		internal static string GetLastAnimationSegmentStart(AnimatedIcon animatedIcon)
		{
			if (animatedIcon != null)
			{
				return animatedIcon.GetLastAnimationSegmentStart();
			}
			return "";
		}

		internal static string GetLastAnimationSegmentEnd(AnimatedIcon animatedIcon)
		{
			if (animatedIcon != null)
			{
				return animatedIcon.GetLastAnimationSegmentEnd();
			}
			return "";
		}

		internal static void NotifyLastAnimationSegmentChanged(AnimatedIcon sender)
		{
			LastAnimationSegmentChanged?.Invoke(sender, null);
		}

		internal static TypedEventHandler<AnimatedIcon, object> LastAnimationSegmentChanged;
	}
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference components/animation/Animation.cpp, tag winui3/release/2.5.1, commit ba3a8d59e

#nullable enable

using System.Collections.Generic;

namespace Microsoft.UI.Xaml.Media.Animation;

partial class Timeline
{
	internal bool IsZeroDuration() => m_isZeroDuration;

	// Compute the duration when one is not specified in the markup.
	// Animations override this method to provide their own default.
	// CAnimation::GetNaturalDuration
	private void AnimationGetNaturalDuration(out DurationType pDurationType, out float pDurationValue)
	{
		var durationValue = m_duration;

		if (durationValue.Type == DurationType.TimeSpan)
		{
			pDurationType = durationValue.Type;
			pDurationValue = (float)durationValue.TimeSpan.TotalSeconds;
		}
		else if (m_fUsesKeyFrames && KeyFrames_Core != null)
		{
			// Versions of Xaml before Threshold will prioritize key frames ahead of Duration="Forever".
			// If a repeating animation has an explicit Duration="Forever" and key frames lasting 3 seconds, then
			// the animation will repeat every 3 seconds in spite of the explicit duration. If the same repeating
			// animation is done without key frames, then the animation never repeats because its duration is forever.

			GetNaturalDurationFromKeyFrames(
				GetKeyFrameCollection().GetSortedCollection(),
				out pDurationType,
				out pDurationValue);
		}
		else if (durationValue.Type != DurationType.Automatic)
		{
			pDurationType = durationValue.Type;
			pDurationValue = (float)durationValue.TimeSpan.TotalSeconds;
		}
		else
		{
			pDurationType = DurationType.TimeSpan;
			pDurationValue = NULL_DURATION_DEFAULT;
		}
	}

	internal static void GetNaturalDurationFromKeyFrames(
		IReadOnlyList<IKeyFrame> sortedKeyFrames,
		out DurationType pDurationType,
		out float pDurationValue)
	{
		// Duration defaults to 1 second
		pDurationType = DurationType.TimeSpan;
		pDurationValue = 1.0f;

		var keyFrameCount = sortedKeyFrames.Count;
		if (keyFrameCount > 0)
		{
			// Find the largest TimeSpan duration in our keyframes
			// loop backwards since the last keyframe should be the one
			float localDuration = 0;
			for (var i = keyFrameCount; i > 0; i--)
			{
				// TODO Uno: KeyTime is a struct in Uno and can't be missing (WinUI tolerates a null key time here).
				var value = sortedKeyFrames[i - 1].KeyTime;

				if (localDuration < (float)value.TimeSpan.TotalSeconds)
				{
					localDuration = (float)value.TimeSpan.TotalSeconds;
					break;
				}
			}

			pDurationValue = localDuration;
		}
	}

#if !HAS_UNO
	// TODO Uno: NOT PORTED - ShouldDiscardDCompAnimationOnDetach and MapToDCompAnimationProperty: composition animations.
#endif

	// CAnimation::OnRemoveFromTimeManager
	private void AnimationOnRemoveFromTimeManager()
	{
		// If the target is gone, then remove ourselves from the time manager's table of active animations.
		// TODO Uno: The table entries are weak on their target and go away with it.
	}

#if !HAS_UNO
	// TODO Uno: NOT PORTED - MakeCompositionAnimationVirtual, MakeCompositionKeyFrameAnimationVirtual, ReleaseDCompResources,
	// TraceTickPausedAnimationHelper and UpdateWUCAnimationTelemetryString: composition animations and tracing.
#endif
}

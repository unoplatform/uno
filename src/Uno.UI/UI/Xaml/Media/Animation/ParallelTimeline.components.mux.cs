// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference components/animation/ParallelTimeline.cpp, tag winui3/release/2.5.1, commit ba3a8d59e

#nullable enable

namespace Microsoft.UI.Xaml.Media.Animation;

partial class Storyboard
{
#if !HAS_UNO
	// TODO Uno: NOT PORTED - the destructor, AttachDCompAnimations, AttachDCompAnimationInstancesToTarget,
	// DetachDCompAnimationInstancesFromTarget, ResolvePendingDCompAnimationOperationsRecursive,
	// ClearPendingDCompAnimationOperationsRecursive, SeekDCompAnimationInstances, PauseDCompAnimationsOnSuspend,
	// ResumeDCompAnimationsOnResume, ReleaseDCompResources, MakeCompositionAnimationVirtual,
	// DetachDCompCompletedHandlerOnStop and UpdateIsWaitingForDCompAnimationCompleted: composition animations.
#endif

	internal override void GetNaturalDuration(out DurationType pDurationType, out float pDurationValue)
	{
		float duration = 0;
		var durationType = DurationType.TimeSpan;

		if (m_duration.Type == DurationType.TimeSpan)
		{
			duration = (float)m_duration.TimeSpan.TotalSeconds;
			durationType = m_duration.Type;
		}
		// Versions of Xaml before Threshold did not respect an explicit Duration="Forever" on
		// a ParallelTimeline, and will instead resolve its natural duration from its children.
		else
		{
			var hasInfiniteDurationChild = false;

			for (var i = 0; !hasInfiniteDurationChild && i < Children.Count; i++)
			{
				var pTimelineNoRef = Children[i];

				pTimelineNoRef.GetDurationWithProperties(out var childDurationType, out var childDuration);

				switch (childDurationType)
				{
					case DurationType.TimeSpan:
					{
						if (childDuration > duration)
						{
							duration = childDuration;
							durationType = childDurationType;
						}
						break;
					}

					case DurationType.Forever:
					{
						hasInfiniteDurationChild = true;
						duration = childDuration;
						durationType = childDurationType;
						break;
					}

					case DurationType.Automatic:
					default:
					{
						// Automatic means that the subtree could not be resolved, which cannot happen due to the recursive nature of this call:
						//  - either the child is an animation, which will default to 1sec if automatic
						//  - or the child is another storyboard which will recursively resolve to its child times
						//  - or the child is another empty storyboard which will default to 0sec
						throw new global::System.InvalidOperationException("Automatic duration is not expected here.");
					}
				}
			}
		}

		pDurationType = durationType;
		pDurationValue = duration;
	}
}

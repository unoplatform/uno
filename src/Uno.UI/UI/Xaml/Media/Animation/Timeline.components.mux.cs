// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference components/animation/Timeline.cpp, tag winui3/release/2.5.1, commit ba3a8d59e

#nullable enable

using System;

namespace Microsoft.UI.Xaml.Media.Animation;

partial class Timeline
{
	protected Timeline()
	{
#if HAS_UNO
		IsAutoPropertyInheritanceEnabled = false;
#endif
	}

	internal bool IsFilling() => m_clockState == TimelineClockState.Filling;

#if !HAS_UNO
	// TODO Uno: NOT PORTED - MakeCompositionAnimationsWithProperties, MakeCompositionAnimationVirtual, SetForceDCompAnimationSubtreeDirty,
	// SetDCompAnimationDirty, SetDCompAnimationInSubtreeDirty, NWPropagateDirtyFlag, MarkIsDCompAnimationDirty,
	// MarkIsDCompAnimationDirtyInSubtree, MarkForceDCompAnimationDirtyInSubtree, MarkIsDCompAnimationInstanceDirty,
	// MarkShouldAttachDCompAnimationInstance, AttachDCompAnimations, AddPendingDCompPause, AddPendingDCompResume,
	// AddPendingDCompSeek, ClearPendingDCompAnimationOperationsRecursive, ResolvePendingDCompAnimationOperations,
	// ResolvePendingDCompAnimationOperationsRecursive, SeekDCompAnimationInstances and SynchronizeDCompAnimationAfterResume:
	// composition (DComp/WUC) animations.
#endif

	internal static bool ExtendDurationWithRepeat(ref float pDuration, RepeatBehavior repeatBehavior)
	{
		switch (repeatBehavior.Type)
		{
			case RepeatBehaviorType.Forever:
				return true;

			case RepeatBehaviorType.Duration:
				pDuration = (float)repeatBehavior.Duration.TotalSeconds;
				break;

			case RepeatBehaviorType.Count:
			default:
				pDuration *= (float)repeatBehavior.Count;
				break;
		}

		return false;
	}

	internal static float ExtendDurationWithReverse(float duration, bool hasReverse) => hasReverse ? (duration * 2) : duration;

	internal static float AdjustDurationWithSpeedRatio(float duration, float speedRatio)
	{
		global::System.Diagnostics.Debug.Assert(speedRatio > 0);

		return (speedRatio != 1.0f) ? (duration / speedRatio) : duration;
	}

	internal static float AdjustDurationWithBeginTime(float duration, TimeSpan? pBeginTime)
		=> (pBeginTime != null) ? (duration + (float)pBeginTime.Value.TotalSeconds) : duration;

	// Returns the duration of this timeline, taking into account its natural duration, the begin time, speed ratio, auto reverse,
	// and repeat behavior.
	internal void GetDurationWithProperties(out DurationType pDurationType, out float pDurationValue)
	{
		GetNaturalDuration(out var durationType, out var duration);

		switch (durationType)
		{
			case DurationType.TimeSpan:
			{
				duration = ExtendDurationWithReverse(duration, m_fAutoReverse);

				var loopsInfinitely = ExtendDurationWithRepeat(ref duration, m_repeatBehavior);

				if (loopsInfinitely)
				{
					durationType = DurationType.Forever;
				}
				else
				{
					duration = AdjustDurationWithSpeedRatio(duration, m_rSpeedRatio);
					duration = AdjustDurationWithBeginTime(duration, m_pBeginTime);
				}

				break;
			}

			case DurationType.Forever:
			{
				break;
			}

			case DurationType.Automatic:
			default:
			{
				throw new InvalidOperationException("Automatic duration is not expected here.");
			}
		}

		pDurationType = durationType;
		pDurationValue = duration;
	}

#if !HAS_UNO
	// TODO Uno: NOT PORTED - AttachDCompAnimationInstancesToTarget, DetachDCompAnimationInstancesFromTarget,
	// PauseDCompAnimationsOnSuspend, ResumeDCompAnimationsOnResume, ShouldDiscardDCompAnimationOnDetach, ReleaseDCompResources
	// and OnDCompAnimationTimeEventStatic: composition (DComp/WUC) animations.
#endif

	internal virtual void InitializeIteration()
	{
		m_fInitialized = true;

#if !HAS_UNO
		CTimeline::SetDCompAnimationDirty(this, DirtyFlags::None /* flags - ignored */);
#endif
	}

#if !HAS_UNO
	// TODO Uno: NOT PORTED - SetWUCScopedBatch, SetWUCAnimator, GetWUCAnimationCompletedToken: composition (WUC) animations.
#endif

	internal void ResetCompletedEventFired() => m_IsCompletedEventFired = false;

#if !HAS_UNO
	// TODO Uno: NOT PORTED - DetachDCompCompletedHandlerOnStop, OnDCompAnimationCompleted, IsWaitingForDCompAnimationCompleted,
	// SetIsWaitingForDCompAnimationCompleted and UpdateIsWaitingForDCompAnimationCompleted: composition (DComp/WUC) animations.
#endif

	internal bool IsTopLevelTimeline()
		=> GetTimingParent() == null
			|| TimeManager.Instance.GetRootTimeline() == GetTimingParent();

	internal void SetTimingParent(Timeline? parent)
		=> m_timingParent = parent is null ? null : new WeakReference<Timeline>(parent);

	internal Timeline? GetTimingParent()
	{
		if (m_timingParent is not null && m_timingParent.TryGetTarget(out var parent))
		{
			return parent;
		}

		return null;
	}

#if !HAS_UNO
	// TODO Uno: SetTemplatedParentImpl / GetTemplatedParent: Uno resolves template names through the element tree.
#endif

	private protected static bool CanRequestTicksWhileActive()
	{
		// TODO Uno: WinUI stops requesting ticks for an animation that expired while waiting for its DComp animation to
		// complete. Uno has no DComp animations.
		return true;
	}
}

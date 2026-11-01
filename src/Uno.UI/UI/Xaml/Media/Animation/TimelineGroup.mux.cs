// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference TimelineGroup.cpp, tag winui3/release/2.5.1, commit ba3a8d59e

#nullable enable

namespace Microsoft.UI.Xaml.Media.Animation;

// TODO Uno specific: CTimelineGroup has no public counterpart; Storyboard is its only Uno subclass. m_pChild is Children.
partial class Storyboard
{
#if !HAS_UNO
	// TODO Uno: The destructor (DetachChildren), Create, SetValue, AddChild and RemoveChild: the timing parent of the children
	// is maintained by TimelineCollection, which refuses modifications while the root storyboard runs.
#endif

	// CTimelineGroup::ComputeStateImpl
	private void TimelineGroupComputeStateImpl(
		in ComputeStateParams parentParams,
		ref ComputeStateParams myParams,
		ref bool pHasNoExternalReferences,
		bool hasExternalReferencesTracking)
	{
		var previousState = m_clockState;

		// Compute local state
		base.ComputeStateImpl(
			parentParams,
			ref myParams,
			ref pHasNoExternalReferences,
			hasExternalReferencesTracking);

		// Compute child timelines
		{
			// Compute the state of the timing children when:
			//   - the timeline hasn't started yet
			//      (e.g. We need to compute children to clone them for the composition thread in case they are independent animations with BeginTimes set)
			//   - the timeline is active
			//      (e.g. The children animations need to update their targets' values)
			//   - the timeline has transitioned to filling
			//      (e.g. The children timelines need to update their values one last time and raise Completed events)
			//   - a child needs a pending theme change to be processed
			//   - this is the final tick of the entire timing tree
			//      (see CAnimation::ShouldTickOnUIThread for detailed comment)
			var shouldComputeChildState = m_clockState == TimelineClockState.NotStarted
				|| m_clockState == TimelineClockState.Active
				|| (m_clockState == TimelineClockState.Filling && previousState != TimelineClockState.Filling)
				|| HasPendingThemeChange()
				|| !GetRootTimingParent().IsInActiveState();

			// The current time of this timeline is used as the baseline for each child timeline to calculate its relative progress.
			// If this timeline hasn't started, the current time is not valid and there's no meaningful way for children to calculate
			// relative progress either. This is okay since all the children will be in the 'not started' state as well.
			// If the root storyboard has stopped, then its current time has been reset to 0 by FinalizeIteration and is not valid. Its
			// children should not be active, so tick them with no parent time.
			// Note: A child with a negative BeginTime will not actually start sooner - it will be limited by its parent starting first.
			//       The negative BeginTime will just adjust the progress ahead once the animation has started.
			// Note: We check the state of the root storyboard explicitly. If this is a nested storyboard, it could be Filling because
			//       the Stopped root ticked it at time infinity. We also check the clock state explicitly rather than use IsInStoppedState(),
			//       because a storyboard that naturally expires with FillBehavior.Stop will not be IsInStoppedState.
			myParams.hasTime =
				m_clockState != TimelineClockState.NotStarted
				&& GetRootTimingParent().m_clockState != TimelineClockState.Stopped;

			if (myParams.hasTime)
			{
				myParams.time = m_rCurrentTime;
			}

			foreach (var pTimelineNoRef in Children.Items)
			{
				if (shouldComputeChildState)
				{
					if (hasExternalReferencesTracking)
					{
						pTimelineNoRef.ComputeState(myParams, ref pHasNoExternalReferences);
					}
					else
					{
						pTimelineNoRef.ComputeState(myParams);
					}
				}

#if !HAS_UNO
				// A TimelineGroup has independent animations if any of its children does.
				if (pTimelineNoRef->HasIndependentAnimation())
				{
					hasIndependentAnimation = TRUE;
				}
#endif
			}
		}
	}

	internal override void FinalizeIteration()
	{
		if (m_clockState != TimelineClockState.Stopped)
		{
			m_clockState = TimelineClockState.Stopped;
		}

		// Notify children
		foreach (var pTimeline in Children.Items)
		{
			pTimeline.FinalizeIteration();
		}

		// Release the target object.  In the case of an explicitly set target,
		// we have another pointer through m_pManualTargetObjectWeakRef.
		ReleaseTarget();

		base.FinalizeIteration();
	}

	internal override void OnAddToTimeManager()
	{
		base.OnAddToTimeManager();

		// Notify children
		foreach (var pTimeline in Children.Items)
		{
			pTimeline.OnAddToTimeManager();
		}
	}

	internal override void OnRemoveFromTimeManager()
	{
		base.OnRemoveFromTimeManager();

		// Notify children
		foreach (var pTimeline in Children.Items)
		{
			pTimeline.OnRemoveFromTimeManager();
		}
	}

	internal override bool IsFinite()
	{
		var isFiniteAnimation = true;

		if (!base.IsFinite())
		{
			return false;
		}

		// If we have children, and we're finite, then this timeline is finite only if all children are finite.
		for (var i = 0; i < Children.Count && isFiniteAnimation; ++i)
		{
			if (!Children[i].IsFinite())
			{
				isFiniteAnimation = false;
			}
		}

		return isFiniteAnimation;
	}
}

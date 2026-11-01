// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference components/animation/TimeManager.cpp, tag winui3/release/2.5.1, commit ba3a8d59e

#nullable enable

namespace Microsoft.UI.Xaml.Media.Animation;

partial class TimeManager
{
#if !HAS_UNO
	// TODO Uno: NOT PORTED - CTimeManager::CleanupDeviceRelatedResourcesRecursive: DComp device resources.
#endif

	internal void SetAnimationOnProperty(DependencyObject DOWeakRef, DependencyProperty nPropertyIndex, Timeline pAnimation)
	{
		EnsureTimeStarted();

		global::System.Diagnostics.Debug.Assert(pAnimation is not null);

		// Inserts, or replaces the animation already registered for the property.
		m_hashTable.GetOrCreateValue(DOWeakRef)[nPropertyIndex] = pAnimation;
	}

	internal void ClearAnimationOnProperty(DependencyObject DOWeakRef, DependencyProperty nPropertyIndex)
	{
		if (m_hashTable.TryGetValue(DOWeakRef, out var position))
		{
			position.Remove(nPropertyIndex);
		}
	}

	internal Timeline? GetAnimationOnProperty(DependencyObject? DOWeakRef, DependencyProperty? nPropertyIndex)
	{
		Timeline? result = null;

		EnsureTimeStarted();

		// we should be robust to this condition;
		if (DOWeakRef == null || nPropertyIndex == null)
		{
			return result;
		}

		if (m_hashTable.TryGetValue(DOWeakRef, out var position) && position.TryGetValue(nPropertyIndex, out var animation))
		{
			result = animation;
		}

		return result;
	}

	// Updates the start time and mark as loaded if we are unlocked.
	private void EnsureTimeStarted()
	{
		if (!m_isLoaded)
		{
			// We do this once for the first trigger
			m_isLoaded = true;
		}

		if (m_unlockRequested)
		{
			// We do this once for the first trigger
			m_rTimeStarted = GetLastTickTimeInSeconds();
			m_unlockRequested = false;
		}
	}

	internal void SetTimeManagerClockOverrideConstant(double newTime) => m_clockOverride = newTime;

#if !HAS_UNO
	// TODO Uno: NOT PORTED - OnWUCAnimationCompleted, MarkWUCAnimationCompleted, AddActiveWUCAnimation, ResetWUCCompletedEvents,
	// DetachWUCCompletedHandler, StartWUCAnimation, AddTargetDO, UpdateTargetDOs, HasTargetDOs, PauseDCompAnimationsOnSuspend
	// and ResumeDCompAnimationsOnResume: composition (WUC/DComp) animations.
	// TODO Uno: NOT PORTED - ShouldFailFast: the AnimationFailFast runtime feature; errors are always reported as exceptions.
#endif
}

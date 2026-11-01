// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference timemgr.cpp, tag winui3/release/2.5.1, commit ba3a8d59e

#nullable enable

namespace Microsoft.UI.Xaml.Media.Animation;

partial class TimeManager
{
	//------------------------------------------------------------------------
	//
	//  Synopsis:
	//      ctor
	//
	//------------------------------------------------------------------------
	private TimeManager()
	{
		m_isLoaded = false;
#if HAS_UNO
		// TODO Uno specific: WinUI keeps time locked to zero until the first frame after the splash screen drops
		// (CCoreServices unlocks it). Uno has no such signal, so time runs from the start.
		m_lockTimeToZero = false;
#else
		m_lockTimeToZero = true;
#endif
		m_unlockRequested = false;
		m_rTimeStarted = 0;
		m_rLastTickTime = 0;
		m_pTimelineListHead = null;
		m_pSnappedTimelineHeadNoRef = null;
		m_pTimelinePreviousHead = null;
		m_pRootTimeline = null;
		m_clockOverride = -1;
	}

	//------------------------------------------------------------------------
	//
	//  Synopsis:
	//      Resets this object back to creation state
	//
	//------------------------------------------------------------------------
	internal void Reset()
	{
#if !HAS_UNO
		ResetWUCCompletedEvents();
#endif

		// when calling DeleteTimelineList(), the node being pointed to here is also deleted.
		m_pTimelinePreviousHead = null;

		DeleteTimelineList(m_pTimelineListHead);
		m_pTimelineListHead = null;

		m_pSnappedTimelineHeadNoRef = null;

		// Destroy the root Timeline
		m_pRootTimeline = null;

		// Reset the hash table
		m_hashTable.Clear();

		// Reset State
		m_rTimeStarted = 0;
		m_isLoaded = false;

#if !HAS_UNO
		// Clear any unprocessed counters; there's no need to mark them if we're resetting.
		GetPendingIACounters().clear();
		GetPendingIACounters().shrink_to_fit();

		// Processing targets with no current frame will clear all existing markings.
		VERIFYHR(UpdateIATargets());

		// Clear the processed counters as well.
		GetProcessedIACounters().clear();
		GetProcessedIACounters().shrink_to_fit();
#endif
	}

	//---------------------------------------------------------------------------
	//
	//  Synopsis:
	//      Deletes a linked list of timelines
	//
	//---------------------------------------------------------------------------
	private static void DeleteTimelineList(TimelineListNode? pNode)
	{
		if (pNode != null)
		{
			// Detach list from previous link
			var pPrev = pNode.m_pPreviousNoRef;
			if (pPrev != null)
			{
				pPrev.m_pNextNoRef = null;
			}

			// Release all Timelines and nodes in linked list
			while (pNode != null)
			{
				// get current pointer, and save next pointer for next iteration
				var pTemp = pNode;
				pNode = pTemp.m_pNextNoRef;

				var pTimeline = pTemp.m_pTimeline;
				if (pTimeline != null)
				{
					pTimeline.SetTimingParent(null);
					pTimeline.OnRemoveFromTimeManager();
				}
			}
		}
	}

	//------------------------------------------------------------------------
	//
	//  Synopsis:
	//      Helper method to links a TimelineListNode to the head of a list.
	//
	//------------------------------------------------------------------------
	private static void InsertNodeAtHead(TimelineListNode pNewHead, TimelineListNode? pPreviousHead)
	{
		pNewHead.m_pNextNoRef = pPreviousHead;

		if (pPreviousHead != null)
		{
			global::System.Diagnostics.Debug.Assert(pPreviousHead.m_pPreviousNoRef == null);
			pPreviousHead.m_pPreviousNoRef = pNewHead;
		}
	}

	//------------------------------------------------------------------------
	//
	//  Synopsis:
	//      Gets an estimate of the 'wall clock' time in the TimeManager's frame of reference
	//      (i.e. since this time manager started).
	//      This time is _not_ aligned to the last tick - in fact it always reports a greater value.
	//
	//      This time is safe to use any time on the UI thread, especially for async work that
	//      happens outside of ticking/generating a frame.
	//
	//------------------------------------------------------------------------
	internal double GetEstimatedNextTickTime()
		=> (m_isLoaded && !m_lockTimeToZero)
			? GetNextTickTimeInSeconds() - m_rTimeStarted
			: 0.0;

	//------------------------------------------------------------------------
	//
	//  Synopsis:
	//      Returns the last time that the TimeManager ticked, relative to when it first ticked.
	//
	//      This time is _not safe_ to use any time on the UI thread, especially for async work.
	//      It is only reliable inside frame generation after the TimeManager has been ticked.
	//      Up until that point, the time returned is unpredictable since the UI thread may
	//      have been idle for an indefinite period.
	//
	//------------------------------------------------------------------------
	internal float GetLastTickTime() => (float)m_rLastTickTime;

	//------------------------------------------------------------------------
	//
	//  Synopsis:
	//      Update the timing tree
	//
	//------------------------------------------------------------------------
	internal void Tick(bool newTimelinesOnly, IFrameScheduler pFrameScheduler)
	{
		Timeline? pTimeline = null;
		var pCurrNode = m_pTimelineListHead;

#if !HAS_UNO
		// We only want to check for the completion of all finite animations if we've successfully retrieved the animations complete event
		bool bCheckForAnimationsComplete = !!GetContext()->HasAnimationEvents();
#endif

		var rTimeCurrent = 0.0;

		var parentParams = new ComputeStateParams();

		EnsureTimeStarted();

#if !HAS_UNO
		// Save this bit so animations can determine whether they need to mark independent-animation
		// targets by calling ShouldCollectIATargets during this Tick().
		m_processIATargets = processIATargets;

		// The list of pending animation targets should be processed each frame.
		// The only time we can already have pending targets when ticking is if this is a second tick
		// in the same frame to update just the newly added timelines.
		ASSERT(GetPendingIACounters().empty() || newTimelinesOnly);
#endif

		// Compute global clock
		if (!newTimelinesOnly && !m_lockTimeToZero)
		{
			m_rLastTickTime = GetLastTickTimeInSeconds() - m_rTimeStarted;
		}
		rTimeCurrent = m_rLastTickTime;

		try
		{
			// If not loaded or if there are no active timelines, do nothing
			if (!m_isLoaded || m_pTimelineListHead == null)
			{
				return;
			}

			// New timelines are added to the front of the list of animations. We tick animations multiple times each frame,
			// because things like layout can add new animations. We don't want to tick any animation more than once, so we
			// keep a marker for the part of the list that has already been ticked.
			//
			// Save the current head of the list of animations. At the end of ticking, we'll update the marker to this saved
			// value. We don't use m_pTimelineListHead when we update the marker, because ticking the current animations could
			// have added new animations to the head of the list. These new animations haven't been ticked yet, and should be
			// in front of the marker so that we can tick them.
			//
			// Timelines can be removed from the time manager as it ticks, and we have to update this saved head of the list
			// as timelines are removed. Otherwise this node (representing the tail of the timeline list that has already been
			// ticked) may itself no longer be in the timeline list.
			m_pSnappedTimelineHeadNoRef = m_pTimelineListHead;

			if (!newTimelinesOnly)
			{
				m_pTimelinePreviousHead = null;
			}

			if (m_clockOverride >= 0)
			{
				rTimeCurrent = m_clockOverride;
			}

#if !HAS_UNO
			if (pCurrNode != nullptr && !s_slowDownAnimationsLoaded)
			{
				static auto runtimeEnabledFeatureDetector = RuntimeFeatureBehavior::GetRuntimeEnabledFeatureDetector();
				s_slowDownAnimations = runtimeEnabledFeatureDetector->IsFeatureEnabled(RuntimeFeatureBehavior::RuntimeEnabledFeature::SlowDownAnimations, true /* disableCaching */);
				s_slowDownAnimationsLoaded = true;
			}

			const float initialSpeedRatio = s_slowDownAnimations ? 0.1f : 1.0f;
#else
			const float initialSpeedRatio = 1.0f;
#endif

			parentParams.hasTime = true;
			parentParams.time = rTimeCurrent;
			parentParams.pFrameSchedulerNoRef = pFrameScheduler;
			parentParams.isReversed = false;
			parentParams.speedRatio = initialSpeedRatio;
			parentParams.isPaused = false;

			while (pCurrNode != m_pTimelinePreviousHead && pCurrNode != null)
			{
				pTimeline = pCurrNode.m_pTimeline;

				// TODO Uno: DispatcherTimer is not a timeline in Uno, so there are no timers to tick (TickTimers).
				if (pTimeline != null)
				{
					var hasNoExternalReferences = true;

#if !HAS_UNO
					if (!tickOnlyTimers && pDCompDevice != nullptr)
					{
						// This is the UI thread. Make Composition animations.
						CompositionAnimationConversionContext compContext(easingFunctionStatics, wucCompositor, initialSpeedRatio);

						// Disregard this returned result. Storyboards return a failure if any part of them failed to convert
						// to a Composition animation. There may be other timelines inside that successfully converted, and we
						// shouldn't skip this Storyboard just because part of it failed to convert.
						//
						// Each timeline in this timing tree has recorded its own conversion result. The parts that failed to
						// convert will tick dependently during ComputeState. The parts that succeeded will attach them to
						// animation targets during AttachDCompAnimations.
						(void)pTimeline->MakeCompositionAnimationsWithProperties(&compContext);
					}
#endif

					pTimeline.ComputeState(parentParams, ref hasNoExternalReferences);

#if !HAS_UNO
					if (!tickOnlyTimers && pDCompDevice != nullptr)
					{
						pTimeline->AttachDCompAnimations();
						pTimeline->ResolvePendingDCompAnimationOperationsRecursive();

						if (pTimeline->ShouldSynchronizeDCompAnimationAfterResume())
						{
							pTimeline->SynchronizeDCompAnimationAfterResume(rTimeCurrent);
						}
					}
#endif

					// With independent animations ticking in the DWM, we can't replace DComp animations with static values because
					// we don't know what that static value is. We'll leave paused animations (with the paused WUC animators) attached
					// to the DComp tree. That requires keeping paused Xaml animations in the time manager.
					var isStoryboardPaused = pTimeline is Storyboard storyboard
						&& storyboard.IsPaused();

					// We want to remove this Timeline if it's not active and if we are not keeping it alive.
					if (hasNoExternalReferences
							|| (!pTimeline.IsInActiveState() && !isStoryboardPaused && !pTimeline.HasPendingThemeChange()))
					{
						var pNodeToRemoveNoRef = pCurrNode;
						pCurrNode = pCurrNode.m_pNextNoRef;
						RemoveTimeline(pTimeline, pNodeToRemoveNoRef);
					}
					else
					{
#if !HAS_UNO
						// We want to check for whether or not the current timeline is finite only if the following conditions are met:
						//
						// 1. We want to check for animations having completed in the first place;
						// 2. We haven't already found a finite animation;
						// 3. The current timeline is active; and
						// 4. The current timeline actually is an animation.
						//
						if (bCheckForAnimationsComplete &&
							!bHasActiveFiniteAnimations &&
							pTimeline->IsInActiveState() &&
							!pTimeline->OfTypeByIndex<KnownTypeIndex::DispatcherTimer>())
						{
							bHasActiveFiniteAnimations = !!pTimeline->IsFinite();
						}
#endif

						pCurrNode = pCurrNode.m_pNextNoRef;
					}
				}
				else
				{
					pCurrNode = pCurrNode.m_pNextNoRef;
				}
			}

#if !HAS_UNO
			// Run a pass over the hash table to clean up any leftover registered animations
			// TODO Uno: The ConditionalWeakTable drops expired entries by itself.
#endif
		}
		finally
		{
			m_pTimelinePreviousHead = m_pSnappedTimelineHeadNoRef;
			m_pSnappedTimelineHeadNoRef = null;
#if !HAS_UNO
			m_processIATargets = FALSE;
#endif
		}
	}

#if !HAS_UNO
	// TODO Uno: NOT PORTED - CTimeManager::TickTimers: DispatcherTimer is not a timeline in Uno.
	// TODO Uno: NOT PORTED - CTimeManager::UpdateAnimationEvents: the HasAnimation/AnimationComplete test automation events.
#endif

	//------------------------------------------------------------------------
	//
	//  Synopsis:
	//      Add a Timeline to the list of objects we tick
	//
	//------------------------------------------------------------------------
	internal void AddTimeline(Timeline? pTimeline)
	{
		if (pTimeline == null)
		{
			return;
		}

		// Ensure we have a root time group
		m_pRootTimeline ??= new RootTimeline();

		var pNewNode = new TimelineListNode();

		// set timing parent on the child timeline
		// Note: Although pTimeline's timing parent is set to the root timeline, it's not added to the root timeline's child collection.
		pTimeline.SetTimingParent(m_pRootTimeline);
		pTimeline.OnAddToTimeManager();

		// update pointers and addref appropriately
		pNewNode.m_pTimeline = pTimeline;
		// note: ticking with the 'newTimelines' option relies on inserting new nodes at the front
		InsertNodeAtHead(pNewNode, m_pTimelineListHead);
		m_pTimelineListHead = pNewNode;

		// Do not update m_pSnappedTimelineHeadNoRef. This new timeline has already missed out on being ticked,
		// and should be in front of the marker for the ticked tail end of the list.
	}

	internal void RemoveTimeline(Timeline pTimeline) => RemoveTimeline(pTimeline, null);

	//------------------------------------------------------------------------
	//
	//  Synopsis:
	//      Remove a timeline from the list of objects we tick.  This takes a
	//      second optional parameter which is the node to start the search
	//      on (typically the node of the timeline being removed).
	//
	//------------------------------------------------------------------------
	private void RemoveTimeline(Timeline pTimeline, TimelineListNode? pCurrNode)
	{
#if !HAS_UNO
		// Record whether the timeline being removed had independent animations.
		if (pTimeline->HasIndependentAnimation())
		{
			NotifyIndependentAnimationChange();
		}
#endif

		if (pCurrNode == null)
		{
			pCurrNode = m_pTimelineListHead;
		}

		// Find and remove the timeline from the root time group.
		while (pCurrNode != null)
		{
			if (pTimeline == pCurrNode.m_pTimeline)
			{
				if (pCurrNode.m_pPreviousNoRef != null)
				{
					pCurrNode.m_pPreviousNoRef.m_pNextNoRef = pCurrNode.m_pNextNoRef;
				}

				if (pCurrNode.m_pNextNoRef != null)
				{
					pCurrNode.m_pNextNoRef.m_pPreviousNoRef = pCurrNode.m_pPreviousNoRef;
				}

				if (m_pTimelineListHead == pCurrNode)
				{
					global::System.Diagnostics.Debug.Assert(m_pTimelineListHead.m_pPreviousNoRef == null);
					m_pTimelineListHead = pCurrNode.m_pNextNoRef;
					global::System.Diagnostics.Debug.Assert(pCurrNode.m_pPreviousNoRef == null);
				}

				if (m_pSnappedTimelineHeadNoRef == pCurrNode)
				{
					// The node where we started ticking the time manager has been removed. Update it to point to the next node
					// in the list. When we're done ticking, this will become the marker for the tail of the list that has
					// already been ticked.
					m_pSnappedTimelineHeadNoRef = pCurrNode.m_pNextNoRef;
				}

				if (m_pTimelinePreviousHead == pCurrNode)
				{
					// The previous head is a marker used stop walking the list when ticking only new animations.
					// It's allowed to have a previous node.
					m_pTimelinePreviousHead = pCurrNode.m_pNextNoRef;
				}

#if HAS_UNO
				// The node may still be referenced by the tick loop: unhook it so it cannot be ticked again.
				pCurrNode.m_pTimeline = null;
#endif
				pCurrNode = null;

				// set Timing parent to NULL and notify Timeline
				pTimeline.SetTimingParent(null);
				pTimeline.OnRemoveFromTimeManager();
				break;
			}

			pCurrNode = pCurrNode.m_pNextNoRef;
		}
	}

#if !HAS_UNO
	// TODO Uno: NOT PORTED - ShouldCollectIATargets, CollectIATargets, UpdateAllIATargets, UpdateIATargets, UpdateIATarget,
	// NotifyIndependentAnimationChange and ResetIndependentTimelinesChanged: independent (composition) animation targets.
#endif

	internal bool HasActiveAnimations()
	{
		if (HasActiveTimelines())
		{
			// Check there is at least one timeline which is actually an animation (as opposed to a timer).
			// TODO Uno: DispatcherTimer is not a timeline in Uno, every timeline is an animation.
			return true;
		}
		else
		{
			return false;
		}
	}

	internal void StopAllTimelinesAfterTest()
	{
		while (m_pTimelineListHead != null)
		{
			// The reference from m_pTimelineListHead may be the last reference on this timeline. Don't let the timeline delete itself
			// in the middle of calling Stop.
			var timeline = m_pTimelineListHead.m_pTimeline!;
			if (timeline is Storyboard storyboard)
			{
				storyboard.Stop();    // Stop will call RemoveTimeline
			}
			else
			{
				RemoveTimeline(timeline, null);
			}
		}

#if !HAS_UNO
		// Clear the list of objects to go explicitly update. Since the test is ending, we expect all these expressions to be closed.
		// Updating them will return an error.
		m_targetDOs.clear();
#endif
	}
}

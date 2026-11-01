// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference storyboard.cpp, tag winui3/release/2.5.1, commit ba3a8d59e

#nullable enable

using System;

namespace Microsoft.UI.Xaml.Media.Animation;

partial class Storyboard
{
	public Storyboard()
	{
#if HAS_UNO
		Children = new TimelineCollection(owner: this, isAutoPropertyInheritanceEnabled: false);
#endif
	}

	private protected override void UpdateAnimation(
		in ComputeStateParams parentParams,
		ref ComputeStateParams myParams,
		double beginTime,
		DurationType durationType,
		float durationValue,
		in OptionalDouble expirationTime)
	{
		// TODO Uno: The EndStoryboard ETW tracing is not ported.

		ParallelTimelineUpdateAnimation(
			parentParams,
			ref myParams,
			beginTime,
			durationType,
			durationValue,
			expirationTime);
	}

	private protected override void ComputeStateImpl(
		in ComputeStateParams parentParams,
		ref ComputeStateParams myParams,
		ref bool pHasNoExternalReferences,
		bool hasExternalReferencesTracking)
	{
		// Stopped storyboards are skipped over.
		if (m_fIsStopped)
		{
			global::System.Diagnostics.Debug.Assert(!m_fIsPaused && !m_fIsResuming && !m_fIsBeginning && !m_fIsSeeking);
		}
		else
		{
			if (m_fIsPaused && !myParams.isPaused)
			{
				myParams.isPaused = true;
			}

			if (parentParams.hasTime)
			{
				var wasSeeking = m_fIsSeeking;

				// The effective parent time is the adjusted time at which this storyboard will be computed.
				var effectiveParentParams = parentParams;

				// If this Storyboard hasn't been ticked in the timing tree yet, snap its initial parent time now.
				// Storyboards that Begin and Pause immediately, for example, will Pause at the first tick.
				// Note that when SeekAlignedToLastTick is called before the storyboard starts, m_lastParentTime
				// will remain invalid.
				if (m_lastParentTime == double.MaxValue)
				{
					m_lastParentTime = parentParams.time;
				}

				// If this is the first tick of a top-level storyboard after Begin, snap its time
				// delta relative to the parent's time now so this becomes the initial tick time.
				if (m_fIsBeginning)
				{
					// The SB might be paused, resuming, or seeking as well as beginning.
					global::System.Diagnostics.Debug.Assert(IsTopLevelStoryboard());
					m_rTimeDelta = -parentParams.time;
					m_fIsBeginning = false;
				}

				// If the storyboard is paused, the last snapped parent time is when the paused occurred.
				var rPauseStartTime = m_lastParentTime;

				// If paused, use the pause start time instead of the current parent time to compute state.
				// If not paused, keep the snapped parent time value up-to-date.
				if (m_fIsPaused)
				{
					// The SB might have been beginning, and might be seeking as well as paused.
					global::System.Diagnostics.Debug.Assert(IsTopLevelStoryboard());
					global::System.Diagnostics.Debug.Assert(!m_fIsResuming);
					global::System.Diagnostics.Debug.Assert(rPauseStartTime >= 0.0);
					effectiveParentParams.time = rPauseStartTime;

					// NOTE: There's a potential issue where a Pause occurs exactly one tick after a Begin.  In that case,
					//       the independent time delta will have been snapped, but the last tick time (which is zero) will
					//       not yet include it.  The pause time will then end up being zero minus the independent time delta
					//       instead of just zero.  However, CTimeline::ComputeState clamps the current time to zero on the low
					//       end, so this isn't a practical concern.
				}
				else
				{
					m_lastParentTime = parentParams.time;
				}

				// If the storyboard is resuming, increase the time delta from the parent timeline by the time spent paused.
				if (m_fIsResuming)
				{
					// The SB might have been beginning, and might be seeking as well as resuming.
					global::System.Diagnostics.Debug.Assert(IsTopLevelStoryboard());
					global::System.Diagnostics.Debug.Assert(!m_fIsPaused);
					global::System.Diagnostics.Debug.Assert(rPauseStartTime >= 0.0);
					global::System.Diagnostics.Debug.Assert(rPauseStartTime <= parentParams.time);
					m_rTimeDelta += rPauseStartTime - parentParams.time;
					m_fIsResuming = false;
				}

				// Calculate a time delta so that the result of adjusting the parent time is exactly the seeked time.
				if (m_fIsSeeking)
				{
					// The SB might have been beginning, paused, or resuming in addition to seeking.
					global::System.Diagnostics.Debug.Assert(IsTopLevelStoryboard());
					global::System.Diagnostics.Debug.Assert(m_rPendingSeekTime != double.MaxValue);

					// If the SB has a valid parent time, the seek can complete by adjusting the time delta from the parent to the seeked time.
					// Otherwise, this is a SeekAlignedToLastTick call before the SB was actually ticked, which is special-cased later.
					if (m_lastParentTime != double.MaxValue)
					{
						global::System.Diagnostics.Debug.Assert(effectiveParentParams.time >= 0.0);
						m_rTimeDelta = m_rPendingSeekTime - effectiveParentParams.time;
						m_fIsSeeking = false;
					}
				}
				else
				{
					// m_lastParentTime is always initialized on the first tick of the storyboard.
					// ComputeState can only be called before the first tick via SeekAlignedToLastTick,
					// so if we weren't seeking it must be valid.
					global::System.Diagnostics.Debug.Assert(m_lastParentTime >= 0.0 && m_lastParentTime != double.MaxValue);
				}

				// Adjust for the storyboard's difference in time from its parent's time.
				if (m_fIsSeeking)
				{
					// Special case for SeekAlignedToLastTick calls before the SB was ever ticked from the time manager.
					// In this case, we don't have a valid parent time yet so we can't account for the seek time by adjusting
					// our time delta from the parent.  Instead, we use the seek time directly for this ComputeState, so the
					// values get set correctly for the synchronous call, but we remain in the seeking state until the first
					// real tick where the seek adjustment will be incorporated into the stored time delta as usual.
					effectiveParentParams.time = m_rPendingSeekTime;
				}
				else
				{
					// TODO: HWPC: This calculation induces floating point error that can grow the longer the app has been running.
					// TODO: HWPC: This is especially noticeable when calling SeekPrivate(t_seek). The following calculation:
					// TODO: HWPC: m_rTimeDelta = m_rPendingSeekTime - rEffectiveParentTime
					// TODO: HWPC: rEffectiveParentTime = rEffectiveParentTime + m_rTimeDelta
					// TODO: HWPC: is expected to return rEffectiveParentTime == m_rPendingSeekTime. However, the
					// TODO: HWPC: magnitude of the parent time and time delta can be much larger than the seek
					// TODO: HWPC: time, causing the result to differ by more than IsCloseReal allows.
					global::System.Diagnostics.Debug.Assert(m_rTimeDelta != double.MaxValue);
					effectiveParentParams.time += m_rTimeDelta;
				}

				// Time adjustments should never be necessary for nested storyboards.
				global::System.Diagnostics.Debug.Assert(IsTopLevelStoryboard() || m_rTimeDelta == 0.0);

				//
				// Due to floating point errors, sometimes the effective parent time is slightly before the begin time when
				// it should be equal. Include a tolerance in calculations to allow these storyboards to begin. Currently most
				// of the error comes from converting from double to float, but even switching entirely to float will not
				// solve the problem without including a tolerance.
				//
				// We only make this adjustment for root storyboards, since they're the only ones with independent start deltas.
				// We also want to avoid snapping the time forward in each level of nested storyboards.
				//
				if (this == GetRootTimingParent())
				{
					var rBeginTime = m_pBeginTime?.TotalSeconds ?? 0.0;
					var secondsToBeginTime = rBeginTime - effectiveParentParams.time;
					if (0.0 < secondsToBeginTime && secondsToBeginTime < s_timeTolerance
						&& (!wasSeeking || m_rPendingSeekTime >= rBeginTime))
					{
						effectiveParentParams.time = rBeginTime;
					}
				}

				// Compute local state
				TimelineGroupComputeStateImpl(
					effectiveParentParams,
					ref myParams,
					ref pHasNoExternalReferences,
					hasExternalReferencesTracking);
			}
			else
			{
				// Top-level storyboards should always be provided a valid parent time from the TimeManager.
				// Nested storyboards might not have a parent time, in the case where their parent timeline has not started yet.
				global::System.Diagnostics.Debug.Assert(!IsTopLevelStoryboard());

				// The effective parent time is the adjusted time at which this storyboard will be computed.
				var parentParamsNoTime = parentParams;
				parentParamsNoTime.hasTime = false;

				// Compute local state
				TimelineGroupComputeStateImpl(
					parentParamsNoTime,
					ref myParams,
					ref pHasNoExternalReferences,
					hasExternalReferencesTracking);
			}
		}
	}

	// Pauses an unpaused storyboard.  No-ops if stopped.
	private void PausePrivate()
	{
		if (!m_fIsStopped && !m_fIsPaused)
		{
			// When paused, the parent time no longer moves forward during ComputeState.
			m_fIsPaused = true;
			m_fIsResuming = false;

			// There's no need to notify the time manager of a timing change for pausing here, since the pause is
			// handled on the next tick.  If the storyboard was independent and is paused on the next tick, it'll be
			// removed from the time manager and we'll detect the independent-animation change there.

			// Request a tick to update the now-inactive storyboard.
			// The browser host and/or frame scheduler can be NULL during shutdown.
			TimeManager.RequestAdditionalFrame(0 /* immediate */);
		}

#if !HAS_UNO
		// We requested another tick, which will process the pause and removes the timeline from the time manager.
		// That will mark an independent timeline change, which will cause us to commit the DComp device.
		AddPendingDCompPause();
#endif
	}

	// Resumes a paused storyboard.  No-ops if stopped.
	private void ResumePrivate()
	{
		var pTimeManager = TimeManager.Instance;

		if (!m_fIsStopped && m_fIsPaused)
		{
			// When resumed, the time delta from the parent timeline will be adjusted on the next tick.
			m_fIsPaused = false;
			m_fIsResuming = true;

			// Add the Storyboard to the timing tree, if necessary.
			if (GetTimingParent() == null)
			{
				pTimeManager.AddTimeline(this);
			}

			// Request a tick to update the now-active storyboard.
			// The browser host and/or frame scheduler can be NULL during shutdown.
			TimeManager.RequestAdditionalFrame(0 /* immediate */);
		}

#if !HAS_UNO
		AddPendingDCompResume();
#endif
	}

	// Starts the current Storyboard from the beginning.  Overrides any previous stops/pauses/seeks.
	internal void BeginPrivate(bool fIsTopLevel)
	{
		try
		{
			var pTimeManager = TimeManager.Instance;

			// Add the Storyboard to the timing tree if necessary.
			if (GetTimingParent() == null)
			{
				pTimeManager.AddTimeline(this);
			}

			// We need to reset internal iteration helpers by re-initializing
			m_clockState = TimelineClockState.NotStarted;
			ResetCompletedEventFired();

			// Notify child timelines as needed.  It's important that this happens before we reset
			// the rest of the storyboard's state for ticking, e.g. m_fIsStopped, because notifying child
			// timelines might modify the timeline tree, which can only occur when stopped.
			foreach (var pTimeline in Children.Items)
			{
				try
				{
					// Tell children nodes that we are beginning
					// Compute begin behavior for child nodes ...
					pTimeline.OnBegin();
				}
				catch
				{
					// Make sure we destroy any references
					FinalizeIteration();
					throw;
				}
			}

			// Top-level storyboards need to offset time from their parent timeline (the time manager).
			// Nested timelines inherit their parent's time and need no adjustment.
			//
			// Since the framework ticks only as needed, the last tick time is not reliably close to
			// the actual time when the Begin API is called.  It's also not acceptable to use the
			// actual wall clock time, since multiple storyboards started in one callback are expected
			// to start simultaneously.  The only option left is to snap the time at the next tick instead,
			// where the storyboard will actually start.
			m_rTimeDelta = 0.0;
			m_fIsBeginning = fIsTopLevel;

			// Reset the last parent time to put the Storyboard back into its initial state, as though it hasn't ticked yet.
			// This is okay because any Pause before the Begin is cancelled, and any subsequent Pause should work just like
			// when the Storyboard was first added to the timing tree.
			m_lastParentTime = double.MaxValue;

			// Reset other state.  Begin removes the paused state and overrides pending resumes and seeks.
			m_fIsStopped = false;
			m_fIsPaused = false;
			m_fIsResuming = false;
			m_fIsSeeking = false;

			// Request a tick to update the now-active storyboard.
			// The browser host and/or frame scheduler can be NULL during shutdown.
			TimeManager.RequestAdditionalFrame(0 /* immediate */);

			if (fIsTopLevel)
			{
				if (m_fAutoComplete && !m_fIsStopped)
				{
					CompleteInternal(true, true);
				}
			}

			InitializeIteration();
		}
		catch
		{
			// Reset our state if we fail to begin so that completed doesn't fire on error
			m_fIsStopped = true;
			m_IsCompletedEventFired = true;
			throw;
		}
	}

	// Stops the current storyboard and does not fill. Overrides previous begins/pauses/seeks.
	internal void StopPrivate()
	{
		m_fIsStopped = true;
		m_fIsPaused = false;
		m_fIsResuming = false;
		m_fIsBeginning = false;
		m_fIsSeeking = false;
		m_lastParentTime = double.MaxValue;
		m_rPendingSeekTime = double.MaxValue;
		m_rTimeDelta = double.MaxValue;

		if (m_clockState != TimelineClockState.Stopped)
		{
			var pTimeManager = TimeManager.Instance;

			m_clockState = TimelineClockState.Stopped;

			if (pTimeManager.IsRunning())
			{
				// Force all timing subtree back to the base value
				FinalizeIteration();

#if !HAS_UNO
				// WinRT DComp will still fire Completed events if the animations are explicitly stopped. Make sure we unattach from
				// the events and clear out the map in CTimeManager so that Xaml doesn't think the WUC animations completed.
				DetachDCompCompletedHandlerOnStop();
#endif

				// If we are running, we should also remove ourselves from the time manager.
				// If this Storyboard was independently animating, it'll notify the time manager here.
				pTimeManager.RemoveTimeline(this);

				// Request a tick to update the now-inactive storyboard.
				// The browser host and/or frame scheduler can be NULL during shutdown.
				TimeManager.RequestAdditionalFrame(0 /* immediate */);
			}
		}
	}

	// Moves the current time to a specified interval.  No-ops if stopped.
	private void SeekPrivate(double pSeekTime)
	{
		SeekInternal(pSeekTime);

#if !HAS_UNO
		AddPendingDCompSeek(pSeekTime->m_rTimeSpan);
#endif
	}

	// Moves the current time to a specified interval.  No-ops if stopped.
	private void SeekInternal(double pSeekTime)
	{
		var pTimeManager = TimeManager.Instance;

		if (!m_fIsStopped)
		{
			// When we seek to a given time, we update our offset from the parent on the next tick
			// to exactly the time seeked.
			// Note that if we have a nonzero BeginTime, this will seek off the logical zero
			// time of the animation:
			//    e.g. if BeginTime=2sec, and you seek by 1sec,
			//    you will be 1sec before the active period and not 1sec into it.
			m_rPendingSeekTime = pSeekTime;
			m_fIsSeeking = true;

			if (GetTimingParent() == null)
			{
				pTimeManager.AddTimeline(this);
			}

			// Request a tick to update the storyboard.
			// The browser host and/or frame scheduler can be NULL during shutdown.
			TimeManager.RequestAdditionalFrame(0 /* immediate */);
		}
	}

	// Moves the current time to a specified interval and forces an immediate update.
	internal void SeekAlignedToLastTick(double pSeekTime)
	{
		// First, Seek normally. Auto-completion is handled later in this method, if necessary.
		SeekInternal(pSeekTime);

#if !HAS_UNO
		// In addition to requesting a pending seek for later, also do a synchronous DComp animation seek now.
		SeekDCompAnimationInstances(pSeekTime->m_rTimeSpan);
		AddPendingDCompSeek(pSeekTime->m_rTimeSpan);
#endif

		// Now, update the timing tree at the new position.
		{
			// The browser host and/or frame scheduler can be nullptr during shutdown.
			var pScheduler = TimeManager.Instance.GetFrameScheduler();

			var parentParams = new ComputeStateParams();
			parentParams.hasTime = true;
			parentParams.time = m_lastParentTime;
			parentParams.pFrameSchedulerNoRef = pScheduler;
			parentParams.isReversed = false;    // default since this is a top-level storyboard
			parentParams.isPaused = m_fIsPaused;

			// Synchronously re-compute the values for the seeked time, but
			// do not move the clock forward.
			ComputeState(parentParams);
		}

		// Queue up another Seek. Our first seek was resolved by the synchronous ComputeState call,
		// which computed a time delta that gave a local time of pSeekTime. However, since that
		// synchronous call wasn't called with the real global time, but instead with m_lastParentTime,
		// there's no guarantee that the time delta we calculated is actually correct with respect to
		// the real global clock. By queuing up another seek, we update the time delta again on the
		// next UI thread frame using the real global clock.
		//
		// e.g. The last time that this storyboard ticked was at global time 8, so m_lastParentTime = 8.
		// We then went idle for about 92 frames, and the global time is now 100. Somebody calls
		// SeekATLT(0), and we call ComputeState with the global time = m_lastParentTime = 8. That means
		// we set the time delta to (seek time - global time) = 0 - 8 = -8, and calculate the local time
		// to be (global time + time delta) = 8 - 8 = 0, as required. However, on the next UI thread frame,
		// this storyboard ticks with the real global time of 101, and it calculates the local time to be
		// (global + delta) = 101 - 8 = 93. This is way past the end of the storyboard, so we go inactive.
		// Instead we should be ticking in the beginning of the storyboard. The fix is to queue another
		// seek, so that the next UI thread tick will update the time delta to be (seek - global) =
		// (0 - 101) = -101, and the local time will be (global + delta) = 101 - 101 = 0. The next tick at
		// global time 102 will have local tie (global + delta) = 102 - 101 = 1, and this storyboard can
		// tick forward normally.
		SeekInternal(pSeekTime);
	}

	// Synchronously moves the current time to the fill portion of the storyboard.
	// No-ops for Storyboard's with no finite expiration time, or Storyboards that are not
	// running.
	private void SkipToFillPrivate()
	{
		if (!m_fIsStopped)
		{
			CompleteInternal(
				false /*stopInfiniteTimelines*/,
				true /*isSynchronous*/);
		}
	}

	// Seeks to the end if our duration is finite. Stops the storyboard if it's infinite.
	internal void Complete()
		=> CompleteInternal(
			true /*stopInfiniteTimelines*/,
			false /*isSynchronous*/);

	// Seeks to the end if our duration is finite. This can happen synchronously, or on the
	// next tick. Infinite storyboards are either stopped or ignored.
	private void CompleteInternal(bool stopInfiniteTimelines, bool isSynchronous)
	{
		// Assume the storyboard is finite - it we discover otherwise we'll handle that at the end.
		var isFinite = true;

		GetNaturalDuration(out var durationType, out var duration);

		if (durationType == DurationType.TimeSpan)
		{
			// Calculate the true end time of this storyboard and seek to it, taking into account
			// the effect of all timeline properties on our natural duration.
			var beginTime = m_pBeginTime?.TotalSeconds ?? 0.0;

			ComputeExpirationTime(
				beginTime,
				durationType,
				duration,
				m_rSpeedRatio,
				out var expirationTime);

			if (expirationTime.m_fHasValue)
			{
				// This storyboard has a finite expiration time, allowing us to seek to its end time.
				// There is floating point error in the time calculation, however, so seeking exactly to the
				// end time can result in some Active frames. Seeking to any positive value past the expiration
				// time will work, so to avoid this we seek to positive infinity instead.
				//
				// NOTE: We use XFLOAT_MAX for 'unset' values in Storyboard calculations.
				//           XFLOAT_INF is _not_ treated the same way - it is a valid number for calculating time.
				var pTimeSpan = (double)float.PositiveInfinity;

				if (isSynchronous)
				{
					SeekAlignedToLastTick(pTimeSpan);
				}
				else
				{
					SeekInternal(pTimeSpan);
				}
			}
			else
			{
				// This storyboard has a finite natural duration, but an infinite expiration time (e.g. RepeatBehavior=DirectUI::DurationType::Forever).
				isFinite = false;
			}
		}
		else
		{
			// This storyboard has an infinite natural duration (e.g. Duration=DirectUI::DurationType::Forever).
			isFinite = false;
		}

		// If requested, StopPrivate() infinite storyboards. Otherwise, the call will no-op if infinite.
		if (!isFinite && stopInfiniteTimelines)
		{
			StopPrivate();
		}
	}

	// Moves the current time to a specified interval on the next tick.
	// This wrapper is called by the public pInvoke to do argument checking, etc.
	private void SeekCore(double seekTime)
	{
		// Make sure only root storyboards get called on this
		EnsureIsRootStoryboard();

		SeekPrivate(seekTime);

		// TODO Uno: FlushDeferredAnimationOperationQueue - Uno doesn't defer custom property animation values.
	}

	// Moves the current time to a specified interval and forces an immediate update.
	// This wrapper is called by the public pInvoke to do argument checking, etc.
	private void SeekAlignedToLastTickPublic(double seekTime)
	{
		// Make sure only root storyboards get called on this
		EnsureIsRootStoryboard();

		SeekAlignedToLastTick(seekTime);
	}

	// Synchronously moves the current time to the fill portion of the storyboard.
	// No-ops for Storyboard's with no finite expiration time.
	private void SkipToFillPublic()
	{
		// Make sure only root storyboards get called on this
		EnsureIsRootStoryboard();

		SkipToFillPrivate();
	}

	// Static wrapper to Pause the Storyboard
	private void PauseCore()
	{
		// Make sure only root storyboards get called on this
		EnsureIsRootStoryboard();

		PausePrivate();
	}

	// Wrapper to Resume the Storyboard
	private void ResumeCore()
	{
		// Make sure only root storyboards get called on this
		EnsureIsRootStoryboard();

		ResumePrivate();
	}

	// Wrapper to Stop the Storyboard
	internal void StopCore()
	{
		// Make sure only root storyboards get called on this
		EnsureIsRootStoryboard();

		StopPrivate();

#if !HAS_UNO
		// If there are pending DComp pauses or seeks, clear them out so we won't apply them when creating animation instances.
		ClearPendingDCompAnimationOperationsRecursive();
#endif
	}

	// Wrapper to Begin the Storyboard
	private void BeginCore()
	{
		// Make sure only root storyboards get called on this
		// if Begin is called on a non-timeMgr rooted SB, then it must be unparented
		if (!IsTopLevelStoryboard() && GetTimingParent() != null)
		{
			throw new InvalidOperationException("Operation is not valid on a child Storyboard.");
		}

		// At this point we can only begin top-level storyboards - let the Begin
		// call know that.
		BeginPrivate(true /* fIsTopLevel */);

#if !HAS_UNO
		// If there are pending DComp pauses or seeks, clear them out so we won't apply them when creating animation instances.
		ClearPendingDCompAnimationOperationsRecursive();
#endif
	}

	// Returns true if this Storyboard is the root of its timing tree.
	// As implemented, this means it gets its time directly from the time manager.
	private bool IsTopLevelStoryboard() => IsTopLevelTimeline();

	// Returns the duration of the storyboard in seconds, and the duration type
	internal void GetDuration(out DurationType pDurationType, out float prDuration)
	{
		var durationType = DurationType.Automatic;
		var rDuration = 0.0f;

		if (m_duration.Type == DurationType.Automatic)
		{
			GetNaturalDuration(out durationType, out rDuration);
		}
		else
		{
			durationType = m_duration.Type;
			rDuration = (float)m_duration.TimeSpan.TotalSeconds;
		}

		switch (durationType)
		{
			case DurationType.TimeSpan:
				if (m_fAutoReverse)
				{
					rDuration *= 2.0f;
				}

				// Account for RepeatBehavior
				switch (m_repeatBehavior.Type)
				{
					case RepeatBehaviorType.Forever:
						// storyboard repeats forever (override previous value)
						rDuration = float.MaxValue;
						durationType = DurationType.Forever;
						break;

					case RepeatBehaviorType.Duration:
						// We are giving a specific repeat behavior
						//   which either clips our duration or extends our repeat time.
						rDuration = (float)m_repeatBehavior.Duration.TotalSeconds;
						break;

					case RepeatBehaviorType.Count:
					default:
						// If we have a count, use it - this defaults to 1.0
						rDuration *= (float)m_repeatBehavior.Count;
						break;
				}

				prDuration = rDuration;
				break;

			case DurationType.Forever:
				prDuration = float.MaxValue;
				break;

			case DurationType.Automatic:
			default:
				// Automatic means that the subtree could not be resolved ...
				// At the storyboard level, we should always be able to compute the duration.
				throw new InvalidOperationException();
		}

		pDurationType = durationType;
	}

	// CoreImports::Storyboard_SetTarget
	internal static void Storyboard_SetTarget(Timeline pTimeline, DependencyObject pDO)
	{
		if (pTimeline is null)
		{
			throw new ArgumentNullException(nameof(pTimeline));
		}

		if (pDO is null)
		{
			throw new ArgumentNullException(nameof(pDO));
		}

		pTimeline.SetTargetObject(pDO);
	}

#if !HAS_UNO
	// TODO Uno: GetStandardNameScopeParent: if we're not in the visual tree, see if we have a mentor instead.
	// Uno resolves names from the element tree (see Timeline.ResolveName).
#endif

	// StoryboardHelpers::ZeroSeekAlignedToLastTick
	internal static void ZeroSeekAlignedToLastTick(Storyboard pStoryboard) => pStoryboard.SeekAlignedToLastTick(0.0);

	// StoryboardHelpers::EndSeekAlignedToLastTick
	internal static void EndSeekAlignedToLastTick(Storyboard pStoryboard) => pStoryboard.SeekAlignedToLastTick(float.PositiveInfinity);

	// StoryboardHelpers::ModifyStoryboard
	internal static void ModifyStoryboard(Storyboard? storyboard, bool forceStart, Action modifyFunc)
	{
		if (storyboard is null)
		{
			// If no storyboard, just run the modification function
			modifyFunc();
			return;
		}

		// Grab the current time before we stop the storyboard. We must stop it in order
		// to modify it.
		var wasStopped = storyboard.IsInStoppedState();
		var wasPaused = storyboard.IsPaused();
		var currentTime = storyboard.m_rCurrentTime;

		if (!wasStopped)
		{
			storyboard.StopCore();
		}

		modifyFunc();

		if (!wasStopped || forceStart)
		{
			try
			{
				storyboard.BeginCore();
			}
			catch (InvalidOperationException)
			{
			}
		}

		storyboard.SeekAlignedToLastTick(currentTime);

		if (wasPaused)
		{
			storyboard.PauseCore();
		}
	}

	private void EnsureIsRootStoryboard()
	{
		var pParent = GetTimingParent();
		var pRoot = TimeManager.Instance.GetRootTimeline();

		// if called on a non-timeMgr rooted SB, then it must be unparented
		if ((pParent != pRoot) && (pParent != null))
		{
			throw new InvalidOperationException("Operation is not valid on a child Storyboard.");
		}
	}
}

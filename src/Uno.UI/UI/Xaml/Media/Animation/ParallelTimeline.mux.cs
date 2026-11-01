// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference ParallelTimeline.cpp, tag winui3/release/2.5.1, commit ba3a8d59e

#nullable enable

using System;

namespace Microsoft.UI.Xaml.Media.Animation;

// TODO Uno specific: CParallelTimeline has no public counterpart; Storyboard is its only Uno subclass.
partial class Storyboard
{
	// Recursively fire Complete event for itself and children Timelines.
	internal override void FireCompletedEvent()
	{
		global::System.Diagnostics.Debug.Assert(!IsCompletedEventFired());

		foreach (var pTimeline in Children.Items)
		{
			if (!pTimeline.IsCompletedEventFired())
			{
				pTimeline.FireCompletedEvent();
			}
		}

		TimelineFireCompletedEvent();
	}

	// Handles scheduling frames and firing the Completed event.
	// TODO: ANIM: This common implementation should probably move all the way down to Timeline (where the Completed event is defined), but I didn't want to impact DispatcherTimer or the root timelines for now.
	// CParallelTimeline::UpdateAnimation
	private void ParallelTimelineUpdateAnimation(
		in ComputeStateParams parentParams,
		ref ComputeStateParams myParams,
		double beginTime,
		DurationType durationType,
		float durationValue,
		in OptionalDouble expirationTime)
	{
		switch (m_clockState)
		{
			case TimelineClockState.NotStarted:
				if (parentParams.pFrameSchedulerNoRef != null)
				{
					// Request a tick to the active period when running forwards.
					RequestTickToActivePeriodBegin(parentParams, myParams, beginTime);
				}
				break;

			case TimelineClockState.Stopped:
				// We only want to call any event handlers and/or finalization code if we actually started once
				if (m_fInitialized)
				{
					// We only want this to be fired once on the last transition
					// There could be the case when the previous state was Stopped and current state was so late that
					// it got computed stopped, in such case we need to make sure that we fire the completed event.
					if (!m_IsCompletedEventFired)
					{
						FireCompletedEvent();
					}

					FinalizeIteration();
				}

				if (parentParams.pFrameSchedulerNoRef != null)
				{
					// Request a tick to the active period when running in reverse.
					RequestTickToActivePeriodEnd(parentParams, myParams, expirationTime);
				}
				break;

			case TimelineClockState.Active:
				global::System.Diagnostics.Debug.Assert(parentParams.hasTime);
				global::System.Diagnostics.Debug.Assert(parentParams.time >= beginTime);

				// The natural duration should always be a finite timespan or infinite duration.
				// Automatic durations will have been resolved to one or the other.
				global::System.Diagnostics.Debug.Assert(durationType == DurationType.TimeSpan || durationType == DurationType.Forever);

				if (CanRequestTicksWhileActive() && parentParams.pFrameSchedulerNoRef != null && !myParams.isPaused)
				{
					// ParallelTimelines don't need to constantly tick while active. The frequency of ticks in a single iteration
					// is determined by the animation children of the timeline, if any. The ParallelTimeline does have to
					// tick at the beginning of each iteration, however, because the animation children could be 'Filling' in
					// the current iteration, and not requesting ticks, but will restart in 'Active' mode in the next iteration.
					// The ParallelTimeline also needs to ensure it requests a tick for the time it expires, so that we can fire
					// the Completed event, even if there are no animation children. Since the expiration time will also be at
					// the terminus of an iteration, it's covered by the same logic.

					// The timeline only has meaningful endpoints for each iteration if it is finite.
					if (durationType == DurationType.TimeSpan)
					{
						var timeElapsedInIteration = XcpMod(m_rCurrentTime, durationValue);

						// Note that we use the combined 'isInReverse' flag, including the local AutoReverse value,
						// to match that the inputs to this calculation are in local time units.
						var timeRemainingInIterationInSeconds = myParams.isReversed
																 ? timeElapsedInIteration
																 : durationValue - timeElapsedInIteration;

						// Note that if the timeline ends up exactly at (or over, due to floating point error) the end of an
						// iteration and is still active, it still requests a frame. This guarantees that we always tick at the
						// beginning of the next iteration, so any child timelines have a chance to restart.
						timeRemainingInIterationInSeconds = Math.Max(0.0f, timeRemainingInIterationInSeconds);

						parentParams.pFrameSchedulerNoRef.RequestAdditionalFrame(
							XcpCeiling(timeRemainingInIterationInSeconds * 1000.0 / myParams.speedRatio));
					}
				}
				break;

			case TimelineClockState.Filling:
				// Also want to send a completed event here so that animations without
				// FillBehavior="Stop" get the events as well
				if (m_fInitialized)
				{
					if (!m_IsCompletedEventFired)
					{
						FireCompletedEvent();
					}
				}

				if (parentParams.pFrameSchedulerNoRef != null)
				{
					// Request a tick to the active period when running in reverse.
					RequestTickToActivePeriodEnd(parentParams, myParams, expirationTime);
				}
				break;

			default:
				global::System.Diagnostics.Debug.Fail("Unsupported state");
				break;
		}

		// Parallel timelines are not independent animations themselves (e.g. Storyboard, DynamicTimeline).
		// They might have independent animation children, which are accounted for in CTimelineGroup::ComputeState.
	}

	// XcpMod (fmod)
	private static double XcpMod(double x, double y) => x % y;
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference: dxaml/xcp/core/animation/storyboard.cpp — CStoryboard::ComputeStateImpl
//                dxaml/xcp/core/animation/ParallelTimeline.cpp — CParallelTimeline::UpdateAnimation
//                dxaml/xcp/core/animation/TimelineGroup.cpp — CTimelineGroup::ComputeStateImpl

#if __SKIA__
using System;
using System.Linq;
using Uno.UI.Xaml.Core;

namespace Microsoft.UI.Xaml.Media.Animation
{
	public partial class Storyboard
	{
		// MUX: m_rTimeDelta — Offset from parent time to local time.
		private double _timeDelta;

		// MUX: m_lastParentTime — Snapped parent time for pause/seek.
		private double _lastParentTime = double.MaxValue;

		// MUX: m_fIsBeginning, m_fIsPaused, m_fIsResuming, m_fIsSeeking, m_fIsStopped
		private bool _isBeginning;
		private bool _isPausedForTimeManager;
		private bool _isResuming;
		private bool _isSeeking;
		private double _pendingSeekTime = double.MaxValue;
		private bool _isStopped;

		// Whether this storyboard is registered with the TimeManager.
		internal bool _isRegisteredWithTimeManager;

		// Whether the Completed event has been fired for this run.
		private bool _completedEventFiredByTimeManager;

		/// <summary>
		/// Computes the natural duration of this Storyboard from its children.
		/// MUX: CParallelTimeline resolves Duration.Automatic to max of children's
		/// effective durations (accounting for RepeatBehavior and AutoReverse).
		/// </summary>
		private TimeSpan GetNaturalDurationFromChildren()
		{
			if (Duration.Type == DurationType.TimeSpan)
			{
				return Duration.TimeSpan;
			}

			// Duration.Automatic: max of children's effective durations.
			var maxDuration = TimeSpan.Zero;
			if (Children is { Count: > 0 })
			{
				for (int i = 0; i < Children.Count; i++)
				{
					var child = Children[i];

					// A child with RepeatBehavior.Forever makes the storyboard infinite.
					// MUX: CParallelTimeline::GetNaturalDuration considers effective duration.
					if (child.RepeatBehavior.Type == RepeatBehaviorType.Forever)
					{
						return TimeSpan.MaxValue;
					}

					var childSingleDuration = child.GetCalculatedDuration();
					var childBeginTime = child.BeginTime ?? TimeSpan.Zero;

					// Compute effective duration considering RepeatBehavior and AutoReverse.
					TimeSpan effectiveDuration;
					var repeat = child.RepeatBehavior;
					if (repeat.HasDuration)
					{
						effectiveDuration = repeat.Duration;
					}
					else if (repeat.HasCount && repeat.Count > 0)
					{
						var scale = repeat.Count;
						if (child.AutoReverse) { scale *= 2.0; }
						effectiveDuration = TimeSpan.FromSeconds(childSingleDuration.TotalSeconds * scale);
					}
					else
					{
						// Default (play once): scale by 2 if AutoReverse, else single duration.
						effectiveDuration = child.AutoReverse
							? TimeSpan.FromSeconds(childSingleDuration.TotalSeconds * 2.0)
							: childSingleDuration;
					}

					var childTotal = childBeginTime + effectiveDuration;
					if (childTotal > maxDuration)
					{
						maxDuration = childTotal;
					}
				}
			}

			return maxDuration;
		}

		/// <summary>
		/// Ported from CStoryboard::ComputeStateImpl (storyboard.cpp lines 94-292)
		/// + CTimelineGroup::ComputeStateImpl (TimelineGroup.cpp lines 137-214)
		/// + CParallelTimeline::UpdateAnimation (ParallelTimeline.cpp lines 45-158).
		/// </summary>
		internal override void ComputeState(ComputeStateParams parentParams)
		{
			if (_isStopped)
			{
				return;
			}

			if (!parentParams.HasTime)
			{
				return;
			}

			var effectiveParentParams = parentParams;

			// --- CStoryboard::ComputeStateImpl: timing delta management ---

			// Snap initial parent time on first tick.
			if (_lastParentTime == double.MaxValue)
			{
				_lastParentTime = parentParams.Time;
			}

			// Snap timing delta on first tick after Begin().
			if (_isBeginning)
			{
				_timeDelta = -parentParams.Time;
				_isBeginning = false;
			}

			// Pause/resume/seek handling.
			var pauseStartTime = _lastParentTime;

			if (_isPausedForTimeManager)
			{
				effectiveParentParams.Time = pauseStartTime;
			}
			else
			{
				_lastParentTime = parentParams.Time;
			}

			if (_isResuming)
			{
				_timeDelta += pauseStartTime - parentParams.Time;
				_isResuming = false;
			}

			if (_isSeeking)
			{
				if (_lastParentTime != double.MaxValue)
				{
					_timeDelta = _pendingSeekTime - effectiveParentParams.Time;
					_isSeeking = false;
				}
			}

			// Apply the timing delta.
			if (_isSeeking)
			{
				effectiveParentParams.Time = _pendingSeekTime;
			}
			else
			{
				effectiveParentParams.Time += _timeDelta;
			}

			// Floating point tolerance: snap to begin time if very close.
			var beginTime = BeginTime?.TotalSeconds ?? 0.0;
			var secondsToBeginTime = beginTime - effectiveParentParams.Time;
			if (secondsToBeginTime > 0.0 && secondsToBeginTime < 0.0005)
			{
				effectiveParentParams.Time = beginTime;
			}

			// --- CTimeline::ComputeStateImpl: compute own clock state ---

			var localTime = effectiveParentParams.Time - beginTime;
			if (localTime < 0)
			{
				_computedClockState = InternalClockState.NotStarted;
				_computedCurrentTime = 0;
				RequestAdditionalFrameIfNeeded();
				return;
			}

			var naturalDuration = GetNaturalDurationFromChildren();
			var singleIterationSeconds = naturalDuration.TotalSeconds;

			// Apply the storyboard's own AutoReverse and RepeatBehavior to compute
			// its effective (total) duration. MUX: CStoryboard::GetDuration().
			var effectiveDurationSeconds = ComputeStoryboardEffectiveDuration(singleIterationSeconds);

			// Compute the iteration-wrapped current time to pass to children.
			// When RepeatBehavior.Count > 1, localTime advances past singleIterationSeconds
			// but children should receive a time in [0, singleIterationSeconds].
			// MUX: CTimeline::ComputeLocalProgressAndTime — iteration wrapping.
			_computedCurrentTime = ComputeWrappedChildTime(localTime, singleIterationSeconds);

			// Determine clock state from effective duration.
			// MUX: Zero-duration timelines expire immediately (progress=1.0).
			// The check uses >= 0 (not > 0) so that zero-duration storyboards
			// transition to Filling/Stopped on the first tick.
			if (effectiveDurationSeconds >= 0 && localTime >= effectiveDurationSeconds - 0.0005)
			{
				// Expired — clamp child time to end of the last iteration.
				_computedCurrentTime = singleIterationSeconds;

				if (FillBehavior == FillBehavior.HoldEnd)
				{
					_computedClockState = InternalClockState.Filling;
				}
				else
				{
					_computedClockState = InternalClockState.Stopped;
				}
			}
			else
			{
				_computedClockState = InternalClockState.Active;
			}

			// --- CTimelineGroup::ComputeStateImpl: propagate to children ---

			if (Children is { Count: > 0 })
			{
				// MUX: myParams.hasTime = m_clockState != NotStarted && root not Stopped
				// MUX: myParams.time = m_rCurrentTime
				var childParams = new ComputeStateParams
				{
					HasTime = _computedClockState != InternalClockState.NotStarted,
					Time = _computedCurrentTime,
					IsPaused = _isPausedForTimeManager,
				};

				for (int i = 0; i < Children.Count; i++)
				{
					Children[i].ComputeState(childParams);
				}
			}

			// --- CParallelTimeline::UpdateAnimation: handle state transitions ---

			switch (_computedClockState)
			{
				case InternalClockState.Active:
					// Ensure continuous VSync-driven ticking while any animation is Active.
					// MUX: CParallelTimeline::UpdateAnimation calls RequestAdditionalFrame via
					// the frame scheduler. In Uno, TimeManager.EnsureTicking subscribes to
					// CompositionTarget.Rendering (VSync-driven, not dispatcher Normal queue)
					// to avoid blocking WaitForIdle/RunIdleAsync.
					TimeManager.Instance.EnsureTicking();
					break;

				case InternalClockState.Filling:
					if (!_completedEventFiredByTimeManager)
					{
						_completedEventFiredByTimeManager = true;

						// Tick children one last time at the fill boundary.
						// Then fire the Completed event.
						// MUX: FireCompletedEvent() (ParallelTimeline.cpp line 143)
						State = TimelineState.Filling;
						OnCompleted();
					}
					break;

				case InternalClockState.Stopped:
					if (!_completedEventFiredByTimeManager)
					{
						_completedEventFiredByTimeManager = true;
						State = TimelineState.Stopped;
						OnCompleted();
					}
					break;
			}
		}

		private void RequestAdditionalFrameIfNeeded()
		{
			// If still not started but has a future begin time, request a frame.
			if (_computedClockState == InternalClockState.NotStarted && !_isStopped)
			{
				CoreServices.RequestAdditionalFrame();
			}
		}

		// Computed state from the latest ComputeState call.
		private InternalClockState _computedClockState = InternalClockState.NotStarted;
		private double _computedCurrentTime;

		/// <summary>
		/// Override to include computed state in the active check.
		/// MUX: CTimeline::IsInActiveState
		///
		/// Filling storyboards must stay active in the TimeManager to re-apply
		/// fill values on every tick. This ensures that if multiple concurrent
		/// animations target the same property, the last one to tick wins
		/// (matching WinUI's behavior where older animations tick last and hold
		/// their fill values over newer animations until handoff occurs).
		/// </summary>
		internal override bool IsInActiveState =>
			base.IsInActiveState
			|| _computedClockState == InternalClockState.Active
			|| _computedClockState == InternalClockState.Filling;

		/// <summary>
		/// Computes the storyboard's effective total duration by applying its own
		/// AutoReverse and RepeatBehavior to the single-iteration natural duration.
		/// MUX: CStoryboard::GetDuration (storyboard.cpp lines 1087-1157).
		/// </summary>
		private double ComputeStoryboardEffectiveDuration(double singleIterationSeconds)
		{
			var scalingFactor = 1.0;

			// Apply own AutoReverse (doubles the single iteration).
			if (AutoReverse)
			{
				scalingFactor *= 2.0;
			}

			// Apply own RepeatBehavior.
			var repeat = RepeatBehavior;
			if (repeat.Type == RepeatBehaviorType.Forever)
			{
				return double.MaxValue;
			}
			if (repeat.HasDuration)
			{
				return repeat.Duration.TotalSeconds;
			}
			if (repeat.HasCount && repeat.Count > 0)
			{
				scalingFactor *= repeat.Count;
			}
			// Default (Count=0): play once → scalingFactor stays 1.0 (or 2.0 for AutoReverse).

			return singleIterationSeconds * scalingFactor;
		}

		/// <summary>
		/// Wraps the storyboard's local time back into [0, singleIterationSeconds]
		/// for repeating and AutoReverse storyboards.
		/// MUX: CTimeline::ComputeLocalProgressAndTime — iteration/AutoReverse logic.
		/// </summary>
		private double ComputeWrappedChildTime(double localTime, double singleIterationSeconds)
		{
			if (singleIterationSeconds <= 0)
			{
				return 0.0;
			}

			// AutoReverse: each full cycle = forward + reverse = 2 * singleIteration.
			var iterDuration = AutoReverse ? 2.0 * singleIterationSeconds : singleIterationSeconds;
			var iterations = localTime / iterDuration;
			var iterIndex = (int)Math.Floor(iterations);
			var progress = iterations - iterIndex;

			// Position within the forward half (0 to singleIterationSeconds).
			double wrappedSeconds;
			if (AutoReverse)
			{
				// forward half: progress in [0, 0.5) → time in [0, singleIteration)
				// reverse half: progress in [0.5, 1) → time in (singleIteration, 0]
				var halfProgress = progress * 2.0;
				if (halfProgress > 1.0)
				{
					// Reverse phase.
					wrappedSeconds = (2.0 - halfProgress) * singleIterationSeconds;
				}
				else
				{
					// Forward phase.
					wrappedSeconds = halfProgress * singleIterationSeconds;
				}
			}
			else
			{
				wrappedSeconds = progress * singleIterationSeconds;
			}

			return wrappedSeconds;
		}

		internal override void OnAddToTimeManager()
		{
			_isRegisteredWithTimeManager = true;
		}

		internal override void OnRemoveFromTimeManager()
		{
			_isRegisteredWithTimeManager = false;
		}
	}
}
#endif

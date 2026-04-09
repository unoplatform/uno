// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference: dxaml/xcp/core/animation/storyboard.cpp — CStoryboard::ComputeStateImpl

#if __SKIA__
using Uno.UI.Xaml.Core;

namespace Microsoft.UI.Xaml.Media.Animation
{
	public partial class Storyboard
	{
		// MUX: m_rTimeDelta — Offset from parent time to local time.
		// Set on first tick so this storyboard's local time starts at 0.
		private double _timeDelta;

		// MUX: m_lastParentTime — Snapped parent time, used for pause and SeekAlignedToLastTick.
		// Initialized to double.MaxValue (== XDOUBLE_MAX) meaning "not yet ticked".
		private double _lastParentTime = double.MaxValue;

		// MUX: m_fIsBeginning — True from Begin() until the first ComputeState tick.
		// Signals that the timing delta should be snapped on the next tick.
		private bool _isBeginning;

		// MUX: m_fIsPaused, m_fIsResuming
		private bool _isPausedForTimeManager;
		private bool _isResuming;

		// MUX: m_fIsSeeking, m_rPendingSeekTime
		private bool _isSeeking;
		private double _pendingSeekTime = double.MaxValue;

		// MUX: m_fIsStopped — Set by Stop(), checked in ComputeState to skip processing.
		private bool _isStopped;

		// Whether this storyboard is registered with the TimeManager.
		internal bool _isRegisteredWithTimeManager;

		/// <summary>
		/// Ported from CStoryboard::ComputeStateImpl (storyboard.cpp lines 94-292).
		///
		/// Manages the timing delta from parent clock to local clock, handles
		/// pause/resume/seek state, and propagates timing to child timelines.
		/// </summary>
		internal override void ComputeState(ComputeStateParams parentParams)
		{
			if (_isStopped)
			{
				return;
			}

			if (!parentParams.HasTime)
			{
				// No valid time from parent — nothing to compute.
				// MUX: nested storyboards with parent not started.
				return;
			}

			var effectiveParentParams = parentParams;

			// Snap initial parent time on first tick.
			// MUX: storyboard.cpp line 127-131
			if (_lastParentTime == double.MaxValue)
			{
				_lastParentTime = parentParams.Time;
			}

			// Snap timing delta on first tick after Begin().
			// MUX: storyboard.cpp lines 135-143
			if (_isBeginning)
			{
				_timeDelta = -parentParams.Time;
				_isBeginning = false;
			}

			// Pause/resume/seek handling.
			// MUX: storyboard.cpp lines 145-232
			var pauseStartTime = _lastParentTime;

			if (_isPausedForTimeManager)
			{
				// Use the pause start time instead of the current parent time.
				// MUX: storyboard.cpp line 162
				effectiveParentParams.Time = pauseStartTime;
			}
			else
			{
				_lastParentTime = parentParams.Time;
			}

			if (_isResuming)
			{
				// Increase time delta by the pause duration.
				// MUX: storyboard.cpp lines 173-184
				_timeDelta += pauseStartTime - parentParams.Time;
				_isResuming = false;
			}

			if (_isSeeking)
			{
				if (_lastParentTime != double.MaxValue)
				{
					// Adjust time delta to achieve the seek target.
					// MUX: storyboard.cpp lines 192-200
					_timeDelta = _pendingSeekTime - effectiveParentParams.Time;
					_isSeeking = false;
				}
			}

			// Apply the timing delta.
			// MUX: storyboard.cpp line 240
			if (_isSeeking)
			{
				// SeekAlignedToLastTick before first tick — use seek time directly.
				// MUX: storyboard.cpp lines 227-233
				effectiveParentParams.Time = _pendingSeekTime;
			}
			else
			{
				effectiveParentParams.Time += _timeDelta;
			}

			// Floating point tolerance: snap to begin time if very close.
			// MUX: storyboard.cpp lines 249-258 (s_timeTolerance = 0.0005)
			var beginTime = BeginTime?.TotalSeconds ?? 0.0;
			var secondsToBeginTime = beginTime - effectiveParentParams.Time;
			if (secondsToBeginTime > 0.0 && secondsToBeginTime < 0.0005)
			{
				effectiveParentParams.Time = beginTime;
			}

			// --- CTimelineGroup::ComputeStateImpl equivalent ---
			// First compute this storyboard's own state (progress, clock state).
			// MUX: CTimeline::ComputeStateImpl called from CTimelineGroup::ComputeStateImpl line 149

			// Compute the storyboard's own clock state from effective parent time.
			ComputeClockStateFromParentTime(effectiveParentParams);

			// Propagate to children.
			// MUX: CTimelineGroup::ComputeStateImpl lines 171-206
			if (Children is { Count: > 0 })
			{
				// Pass our current time as the children's parent time.
				// MUX: myParams.time = m_rCurrentTime (line 191)
				var childParams = effectiveParentParams;
				childParams.HasTime = _computedClockState != InternalClockState.NotStarted;
				if (childParams.HasTime)
				{
					childParams.Time = _computedCurrentTime;
				}

				for (int i = 0; i < Children.Count; i++)
				{
					Children[i].ComputeState(childParams);
				}
			}
		}

		/// <summary>
		/// Computes this storyboard's clock state from the effective parent time.
		/// Simplified port of CTimeline::ComputeStateImpl timing logic.
		/// </summary>
		private void ComputeClockStateFromParentTime(ComputeStateParams parentParams)
		{
			var beginTime = BeginTime?.TotalSeconds ?? 0.0;

			if (!parentParams.HasTime || parentParams.Time < beginTime)
			{
				_computedClockState = InternalClockState.NotStarted;
				_computedCurrentProgress = 0.0;
				_computedCurrentTime = 0.0;
				return;
			}

			var duration = GetCalculatedDuration();
			var durationSeconds = duration.TotalSeconds;

			// Compute expiration time.
			// For Storyboard (ParallelTimeline), duration is the maximum of children.
			// For simplicity, use the explicit Duration if set, otherwise "forever".
			double? expirationTime = null;
			if (durationSeconds > 0)
			{
				expirationTime = beginTime + durationSeconds;
			}

			// Compute local time relative to begin time.
			_computedCurrentTime = parentParams.Time - beginTime;

			if (expirationTime.HasValue && parentParams.Time >= expirationTime.Value - 0.0005)
			{
				// Expired.
				if (FillBehavior == FillBehavior.HoldEnd)
				{
					_computedClockState = InternalClockState.Filling;
					_computedCurrentTime = expirationTime.Value - beginTime;
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

			// Compute progress (for Storyboard this is mainly informational).
			if (durationSeconds > 0)
			{
				_computedCurrentProgress = _computedCurrentTime / durationSeconds;
				_computedCurrentProgress = System.Math.Clamp(_computedCurrentProgress, 0.0, 1.0);
			}
			else
			{
				_computedCurrentProgress = 1.0;
			}
		}

		// Computed state from the latest ComputeState call.
		private InternalClockState _computedClockState = InternalClockState.NotStarted;
		private double _computedCurrentTime;
		private double _computedCurrentProgress;

		/// <summary>
		/// Override to include computed state in the active check.
		/// </summary>
		internal override bool IsInActiveState =>
			base.IsInActiveState
			|| _computedClockState == InternalClockState.Active
			|| _computedClockState == InternalClockState.Filling;

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

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference: dxaml/xcp/core/animation/Timeline.cpp
//   CTimeline::ComputeStateImpl, ComputeCurrentClockState, ComputeLocalProgressAndTime

#if __SKIA__
using System;

namespace Microsoft.UI.Xaml.Media.Animation
{
	public partial class Timeline
	{
		// MUX: s_timeTolerance — floating point tolerance for timing comparisons
		private const double TimeTolerance = 0.0005;

		// --- Computed timing state (per-tick) ---
		// MUX: m_clockState, m_rCurrentProgress, m_rCurrentTime, m_nIteration
		private InternalClockState _tmClockState = InternalClockState.NotStarted;
		private double _tmCurrentProgress;
		private double _tmCurrentTime;
		private int _tmIteration;
		private protected bool _tmInitialized;
		private protected bool _tmCompletedEventFired;

		/// <summary>
		/// The current clock state as computed by the TimeManager timing hierarchy.
		/// MUX: m_clockState
		/// </summary>
		internal InternalClockState TimeManagerClockState => _tmClockState;

		/// <summary>
		/// The current progress (0.0 to 1.0) as computed by the timing hierarchy.
		/// MUX: m_rCurrentProgress
		/// </summary>
		internal double TimeManagerProgress => _tmCurrentProgress;

		/// <summary>
		/// The current local time in seconds as computed by the timing hierarchy.
		/// MUX: m_rCurrentTime
		/// </summary>
		internal double TimeManagerCurrentTime => _tmCurrentTime;

		/// <summary>
		/// Base timing computation that computes clock state and progress from parent
		/// time, then calls UpdateAnimation (virtual) for value application.
		/// Called from animation type's ComputeState override.
		///
		/// Ported from CTimeline::ComputeStateImpl (Timeline.cpp lines 185-279).
		/// </summary>
		internal void ComputeStateBase(ComputeStateParams parentParams)
		{
			var beginTime = BeginTime?.TotalSeconds ?? 0.0;

			// Start as NotStarted until we determine otherwise.
			// MUX: m_clockState = DirectUI::ClockState::NotStarted (line 200)
			_tmClockState = InternalClockState.NotStarted;
			_tmIteration = 0;

			// Get natural duration.
			// MUX: GetNaturalDuration (line 205)
			var duration = GetCalculatedDuration();
			var durationSeconds = duration.TotalSeconds;
			var hasDuration = durationSeconds >= 0 && durationSeconds < double.MaxValue;

			// Compute expiration time.
			// MUX: ComputeExpirationTime (line 209)
			// Zero-duration animations expire at beginTime (WinUI: rDurationValue <= 0 → progress=1.0).
			double? expirationTime = null;
			if (hasDuration)
			{
				expirationTime = beginTime + ComputeEffectiveDuration(durationSeconds);
			}

			// Determine clock state from parent time.
			// MUX: ComputeCurrentClockState (line 221)
			bool shouldComputeProgress = ComputeCurrentClockState(
				ref parentParams, beginTime, expirationTime);

			if (shouldComputeProgress)
			{
				// Compute local progress and time.
				// MUX: ComputeLocalProgressAndTime (line 238)
				ComputeLocalProgressAndTime(
					beginTime, parentParams.Time, durationSeconds, hasDuration, expirationTime);

				// Fire per-iteration initialization on first Active tick.
				// MUX: InitializeIteration (line 249)
				if (!_tmInitialized)
				{
					_tmInitialized = true;
					InitializeIteration();
				}
			}

			// Update animated values based on clock state.
			// MUX: UpdateAnimation (line 263)
			UpdateAnimation(parentParams);
		}

		/// <summary>
		/// Determines clock state (NotStarted, Active, Filling, Stopped) from parent time.
		/// Ported from CTimeline::ComputeCurrentClockState (Timeline.cpp lines 835-906).
		/// </summary>
		private bool ComputeCurrentClockState(
			ref ComputeStateParams parentParams,
			double beginTime,
			double? expirationTime)
		{
			// Not started if no valid parent time or before begin time.
			// MUX: line 844
			if (!parentParams.HasTime || parentParams.Time < beginTime)
			{
				_tmClockState = InternalClockState.NotStarted;
				_tmCurrentProgress = 0.0;
				return false;
			}

			// Check if expired.
			// MUX: IsExpired (line 853)
			if (IsExpired(expirationTime, parentParams.Time))
			{
				// MUX: line 881 — check FillBehavior
				if (FillBehavior == FillBehavior.HoldEnd)
				{
					_tmClockState = InternalClockState.Filling;
					// Clamp time to expiration.
					if (expirationTime.HasValue)
					{
						parentParams.Time = expirationTime.Value;
					}
				}
				else
				{
					_tmClockState = InternalClockState.Stopped;
				}
			}
			else
			{
				_tmClockState = InternalClockState.Active;
			}

			return true;
		}

		/// <summary>
		/// Computes local progress (0-1) and time from parent time.
		/// Ported from CTimeline::ComputeLocalProgressAndTime (Timeline.cpp lines 909-1027).
		/// </summary>
		private void ComputeLocalProgressAndTime(
			double beginTime,
			double parentTime,
			double durationSeconds,
			bool hasDuration,
			double? expirationTime)
		{
			// Local time relative to parent, accounting for speed ratio.
			// MUX: m_rCurrentTime = (rParentTime - rBeginTime) * m_rSpeedRatio (line 925)
			var speedRatio = SpeedRatio;
			if (speedRatio <= 0)
			{
				speedRatio = 1.0;
			}

			_tmCurrentTime = (parentTime - beginTime) * speedRatio;

			if (hasDuration)
			{
				if (durationSeconds <= 0)
				{
					// Zero-duration: hold END value.
					// MUX: lines 956-961
					_tmCurrentTime = 0.0;
					_tmCurrentProgress = 1.0;
				}
				else
				{
					// Compute iteration and progress.
					// MUX: lines 965-967
					var iterations = _tmCurrentTime / durationSeconds;
					_tmIteration = (int)Math.Floor(iterations);
					_tmCurrentProgress = iterations - _tmIteration;

					// Boundary precision handling.
					// MUX: lines 992-1006
					if ((iterations >= 1.0 && _tmCurrentProgress <= 0.001)
						|| (_tmCurrentProgress >= 0.999))
					{
						if (IsExpired(expirationTime, parentTime + beginTime))
						{
							// AutoReverse: at expiration, even iterations hold at start.
							if (AutoReverse && (_tmIteration % 2) == 0)
							{
								_tmCurrentProgress = 0.0;
							}
							else
							{
								_tmCurrentProgress = 1.0;
							}
						}
					}

					// Invert odd iterations for AutoReverse.
					// MUX: lines 1010-1015
					if (AutoReverse && (_tmIteration % 2) == 1)
					{
						_tmCurrentProgress = 1.0 - _tmCurrentProgress;
					}

					// Set child time from progress.
					// MUX: line 1018
					_tmCurrentTime = _tmCurrentProgress * durationSeconds;
				}
			}
			else
			{
				// Duration is Forever.
				// MUX: lines 1021-1024
				_tmCurrentProgress = 0.0;
			}
		}

		/// <summary>
		/// Computes the effective duration accounting for RepeatBehavior and AutoReverse.
		/// Simplified port of CTimeline::ComputeEffectiveDuration (Timeline.cpp lines 727-800).
		/// </summary>
		private double ComputeEffectiveDuration(double singleIterationDuration)
		{
			var repeat = RepeatBehavior;
			double scalingFactor;

			if (repeat.Type == RepeatBehaviorType.Forever)
			{
				return double.MaxValue; // Infinite
			}

			if (repeat.HasDuration)
			{
				return repeat.Duration.TotalSeconds;
			}

			if (repeat.HasCount && repeat.Count > 0)
			{
				scalingFactor = repeat.Count;
			}
			else
			{
				// Default RepeatBehavior (Count=0 in Uno's struct default) means "play once".
				// WinUI's default RepeatBehavior has Count=1.
				scalingFactor = 1.0;
			}

			if (AutoReverse)
			{
				scalingFactor *= 2.0;
			}

			return scalingFactor * singleIterationDuration;
		}

		/// <summary>
		/// Checks if the timeline has expired (parent time >= expiration time).
		/// MUX: CTimeline::IsExpired (Timeline.cpp lines 1029-1042)
		/// </summary>
		private static bool IsExpired(double? expirationTime, double parentTime)
		{
			return expirationTime.HasValue
				&& expirationTime.Value <= parentTime + TimeTolerance;
		}

		/// <summary>
		/// Called when the timeline transitions to Active for the first time.
		/// Override in subclasses for per-iteration setup.
		/// MUX: CTimeline::InitializeIteration() (virtual)
		/// </summary>
		internal virtual void InitializeIteration()
		{
		}

		/// <summary>
		/// Called by ComputeState after clock state and progress are computed.
		/// Override in each animation type to apply values based on _tmClockState
		/// and _tmCurrentProgress.
		///
		/// MUX: CAnimation::UpdateAnimation (animation.cpp lines 41-245)
		/// </summary>
		internal virtual void UpdateAnimation(ComputeStateParams parentParams)
		{
			// Base implementation handles common state transitions.
			switch (_tmClockState)
			{
				case InternalClockState.Stopped:
					if (_tmInitialized && !_tmCompletedEventFired)
					{
						_tmCompletedEventFired = true;
						OnCompleted();
					}
					FinalizeAnimationIteration();
					break;

				case InternalClockState.Filling:
					if (_tmInitialized && !_tmCompletedEventFired)
					{
						_tmCompletedEventFired = true;
						OnCompleted();
					}
					break;
			}
		}

		/// <summary>
		/// Cleanup when animation iteration finishes.
		/// MUX: CTimeline::FinalizeIteration (Timeline.cpp line 281)
		/// </summary>
		internal virtual void FinalizeAnimationIteration()
		{
			_tmInitialized = false;
			_tmCurrentTime = 0.0;
		}

		/// <summary>
		/// Resets all TimeManager timing state. Called from Stop/Deactivate.
		/// </summary>
		internal void ResetTimeManagerState()
		{
			_tmClockState = InternalClockState.NotStarted;
			_tmCurrentProgress = 0.0;
			_tmCurrentTime = 0.0;
			_tmIteration = 0;
			_tmInitialized = false;
			_tmCompletedEventFired = false;
		}
	}
}
#endif

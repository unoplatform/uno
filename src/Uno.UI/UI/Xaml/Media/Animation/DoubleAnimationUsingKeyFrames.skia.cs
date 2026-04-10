// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference: dxaml/xcp/core/animation/animation.cpp — CAnimation::UpdateAnimationUsingKeyFrames

#if __SKIA__
using System;
using System.Linq;

namespace Microsoft.UI.Xaml.Media.Animation
{
	public partial class DoubleAnimationUsingKeyFrames
	{
		private bool ReportEachFrame() => true;

		private bool _isTimeManagerDriven;
		private bool _deferredPlayPending;

		partial void OnFrame(IValueAnimator currentAnimator)
		{
			SetValue(currentAnimator.AnimatedValue);
		}

		/// <summary>
		/// Detect if this animation is a child of a TimeManager-registered Storyboard
		/// and use the ComputeState/UpdateAnimation path if so.
		/// </summary>
		private void PlayDeferred()
		{
			if (IsParentStoryboardRegistered())
			{
				_isTimeManagerDriven = true;
				_tmCompletedEventFired = false;
				_tmInitialized = false;
				_startingValue = ComputeFromValue();

				// Pre-compute final value for HoldValue/SkipToFill.
				var lastFrame = KeyFrames.OrderBy(k => k.KeyTime.TimeSpan).LastOrDefault();
				if (lastFrame != null)
				{
					_finalValue = lastFrame.Value;
				}

				return;
			}

			// Fallback: old deferred play for standalone animations.
			if (_deferredPlayPending)
			{
				return;
			}

			_deferredPlayPending = true;
			State = TimelineState.Active;

			_ = Dispatcher.RunAsync(global::Windows.UI.Core.CoreDispatcherPriority.High, () =>
			{
				_deferredPlayPending = false;

				if (State != TimelineState.Active)
				{
					return;
				}

				PlayImmediate();
			});
		}

		private void CancelDeferredPlay()
		{
			_deferredPlayPending = false;
		}

		/// <summary>
		/// Called by Storyboard.ComputeState via child propagation.
		/// Computes timing (base class) then interpolates keyframe values.
		/// </summary>
		internal override void ComputeState(ComputeStateParams parentParams)
		{
			if (!_isTimeManagerDriven)
			{
				return;
			}

			// Base timing computation: clock state, progress, iteration.
			ComputeStateBase(parentParams);

			// Apply values based on computed state.
			UpdateAnimationCore();
		}

		/// <summary>
		/// Applies interpolated keyframe value based on computed clock state.
		///
		/// MUX: CAnimation::UpdateAnimation → UpdateAnimationUsingKeyFrames
		/// </summary>
		private void UpdateAnimationCore()
		{
			if (!_isTimeManagerDriven)
			{
				return;
			}

			switch (TimeManagerClockState)
			{
				case InternalClockState.Active:
					UpdateUsingKeyFrames();
					break;

				case InternalClockState.Filling:
					// Apply final interpolated value and fire completion.
					UpdateUsingKeyFrames();
					if (!_tmCompletedEventFired)
					{
						_tmCompletedEventFired = true;
						State = TimelineState.Filling;
						OnCompleted();
					}
					break;

				case InternalClockState.Stopped:
					if (_tmInitialized && !_tmCompletedEventFired)
					{
						_tmCompletedEventFired = true;
						ClearValue();
						State = TimelineState.Stopped;
						OnCompleted();
					}
					FinalizeAnimationIteration();
					break;
			}
		}

		/// <summary>
		/// Finds the current keyframe segment from progress, interpolates
		/// with easing, and applies the value.
		///
		/// MUX: CAnimation::UpdateAnimationUsingKeyFrames (animation.cpp lines 247-369)
		/// </summary>
		private void UpdateUsingKeyFrames()
		{
			var sortedFrames = KeyFrames.OrderBy(k => k.KeyTime.TimeSpan).ToList();
			if (sortedFrames.Count == 0)
			{
				return;
			}

			var progress = TimeManagerProgress;
			var duration = GetCalculatedDuration();
			var durationSeconds = duration.TotalSeconds;

			// Walk through sorted keyframes to find the current segment.
			// MUX: while loop at line 298 — finds segment where progress falls.
			var fromValue = _startingValue ?? 0.0;
			double cumulativePercent = 0;
			double segmentPercent = 0;
			int currentSegment = 0;

			// Set initial From/To to base value (MUX: lines 275-276).
			double toValue = fromValue;

			if (sortedFrames.Count > 0)
			{
				var firstFrame = sortedFrames[0];
				var firstPercent = durationSeconds > 0
					? firstFrame.KeyTime.TimeSpan.TotalSeconds / durationSeconds
					: 0;

				if (firstPercent > 0)
				{
					// First keyframe not at time 0: interpolate from base value.
					segmentPercent = firstPercent;
				}

				toValue = firstFrame.Value;
			}

			// Advance through segments until we find the one containing current progress.
			// MUX: while (nCurrentSegment < frameCount && keyframe.percent <= progress)
			while (currentSegment < sortedFrames.Count)
			{
				var frame = sortedFrames[currentSegment];
				var framePercent = durationSeconds > 0
					? frame.KeyTime.TimeSpan.TotalSeconds / durationSeconds
					: 1.0;

				if (framePercent > progress)
				{
					break;
				}

				// Move to next segment.
				// MUX: lines 301-312
				currentSegment++;
				fromValue = toValue;
				cumulativePercent += segmentPercent;

				if (currentSegment < sortedFrames.Count)
				{
					var nextFrame = sortedFrames[currentSegment];
					toValue = nextFrame.Value;
					segmentPercent = (durationSeconds > 0
						? nextFrame.KeyTime.TimeSpan.TotalSeconds / durationSeconds
						: 1.0) - cumulativePercent;
				}
				else
				{
					// Past last keyframe: hold last value.
					// MUX: lines 316-332
					var lastFrame = sortedFrames[^1];
					toValue = lastFrame.Value;
					segmentPercent = cumulativePercent < 1.0
						? 1.0 - cumulativePercent
						: 1.0;
				}
			}

			// Compute segment-local progress.
			// MUX: rSegmentProgress = (m_rCurrentProgress - rCumulativePercentSpan) / rSegmentPercentSpan (line 339)
			var segmentProgress = segmentPercent > 0
				? (progress - cumulativePercent) / segmentPercent
				: 1.0;
			segmentProgress = Math.Clamp(segmentProgress, 0.0, 1.0);

			// Apply per-keyframe easing.
			// MUX: GetEffectiveProgress (line 343)
			var targetFrameIndex = Math.Min(currentSegment, sortedFrames.Count - 1);
			var easing = sortedFrames[targetFrameIndex].GetEasingFunction();

			double value;
			if (easing != null)
			{
				// IEasingFunction.Ease takes (currentTime, startValue, finalValue, duration).
				value = easing.Ease(segmentProgress, fromValue, toValue, 1.0);
			}
			else
			{
				// Linear interpolation.
				value = fromValue + (toValue - fromValue) * segmentProgress;
			}

			SetValue(value);
		}

		/// <summary>
		/// Re-reads the base value for interpolation on each iteration start.
		/// MUX: CAnimation::UpdateAnimationUsingKeyFrames reads BaseValue on each
		/// tick (line 276: ValueAssign(AssignmentOperand::BaseValue)).
		/// </summary>
		internal override void InitializeIteration()
		{
			if (_isTimeManagerDriven)
			{
				_startingValue = ComputeFromValue();
			}
		}

		partial void DisposePartial()
		{
			_isTimeManagerDriven = false;
		}

		partial void UseHardware()
		{
			// No-op on Skia.
		}

		// HoldValue is intentionally left as a no-op on Skia (no partial body).
		// For the TimeManager path, ComputeState holds the fill value.
		// For the old deferred play path, SkipToFill/OnEnd handles it
		// by reading the last keyframe value directly.

		private void StopTimeManagerDriven()
		{
			_isTimeManagerDriven = false;
			ResetTimeManagerState();
		}

		private bool IsParentStoryboardRegistered()
		{
			object current = this;
			while ((current = (current as DependencyObject)?.GetParent()) != null)
			{
				if (current is Storyboard sb)
				{
					return sb._isRegisteredWithTimeManager;
				}
			}

			return false;
		}
	}
}
#endif

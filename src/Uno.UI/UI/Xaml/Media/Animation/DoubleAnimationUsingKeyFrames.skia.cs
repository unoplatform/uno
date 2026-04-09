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

		// Whether ComputeState-driven playback is active.
		private bool _isTimeManagerDriven;
		private bool _completedFired;
		private bool _deferredPlayPending;

		partial void OnFrame(IValueAnimator currentAnimator)
		{
			SetValue(currentAnimator.AnimatedValue);
		}

		/// <summary>
		/// On Skia, when driven by TimeManager (parent Storyboard registered),
		/// Begin() just sets state to Active and resolves theme resources.
		/// MUX: CAnimation::OnBegin()
		/// </summary>
		private void BeginViaTimeManager()
		{
			_isTimeManagerDriven = true;
			_completedFired = false;
			_startingValue = ComputeFromValue();

			// Pre-compute final value for HoldValue/SkipToFill.
			// MUX: In WinUI, OnBegin reads base values; the final keyframe value
			// is read at tick time but we cache it here for the HoldValue path.
			var lastFrame = KeyFrames.OrderBy(k => k.KeyTime.TimeSpan).LastOrDefault();
			if (lastFrame != null)
			{
				_finalValue = lastFrame.Value;
			}
		}

		/// <summary>
		/// Defers animator initialization to the next dispatcher tick.
		/// Falls back to ComputeState path if driven by TimeManager.
		/// </summary>
		private void PlayDeferred()
		{
			if (this.GetParent()?.GetParent() is Storyboard sb && sb._isRegisteredWithTimeManager)
			{
				BeginViaTimeManager();
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
		/// Called by TimeManager via Storyboard.ComputeState() → child.ComputeState().
		/// Computes the current keyframe segment from parent time, interpolates with
		/// easing, and applies the value.
		///
		/// MUX: CAnimation::UpdateAnimationUsingKeyFrames (animation.cpp lines 247-369)
		/// </summary>
		internal override void ComputeState(ComputeStateParams parentParams)
		{
			if (!_isTimeManagerDriven)
			{
				return;
			}

			if (State == TimelineState.Stopped)
			{
				return;
			}

			if (!parentParams.HasTime)
			{
				return;
			}

			var beginTime = BeginTime?.TotalSeconds ?? 0.0;
			var parentTime = parentParams.Time;

			if (parentTime < beginTime)
			{
				return;
			}

			// Compute local time and duration.
			var localTime = parentTime - beginTime;
			var duration = GetCalculatedDuration();
			var durationSeconds = duration.TotalSeconds;

			// Sort keyframes by KeyTime.
			// MUX: m_pKeyFrames->GetSortedCollection()
			var sortedFrames = KeyFrames.OrderBy(k => k.KeyTime.TimeSpan).ToList();
			if (sortedFrames.Count == 0)
			{
				return;
			}

			// Check expiration.
			if (durationSeconds > 0 && localTime >= durationSeconds - 0.0005)
			{
				if (!_completedFired)
				{
					if (FillBehavior == FillBehavior.HoldEnd)
					{
						// Apply the last keyframe value and hold.
						// MUX: progress=1.0 → last keyframe value
						var lastFrame = sortedFrames[^1];
						SetValue(lastFrame.Value);
						State = TimelineState.Filling;
					}
					else
					{
						ClearValue();
						State = TimelineState.Stopped;
					}

					_completedFired = true;
					OnCompleted();
				}

				return;
			}

			// Active: find current keyframe segment and interpolate.
			// MUX: UpdateAnimationUsingKeyFrames — find segment from progress.
			var fromValue = _startingValue ?? 0.0;
			var previousKeyTime = 0.0;

			for (int i = 0; i < sortedFrames.Count; i++)
			{
				var frame = sortedFrames[i];
				var frameTime = frame.KeyTime.TimeSpan.TotalSeconds;

				if (localTime < frameTime || i == sortedFrames.Count - 1)
				{
					// This is the target segment.
					// MUX: Read keyframe value FRESH (animation.cpp line 289)
					var toValue = frame.Value;
					var segmentDuration = frameTime - previousKeyTime;

					if (segmentDuration <= 0)
					{
						// Zero-duration segment: apply target value immediately.
						// MUX: DiscreteKeyFrame or time-0 keyframe
						SetValue(toValue);
					}
					else
					{
						// Compute segment-local time and apply easing.
						var segmentLocalTime = localTime - previousKeyTime;
						var easing = frame.GetEasingFunction();

						double value;
						if (easing != null)
						{
							value = easing.Ease(segmentLocalTime, fromValue, toValue, segmentDuration);
						}
						else
						{
							// Linear interpolation (no easing).
							var t = segmentLocalTime / segmentDuration;
							value = fromValue + (toValue - fromValue) * t;
						}

						SetValue(value);
					}

					break;
				}

				// Move to next segment.
				fromValue = frame.Value;
				previousKeyTime = frameTime;
			}
		}

		partial void DisposePartial()
		{
			_isTimeManagerDriven = false;
		}

		partial void UseHardware()
		{
			// No-op on Skia — no GPU acceleration for keyframe animations.
		}

		// HoldValue is intentionally left as a no-op on Skia (no partial body).
		// For the TimeManager path, ComputeState holds the fill value.
		// For the old deferred play path, SkipToFill/OnEnd handles it
		// by reading the last keyframe value directly.

		private void StopTimeManagerDriven()
		{
			_isTimeManagerDriven = false;
			_completedFired = false;
		}
	}
}
#endif

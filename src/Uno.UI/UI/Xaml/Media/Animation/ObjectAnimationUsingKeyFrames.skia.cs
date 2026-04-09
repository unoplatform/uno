// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference: dxaml/xcp/core/animation/animation.cpp — CAnimation::UpdateAnimationUsingKeyFrames

#if __SKIA__
using System;
using System.Linq;
using Uno.UI.Xaml.Core;

namespace Microsoft.UI.Xaml.Media.Animation
{
	public partial class ObjectAnimationUsingKeyFrames
	{
		// Whether ComputeState-driven playback is active (vs. the old KeyFrameScheduler path).
		// True when this animation is a child of a Storyboard registered with TimeManager.
		private bool _isTimeManagerDriven;

		// Whether OnCompleted has been fired for the current iteration.
		private bool _completedFired;

		/// <summary>
		/// On Skia, when driven by TimeManager (parent Storyboard registered),
		/// Begin() just sets state to Active and resolves theme resources.
		/// The actual value computation is done by ComputeState() each tick.
		///
		/// MUX: CAnimation::OnBegin() — sets up target, reads base values,
		/// but does NOT apply first frame value (that happens at first Tick).
		/// </summary>
		private void BeginViaTimeManager()
		{
			_isTimeManagerDriven = true;
			_completedFired = false;

			// MUX: CAnimation::OnBegin() → EnsureKeyFrameThemeResources equivalent
			EnsureKeyFrameThemeResources();
		}

		/// <summary>
		/// Falls back to the old deferred play mechanism for standalone Begin()
		/// (not through a TimeManager-registered Storyboard).
		/// </summary>
		private void PlayDeferred()
		{
			// If this animation is a child of a TimeManager-registered Storyboard,
			// use the ComputeState path instead.
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

		private bool _deferredPlayPending;

		private void CancelDeferredPlay()
		{
			_deferredPlayPending = false;
		}

		/// <summary>
		/// Called by TimeManager via Storyboard.ComputeState() → child.ComputeState().
		/// Computes the current keyframe from parent time and applies its value.
		///
		/// MUX: CAnimation::UpdateAnimationUsingKeyFrames (animation.cpp lines 247-369)
		/// — finds current keyframe segment from progress, reads value fresh, applies.
		/// </summary>
		internal override void ComputeState(ComputeStateParams parentParams)
		{
			if (!_isTimeManagerDriven)
			{
				// Not driven by TimeManager — using old mechanism.
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

			// Not started yet (before begin time).
			if (parentTime < beginTime)
			{
				return;
			}

			// Compute local time and duration.
			var localTime = parentTime - beginTime;
			var duration = GetCalculatedDuration();
			var durationSeconds = duration.TotalSeconds;

			// Sort keyframes by KeyTime for segment lookup.
			// MUX: m_pKeyFrames->GetSortedCollection()
			var sortedFrames = KeyFrames.OrderBy(k => k.KeyTime.TimeSpan).ToList();
			if (sortedFrames.Count == 0)
			{
				return;
			}

			// Check expiration.
			if (durationSeconds > 0 && localTime >= durationSeconds - 0.0005)
			{
				// Animation has expired.
				if (!_completedFired)
				{
					if (FillBehavior == FillBehavior.HoldEnd)
					{
						// Apply the last keyframe value and hold.
						var lastFrame = sortedFrames[^1];
						_currentFrame = lastFrame;
						SetValue(lastFrame.Value);
						State = TimelineState.Filling;
					}
					else
					{
						// FillBehavior.Stop: clear animated value.
						_currentFrame = null;
						ClearValue();
						State = TimelineState.Stopped;
					}

					_completedFired = true;
					OnCompleted();
				}

				return;
			}

			// Active: find the current keyframe from local time.
			// MUX: UpdateAnimationUsingKeyFrames — linearly search sorted keyframes.
			IKeyFrame<object> targetFrame = sortedFrames[0];
			for (int i = 0; i < sortedFrames.Count; i++)
			{
				if (localTime < sortedFrames[i].KeyTime.TimeSpan.TotalSeconds)
				{
					break;
				}

				targetFrame = sortedFrames[i];
			}

			// Apply the keyframe value (read fresh each tick).
			// MUX: pKeyFrame->GetValue() called each tick, not cached.
			_currentFrame = targetFrame;
			SetValue(targetFrame.Value);
		}

		/// <summary>
		/// Cleanup when stopping a TimeManager-driven animation.
		/// </summary>
		private void StopTimeManagerDriven()
		{
			_isTimeManagerDriven = false;
			_completedFired = false;
		}
	}
}
#endif

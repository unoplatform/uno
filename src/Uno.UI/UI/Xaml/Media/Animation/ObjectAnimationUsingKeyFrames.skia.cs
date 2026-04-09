// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference: dxaml/xcp/core/animation/animation.cpp — CAnimation::UpdateAnimationUsingKeyFrames

#if __SKIA__
using System;
using System.Linq;

namespace Microsoft.UI.Xaml.Media.Animation
{
	public partial class ObjectAnimationUsingKeyFrames
	{
		// Whether this animation is driven by the TimeManager (via parent Storyboard).
		private bool _isTimeManagerDriven;
		private bool _deferredPlayPending;

		/// <summary>
		/// Detect if this animation is a child of a TimeManager-registered Storyboard
		/// and use the ComputeState/UpdateAnimation path if so.
		/// </summary>
		private void PlayDeferred()
		{
			if (IsParentStoryboardRegistered())
			{
				_isTimeManagerDriven = true;
				// Resolve theme resources now (MUX: CAnimation::OnBegin).
				EnsureKeyFrameThemeResources();
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

		private void CancelDeferredPlay()
		{
			_deferredPlayPending = false;
		}

		/// <summary>
		/// Called by Storyboard.ComputeState via child propagation.
		/// Computes timing (base class) then applies keyframe values.
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
		/// Applies the appropriate keyframe value based on computed clock state.
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
					// Apply final keyframe and fire completion.
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
		/// Finds the current keyframe from progress and applies its value.
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

			// Find the keyframe at or before the current progress.
			// MUX: while loop at line 298
			IKeyFrame<object> targetFrame = sortedFrames[0];
			for (int i = 0; i < sortedFrames.Count; i++)
			{
				var frameProgress = durationSeconds > 0
					? sortedFrames[i].KeyTime.TimeSpan.TotalSeconds / durationSeconds
					: 1.0;

				if (progress < frameProgress)
				{
					break;
				}

				targetFrame = sortedFrames[i];
			}

			// Apply the keyframe value (read FRESH each tick).
			// MUX: pKeyFrame->GetValue() called each tick.
			_currentFrame = targetFrame;
			SetValue(targetFrame.Value);
		}

		private void StopTimeManagerDriven()
		{
			_isTimeManagerDriven = false;
			ResetTimeManagerState();
		}

		private bool IsParentStoryboardRegistered()
		{
			// Walk up to find the parent Storyboard.
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

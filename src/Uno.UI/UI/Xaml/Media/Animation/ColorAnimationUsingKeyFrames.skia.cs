// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference: dxaml/xcp/core/animation/animation.cpp — CAnimation::UpdateAnimationUsingKeyFrames

#if __SKIA__
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.UI;

namespace Microsoft.UI.Xaml.Media.Animation
{
	partial class ColorAnimationUsingKeyFrames
	{
		private bool ReportEachFrame() => true;

		private bool _isTimeManagerDriven;
		private bool _deferredPlayPending;
		private List<ColorKeyFrame> _sortedKeyFramesCache;

		partial void OnFrame(IValueAnimator currentAnimator)
		{
			SetValue(currentAnimator.AnimatedValue);
		}

		private void PlayDeferred()
		{
			if (IsParentStoryboardRegistered())
			{
				_isTimeManagerDriven = true;
				_tmCompletedEventFired = false;
				_tmInitialized = false;
				PropertyInfo?.CloneShareableObjectsInPath();
				_startingValue = ComputeFromValue();

				var lastFrame = KeyFrames.OrderBy(k => k.KeyTime.TimeSpan).LastOrDefault();
				if (lastFrame != null)
				{
					_finalValue = (ColorOffset)lastFrame.Value;
				}

				return;
			}

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

		internal override void ComputeState(ComputeStateParams parentParams)
		{
			if (!_isTimeManagerDriven)
			{
				return;
			}

			ComputeStateBase(parentParams);
			UpdateAnimationCore();
		}

		private void UpdateAnimationCore()
		{
			switch (TimeManagerClockState)
			{
				case InternalClockState.Active:
					UpdateUsingKeyFrames();
					break;

				case InternalClockState.Filling:
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

		private void UpdateUsingKeyFrames()
		{
			var sortedFrames = _sortedKeyFramesCache ??= KeyFrames.OrderBy(k => k.KeyTime.TimeSpan).ToList();
			if (sortedFrames.Count == 0)
			{
				return;
			}

			var progress = TimeManagerProgress;
			var duration = GetCalculatedDuration();
			var durationSeconds = duration.TotalSeconds;

			var fromValue = _startingValue ?? default(ColorOffset);
			double cumulativePercent = 0;
			double segmentPercent = 0;
			int currentSegment = 0;
			var toValue = fromValue;

			if (sortedFrames.Count > 0)
			{
				var firstFrame = sortedFrames[0];
				var firstPercent = durationSeconds > 0
					? firstFrame.KeyTime.TimeSpan.TotalSeconds / durationSeconds
					: 0;

				if (firstPercent > 0)
				{
					segmentPercent = firstPercent;
				}

				toValue = (ColorOffset)firstFrame.Value;
			}

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

				currentSegment++;
				fromValue = toValue;
				cumulativePercent += segmentPercent;

				if (currentSegment < sortedFrames.Count)
				{
					var nextFrame = sortedFrames[currentSegment];
					toValue = (ColorOffset)nextFrame.Value;
					segmentPercent = (durationSeconds > 0
						? nextFrame.KeyTime.TimeSpan.TotalSeconds / durationSeconds
						: 1.0) - cumulativePercent;
				}
				else
				{
					var lastFrame = sortedFrames[^1];
					toValue = (ColorOffset)lastFrame.Value;
					segmentPercent = cumulativePercent < 1.0
						? 1.0 - cumulativePercent
						: 1.0;
				}
			}

			var segmentProgress = segmentPercent > 0
				? (progress - cumulativePercent) / segmentPercent
				: 1.0;
			segmentProgress = Math.Clamp(segmentProgress, 0.0, 1.0);

			// Apply per-keyframe easing.
			var targetFrameIndex = Math.Min(currentSegment, sortedFrames.Count - 1);
			var easing = sortedFrames[targetFrameIndex].GetEasingFunction();

			ColorOffset value;
			if (easing != null)
			{
				// Color interpolation: apply easing to get normalized progress, then lerp.
				var t = easing.Ease(segmentProgress, 0.0, 1.0, 1.0);
				value = fromValue + (float)t * (toValue - fromValue);
			}
			else
			{
				value = fromValue + (float)segmentProgress * (toValue - fromValue);
			}

			SetValue(value);
		}

		/// <summary>
		/// Re-reads the base value for interpolation on each iteration start.
		/// MUX: CAnimation::UpdateAnimationUsingKeyFrames reads BaseValue each tick.
		/// </summary>
		internal override void InitializeIteration()
		{
			if (_isTimeManagerDriven)
			{
				PropertyInfo?.CloneShareableObjectsInPath();
				_startingValue = ComputeFromValue();
				_sortedKeyFramesCache = KeyFrames.OrderBy(k => k.KeyTime.TimeSpan).ToList();
			}
		}

		partial void DisposePartial()
		{
			_isTimeManagerDriven = false;
		}

		partial void UseHardware() { }

		private void StopTimeManagerDriven()
		{
			_isTimeManagerDriven = false;
			_sortedKeyFramesCache = null;
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

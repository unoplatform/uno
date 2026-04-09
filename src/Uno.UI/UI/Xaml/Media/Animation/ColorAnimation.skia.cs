// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference: dxaml/xcp/core/animation/animation.cpp — CAnimation::UpdateAnimation (non-keyframe)

#if __SKIA__
using System;
using Windows.UI;

namespace Microsoft.UI.Xaml.Media.Animation
{
	public partial class ColorAnimation
	{
		private bool _isTimeManagerDriven;
		private ColorOffset _tmFromValue;
		private ColorOffset _tmToValue;

		internal override void ComputeState(ComputeStateParams parentParams)
		{
			if (!_isTimeManagerDriven)
			{
				return;
			}

			ComputeStateBase(parentParams);

			switch (TimeManagerClockState)
			{
				case InternalClockState.Active:
				case InternalClockState.Filling:
					{
						var progress = TimeManagerProgress;

						// Linear color interpolation (ColorAnimation doesn't support EasingFunction in WinUI).
						var value = _tmFromValue + (float)progress * (_tmToValue - _tmFromValue);
						SetValue(value);

						if (TimeManagerClockState == InternalClockState.Filling && !_tmCompletedEventFired)
						{
							_tmCompletedEventFired = true;
							State = TimelineState.Filling;
							OnCompleted();
						}

						break;
					}

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

		internal override void InitializeIteration()
		{
			if (_isTimeManagerDriven)
			{
				PropertyInfo?.CloneShareableObjectsInPath();

				_tmFromValue = ComputeFromValueForTimeManager();
				_tmToValue = ComputeToValueForTimeManager();
			}
		}

		private ColorOffset ComputeFromValueForTimeManager()
		{
			if (From.HasValue)
			{
				return (ColorOffset)From.Value;
			}

			if (By.HasValue && To.HasValue)
			{
				return (ColorOffset)To.Value - (ColorOffset)By.Value;
			}

			var value = GetNonAnimatedValue();
			if (value is Color c)
			{
				return (ColorOffset)c;
			}
			if (value is ColorOffset co)
			{
				return co;
			}

			return default;
		}

		private ColorOffset ComputeToValueForTimeManager()
		{
			if (To.HasValue)
			{
				return (ColorOffset)To.Value;
			}

			if (By.HasValue)
			{
				return _tmFromValue + (ColorOffset)By.Value;
			}

			var value = GetNonAnimatedValue();
			if (value is Color c)
			{
				return (ColorOffset)c;
			}
			if (value is ColorOffset co)
			{
				return co;
			}

			return default;
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

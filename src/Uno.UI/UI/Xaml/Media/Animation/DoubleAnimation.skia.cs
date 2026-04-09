// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference: dxaml/xcp/core/animation/animation.cpp — CAnimation::UpdateAnimation (non-keyframe)

#if __SKIA__
using System;
using System.Globalization;

namespace Microsoft.UI.Xaml.Media.Animation
{
	public partial class DoubleAnimation
	{
		private bool _isTimeManagerDriven;
		private float _tmFromValue;
		private float _tmToValue;

		/// <summary>
		/// Called by Storyboard.ComputeState via child propagation.
		/// For From/To/By animations: computes timing (base), eases progress,
		/// interpolates value, applies.
		///
		/// MUX: CAnimation::UpdateAnimation (non-keyframe path, lines 153-173)
		/// </summary>
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
						// MUX: XFLOAT rEasedProgress = CEasingFunctionBase::EaseValue(m_pEasingFunction, m_rCurrentProgress)
						var progress = TimeManagerProgress;
						var easing = EasingFunction;

						double value;
						if (easing != null)
						{
							value = easing.Ease(progress, _tmFromValue, _tmToValue, 1.0);
						}
						else
						{
							value = _tmFromValue + (_tmToValue - _tmFromValue) * progress;
						}

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
				// Read From/To values at first Active tick (after layout).
				// MUX: CAnimation::OnBegin → ReadBaseValuesFromTargetOrHandoff
				PropertyInfo?.CloneShareableObjectsInPath();

				_tmFromValue = ComputeFromValueForTimeManager();
				_tmToValue = ComputeToValueForTimeManager();
			}
		}

		private float ComputeFromValueForTimeManager()
		{
			if (From.HasValue)
			{
				return (float)From.Value;
			}

			if (By.HasValue && To.HasValue)
			{
				return (float)(To.Value - By.Value);
			}

			// Read current property value.
			var value = GetNonAnimatedValue();
			if (value != null)
			{
				return Convert.ToSingle(value, CultureInfo.InvariantCulture);
			}

			return 0f;
		}

		private float ComputeToValueForTimeManager()
		{
			if (To.HasValue)
			{
				return (float)To.Value;
			}

			if (By.HasValue)
			{
				return _tmFromValue + (float)By.Value;
			}

			// No To or By: animate to current property value.
			var value = GetNonAnimatedValue();
			if (value != null)
			{
				return Convert.ToSingle(value, CultureInfo.InvariantCulture);
			}

			return 0f;
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

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference: dxaml/xcp/core/animation/PointAnimation.cpp — CPointAnimation::InterpolateCurrentValue

#if __SKIA__
using System;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Media.Animation
{
	public partial class PointAnimation
	{
		private bool _isTimeManagerDriven;
		private Point _tmFromValue;
		private Point _tmToValue;

		/// <summary>
		/// Called by Storyboard.ComputeState via child propagation.
		/// MUX: CPointAnimation::InterpolateCurrentValue — independent X/Y interpolation.
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

						Point value;
						if (easing != null)
						{
							var easedProgress = easing.Ease(progress);
							value = new Point(
								_tmFromValue.X + (_tmToValue.X - _tmFromValue.X) * easedProgress,
								_tmFromValue.Y + (_tmToValue.Y - _tmFromValue.Y) * easedProgress);
						}
						else
						{
							// MUX: m_ptCurrentValue.x = m_ptFrom.x * rPercentStart + rPercentEnd * m_ptTo.x
							value = new Point(
								_tmFromValue.X + (_tmToValue.X - _tmFromValue.X) * progress,
								_tmFromValue.Y + (_tmToValue.Y - _tmFromValue.Y) * progress);
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
				PropertyInfo?.CloneShareableObjectsInPath();

				_tmFromValue = ComputeFromValueForTimeManager();
				_tmToValue = ComputeToValueForTimeManager();
			}
		}

		private Point ComputeFromValueForTimeManager()
		{
			if (From.HasValue)
			{
				return From.Value;
			}

			if (By.HasValue && To.HasValue)
			{
				return new Point(To.Value.X - By.Value.X, To.Value.Y - By.Value.Y);
			}

			var value = GetNonAnimatedValue();
			if (value is Point p)
			{
				return p;
			}

			return default;
		}

		private Point ComputeToValueForTimeManager()
		{
			if (To.HasValue)
			{
				return To.Value;
			}

			if (By.HasValue)
			{
				return new Point(_tmFromValue.X + By.Value.X, _tmFromValue.Y + By.Value.Y);
			}

			var value = GetNonAnimatedValue();
			if (value is Point p)
			{
				return p;
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

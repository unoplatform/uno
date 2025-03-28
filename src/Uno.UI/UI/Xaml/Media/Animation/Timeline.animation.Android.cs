using Android.Animation;
using Android.Views;
using Android.Views.Animations;
using Uno.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Windows.UI.Xaml.Media.Animation
{
	partial class Timeline
	{
		partial class AnimationImplementation<T>
		{
			partial void OnFrame()
			{
				if (_endValue == null || _startingValue == null)
				{
					SetValue(null);
					return;
				}

				// TODO: apply easing function - https://github.com/unoplatform/uno/issues/2948

				if (Duration.HasTimeSpan && Duration.TimeSpan == TimeSpan.Zero)
				{
					Debug.Assert(_animator is ImmediateAnimator<T>);
					SetValue(_endValue.Value);
					return;
				}

				var totalDiff = AnimationOwner.Subtract(_endValue.Value, _startingValue.Value);
				var currentDiff = AnimationOwner.Multiply(((NativeValueAnimatorAdapter)_animator).AnimatedFraction, totalDiff);
				var currentValue = AnimationOwner.Add(_startingValue.Value, currentDiff);
				SetValue(currentValue);
			}

			partial void UseHardware()
			{
				var target = _owner?.Target;
				var view = target as View ?? (target as Transform)?.View;
				if (view != null)
				{
					view.SetLayerType(LayerType.Hardware, null);
					_animator.AnimationEnd += (sender, e) =>
					{
						view.SetLayerType(LayerType.None, null);
					};
				}
			}

			// For performance considerations, do not report each frame if we are GPU bound
			// Frame will be reported on Pause or End (cf. InitializeAnimator)
			private bool ReportEachFrame() => _owner.GetIsDependantAnimation() || _owner.GetIsDurationZero();
		}
	}
}

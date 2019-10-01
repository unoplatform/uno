using Android.Animation;
using Android.Views;
using Android.Views.Animations;
using Uno.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Windows.UI.Xaml.Media.Animation
{
	public partial class DoubleAnimation
	{
		partial void OnFrame()
		{
			var currentValue = _startingValue + (((NativeValueAnimatorAdapter)_animator).AnimatedFraction * (_endValue - _startingValue));
			SetValue(currentValue);
		}

		partial void UseHardware()
		{
			var view = Target as View;
			var transform = Target as Transform;

			if (transform != null)
			{
				view = transform.View;
			}

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
		// Frame will be repported on Pause or End (cf. InitializeAnimator)
		private bool ReportEachFrame() => this.GetIsDependantAnimation() || this.GetIsDurationZero();
	}
}

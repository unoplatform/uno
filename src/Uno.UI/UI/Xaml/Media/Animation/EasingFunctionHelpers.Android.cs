#nullable disable

using Android.Animation;
using Android.Views.Animations;
using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.UI.Xaml.Media.Animation
{
	internal static class EasingFunctionHelpers
	{
		internal static ITimeInterpolator GetPowerTimeInterpolator(double power, EasingMode mode)
		{
			switch (mode)
			{
				case EasingMode.EaseIn:
					return new AccelerateInterpolator((float)(power * 0.5));
				case EasingMode.EaseOut:
					return new DecelerateInterpolator((float)(power * 0.5));
				case EasingMode.EaseInOut:
					//We cannot set the power for AccelerateDecelerateInterpolator therefore we use the default one.
					return new AccelerateDecelerateInterpolator();
				default:
					throw new NotSupportedException("This easing mode is not supported.");
			}
		}
	}
}

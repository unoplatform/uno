using Android.Animation;
using Android.Views.Animations;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.UI.Xaml.Media.Animation
{
	internal static class EasingFunctionHelpers
	{
		internal static ITimeInterpolator GetPowerTimeInterpolator(float power, EasingMode mode)
		{
			switch (mode)
			{
				case EasingMode.EaseIn:
					return new AccelerateInterpolator(power * 0.5f);
				case EasingMode.EaseOut:
					return new DecelerateInterpolator(power * 0.5f);
				case EasingMode.EaseInOut:
					//We cannot set the power for AccelerateDecelerateInterpolator therefore we use the default one.
					return new AccelerateDecelerateInterpolator();
				default:
					throw new NotSupportedException("This easing mode is not supported.");
			}
		}
	}
}

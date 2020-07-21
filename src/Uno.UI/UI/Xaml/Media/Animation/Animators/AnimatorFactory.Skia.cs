using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Uno.Extensions;
using Uno.Logging;
using Windows.UI;

namespace Windows.UI.Xaml.Media.Animation
{
	internal static partial class AnimatorFactory
	{
		internal static IValueAnimator Create(Timeline timeline, double startingValue, double targetValue)
		{
			return new DispatcherFloatAnimator((float)startingValue, (float)targetValue);
		}

		private static IValueAnimator CreateDouble(Timeline timeline, double startingValue, double targetValue)
		{
			return new NotSupportedAnimator();
		}

		private static IValueAnimator CreateColor(Timeline timeline, ColorOffset startingValue, ColorOffset targetValue)
		{
			return new NotSupportedAnimator();
		}
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Windows.UI;

namespace Windows.UI.Xaml.Media.Animation
{
	internal static partial class AnimatorFactory
	{
		private static IValueAnimator CreateDouble(Timeline timeline, double startingValue, double targetValue)
			=> new DispatcherDoubleAnimator(startingValue, targetValue);

		private static IValueAnimator CreateColor(Timeline timeline, ColorOffset startingValue, ColorOffset targetValue)
			=> new ImmediateAnimator<ColorOffset>(targetValue);
	}
}

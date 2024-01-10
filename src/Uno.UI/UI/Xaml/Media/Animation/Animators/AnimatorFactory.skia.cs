using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Windows.UI;

namespace Microsoft.UI.Xaml.Media.Animation
{
	internal static partial class AnimatorFactory
	{
		internal static IValueAnimator Create(Timeline timeline, double startingValue, double targetValue)
			=> new DispatcherFloatAnimator((float)startingValue, (float)targetValue);

		private static IValueAnimator CreateDouble(Timeline timeline, double startingValue, double targetValue)
			=> new DispatcherDoubleAnimator(startingValue, targetValue);

		private static IValueAnimator CreateColor(Timeline timeline, ColorOffset startingValue, ColorOffset targetValue)
			=> new ImmediateAnimator<ColorOffset>(targetValue);
	}
}

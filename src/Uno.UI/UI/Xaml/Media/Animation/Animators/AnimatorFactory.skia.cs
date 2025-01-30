using System;
using Windows.UI;

namespace Microsoft.UI.Xaml.Media.Animation
{
	internal static partial class AnimatorFactory
	{
		private static IValueAnimator CreateDouble(Timeline timeline, double startingValue, double targetValue)
			=> new DispatcherDoubleAnimator(startingValue, targetValue);

		private static IValueAnimator CreateColor(Timeline timeline, ColorOffset startingValue, ColorOffset targetValue)
			=> new DispatcherColorAnimator(startingValue, targetValue);
	}
}

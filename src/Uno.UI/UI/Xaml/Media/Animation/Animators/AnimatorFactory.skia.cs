using System;
using Windows.Foundation;
using Windows.UI;

namespace Microsoft.UI.Xaml.Media.Animation
{
	internal static partial class AnimatorFactory
	{
		private static IValueAnimator CreateDouble(Timeline timeline, double startingValue, double targetValue)
			=> new DispatcherDoubleAnimator(startingValue, targetValue);

		private static IValueAnimator CreateColor(Timeline timeline, ColorOffset startingValue, ColorOffset targetValue)
			=> new DispatcherColorAnimator(startingValue, targetValue);

		private static IValueAnimator CreatePoint(Timeline timeline, Point startingValue, Point targetValue)
			=> new DispatcherPointAnimator(startingValue, targetValue);
	}
}

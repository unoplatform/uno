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
		/// <summary>
		/// Creates the actual animator instance
		/// </summary>
		private static IValueAnimator CreateDouble(Timeline timeline, double startingValue, double targetValue)
		{
			if (timeline.GetIsDurationZero())
			{
				// Avoid unnecessary JS interop in the case of a zero-duration animation
				return new ImmediateAnimator<double>(targetValue);
			}

			return new RenderingLoopFloatAnimator((float)startingValue, (float)targetValue);
		}

		private static IValueAnimator CreateColor(Timeline timeline, ColorOffset startingValue, ColorOffset targetValue)
		{
			if (timeline.GetIsDurationZero())
			{
				// Avoid unnecessary JS interop in the case of a zero-duration animation
				return new ImmediateAnimator<ColorOffset>(targetValue);
			}

			// TODO: GPU-bound color animations - https://github.com/unoplatform/uno/issues/2947

			return new RenderingLoopColorAnimator(startingValue, targetValue);
		}
	}
}

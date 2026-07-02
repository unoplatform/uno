using Windows.Foundation;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.UI.Xaml.Media.Animation
{
	internal class SplineEasingFunction : IEasingFunction
	{
		public KeySpline KeySpline { get; }

		public SplineEasingFunction(KeySpline keySpline)
		{
			KeySpline = keySpline;
		}

		public double Ease(double currentTime, double startValue, double finalValue, double duration)
		{
			// A null KeySpline holds the segment start value rather than throwing (MUX parity).
			if (KeySpline is null)
			{
				return startValue;
			}

			// 0 <= t <= 1
			var t = currentTime / duration;

			var splineProgress = KeySpline.GetSplineProgress(t);

			return startValue + (finalValue - startValue) * splineProgress;
		}
	}
}

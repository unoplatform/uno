using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI;

namespace Microsoft.UI.Xaml.Media.Animation
{
	partial class AnimatorFactory
	{
		/// <summary>
		/// Creates the actual animator instance
		/// </summary>
		internal static IValueAnimator Create<T>(Timeline timeline, T startingValue, T targetValue) where T : struct
		{
			if (timeline.Duration.HasTimeSpan && timeline.Duration.TimeSpan == TimeSpan.Zero)
			{
				return new ImmediateAnimator<T>(targetValue);
			}

			return CreatePlatformAnimator(timeline, startingValue, targetValue);
		}

		/// <summary>
		/// Creates an animator for a keyframe segment with a known duration.
		/// Uses <see cref="ImmediateAnimator{T}"/> for zero-duration segments so that
		/// time-0 keyframe values are applied synchronously within the same tick.
		/// This matches WinUI where the first TimeManager tick applies
		/// time-0 keyframe values before the render pass.
		/// </summary>
		internal static IValueAnimator Create<T>(Timeline timeline, T startingValue, T targetValue, TimeSpan segmentDuration) where T : struct
		{
			if (segmentDuration == TimeSpan.Zero)
			{
				return new ImmediateAnimator<T>(targetValue);
			}

			if (timeline.Duration.HasTimeSpan && timeline.Duration.TimeSpan == TimeSpan.Zero)
			{
				return new ImmediateAnimator<T>(targetValue);
			}

			return CreatePlatformAnimator(timeline, startingValue, targetValue);
		}

		private static IValueAnimator CreatePlatformAnimator<T>(Timeline timeline, T startingValue, T targetValue) where T : struct
		{
			if (startingValue is float startingFloat && targetValue is float targetFloat)
			{
				return CreateDouble(timeline, startingFloat, targetFloat);
			}

			if (startingValue is double startingDouble && targetValue is double targetDouble)
			{
				return CreateDouble(timeline, startingDouble, targetDouble);
			}

			if (startingValue is ColorOffset startingColor && targetValue is ColorOffset targetColor)
			{
				return CreateColor(timeline, startingColor, targetColor);
			}

			if (startingValue is Point startingPoint && targetValue is Point targetPoint)
			{
				return CreatePoint(timeline, startingPoint, targetPoint);
			}

			throw new NotSupportedException();
		}
	}
}

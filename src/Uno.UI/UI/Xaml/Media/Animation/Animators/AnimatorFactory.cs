using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;

namespace Windows.UI.Xaml.Media.Animation
{
	partial class AnimatorFactory
	{
		/// <summary>
		/// Creates the actual animator instance, new parameter autoReverse
		/// </summary>
		internal static IValueAnimator Create<T>(Timeline timeline, T startingValue, T targetValue, bool autoReverse) where T : struct
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
				if (autoReverse)
					return CreateColor(timeline, targetColor, startingColor);
				else
					return CreateColor(timeline, startingColor, targetColor);
			}

			throw new NotSupportedException();
		}
		/// <summary>
		/// Creates the actual animator instance
		/// </summary>
		internal static IValueAnimator Create<T>(Timeline timeline, T startingValue, T targetValue) where T : struct
		{
			return Create(timeline, startingValue, targetValue, false);
		}
	}
}

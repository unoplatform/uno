using CoreAnimation;
using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.UI.Xaml.Media.Animation
{
	internal static class EasingFunctionExtensions
	{
		/// <summary>
		/// Based on information found at :
		/// https://github.com/bfolder/UIView-Visuals/blob/master/CAMediaTimingFunction%2BAdditionalEquations.m
		/// Those easing function try to use a bezier to best match the equivalent easing function
		/// </summary>
		internal static CAMediaTimingFunction GetTimingFunction(this IEasingFunction easingFunction)
		{
			switch (easingFunction)
			{
				case QuadraticEase quadraticEase:
					return GetQuadraticTimingFunction(quadraticEase.EasingMode);

				case CubicEase cubicEase:
					return GetCubicTimingFunction(cubicEase.EasingMode);

				case QuarticEase quarticEase:
					return GetQuarticTimingFunction(quarticEase.EasingMode);

				case QuinticEase quinticEase:
					return GetQuinticTimingFunction(quinticEase.EasingMode);

				case SineEase sineEase:
					return GetSineTimingFunction(sineEase.EasingMode);

				case BackEase backEase:
					return GetBackTimingFunction(backEase.EasingMode);

				case PowerEase powerEase:
					return GetPowerTimingFunction(powerEase.Power, powerEase.EasingMode);

				// Fallback for unsupported base function
				case EasingFunctionBase ease:
					return GetDefaultTimingFunction(ease.EasingMode);

				case SplineEasingFunction splineEase:
					var cp1 = splineEase.KeySpline.ControlPoint1;
					var cp2 = splineEase.KeySpline.ControlPoint2;
					return CAMediaTimingFunction.FromControlPoints((float)cp1.X, (float)cp1.Y, (float)cp2.X, (float)cp2.Y);

				default:
					return GetDefaultTimingFunction();
			}
		}

		private static CAMediaTimingFunction GetQuadraticTimingFunction(EasingMode easingMode)
		{
			switch (easingMode)
			{
				case EasingMode.EaseIn:
					return CAMediaTimingFunction.FromControlPoints(0.55f, 0.085f, 0.68f, 0.53f);
				case EasingMode.EaseOut:
					return CAMediaTimingFunction.FromControlPoints(0.25f, 0.46f, 0.45f, 0.94f);
				case EasingMode.EaseInOut:
					return CAMediaTimingFunction.FromControlPoints(0.455f, 0.03f, 0.515f, 0.955f);
			}

			return GetDefaultTimingFunction();
		}

		private static CAMediaTimingFunction GetCubicTimingFunction(EasingMode easingMode)
		{
			switch (easingMode)
			{
				case EasingMode.EaseIn:
					return CAMediaTimingFunction.FromControlPoints(0.55f, 0.055f, 0.675f, 0.19f);
				case EasingMode.EaseOut:
					return CAMediaTimingFunction.FromControlPoints(0.215f, 0.61f, 0.355f, 1.0f);
				case EasingMode.EaseInOut:
					return CAMediaTimingFunction.FromControlPoints(0.645f, 0.045f, 0.355f, 1.0f);
			}

			return GetDefaultTimingFunction();
		}

		private static CAMediaTimingFunction GetQuarticTimingFunction(EasingMode easingMode)
		{
			switch (easingMode)
			{
				case EasingMode.EaseIn:
					return CAMediaTimingFunction.FromControlPoints(0.895f, 0.03f, 0.685f, 0.22f);
				case EasingMode.EaseOut:
					return CAMediaTimingFunction.FromControlPoints(0.165f, 0.84f, 0.44f, 1.0f);
				case EasingMode.EaseInOut:
					return CAMediaTimingFunction.FromControlPoints(0.77f, 0.0f, 0.175f, 1.0f);
			}

			return GetDefaultTimingFunction();
		}

		private static CAMediaTimingFunction GetQuinticTimingFunction(EasingMode easingMode)
		{
			switch (easingMode)
			{
				case EasingMode.EaseIn:
					return CAMediaTimingFunction.FromControlPoints(0.755f, 0.05f, 0.855f, 0.06f);
				case EasingMode.EaseOut:
					return CAMediaTimingFunction.FromControlPoints(0.23f, 1.0f, 0.320f, 1.0f);
				case EasingMode.EaseInOut:
					return CAMediaTimingFunction.FromControlPoints(0.86f, 0.0f, 0.07f, 1.0f);
			}

			return GetDefaultTimingFunction();
		}

		private static CAMediaTimingFunction GetSineTimingFunction(EasingMode easingMode)
		{
			switch (easingMode)
			{
				case EasingMode.EaseIn:
					return CAMediaTimingFunction.FromControlPoints(0.47f, 0.0f, 0.745f, 0.715f);
				case EasingMode.EaseOut:
					return CAMediaTimingFunction.FromControlPoints(0.39f, 0.575f, 0.565f, 1.0f);
				case EasingMode.EaseInOut:
					return CAMediaTimingFunction.FromControlPoints(0.445f, 0.05f, 0.55f, 0.95f);
			}

			return GetDefaultTimingFunction();
		}

		private static CAMediaTimingFunction GetBackTimingFunction(EasingMode easingMode)
		{
			switch (easingMode)
			{
				case EasingMode.EaseIn:
					return CAMediaTimingFunction.FromControlPoints(0.6f, -0.28f, 0.735f, 0.045f);
				case EasingMode.EaseOut:
					return CAMediaTimingFunction.FromControlPoints(0.175f, 0.885f, 0.320f, 1.275f);
				case EasingMode.EaseInOut:
					return CAMediaTimingFunction.FromControlPoints(0.68f, -0.55f, 0.265f, 1.55f);
			}

			return GetDefaultTimingFunction();
		}

		private static CAMediaTimingFunction GetPowerTimingFunction(double power, EasingMode easingMode)
		{
			switch (power)
			{
				case 1.0:
					return CAMediaTimingFunction.FromName(CAMediaTimingFunction.Linear);
				case 2.0:
					return GetQuadraticTimingFunction(easingMode);
				case 3.0:
					return GetCubicTimingFunction(easingMode);
				case 4.0:
					return GetQuarticTimingFunction(easingMode);
				case 5.0:
					return GetQuinticTimingFunction(easingMode);
				default:
					return CAMediaTimingFunction.FromName(CAMediaTimingFunction.Default);
			}
		}

		private static CAMediaTimingFunction GetDefaultTimingFunction(EasingMode? easingMode = null)
		{
			if (easingMode.HasValue)
			{
				switch (easingMode.Value)
				{
					case EasingMode.EaseIn:
						return CAMediaTimingFunction.FromName(CAMediaTimingFunction.EaseIn);
					case EasingMode.EaseOut:
						return CAMediaTimingFunction.FromName(CAMediaTimingFunction.EaseOut);
					case EasingMode.EaseInOut:
						return CAMediaTimingFunction.FromName(CAMediaTimingFunction.EaseInEaseOut);
				}
			}

			return CAMediaTimingFunction.FromName(CAMediaTimingFunction.Default);
		}
	}
}

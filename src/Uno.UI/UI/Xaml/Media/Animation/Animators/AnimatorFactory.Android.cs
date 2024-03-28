using Android.Animation;
using Android.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Windows.Foundation;
using Uno.UI;
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
			if (timeline.GetIsDependantAnimation() || timeline.GetIsDurationZero())
			{
				return new NativeValueAnimatorAdapter(ValueAnimator.OfFloat((float)startingValue, (float)targetValue));
			}
			else
			{
				return timeline.GetGPUAnimator(startingValue, targetValue);
			}
		}

		/// <summary>
		/// Creates the actual animator instance
		/// </summary>
		private static IValueAnimator CreateColor(Timeline timeline, ColorOffset startingValue, ColorOffset targetValue)
		{
			// TODO: GPU-bound color animations - https://github.com/unoplatform/uno/issues/2947

			var startingColor = (Android.Graphics.Color)(Color)startingValue;
			var targetColor = (Android.Graphics.Color)(Color)targetValue;
			var valueAnimator = ValueAnimator.OfArgb(startingColor, targetColor);
			return new NativeValueAnimatorAdapter(valueAnimator);
		}

		private static IValueAnimator GetGPUAnimator(this Timeline timeline, double startingValue, double targetValue)
		{
			// Overview    : http://developer.android.com/guide/topics/graphics/prop-animation.html#property-vs-view
			// Performance : http://developer.android.com/guide/topics/graphics/hardware-accel.html#layers-anims
			// Properties  : http://developer.android.com/guide/topics/graphics/prop-animation.html#views

			var (target, property) = timeline.PropertyInfo.GetTargetContextAndPropertyName();

			// note: implementation below should be mirrored in TryGetNativeAnimatedValue
			if (target is View view)
			{
				switch (property)
				{
					case nameof(FrameworkElement.Opacity):
						return new NativeValueAnimatorAdapter(GetRelativeAnimator(view, "alpha", startingValue, targetValue));
				}
			}

			if (target is TranslateTransform translate)
			{
				switch (property)
				{
					case nameof(TranslateTransform.X):
						return new NativeValueAnimatorAdapter(GetPixelsAnimator(translate.View, "translationX", startingValue, targetValue), PrepareTranslateX(translate, startingValue), Complete(translate));

					case nameof(TranslateTransform.Y):
						return new NativeValueAnimatorAdapter(GetPixelsAnimator(translate.View, "translationY", startingValue, targetValue), PrepareTranslateY(translate, startingValue), Complete(translate));
				}
			}

			if (target is RotateTransform rotate)
			{
				switch (property)
				{
					case nameof(RotateTransform.Angle):
						return new NativeValueAnimatorAdapter(GetRelativeAnimator(rotate.View, "rotation", startingValue, targetValue), PrepareAngle(rotate, startingValue), Complete(rotate));
				}
			}

			if (target is ScaleTransform scale)
			{
				var nativeStartingValue = ToNativeScale(startingValue);
				var nativeTargetValue = ToNativeScale(targetValue);

				switch (property)
				{
					case nameof(ScaleTransform.ScaleX):
						return new NativeValueAnimatorAdapter(GetRelativeAnimator(scale.View, "scaleX", nativeStartingValue, nativeTargetValue), PrepareScaleX(scale, nativeStartingValue), Complete(scale));

					case nameof(ScaleTransform.ScaleY):
						return new NativeValueAnimatorAdapter(GetRelativeAnimator(scale.View, "scaleY", nativeStartingValue, nativeTargetValue), PrepareScaleY(scale, nativeStartingValue), Complete(scale));
				}
			}

			//if (target is SkewTransform skew)
			//{
			//	switch (property)
			//	{
			//		case nameof(SkewTransform.AngleX):
			//			return ObjectAnimator.OfFloat(skew.View, "scaleX", ViewHelper.LogicalToPhysicalPixels(targetValue), startingValue);

			//		case nameof(SkewTransform.AngleY):
			//			return ObjectAnimator.OfFloat(skew.View, "scaleY", ViewHelper.LogicalToPhysicalPixels(targetValue), startingValue);
			//	}
			//}

			if (target is CompositeTransform composite)
			{

				switch (property)
				{
					case nameof(CompositeTransform.TranslateX):
						return new NativeValueAnimatorAdapter(GetPixelsAnimator(composite.View, "translationX", startingValue, targetValue), PrepareTranslateX(composite, startingValue), Complete(composite));

					case nameof(CompositeTransform.TranslateY):
						return new NativeValueAnimatorAdapter(GetPixelsAnimator(composite.View, "translationY", startingValue, targetValue), PrepareTranslateY(composite, startingValue), Complete(composite));

					case nameof(CompositeTransform.Rotation):
						return new NativeValueAnimatorAdapter(GetRelativeAnimator(composite.View, "rotation", startingValue, targetValue), PrepareAngle(composite, startingValue), Complete(composite));

					case nameof(CompositeTransform.ScaleX):
						var nativeStartingValueX = ToNativeScale(startingValue);
						var nativetargetValueX = ToNativeScale(targetValue);
						return new NativeValueAnimatorAdapter(GetRelativeAnimator(composite.View, "scaleX", nativeStartingValueX, nativetargetValueX), PrepareScaleX(composite, nativeStartingValueX), Complete(composite));

					case nameof(CompositeTransform.ScaleY):
						var nativeStartingValueY = ToNativeScale(startingValue);
						var nativetargetValueY = ToNativeScale(targetValue);
						return new NativeValueAnimatorAdapter(GetRelativeAnimator(composite.View, "scaleY", nativeStartingValueY, nativetargetValueY), PrepareScaleY(composite, nativeStartingValueY), Complete(composite));

						//case nameof(CompositeTransform.SkewX):
						//	return ObjectAnimator.OfFloat(composite.View, "scaleX", ViewHelper.LogicalToPhysicalPixels(targetValue), startingValue);

						//case nameof(CompositeTransform.SkewY):
						//	return ObjectAnimator.OfFloat(composite.View, "scaleY", ViewHelper.LogicalToPhysicalPixels(targetValue), startingValue);
				}
			}

			Application.Current.RaiseRecoverableUnhandledException(new NotSupportedException($"GPU bound animation of {property} is not supported on {target}. Use a discrete animation instead."));
			return new NativeValueAnimatorAdapter(ValueAnimator.OfFloat((float)startingValue, (float)targetValue));
		}

		internal static void UpdatePivotWhileAnimating(Transform transform, double pivotX, double pivotY)
		{
			switch (transform)
			{
				case RotateTransform rotate:
					rotate.View.PivotX = ViewHelper.LogicalToPhysicalPivotPixels(pivotX + rotate.CenterX);
					rotate.View.PivotY = ViewHelper.LogicalToPhysicalPivotPixels(pivotY + rotate.CenterY);
					break;

				case ScaleTransform scale:
					scale.View.PivotX = ViewHelper.LogicalToPhysicalPivotPixels(pivotX + scale.CenterX);
					scale.View.PivotY = ViewHelper.LogicalToPhysicalPivotPixels(pivotY + scale.CenterY);
					break;

				case CompositeTransform composite:
					composite.View.PivotX = ViewHelper.LogicalToPhysicalPivotPixels(pivotX + composite.CenterX);
					composite.View.PivotY = ViewHelper.LogicalToPhysicalPivotPixels(pivotY + composite.CenterY);
					break;
			}
		}

		private static void OverridePivot(View view, double centerX, double centerY)
		{
			if (view is IFrameworkElement elt)
			{
				var origin = elt.RenderTransformOrigin;

				view.PivotX = ViewHelper.LogicalToPhysicalPivotPixels(elt.ActualWidth * origin.X + centerX);
				view.PivotY = ViewHelper.LogicalToPhysicalPivotPixels(elt.ActualHeight * origin.Y + centerY);
			}
			else if (view != null)
			{
				view.PivotX = ViewHelper.LogicalToPhysicalPivotPixels(centerX);
				view.PivotY = ViewHelper.LogicalToPhysicalPivotPixels(centerY);
			}
		}

		private static void ResetPivot(View view)
		{
			view.PivotX = 0;
			view.PivotY = 0;
		}

		private static Action PrepareTranslateX(TranslateTransform translate, double from) => () =>
		{
			// Suspend the matrix transform
			translate.IsAnimating = true;

			// Apply transform using native values
			translate.View.TranslationX = ViewHelper.LogicalToPhysicalPixels(from);
			translate.View.TranslationY = ViewHelper.LogicalToPhysicalPixels(translate.Y);
		};

		private static Action PrepareTranslateY(TranslateTransform translate, double from) => () =>
		{
			// Suspend the matrix transform
			translate.IsAnimating = true;

			// Apply transform using native values
			translate.View.TranslationX = ViewHelper.LogicalToPhysicalPixels(translate.X);
			translate.View.TranslationY = ViewHelper.LogicalToPhysicalPixels(from);
		};

		private static Action PrepareAngle(RotateTransform rotate, double from) => () =>
		{
			// Suspend the matrix transform
			rotate.IsAnimating = true;

			// Apply transform using native values
			OverridePivot(rotate.View, rotate.CenterX, rotate.CenterY);
			rotate.View.Rotation = (float)from;
		};

		private static Action PrepareScaleX(ScaleTransform scale, double from) => () =>
		{
			// Suspend the matrix transform
			scale.IsAnimating = true;

			// Apply transform using native values
			OverridePivot(scale.View, scale.CenterX, scale.CenterY);
			scale.View.ScaleX = (float)ToNativeScale(from);
			scale.View.ScaleY = (float)ToNativeScale(scale.ScaleY);
		};

		private static Action PrepareScaleY(ScaleTransform scale, double from) => () =>
		{
			// Suspend the matrix transform
			scale.IsAnimating = true;

			// Apply transform using native values
			OverridePivot(scale.View, scale.CenterX, scale.CenterY);
			scale.View.ScaleX = (float)ToNativeScale(scale.ScaleX);
			scale.View.ScaleY = (float)ToNativeScale(from);
		};

		private static Action PrepareTranslateX(CompositeTransform composite, double from) => () =>
		{
			// Suspend the matrix transform
			composite.IsAnimating = true;

			// Apply transform using native values
			composite.View.TranslationX = ViewHelper.LogicalToPhysicalPixels(from);
			composite.View.TranslationY = ViewHelper.LogicalToPhysicalPixels(composite.TranslateY);
			composite.View.ScaleX = (float)ToNativeScale(composite.ScaleX);
			composite.View.ScaleY = (float)ToNativeScale(composite.ScaleY);
			composite.View.Rotation = (float)composite.Rotation;
		};

		private static Action PrepareTranslateY(CompositeTransform composite, double from) => () =>
		{
			// Suspend the matrix transform
			composite.IsAnimating = true;

			// Apply transform using native values
			composite.View.TranslationX = ViewHelper.LogicalToPhysicalPixels(composite.TranslateX);
			composite.View.TranslationY = ViewHelper.LogicalToPhysicalPixels(from);
			composite.View.ScaleX = (float)ToNativeScale(composite.ScaleX);
			composite.View.ScaleY = (float)ToNativeScale(composite.ScaleY);
			composite.View.Rotation = (float)composite.Rotation;
		};

		private static Action PrepareAngle(CompositeTransform composite, double from) => () =>
		{
			// Suspend the matrix transform
			composite.IsAnimating = true;

			// Apply transform using native values
			OverridePivot(composite.View, composite.CenterX, composite.CenterY);
			composite.View.TranslationX = ViewHelper.LogicalToPhysicalPixels(composite.TranslateX);
			composite.View.TranslationY = ViewHelper.LogicalToPhysicalPixels(composite.TranslateY);
			composite.View.ScaleX = (float)ToNativeScale(composite.ScaleX);
			composite.View.ScaleY = (float)ToNativeScale(composite.ScaleY);
			composite.View.Rotation = (float)from;
		};

		private static Action PrepareScaleX(CompositeTransform composite, double from) => () =>
		{
			// Suspend the matrix transform
			composite.IsAnimating = true;

			// Apply transform using native values
			OverridePivot(composite.View, composite.CenterX, composite.CenterY);
			composite.View.TranslationX = ViewHelper.LogicalToPhysicalPixels(composite.TranslateX);
			composite.View.TranslationY = ViewHelper.LogicalToPhysicalPixels(composite.TranslateY);
			composite.View.ScaleX = (float)ToNativeScale(from);
			composite.View.ScaleY = (float)ToNativeScale(composite.ScaleY);
			composite.View.Rotation = (float)composite.Rotation;
		};

		private static Action PrepareScaleY(CompositeTransform composite, double from) => () =>
		{
			// Suspend the matrix transform
			composite.IsAnimating = true;

			// Apply transform using native values
			OverridePivot(composite.View, composite.CenterX, composite.CenterY);
			composite.View.TranslationX = ViewHelper.LogicalToPhysicalPixels(composite.TranslateX);
			composite.View.TranslationY = ViewHelper.LogicalToPhysicalPixels(composite.TranslateY);
			composite.View.ScaleX = (float)ToNativeScale(composite.ScaleX);
			composite.View.ScaleY = (float)ToNativeScale(from);
			composite.View.Rotation = (float)composite.Rotation;
		};

		private static Action Complete(Transform transform) => () =>
		{
			// Remove the native values
			ResetPivot(transform.View);
			transform.View.TranslationX = 0;
			transform.View.TranslationY = 0;
			transform.View.ScaleX = 1;
			transform.View.ScaleY = 1;
			transform.View.Rotation = 0;

			// Restore the transform matrix (this will also Invalidate the view)
			transform.IsAnimating = false;
		};

		private static ValueAnimator GetPixelsAnimator(Java.Lang.Object target, string property, double from, double to)
		{
			return ObjectAnimator.OfFloat(target, property, ViewHelper.LogicalToPhysicalPixels(from), ViewHelper.LogicalToPhysicalPixels(to));
		}

		private static ValueAnimator GetRelativeAnimator(Java.Lang.Object target, string property, double from, double to)
		{
			return ObjectAnimator.OfFloat(target, property, (float)from, (float)to);
		}

		/// <summary>
		/// Ensures that scale value is within the android accepted values
		/// </summary>
		private static double ToNativeScale(double value)
			=> double.IsNaN(value) ? 1 : value;

		/// <summary>
		/// Get the underlying native property value which the target dependency property is associated with when animating.
		/// </summary>
		/// <param name="timeline"></param>
		/// <param name="value">The associated native animated value <b>in logical unit</b>.</param>
		/// <returns>Whether a native animation value is present and active.</returns>
		/// <remarks>The <paramref name="value"/> will be <b>in logical unit</b>, as the consumer of this method will have no idea which property are scaled or not.</remarks>
		internal static bool TryGetNativeAnimatedValue(Timeline timeline, out object value)
		{
			value = null;

			if (timeline.GetIsDependantAnimation() || timeline.GetIsDurationZero())
			{
				return false;
			}

			var (target, property) = timeline.PropertyInfo.GetTargetContextAndPropertyName();
			if (target is Transform { IsAnimating: false })
			{
				// While not animating, these native properties will be reset.
				// In that case, the dp actual value should be read instead (by returning false here).
				return false;
			}

			value = property switch
			{
				// note: Implementation here should be mirrored in GetGPUAnimator
				nameof(FrameworkElement.Opacity) when target is View view => (double)view.Alpha,

				nameof(TranslateTransform.X) when target is TranslateTransform translate => ViewHelper.PhysicalToLogicalPixels(translate.View.TranslationX),
				nameof(TranslateTransform.Y) when target is TranslateTransform translate => ViewHelper.PhysicalToLogicalPixels(translate.View.TranslationY),
				nameof(RotateTransform.Angle) when target is RotateTransform rotate => (double)rotate.View.Rotation,
				nameof(ScaleTransform.ScaleX) when target is ScaleTransform scale => (double)scale.View.ScaleX,
				nameof(ScaleTransform.ScaleY) when target is ScaleTransform scale => (double)scale.View.ScaleY,
				//nameof(SkewTransform.AngleX) when target is SkewTransform skew => ViewHelper.PhysicalToLogicalPixels(skew.View.ScaleX), // copied as is from GetGPUAnimator
				//nameof(SkewTransform.AngleY) when target is SkewTransform skew => ViewHelper.PhysicalToLogicalPixels(skew.View.ScaleY),

				nameof(CompositeTransform.TranslateX) when target is CompositeTransform composite => ViewHelper.PhysicalToLogicalPixels(composite.View.TranslationX),
				nameof(CompositeTransform.TranslateY) when target is CompositeTransform composite => ViewHelper.PhysicalToLogicalPixels(composite.View.TranslationY),
				nameof(CompositeTransform.Rotation) when target is CompositeTransform composite => (double)composite.View.Rotation,
				nameof(CompositeTransform.ScaleX) when target is CompositeTransform composite => (double)composite.View.ScaleX,
				nameof(CompositeTransform.ScaleY) when target is CompositeTransform composite => (double)composite.View.ScaleY,
				//nameof(CompositeTransform.SkewX) when target is CompositeTransform composite => ViewHelper.PhysicalToLogicalPixels(composite.View.ScaleX), // copied as is from GetGPUAnimator
				//nameof(CompositeTransform.SkewY) when target is CompositeTransform composite => ViewHelper.PhysicalToLogicalPixels(composite.View.ScaleY),

				_ => null,
			};

			return value != null;
		}
	}
}

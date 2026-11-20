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

namespace Microsoft.UI.Xaml.Media.Animation
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

			var startingColor = (AColor)(Color)startingValue;
			var targetColor = (AColor)(Color)targetValue;
			var valueAnimator = ValueAnimator.OfArgb(startingColor, targetColor);
			return new NativeValueAnimatorAdapter(valueAnimator);
		}

		private static IValueAnimator GetGPUAnimator(this Timeline timeline, double startingValue, double targetValue)
		{
			// Overview    : http://developer.android.com/guide/topics/graphics/prop-animation.html#property-vs-view
			// Performance : http://developer.android.com/guide/topics/graphics/hardware-accel.html#layers-anims
			// Properties  : http://developer.android.com/guide/topics/graphics/prop-animation.html#views

			var (target, property) = timeline.PropertyInfo.GetTargetContextAndPropertyName();

			var transformView = (target as Transform)?.View;
			if (target is Transform && transformView is null)
			{
				// The transform isn't applied to a live view, so there's nothing to animate natively.
				return new NativeValueAnimatorAdapter(ValueAnimator.OfFloat((float)startingValue, (float)targetValue));
			}

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
						return new NativeValueAnimatorAdapter(GetPixelsAnimator(transformView, "translationX", startingValue, targetValue), PrepareTranslateX(translate, startingValue), Complete(translate));

					case nameof(TranslateTransform.Y):
						return new NativeValueAnimatorAdapter(GetPixelsAnimator(transformView, "translationY", startingValue, targetValue), PrepareTranslateY(translate, startingValue), Complete(translate));
				}
			}

			if (target is RotateTransform rotate)
			{
				switch (property)
				{
					case nameof(RotateTransform.Angle):
						return new NativeValueAnimatorAdapter(GetRelativeAnimator(transformView, "rotation", startingValue, targetValue), PrepareAngle(rotate, startingValue), Complete(rotate));
				}
			}

			if (target is ScaleTransform scale)
			{
				var nativeStartingValue = ToNativeScale(startingValue);
				var nativeTargetValue = ToNativeScale(targetValue);

				switch (property)
				{
					case nameof(ScaleTransform.ScaleX):
						return new NativeValueAnimatorAdapter(GetRelativeAnimator(transformView, "scaleX", nativeStartingValue, nativeTargetValue), PrepareScaleX(scale, nativeStartingValue), Complete(scale));

					case nameof(ScaleTransform.ScaleY):
						return new NativeValueAnimatorAdapter(GetRelativeAnimator(transformView, "scaleY", nativeStartingValue, nativeTargetValue), PrepareScaleY(scale, nativeStartingValue), Complete(scale));
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
						return new NativeValueAnimatorAdapter(GetPixelsAnimator(transformView, "translationX", startingValue, targetValue), PrepareTranslateX(composite, startingValue), Complete(composite));

					case nameof(CompositeTransform.TranslateY):
						return new NativeValueAnimatorAdapter(GetPixelsAnimator(transformView, "translationY", startingValue, targetValue), PrepareTranslateY(composite, startingValue), Complete(composite));

					case nameof(CompositeTransform.Rotation):
						return new NativeValueAnimatorAdapter(GetRelativeAnimator(transformView, "rotation", startingValue, targetValue), PrepareAngle(composite, startingValue), Complete(composite));

					case nameof(CompositeTransform.ScaleX):
						var nativeStartingValueX = ToNativeScale(startingValue);
						var nativetargetValueX = ToNativeScale(targetValue);
						return new NativeValueAnimatorAdapter(GetRelativeAnimator(transformView, "scaleX", nativeStartingValueX, nativetargetValueX), PrepareScaleX(composite, nativeStartingValueX), Complete(composite));

					case nameof(CompositeTransform.ScaleY):
						var nativeStartingValueY = ToNativeScale(startingValue);
						var nativetargetValueY = ToNativeScale(targetValue);
						return new NativeValueAnimatorAdapter(GetRelativeAnimator(transformView, "scaleY", nativeStartingValueY, nativetargetValueY), PrepareScaleY(composite, nativeStartingValueY), Complete(composite));

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
			if (transform.View is not { } view)
			{
				return;
			}

			switch (transform)
			{
				case RotateTransform rotate:
					view.PivotX = ViewHelper.LogicalToPhysicalPivotPixels(pivotX + rotate.CenterX);
					view.PivotY = ViewHelper.LogicalToPhysicalPivotPixels(pivotY + rotate.CenterY);
					break;

				case ScaleTransform scale:
					view.PivotX = ViewHelper.LogicalToPhysicalPivotPixels(pivotX + scale.CenterX);
					view.PivotY = ViewHelper.LogicalToPhysicalPivotPixels(pivotY + scale.CenterY);
					break;

				case CompositeTransform composite:
					view.PivotX = ViewHelper.LogicalToPhysicalPivotPixels(pivotX + composite.CenterX);
					view.PivotY = ViewHelper.LogicalToPhysicalPivotPixels(pivotY + composite.CenterY);
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

			if (translate.View is not { } view)
			{
				return;
			}

			// Apply transform using native values
			view.TranslationX = ViewHelper.LogicalToPhysicalPixels(from);
			view.TranslationY = ViewHelper.LogicalToPhysicalPixels(translate.Y);
		};

		private static Action PrepareTranslateY(TranslateTransform translate, double from) => () =>
		{
			// Suspend the matrix transform
			translate.IsAnimating = true;

			if (translate.View is not { } view)
			{
				return;
			}

			// Apply transform using native values
			view.TranslationX = ViewHelper.LogicalToPhysicalPixels(translate.X);
			view.TranslationY = ViewHelper.LogicalToPhysicalPixels(from);
		};

		private static Action PrepareAngle(RotateTransform rotate, double from) => () =>
		{
			// Suspend the matrix transform
			rotate.IsAnimating = true;

			if (rotate.View is not { } view)
			{
				return;
			}

			// Apply transform using native values
			OverridePivot(view, rotate.CenterX, rotate.CenterY);
			view.Rotation = (float)from;
		};

		private static Action PrepareScaleX(ScaleTransform scale, double from) => () =>
		{
			// Suspend the matrix transform
			scale.IsAnimating = true;

			if (scale.View is not { } view)
			{
				return;
			}

			// Apply transform using native values
			OverridePivot(view, scale.CenterX, scale.CenterY);
			view.ScaleX = (float)ToNativeScale(from);
			view.ScaleY = (float)ToNativeScale(scale.ScaleY);
		};

		private static Action PrepareScaleY(ScaleTransform scale, double from) => () =>
		{
			// Suspend the matrix transform
			scale.IsAnimating = true;

			if (scale.View is not { } view)
			{
				return;
			}

			// Apply transform using native values
			OverridePivot(view, scale.CenterX, scale.CenterY);
			view.ScaleX = (float)ToNativeScale(scale.ScaleX);
			view.ScaleY = (float)ToNativeScale(from);
		};

		private static Action PrepareTranslateX(CompositeTransform composite, double from) => () =>
		{
			// Suspend the matrix transform
			composite.IsAnimating = true;

			if (composite.View is not { } view)
			{
				return;
			}

			// Apply transform using native values
			view.TranslationX = ViewHelper.LogicalToPhysicalPixels(from);
			view.TranslationY = ViewHelper.LogicalToPhysicalPixels(composite.TranslateY);
			view.ScaleX = (float)ToNativeScale(composite.ScaleX);
			view.ScaleY = (float)ToNativeScale(composite.ScaleY);
			view.Rotation = (float)composite.Rotation;
		};

		private static Action PrepareTranslateY(CompositeTransform composite, double from) => () =>
		{
			// Suspend the matrix transform
			composite.IsAnimating = true;

			if (composite.View is not { } view)
			{
				return;
			}

			// Apply transform using native values
			view.TranslationX = ViewHelper.LogicalToPhysicalPixels(composite.TranslateX);
			view.TranslationY = ViewHelper.LogicalToPhysicalPixels(from);
			view.ScaleX = (float)ToNativeScale(composite.ScaleX);
			view.ScaleY = (float)ToNativeScale(composite.ScaleY);
			view.Rotation = (float)composite.Rotation;
		};

		private static Action PrepareAngle(CompositeTransform composite, double from) => () =>
		{
			// Suspend the matrix transform
			composite.IsAnimating = true;

			if (composite.View is not { } view)
			{
				return;
			}

			// Apply transform using native values
			OverridePivot(view, composite.CenterX, composite.CenterY);
			view.TranslationX = ViewHelper.LogicalToPhysicalPixels(composite.TranslateX);
			view.TranslationY = ViewHelper.LogicalToPhysicalPixels(composite.TranslateY);
			view.ScaleX = (float)ToNativeScale(composite.ScaleX);
			view.ScaleY = (float)ToNativeScale(composite.ScaleY);
			view.Rotation = (float)from;
		};

		private static Action PrepareScaleX(CompositeTransform composite, double from) => () =>
		{
			// Suspend the matrix transform
			composite.IsAnimating = true;

			if (composite.View is not { } view)
			{
				return;
			}

			// Apply transform using native values
			OverridePivot(view, composite.CenterX, composite.CenterY);
			view.TranslationX = ViewHelper.LogicalToPhysicalPixels(composite.TranslateX);
			view.TranslationY = ViewHelper.LogicalToPhysicalPixels(composite.TranslateY);
			view.ScaleX = (float)ToNativeScale(from);
			view.ScaleY = (float)ToNativeScale(composite.ScaleY);
			view.Rotation = (float)composite.Rotation;
		};

		private static Action PrepareScaleY(CompositeTransform composite, double from) => () =>
		{
			// Suspend the matrix transform
			composite.IsAnimating = true;

			if (composite.View is not { } view)
			{
				return;
			}

			// Apply transform using native values
			OverridePivot(view, composite.CenterX, composite.CenterY);
			view.TranslationX = ViewHelper.LogicalToPhysicalPixels(composite.TranslateX);
			view.TranslationY = ViewHelper.LogicalToPhysicalPixels(composite.TranslateY);
			view.ScaleX = (float)ToNativeScale(composite.ScaleX);
			view.ScaleY = (float)ToNativeScale(from);
			view.Rotation = (float)composite.Rotation;
		};

		private static Action Complete(Transform transform) => () =>
		{
			// Remove the native values
			if (transform.View is { } view)
			{
				ResetPivot(view);
				view.TranslationX = 0;
				view.TranslationY = 0;
				view.ScaleX = 1;
				view.ScaleY = 1;
				view.Rotation = 0;
			}

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

			var transformView = (target as Transform)?.View;
			if (target is Transform && transformView is null)
			{
				return false;
			}

			value = property switch
			{
				// note: Implementation here should be mirrored in GetGPUAnimator
				nameof(FrameworkElement.Opacity) when target is View view => (double)view.Alpha,

				nameof(TranslateTransform.X) when target is TranslateTransform => ViewHelper.PhysicalToLogicalPixels(transformView.TranslationX),
				nameof(TranslateTransform.Y) when target is TranslateTransform => ViewHelper.PhysicalToLogicalPixels(transformView.TranslationY),
				nameof(RotateTransform.Angle) when target is RotateTransform => (double)transformView.Rotation,
				nameof(ScaleTransform.ScaleX) when target is ScaleTransform => (double)transformView.ScaleX,
				nameof(ScaleTransform.ScaleY) when target is ScaleTransform => (double)transformView.ScaleY,
				//nameof(SkewTransform.AngleX) when target is SkewTransform skew => ViewHelper.PhysicalToLogicalPixels(skew.View.ScaleX), // copied as is from GetGPUAnimator
				//nameof(SkewTransform.AngleY) when target is SkewTransform skew => ViewHelper.PhysicalToLogicalPixels(skew.View.ScaleY),

				nameof(CompositeTransform.TranslateX) when target is CompositeTransform => ViewHelper.PhysicalToLogicalPixels(transformView.TranslationX),
				nameof(CompositeTransform.TranslateY) when target is CompositeTransform => ViewHelper.PhysicalToLogicalPixels(transformView.TranslationY),
				nameof(CompositeTransform.Rotation) when target is CompositeTransform => (double)transformView.Rotation,
				nameof(CompositeTransform.ScaleX) when target is CompositeTransform => (double)transformView.ScaleX,
				nameof(CompositeTransform.ScaleY) when target is CompositeTransform => (double)transformView.ScaleY,
				//nameof(CompositeTransform.SkewX) when target is CompositeTransform composite => ViewHelper.PhysicalToLogicalPixels(composite.View.ScaleX), // copied as is from GetGPUAnimator
				//nameof(CompositeTransform.SkewY) when target is CompositeTransform composite => ViewHelper.PhysicalToLogicalPixels(composite.View.ScaleY),

				_ => null,
			};

			return value != null;
		}
	}
}

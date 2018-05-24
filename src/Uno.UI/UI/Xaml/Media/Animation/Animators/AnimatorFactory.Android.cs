using Android.Animation;
using Android.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Uno.UI;

namespace Windows.UI.Xaml.Media.Animation
{
    internal static partial class AnimatorFactory
    {
		private static readonly string __notSupportedProperty = "This property is not supported by GPU enabled animations.";

		/// <summary>
		/// Creates the actual animator instance
		/// </summary>
		internal static IValueAnimator Create(Timeline timeline, double startingValue, double targetValue)
		{
			if (timeline.GetIsDependantAnimation() || timeline.GetIsDurationZero())
			{
				return new NativeValueAnimatorAdapter(ValueAnimator.OfFloat((float)startingValue, (float)targetValue));
			}
			else
			{
				return new NativeValueAnimatorAdapter(timeline.GetGPUAnimator(startingValue, targetValue));
			}
		}

		private static ValueAnimator GetGPUAnimator(this Timeline timeline, double startingValue, double targetValue)
		{
			// Overview    : http://developer.android.com/guide/topics/graphics/prop-animation.html#property-vs-view
			// Performance : http://developer.android.com/guide/topics/graphics/hardware-accel.html#layers-anims
			// Properties  : http://developer.android.com/guide/topics/graphics/prop-animation.html#views

			var info = timeline.PropertyInfo.GetPathItems().Last();
			var target = info.DataContext;
			var property = info.PropertyName.Split('.').Last().Replace("(", "").Replace(")", "");

			if (target is View view)
			{
				switch (property)
				{
					case nameof(FrameworkElement.Opacity):
						return GetRelativeAnimator(view, "alpha", startingValue, targetValue);
				}
			}

			if (target is TranslateTransform translate)
			{
				switch (property)
				{
					case nameof(TranslateTransform.X):
						return GetPixelsAnimator(translate.View, "translationX", startingValue, targetValue);

					case nameof(TranslateTransform.Y):
						return GetPixelsAnimator(translate.View, "translationY", startingValue, targetValue);
				}
			}

			if (target is RotateTransform rotate)
			{
				switch (property)
				{
					case nameof(RotateTransform.Angle):
						return GetRelativeAnimator(rotate.View, "rotation", startingValue, targetValue);
				}
			}

			if (target is ScaleTransform scale)
			{
				switch (property)
				{
					case nameof(ScaleTransform.ScaleX):
						return GetRelativeAnimator(scale.View, "scaleX", startingValue, targetValue);

					case nameof(ScaleTransform.ScaleY):
						return GetRelativeAnimator(scale.View, "scaleY", startingValue, targetValue);
				}
			}

			//if (target is SkewTransform skew)
			//{
			//	switch (property)
			//	{
			//		case nameof(SkewTransform.AngleX):
			//			return ObjectAnimator.OfFloat(skew.View, "scaleX", ViewHelper.LogicalToPhysicalPixels(targetValue), startingValue);
			//
			//		case nameof(SkewTransform.AngleY):
			//			return ObjectAnimator.OfFloat(skew.View, "scaleY", ViewHelper.LogicalToPhysicalPixels(targetValue), startingValue);
			//	}
			//}

			if (target is CompositeTransform composite)
			{
				switch (property)
				{
					case nameof(CompositeTransform.TranslateX):
						return GetPixelsAnimator(composite.View, "translationX", startingValue, targetValue);

					case nameof(CompositeTransform.TranslateY):
						return GetPixelsAnimator(composite.View, "translationY", startingValue, targetValue);

					case nameof(CompositeTransform.Rotation):
						return GetRelativeAnimator(composite.View, "rotation", startingValue, targetValue);

					case nameof(CompositeTransform.ScaleX):
						return GetRelativeAnimator(composite.View, "scaleX", startingValue, targetValue);

					case nameof(CompositeTransform.ScaleY):
						return GetRelativeAnimator(composite.View, "scaleY", startingValue, targetValue);

					//case nameof(CompositeTransform.SkewX):
					//	return ObjectAnimator.OfFloat(composite.View, "scaleX", ViewHelper.LogicalToPhysicalPixels(targetValue), startingValue);

					//case nameof(CompositeTransform.SkewY):
					//	return ObjectAnimator.OfFloat(composite.View, "scaleY", ViewHelper.LogicalToPhysicalPixels(targetValue), startingValue);
				}
			}

			throw new NotSupportedException(__notSupportedProperty);
		}

		private static ValueAnimator GetPixelsAnimator(Java.Lang.Object target, string property, double from, double to)
		{
			return ObjectAnimator.OfFloat(target, property, ViewHelper.LogicalToPhysicalPixels(from), ViewHelper.LogicalToPhysicalPixels(to));
		}

		private static ValueAnimator GetRelativeAnimator(Java.Lang.Object target, string property, double from, double to)
		{
			return ObjectAnimator.OfFloat(target, property, (float)from, (float)to);
		}
	}
}

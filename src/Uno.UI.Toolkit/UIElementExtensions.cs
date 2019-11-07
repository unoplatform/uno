using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Uno.UI.Toolkit
{
#if __IOS__
	[Foundation.PreserveAttribute(AllMembers = true)]
#elif __ANDROID__
	[Android.Runtime.PreserveAttribute(AllMembers = true)]
#endif
	public static class UIElementExtensions
	{
		#region Elevation

		public static void SetElevation(this UIElement element, double Elevation)
		{
			element.SetValue(ElevationProperty, Elevation);
		}

		public static double GetElevation(this UIElement element)
		{
			return (double)element.GetValue(ElevationProperty);
		}

		public static DependencyProperty ElevationProperty { get; } =
			DependencyProperty.RegisterAttached(
				"Elevation",
				typeof(double),
				typeof(UIElementExtensions),
				new PropertyMetadata(0, OnElevationChanged)
			);

		private static void OnElevationChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
		{
#if __ANDROID__
			if (dependencyObject is Android.Views.View view && args.NewValue is double elevation)
			{
				AndroidX.Core.View.ViewCompat.SetElevation(view, (float)Uno.UI.ViewHelper.LogicalToPhysicalPixels(elevation));
			}
#elif __IOS__
			if (dependencyObject is UIKit.UIView view && args.NewValue is double elevation)
			{
				// Values for 1dp elevation according to https://material.io/guidelines/resources/shadows.html#shadows-illustrator
				const float Opacity = 0.26f;
				const float X = 0;
				const float Y = 0.92f * 0.5f; // Looks more accurate than the recommended 0.92f. 
				const float Blur = 0.5f;

				view.Layer.MasksToBounds = false;
				view.Layer.ShadowOpacity = Opacity;
				view.Layer.ShadowRadius = (nfloat)(Blur * elevation);
				view.Layer.ShadowOffset = new CoreGraphics.CGSize(X * elevation, Y * elevation);
			}
#endif
		}

		#endregion
	}
}

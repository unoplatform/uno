using System;
using System.Drawing;
using System.Runtime.InteropServices;
using Uno.UI;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Windows.UI.Xaml.Media.Animation;

#if XAMARIN_ANDROID
using Android.Views;
using NativeView = Android.Views.View;
#elif XAMARIN_IOS
using NativeView = UIKit.UIView;
#else
using NativeView = System.Object;
#endif

namespace Windows.UI.Xaml.Controls
{
	public partial class Canvas : Panel
	{
		#region Left

		public static double GetLeft(DependencyObject obj)
		{
			return (double)obj.GetValue(LeftProperty);
		}

		public static void SetLeft(DependencyObject obj, double value)
		{
			obj.SetValue(LeftProperty, value);
		}

		public static readonly DependencyProperty LeftProperty =
			DependencyProperty.RegisterAttached(
				"Left",
				typeof(double), 
				typeof(Canvas),
				new FrameworkPropertyMetadata(
					defaultValue: 0d,
					propertyChangedCallback: OnLeftChanged,
					options: FrameworkPropertyMetadataOptions.AutoConvert
				)
			);

		private static void OnLeftChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
		{
			(dependencyObject as IFrameworkElement)?.InvalidateArrange();

#if __WASM__
			if (FeatureConfiguration.UIElement.AssignDOMXamlProperties && dependencyObject is UIElement element)
			{
				element.UpdateDOMXamlProperty("Canvas.Left", args.NewValue);
			}
#endif
		}

		#endregion

		#region Top

		public static double GetTop(DependencyObject obj)
		{
			return (double)obj.GetValue(TopProperty);
		}

		public static void SetTop(DependencyObject obj, double value)
		{
			obj.SetValue(TopProperty, value);
		}

		public static readonly DependencyProperty TopProperty =
			DependencyProperty.RegisterAttached(
				"Top", 
				typeof(double), 
				typeof(Canvas), 
				new FrameworkPropertyMetadata(
					defaultValue: 0d,
					propertyChangedCallback: OnTopChanged,
					options: FrameworkPropertyMetadataOptions.AutoConvert
				)
			);

		private static void OnTopChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
		{
			(dependencyObject as IFrameworkElement)?.InvalidateArrange();

#if __WASM__
			if (FeatureConfiguration.UIElement.AssignDOMXamlProperties && dependencyObject is UIElement element)
			{
				element.UpdateDOMXamlProperty("Canvas.Top", args.NewValue);
			}
#endif
		}

		#endregion

		#region ZIndex

		public static double GetZIndex(DependencyObject obj)
		{
			return (double)obj.GetValue(ZIndexProperty);
		}

		public static void SetZIndex(DependencyObject obj, double value)
		{
			obj.SetValue(ZIndexProperty, value);
		}

		public static readonly DependencyProperty ZIndexProperty =
			DependencyProperty.RegisterAttached(
				"ZIndex",
				typeof(double), 
				typeof(Canvas), 
				new FrameworkPropertyMetadata(
					defaultValue: 0d,
					propertyChangedCallback: OnZIndexChanged,
					options: FrameworkPropertyMetadataOptions.AutoConvert
				)
			);

		private static void OnZIndexChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
		{
			(dependencyObject as IFrameworkElement)?.InvalidateArrange();
		}

		#endregion

		public Canvas()
		{
			InitializePartial();
		}

		partial void InitializePartial();

		public static double GetLeft(global::Windows.UI.Xaml.UIElement element) => GetLeft((DependencyObject)element);

		public static void SetLeft(global::Windows.UI.Xaml.UIElement element, double length) => SetLeft((DependencyObject)element, length);

		public static double GetTop(global::Windows.UI.Xaml.UIElement element) => GetTop((DependencyObject)element);

		public static void SetTop(global::Windows.UI.Xaml.UIElement element, double length) => SetTop((DependencyObject)element, length);

		public static int GetZIndex(global::Windows.UI.Xaml.UIElement element) => (int)GetZIndex((DependencyObject)element);

		public static void SetZIndex(global::Windows.UI.Xaml.UIElement element, int value) => SetZIndex((DependencyObject)element, value);
	}
}

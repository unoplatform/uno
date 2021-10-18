using System;
using System.Drawing;
using System.Runtime.InteropServices;
using Uno.UI;
using Uno.UI.Xaml;
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
			=> GetLeftValue(obj);

		public static void SetLeft(DependencyObject obj, double value)
			=> SetLeftValue(obj, value);


		[GeneratedDependencyProperty(DefaultValue = 0.0d, AttachedBackingFieldOwner = typeof(UIElement), Attached = true, Options = FrameworkPropertyMetadataOptions.AutoConvert)]
		public static DependencyProperty LeftProperty { get ; } = CreateLeftProperty();

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
			=> GetTopValue(obj);

		public static void SetTop(DependencyObject obj, double value)
			=> SetTopValue(obj, value);

		[GeneratedDependencyProperty(DefaultValue = 0.0d, AttachedBackingFieldOwner = typeof(UIElement), Attached = true, Options = FrameworkPropertyMetadataOptions.AutoConvert)]
		public static DependencyProperty TopProperty { get ; } = CreateTopProperty();

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
		[GeneratedDependencyProperty(DefaultValue = 0, AttachedBackingFieldOwner = typeof(UIElement), Attached = true, Options = FrameworkPropertyMetadataOptions.AutoConvert)]
		public static DependencyProperty ZIndexProperty { get ; } = CreateZIndexProperty();

		private static void OnZIndexChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
		{
#if !__WASM__
			(dependencyObject as IFrameworkElement)?.InvalidateArrange();
#endif
			if (dependencyObject is UIElement element)
			{
				var zindex = args.NewValue is int i ? (int?)i : null;
				OnZIndexChangedPartial(element, zindex);
			}
		}

		static partial void OnZIndexChangedPartial(UIElement element, int? zindex);

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

		public static int GetZIndex(global::Windows.UI.Xaml.UIElement element) => GetZIndexValue(element);

		public static void SetZIndex(global::Windows.UI.Xaml.UIElement element, int value) => SetZIndexValue(element, value);
	}
}

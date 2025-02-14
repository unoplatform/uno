using System;
using System.Drawing;
using System.Runtime.InteropServices;
using Uno.UI;
using Uno.UI.Xaml;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Windows.UI.Xaml.Media.Animation;

#if __ANDROID__
using Android.Views;
using NativeView = Android.Views.View;
#elif __IOS__
using NativeView = UIKit.UIView;
#else
using NativeView = System.Object;
#endif

namespace Windows.UI.Xaml.Controls
{
	public partial class Canvas : Panel
	{
		#region Left

		[GeneratedDependencyProperty(DefaultValue = 0.0d, AttachedBackingFieldOwner = typeof(UIElement), Attached = true, Options = FrameworkPropertyMetadataOptions.AutoConvert | FrameworkPropertyMetadataOptions.AffectsArrange)]
		public static DependencyProperty LeftProperty { get; } = CreateLeftProperty();

		private static void OnLeftChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
		{
			if (dependencyObject is IFrameworkElement { Parent: IFrameworkElement parent })
			{
				parent.InvalidateArrange();
			}

#if __WASM__
			if (FeatureConfiguration.UIElement.AssignDOMXamlProperties && dependencyObject is UIElement element)
			{
				element.UpdateDOMXamlProperty("Canvas.Left", args.NewValue);
			}
#endif
		}

		#endregion

		#region Top

		[GeneratedDependencyProperty(DefaultValue = 0.0d, AttachedBackingFieldOwner = typeof(UIElement), Attached = true, Options = FrameworkPropertyMetadataOptions.AutoConvert | FrameworkPropertyMetadataOptions.AffectsArrange)]
		public static DependencyProperty TopProperty { get; } = CreateTopProperty();

		private static void OnTopChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
		{
			if (dependencyObject is IFrameworkElement { Parent: IFrameworkElement parent })
			{
				parent.InvalidateArrange();
			}

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
		public static DependencyProperty ZIndexProperty { get; } = CreateZIndexProperty();

		private static void OnZIndexChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
		{
#if !__WASM__
			(dependencyObject as IFrameworkElement)?.InvalidateArrange();
#endif
			if (dependencyObject is UIElement element)
			{
				var zindex = args.NewValue is int d ? (int?)d : null;
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

		public static double GetLeft(global::Windows.UI.Xaml.UIElement element) => GetLeftValue(element);

		public static void SetLeft(global::Windows.UI.Xaml.UIElement element, double length) => SetLeftValue(element, length);

		public static double GetTop(global::Windows.UI.Xaml.UIElement element) => GetTopValue(element);

		public static void SetTop(global::Windows.UI.Xaml.UIElement element, double length) => SetTopValue(element, length);

		public static int GetZIndex(global::Windows.UI.Xaml.UIElement element) => GetZIndexValue(element);

		public static void SetZIndex(global::Windows.UI.Xaml.UIElement element, int value) => SetZIndexValue(element, value);
	}
}

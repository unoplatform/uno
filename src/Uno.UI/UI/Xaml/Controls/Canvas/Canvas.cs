using System;
using System.Drawing;
using System.Runtime.InteropServices;
using Uno.UI;
using Uno.UI.Xaml;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Microsoft.UI.Xaml.Media.Animation;

using NativeView = System.Object;

namespace Microsoft.UI.Xaml.Controls
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
		}

		#endregion

		#region ZIndex

		[GeneratedDependencyProperty(DefaultValue = 0, AttachedBackingFieldOwner = typeof(UIElement), Attached = true, Options = FrameworkPropertyMetadataOptions.AutoConvert)]
		public static DependencyProperty ZIndexProperty { get; } = CreateZIndexProperty();

		private static void OnZIndexChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
		{
			(dependencyObject as IFrameworkElement)?.InvalidateArrange();
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

		public static double GetLeft(global::Microsoft.UI.Xaml.UIElement element) => GetLeftValue(element);

		public static void SetLeft(global::Microsoft.UI.Xaml.UIElement element, double length) => SetLeftValue(element, length);

		public static double GetTop(global::Microsoft.UI.Xaml.UIElement element) => GetTopValue(element);

		public static void SetTop(global::Microsoft.UI.Xaml.UIElement element, double length) => SetTopValue(element, length);

		public static int GetZIndex(global::Microsoft.UI.Xaml.UIElement element) => GetZIndexValue(element);

		public static void SetZIndex(global::Microsoft.UI.Xaml.UIElement element, int value) => SetZIndexValue(element, value);
	}
}

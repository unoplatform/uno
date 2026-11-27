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

		[GeneratedDependencyProperty(DefaultValue = 0.0d, AttachedBackingFieldOwner = typeof(UIElement), Options = FrameworkPropertyMetadataOptions.AutoConvert | FrameworkPropertyMetadataOptions.AffectsArrange)]
		public static partial double GetLeft(global::Microsoft.UI.Xaml.UIElement element);

		public static partial void SetLeft(global::Microsoft.UI.Xaml.UIElement element, double length);

		private static void OnLeftChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
		{
			if (dependencyObject is IFrameworkElement { Parent: FrameworkElement parent })
			{
				parent.InvalidateArrange();
			}
		}

		#endregion

		#region Top

		[GeneratedDependencyProperty(DefaultValue = 0.0d, AttachedBackingFieldOwner = typeof(UIElement), Options = FrameworkPropertyMetadataOptions.AutoConvert | FrameworkPropertyMetadataOptions.AffectsArrange)]
		public static partial double GetTop(global::Microsoft.UI.Xaml.UIElement element);

		public static partial void SetTop(global::Microsoft.UI.Xaml.UIElement element, double length);

		private static void OnTopChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
		{
			if (dependencyObject is IFrameworkElement { Parent: FrameworkElement parent })
			{
				parent.InvalidateArrange();
			}
		}

		#endregion

		#region ZIndex

		[GeneratedDependencyProperty(DefaultValue = 0, AttachedBackingFieldOwner = typeof(UIElement), Options = FrameworkPropertyMetadataOptions.AutoConvert)]
		public static partial int GetZIndex(global::Microsoft.UI.Xaml.UIElement element);

		public static partial void SetZIndex(global::Microsoft.UI.Xaml.UIElement element, int value);

		private static void OnZIndexChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
		{
			if (dependencyObject is IFrameworkElement)
			{
				dependencyObject.InvalidateArrange();
			}
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

		static partial void OnZIndexChangedPartial(UIElement element, int? zindex)
		{
			element.Visual.ZIndex = (int)zindex;
			element._children.ClearCachedReverseSortedList();
		}
	}
}

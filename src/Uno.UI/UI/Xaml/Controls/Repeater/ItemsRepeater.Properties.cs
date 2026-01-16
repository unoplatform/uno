// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ItemsRepeater.properties.h, tag winui3/release/1.8.4

using System;
using System.Linq;
using Microsoft.UI.Xaml;

namespace Microsoft.UI.Xaml.Controls
{
	partial class ItemsRepeater
	{
		private static void OnPropertyChanged(DependencyObject snd, DependencyPropertyChangedEventArgs args)
			=> ((ItemsRepeater)snd).OnPropertyChanged(args);

		#region Background (DP) => Commented out as FwElt already has it ...
		//public static DependencyProperty BackgroundProperty { get; } = DependencyProperty.Register(
		//	"Background", typeof(Brush), typeof(ItemsRepeater), new FrameworkPropertyMetadata(default(Brush)));

		//public Brush Background
		//{
		//	get => (Brush)GetValue(BackgroundProperty);
		//	set => SetValue(BackgroundProperty, value);
		//}
		#endregion

		#region ItemsSource (DP - With default callback)
		public static DependencyProperty ItemsSourceProperty { get; } = DependencyProperty.Register(
			"ItemsSource", typeof(object), typeof(ItemsRepeater), new FrameworkPropertyMetadata(default(object), OnPropertyChanged));

		public object ItemsSource
		{
			get => (object)GetValue(ItemsSourceProperty);
			set => SetValue(ItemsSourceProperty, value);
		}
		#endregion

		#region ItemTemplate (DP - With default callback)
		public static DependencyProperty ItemTemplateProperty { get; } = DependencyProperty.Register(
			"ItemTemplate", typeof(object), typeof(ItemsRepeater), new FrameworkPropertyMetadata(default(object), OnPropertyChanged));

		public object ItemTemplate
		{
			get => (object)GetValue(ItemTemplateProperty);
			set => SetValue(ItemTemplateProperty, value);
		}
		#endregion

		#region Layout (DP - With default callback)
		public static DependencyProperty LayoutProperty { get; } = DependencyProperty.Register(
			"Layout", typeof(Layout), typeof(ItemsRepeater), new FrameworkPropertyMetadata(
				defaultValue: new StackLayout(),
				propertyChangedCallback: OnPropertyChanged
			));

#if __ANDROID__
		public new Layout Layout
#else
		public Layout Layout
#endif
		{
			get => (Layout)GetValue(LayoutProperty);
			set => SetValue(LayoutProperty, value);
		}
		#endregion

		// Note: ItemTransitionProvider property is defined in Generated file and updated there with OnPropertyChanged callback

		#region Animator (DP - DEPRECATED - Use ItemTransitionProvider instead)
		[Obsolete("Animator is deprecated. Use ItemTransitionProvider instead.", false)]
		public static DependencyProperty AnimatorProperty { get; } = DependencyProperty.Register(
			"Animator", typeof(ElementAnimator), typeof(ItemsRepeater), new FrameworkPropertyMetadata(default(ElementAnimator), OnPropertyChanged));

		[Obsolete("Animator is deprecated. Use ItemTransitionProvider instead.", false)]
		public ElementAnimator Animator
		{
			get => (ElementAnimator)GetValue(AnimatorProperty);
			set => SetValue(AnimatorProperty, value);
		}
		#endregion

		#region HorizontalCacheLength (DP - With default callback)
		public static DependencyProperty HorizontalCacheLengthProperty { get; } = DependencyProperty.Register(
			"HorizontalCacheLength", typeof(double), typeof(ItemsRepeater), new FrameworkPropertyMetadata(2.0, OnPropertyChanged));

		public double HorizontalCacheLength
		{
			get => (double)GetValue(HorizontalCacheLengthProperty);
			set => SetValue(HorizontalCacheLengthProperty, value);
		}
		#endregion

		#region VerticalCacheLength (DP - With default callback)
		public static DependencyProperty VerticalCacheLengthProperty { get; } = DependencyProperty.Register(
			"VerticalCacheLength", typeof(double), typeof(ItemsRepeater), new FrameworkPropertyMetadata(2.0, OnPropertyChanged));

		public double VerticalCacheLength
		{
			get => (double)GetValue(VerticalCacheLengthProperty);
			set => SetValue(VerticalCacheLengthProperty, value);
		}
		#endregion

		#region VirtualizationInfo (DP - private attached)
		private static readonly DependencyProperty VirtualizationInfoProperty = DependencyProperty.RegisterAttached(
			"VirtualizationInfo", typeof(VirtualizationInfo), typeof(ItemsRepeater), new FrameworkPropertyMetadata(default(VirtualizationInfo)));
		#endregion
	}
}

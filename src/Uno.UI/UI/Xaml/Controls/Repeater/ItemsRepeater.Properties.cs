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

		#region ItemTransitionProvider (DP - With default callback)
		/// <summary>
		/// Identifies the ItemTransitionProvider dependency property.
		/// </summary>
		public static DependencyProperty ItemTransitionProviderProperty { get; } = DependencyProperty.Register(
			nameof(ItemTransitionProvider), typeof(ItemCollectionTransitionProvider), typeof(ItemsRepeater), new FrameworkPropertyMetadata(default(ItemCollectionTransitionProvider), OnPropertyChanged));

		/// <summary>
		/// Gets or sets an object that provides animated transition effects on ItemsRepeater child elements.
		/// </summary>
		public ItemCollectionTransitionProvider ItemTransitionProvider
		{
			get => (ItemCollectionTransitionProvider)GetValue(ItemTransitionProviderProperty);
			set => SetValue(ItemTransitionProviderProperty, value);
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

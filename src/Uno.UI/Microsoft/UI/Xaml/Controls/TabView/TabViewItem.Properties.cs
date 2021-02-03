// MUX Reference: TabViewItem.Properties.cpp, commit de78834

using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class TabViewItem
	{
		public object Header
		{
			get => (object)GetValue(HeaderProperty);
			set => SetValue(HeaderProperty, value);
		}

		public static DependencyProperty HeaderProperty { get; } =
			DependencyProperty.Register(nameof(Header), typeof(object), typeof(TabViewItem), new PropertyMetadata(null, OnHeaderPropertyChanged));

		private static void OnHeaderPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
		{
			var owner = (TabViewItem)sender;
			owner.OnHeaderPropertyChanged(args);
		}

		public DataTemplate HeaderTemplate
		{
			get => (DataTemplate)GetValue(HeaderTemplateProperty);
			set => SetValue(HeaderTemplateProperty, value);
		}

		public static DependencyProperty HeaderTemplateProperty { get; } =
			DependencyProperty.Register(nameof(HeaderTemplate), typeof(DataTemplate), typeof(TabViewItem), new PropertyMetadata(null));

		public IconSource IconSource
		{
			get => (IconSource)GetValue(IconSourceProperty);
			set => SetValue(IconSourceProperty, value);
		}

		public static DependencyProperty IconSourceProperty { get; } =
			DependencyProperty.Register(nameof(IconSource), typeof(IconSource), typeof(TabViewItem), new PropertyMetadata(null, OnIconSourcePropertyChanged));

		private static void OnIconSourcePropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
		{
			var owner = (TabViewItem)sender;
			owner.OnIconSourcePropertyChanged(args);
		}

		public bool IsClosable
		{
			get => (bool)GetValue(IsClosableProperty);
			set => SetValue(IsClosableProperty, value);
		}

		public static DependencyProperty IsClosableProperty { get; } =
			DependencyProperty.Register(nameof(IsClosable), typeof(bool), typeof(TabViewItem), new PropertyMetadata(false, OnIsClosablePropertyChanged));

		private static void OnIsClosablePropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
		{
			var owner = (TabViewItem)sender;
			owner.OnIsClosablePropertyChanged(args);
		}

		public TabViewItemTemplateSettings TabViewTemplateSettings
		{
			get => (TabViewItemTemplateSettings)GetValue(TabViewTemplateSettingsProperty);
			set => SetValue(TabViewTemplateSettingsProperty, value);
		}

		public static DependencyProperty TabViewTemplateSettingsProperty { get; } =
			DependencyProperty.Register(nameof(TabViewTemplateSettings), typeof(TabViewItemTemplateSettings), typeof(TabViewItem), new PropertyMetadata(null));
	}
}

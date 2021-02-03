// MUX Reference: TabViewItem.properties.cpp, commit de78834

using System.Collections.Generic;
using System.Windows.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class TabView
	{
		public ICommand AddTabButtonCommand
		{
			get => (ICommand)GetValue(AddTabButtonCommandProperty);
			set => SetValue(AddTabButtonCommandProperty, value);
		}

		public static DependencyProperty AddTabButtonCommandProperty { get; } =
			DependencyProperty.Register(nameof(AddTabButtonCommand), typeof(ICommand), typeof(TabView), new PropertyMetadata(null));

		public object AddTabButtonCommandParameter
		{
			get => (object)GetValue(AddTabButtonCommandParameterProperty);
			set => SetValue(AddTabButtonCommandParameterProperty, value);
		}

		public static DependencyProperty AddTabButtonCommandParameterProperty { get; } =
			DependencyProperty.Register(nameof(AddTabButtonCommandParameter), typeof(object), typeof(TabView), new PropertyMetadata(null));

		public bool AllowDropTabs
		{
			get => (bool)GetValue(AllowDropTabsProperty);
			set => SetValue(AllowDropTabsProperty, value);
		}

		public static DependencyProperty AllowDropTabsProperty { get; } =
			DependencyProperty.Register(nameof(AllowDropTabs), typeof(bool), typeof(TabView), new PropertyMetadata(true));

		public bool CanDragTabs
		{
			get => (bool)GetValue(CanDragTabsProperty);
			set => SetValue(CanDragTabsProperty, value);
		}

		public static DependencyProperty CanDragTabsProperty { get; } =
			DependencyProperty.Register(nameof(CanDragTabs), typeof(bool), typeof(TabView), new PropertyMetadata(false));

		public bool CanReorderTabs
		{
			get => (bool)GetValue(CanReorderTabsProperty);
			set => SetValue(CanReorderTabsProperty, value);
		}

		public static DependencyProperty CanReorderTabsProperty { get; } =
			DependencyProperty.Register(nameof(CanReorderTabs), typeof(bool), typeof(TabView), new PropertyMetadata(true));

		public TabViewCloseButtonOverlayMode CloseButtonOverlayMode
		{
			get => (TabViewCloseButtonOverlayMode)GetValue(CloseButtonOverlayModeProperty);
			set => SetValue(CloseButtonOverlayModeProperty, value);
		}

		public static DependencyProperty CloseButtonOverlayModeProperty { get; } =
			DependencyProperty.Register(nameof(CloseButtonOverlayMode), typeof(TabViewCloseButtonOverlayMode), typeof(TabView), new PropertyMetadata(TabViewCloseButtonOverlayMode.Auto, OnCloseButtonOverlayModePropertyChanged));

		private static void OnCloseButtonOverlayModePropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
		{
			var owner = (TabView)sender;
			owner.OnCloseButtonOverlayModePropertyChanged(args);
		}

		public bool IsAddTabButtonVisible
		{
			get => (bool)GetValue(IsAddTabButtonVisibleProperty);
			set => SetValue(IsAddTabButtonVisibleProperty, value);
		}

		public static DependencyProperty IsAddTabButtonVisibleProperty { get; } =
			DependencyProperty.Register(nameof(IsAddTabButtonVisible), typeof(bool), typeof(TabView), new PropertyMetadata(true));

		public int SelectedIndex
		{
			get => (int)GetValue(SelectedIndexProperty);
			set => SetValue(SelectedIndexProperty, value);
		}

		public static DependencyProperty SelectedIndexProperty { get; } =
			DependencyProperty.Register(nameof(SelectedIndex), typeof(int), typeof(TabView), new PropertyMetadata(0, OnSelectedIndexPropertyChanged));

		private static void OnSelectedIndexPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
		{
			var owner = (TabView)sender;
			owner.OnSelectedIndexPropertyChanged(args);
		}

		public object SelectedItem
		{
			get => (object)GetValue(SelectedItemProperty);
			set => SetValue(SelectedItemProperty, value);
		}

		public static DependencyProperty SelectedItemProperty { get; } =
			DependencyProperty.Register(nameof(SelectedItem), typeof(object), typeof(TabView), new PropertyMetadata(null, OnSelectedItemPropertyChanged));

		private static void OnSelectedItemPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
		{
			var owner = (TabView)sender;
			owner.OnSelectedItemPropertyChanged(args);
		}

		public IList<object> TabItems
		{
			get => (IList<object>)GetValue(TabItemsProperty);
			private set => SetValue(TabItemsProperty, value);
		}

		public static DependencyProperty TabItemsProperty { get; } =
			DependencyProperty.Register(nameof(TabItems), typeof(IList<object>), typeof(TabView), new PropertyMetadata(null));

		public object TabItemsSource
		{
			get => (object)GetValue(TabItemsSourceProperty);
			set => SetValue(TabItemsSourceProperty, value);
		}

		public static DependencyProperty TabItemsSourceProperty { get; } =
			DependencyProperty.Register(nameof(TabItemsSource), typeof(object), typeof(TabView), new PropertyMetadata(null, OnTabItemsSourcePropertyChanged));

		private static void OnTabItemsSourcePropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
		{
			var owner = (TabView)sender;
			owner.OnTabItemsSourcePropertyChanged(args);
		}

		public DataTemplate TabItemTemplate
		{
			get => (DataTemplate)GetValue(TabItemTemplateProperty);
			set => SetValue(TabItemTemplateProperty, value);
		}

		public static DependencyProperty TabItemTemplateProperty { get; } =
			DependencyProperty.Register(nameof(TabItemTemplate), typeof(DataTemplate), typeof(TabView), new PropertyMetadata(null));

		public DataTemplateSelector TabItemTemplateSelector
		{
			get => (DataTemplateSelector)GetValue(TabItemTemplateSelectorProperty);
			set => SetValue(TabItemTemplateSelectorProperty, value);
		}

		public static DependencyProperty TabItemTemplateSelectorProperty { get; } =
			DependencyProperty.Register(nameof(TabItemTemplateSelector), typeof(DataTemplateSelector), typeof(TabView), new PropertyMetadata(null));

		public object TabStripFooter
		{
			get => (object)GetValue(TabStripFooterProperty);
			set => SetValue(TabStripFooterProperty, value);
		}

		public static DependencyProperty TabStripFooterProperty { get; } =
			DependencyProperty.Register(nameof(TabStripFooter), typeof(object), typeof(TabView), new PropertyMetadata(null));

		public DataTemplate TabStripFooterTemplate
		{
			get => (DataTemplate)GetValue(TabStripFooterTemplateProperty);
			set => SetValue(TabStripFooterTemplateProperty, value);
		}

		public static DependencyProperty TabStripFooterTemplateProperty { get; } =
			DependencyProperty.Register(nameof(TabStripFooterTemplate), typeof(DataTemplate), typeof(TabView), new PropertyMetadata(null));

		public object TabStripHeader
		{
			get => (object)GetValue(TabStripHeaderProperty);
			set => SetValue(TabStripHeaderProperty, value);
		}

		public static DependencyProperty TabStripHeaderProperty { get; } =
			DependencyProperty.Register(nameof(TabStripHeader), typeof(object), typeof(TabView), new PropertyMetadata(null));

		public DataTemplate TabStripHeaderTemplate
		{
			get => (DataTemplate)GetValue(TabStripHeaderTemplateProperty);
			set => SetValue(TabStripHeaderTemplateProperty, value);
		}

		public static DependencyProperty TabStripHeaderTemplateProperty { get; } =
			DependencyProperty.Register(nameof(TabStripHeaderTemplate), typeof(DataTemplate), typeof(TabView), new PropertyMetadata(null));

		public TabViewWidthMode TabWidthMode
		{
			get => (TabViewWidthMode)GetValue(TabWidthModeProperty);
			set => SetValue(TabWidthModeProperty, value);
		}

		public static DependencyProperty TabWidthModeProperty { get; } =
			DependencyProperty.Register(nameof(TabWidthMode), typeof(TabViewWidthMode), typeof(TabView), new PropertyMetadata(TabViewWidthMode.Equal, OnTabWidthModePropertyChanged));

		private static void OnTabWidthModePropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
		{
			var owner = (TabView)sender;
			owner.OnTabWidthModePropertyChanged(args);
		}
	}
}

using System.Collections.Generic;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Microsoft/* UWP don't rename */.UI.Xaml.Controls
{
	public partial class RadioButtons
	{
		public object Header
		{
			get => (object)GetValue(HeaderProperty);
			set => SetValue(HeaderProperty, value);
		}

		public static DependencyProperty HeaderProperty { get; } =
			DependencyProperty.Register(nameof(Header), typeof(object), typeof(RadioButtons), new FrameworkPropertyMetadata(default(object), OnPropertyChanged));

		public DataTemplate HeaderTemplate
		{
			get => (DataTemplate)GetValue(HeaderTemplateProperty);
			set => SetValue(HeaderTemplateProperty, value);
		}

		public static DependencyProperty HeaderTemplateProperty { get; } =
			DependencyProperty.Register(nameof(HeaderTemplate), typeof(DataTemplate), typeof(RadioButtons), new FrameworkPropertyMetadata(default(DataTemplate), FrameworkPropertyMetadataOptions.ValueDoesNotInheritDataContext, OnPropertyChanged));

		public IList<object> Items
		{
			get => (IList<object>)GetValue(ItemsProperty);
			set => SetValue(ItemsProperty, value);
		}

		public static DependencyProperty ItemsProperty { get; } =
			DependencyProperty.Register(nameof(Items), typeof(IList<object>), typeof(RadioButtons), new FrameworkPropertyMetadata(default(IList<object>), OnPropertyChanged));

		public object ItemsSource
		{
			get => (object)GetValue(ItemsSourceProperty);
			set => SetValue(ItemsSourceProperty, value);
		}

		public static DependencyProperty ItemsSourceProperty { get; } =
			DependencyProperty.Register(nameof(ItemsSource), typeof(object), typeof(RadioButtons), new FrameworkPropertyMetadata(default(object), OnPropertyChanged));

		public object ItemTemplate
		{
			get => (object)GetValue(ItemTemplateProperty);
			set => SetValue(ItemTemplateProperty, value);
		}

		public static DependencyProperty ItemTemplateProperty { get; } =
			DependencyProperty.Register(nameof(ItemTemplate), typeof(object), typeof(RadioButtons), new FrameworkPropertyMetadata(default(object), OnPropertyChanged));

		public int MaxColumns
		{
			get => (int)GetValue(MaxColumnsProperty);
			set => SetValue(MaxColumnsProperty, value);
		}

		public static DependencyProperty MaxColumnsProperty { get; } =
			DependencyProperty.Register(nameof(MaxColumns), typeof(int), typeof(RadioButtons), new FrameworkPropertyMetadata(1, OnPropertyChanged));

		public int SelectedIndex
		{
			get => (int)GetValue(SelectedIndexProperty);
			set => SetValue(SelectedIndexProperty, value);
		}

		public static DependencyProperty SelectedIndexProperty { get; } =
			DependencyProperty.Register(nameof(SelectedIndex), typeof(int), typeof(RadioButtons), new FrameworkPropertyMetadata(-1, OnPropertyChanged));

		public object SelectedItem
		{
			get => (object)GetValue(SelectedItemProperty);
			set => SetValue(SelectedItemProperty, value);
		}

		public static DependencyProperty SelectedItemProperty { get; } =
			DependencyProperty.Register(nameof(SelectedItem), typeof(object), typeof(RadioButtons), new FrameworkPropertyMetadata(default(object), OnPropertyChanged));

		public event SelectionChangedEventHandler SelectionChanged;

		private static void OnPropertyChanged(
			DependencyObject sender,
			DependencyPropertyChangedEventArgs args)
		{
			var owner = (RadioButtons)sender;
			owner.OnPropertyChanged(args);
		}
	}
}

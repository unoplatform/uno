// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// MUX Reference ItemsView.properties.cpp, tag winui3/release/1.5.0

using Microsoft.UI.Xaml.Controls.Primitives;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls;

partial class ItemsView
{
	public static DependencyProperty CurrentItemIndexProperty { get; } = DependencyProperty.Register(
		nameof(CurrentItemIndex),
		typeof(int),
		typeof(ItemsView),
		new FrameworkPropertyMetadata(defaultValue: -1, propertyChangedCallback: OnCurrentItemIndexPropertyChanged));

	public static DependencyProperty IsItemInvokedEnabledProperty { get; } = DependencyProperty.Register(
		nameof(IsItemInvokedEnabled),
		typeof(bool),
		typeof(ItemsView),
		new FrameworkPropertyMetadata(defaultValue: false, propertyChangedCallback: OnIsItemInvokedEnabledPropertyChanged));

	public static DependencyProperty ItemsSourceProperty { get; } = DependencyProperty.Register(
		nameof(ItemsSource),
		typeof(object),
		typeof(ItemsView),
		new FrameworkPropertyMetadata(defaultValue: null, OnItemsSourcePropertyChanged));

	public static DependencyProperty ItemTemplateProperty { get; } = DependencyProperty.Register(
		nameof(ItemTemplate),
		typeof(IElementFactory),
		typeof(ItemsView),
		new FrameworkPropertyMetadata(defaultValue: null, propertyChangedCallback: OnItemTemplatePropertyChanged));

	public static DependencyProperty ItemTransitionProviderProperty { get; } = DependencyProperty.Register(
		nameof(ItemTransitionProvider),
		typeof(ItemCollectionTransitionProvider),
		typeof(ItemsView),
		new FrameworkPropertyMetadata(defaultValue: null, propertyChangedCallback: OnItemTransitionProviderPropertyChanged));

	public static DependencyProperty LayoutProperty { get; } = DependencyProperty.Register(
		nameof(Layout),
		typeof(Layout),
		typeof(ItemsView),
		new FrameworkPropertyMetadata(defaultValue: null, propertyChangedCallback: OnLayoutPropertyChanged));

	public static DependencyProperty ScrollViewProperty { get; } = DependencyProperty.Register(
		nameof(ScrollView),
		typeof(ScrollView),
		typeof(ItemsView),
		new FrameworkPropertyMetadata(defaultValue: null, propertyChangedCallback: OnScrollViewPropertyChanged));

	public static DependencyProperty SelectedItemProperty { get; } = DependencyProperty.Register(
		nameof(SelectedItem),
		typeof(object),
		typeof(ItemsView),
		new FrameworkPropertyMetadata(defaultValue: null, propertyChangedCallback: OnSelectedItemPropertyChanged));

	public static DependencyProperty SelectionModeProperty { get; } = DependencyProperty.Register(
		nameof(SelectionMode),
		typeof(ItemsViewSelectionMode),
		typeof(ItemsView),
		new FrameworkPropertyMetadata(defaultValue: s_defaultSelectionMode, propertyChangedCallback: OnSelectionModePropertyChanged));

	public static DependencyProperty VerticalScrollControllerProperty { get; } = DependencyProperty.Register(
		nameof(VerticalScrollController),
		typeof(IScrollController),
		typeof(ItemsView),
		new FrameworkPropertyMetadata(defaultValue: null, propertyChangedCallback: OnVerticalScrollControllerPropertyChanged));

	private static void OnCurrentItemIndexPropertyChanged(
		DependencyObject sender,
	DependencyPropertyChangedEventArgs args)
	{
		var owner = (ItemsView)sender;
		owner.OnPropertyChanged(args);
	}

	private static void OnIsItemInvokedEnabledPropertyChanged(
		DependencyObject sender,
		DependencyPropertyChangedEventArgs args)
	{
		var owner = (ItemsView)sender;
		owner.OnPropertyChanged(args);
	}

	private static void OnItemsSourcePropertyChanged(
		DependencyObject sender,
		DependencyPropertyChangedEventArgs args)
	{
		var owner = (ItemsView)sender;
		owner.OnPropertyChanged(args);
	}

	private static void OnItemTemplatePropertyChanged(
		DependencyObject sender,
		DependencyPropertyChangedEventArgs args)
	{
		var owner = (ItemsView)sender;
		owner.OnPropertyChanged(args);
	}

	private static void OnItemTransitionProviderPropertyChanged(
		DependencyObject sender,
		DependencyPropertyChangedEventArgs args)
	{
		var owner = (ItemsView)sender;
		owner.OnPropertyChanged(args);
	}

	private static void OnLayoutPropertyChanged(
		DependencyObject sender,
		DependencyPropertyChangedEventArgs args)
	{
		var owner = (ItemsView)sender;
		owner.OnPropertyChanged(args);
	}

	private static void OnScrollViewPropertyChanged(
		DependencyObject sender,
		DependencyPropertyChangedEventArgs args)
	{
		var owner = (ItemsView)sender;
		owner.OnPropertyChanged(args);
	}

	private static void OnSelectedItemPropertyChanged(
		DependencyObject sender,
		DependencyPropertyChangedEventArgs args)
	{
		var owner = (ItemsView)sender;
		owner.OnPropertyChanged(args);
	}

	private static void OnSelectionModePropertyChanged(
		DependencyObject sender,
		DependencyPropertyChangedEventArgs args)
	{
		var owner = (ItemsView)sender;
		owner.OnPropertyChanged(args);
	}

	private static void OnVerticalScrollControllerPropertyChanged(
		DependencyObject sender,
		DependencyPropertyChangedEventArgs args)
	{
		var owner = (ItemsView)sender;
		owner.OnPropertyChanged(args);
	}

	public int CurrentItemIndex
	{
		get => (int)GetValue(CurrentItemIndexProperty);
		private set => SetValue(CurrentItemIndexProperty, value);
	}

	public bool IsItemInvokedEnabled
	{
		get => (bool)GetValue(IsItemInvokedEnabledProperty);
		set => SetValue(IsItemInvokedEnabledProperty, value);
	}

	public object ItemsSource
	{
		get => (object)GetValue(ItemsSourceProperty);
		set => SetValue(ItemsSourceProperty, value);
	}

	public IElementFactory ItemTemplate
	{
		get => (IElementFactory)GetValue(ItemTemplateProperty);
		set => SetValue(ItemTemplateProperty, value);
	}

	public ItemCollectionTransitionProvider ItemTransitionProvider
	{
		get => (ItemCollectionTransitionProvider)GetValue(ItemTransitionProviderProperty);
		set => SetValue(ItemTransitionProviderProperty, value);
	}

	public
#if __ANDROID__
		new
#endif
		Layout Layout
	{
		get => (Layout)GetValue(LayoutProperty);
		set => SetValue(LayoutProperty, value);
	}

	internal ScrollView ScrollViewInternal
	{
		get => (ScrollView)GetValue(ScrollViewProperty);
		set => SetValue(ScrollViewProperty, value);
	}

	public object SelectedItem
	{
		get => GetValue(SelectedItemProperty);
		private set => SetValue(SelectedItemProperty, value);
	}

	public ItemsViewSelectionMode SelectionMode
	{
		get => (ItemsViewSelectionMode)GetValue(SelectionModeProperty);
		set => SetValue(SelectionModeProperty, value);
	}

	public IScrollController VerticalScrollController
	{
		get => (IScrollController)GetValue(VerticalScrollControllerProperty);
		set => SetValue(VerticalScrollControllerProperty, value);
	}

	public event TypedEventHandler<ItemsView, ItemsViewItemInvokedEventArgs> ItemInvoked;
	public event TypedEventHandler<ItemsView, ItemsViewSelectionChangedEventArgs> SelectionChanged;
}

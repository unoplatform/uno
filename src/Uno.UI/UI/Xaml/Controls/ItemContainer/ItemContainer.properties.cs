// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// MUX Reference ItemContainer.properties.cpp, tag winui3/release/1.5.0

using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls;

partial class ItemContainer
{
	internal static DependencyProperty CanUserInvokeProperty { get; } = DependencyProperty.Register(
		nameof(CanUserInvoke),
		typeof(ItemContainerUserInvokeMode),
		typeof(ItemContainer),
		new FrameworkPropertyMetadata(defaultValue: ItemContainerUserInvokeMode.Auto, propertyChangedCallback: OnCanUserInvokePropertyChanged));

	internal static DependencyProperty CanUserSelectProperty { get; } = DependencyProperty.Register(
		nameof(CanUserSelect),
		typeof(ItemContainerUserSelectMode),
		typeof(ItemContainer),
		new FrameworkPropertyMetadata(defaultValue: ItemContainerUserSelectMode.Auto, propertyChangedCallback: OnCanUserSelectPropertyChanged));

	public static DependencyProperty ChildProperty { get; } = DependencyProperty.Register(
		nameof(Child),
		typeof(UIElement),
		typeof(ItemContainer),
		new FrameworkPropertyMetadata(defaultValue: null, propertyChangedCallback: OnChildPropertyChanged));

	public static DependencyProperty IsSelectedProperty { get; } = DependencyProperty.Register(
		nameof(IsSelected),
		typeof(bool),
		typeof(ItemContainer),
		new FrameworkPropertyMetadata(defaultValue: false, propertyChangedCallback: OnIsSelectedPropertyChanged));

	internal static DependencyProperty MultiSelectModeProperty { get; } = DependencyProperty.Register(
		nameof(MultiSelectMode),
		typeof(ItemContainerMultiSelectMode),
		typeof(ItemContainer),
		new FrameworkPropertyMetadata(defaultValue: ItemContainerMultiSelectMode.Auto, propertyChangedCallback: OnMultiSelectModePropertyChanged));

	private static void OnCanUserInvokePropertyChanged(
		DependencyObject sender,
	DependencyPropertyChangedEventArgs args)
	{
		var owner = (ItemContainer)sender;
		owner.OnPropertyChanged(args);
	}

	private static void OnCanUserSelectPropertyChanged(
		DependencyObject sender,
		DependencyPropertyChangedEventArgs args)
	{
		var owner = (ItemContainer)sender;
		owner.OnPropertyChanged(args);
	}

	private static void OnChildPropertyChanged(
		DependencyObject sender,
		DependencyPropertyChangedEventArgs args)
	{
		var owner = (ItemContainer)sender;
		owner.OnPropertyChanged(args);
	}

	private static void OnIsSelectedPropertyChanged(
		DependencyObject sender,
		DependencyPropertyChangedEventArgs args)
	{
		var owner = (ItemContainer)sender;
		owner.OnPropertyChanged(args);
	}

	private static void OnMultiSelectModePropertyChanged(
		DependencyObject sender,
		DependencyPropertyChangedEventArgs args)
	{
		var owner = (ItemContainer)sender;
		owner.OnPropertyChanged(args);
	}


	internal ItemContainerUserInvokeMode CanUserInvoke
	{
		get => (ItemContainerUserInvokeMode)GetValue(CanUserInvokeProperty);
		set => SetValue(CanUserInvokeProperty, value);
	}

	internal ItemContainerUserSelectMode CanUserSelect
	{
		get => (ItemContainerUserSelectMode)GetValue(CanUserSelectProperty);
		set => SetValue(CanUserSelectProperty, value);
	}

	public UIElement Child
	{
		get => (UIElement)GetValue(ChildProperty);
		set => SetValue(ChildProperty, value);
	}

	public bool IsSelected
	{
		get => (bool)GetValue(IsSelectedProperty);
		set => SetValue(IsSelectedProperty, value);
	}

	internal ItemContainerMultiSelectMode MultiSelectMode
	{
		get => (ItemContainerMultiSelectMode)GetValue(MultiSelectModeProperty);
		set => SetValue(MultiSelectModeProperty, value);
	}

	internal event TypedEventHandler<ItemContainer, ItemContainerInvokedEventArgs> ItemInvoked;
}

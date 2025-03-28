// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference TreeView.cpp, tag winui3/release/1.4.2

using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;

namespace Microsoft/* UWP don't rename */.UI.Xaml.Controls;

public partial class TreeView
{
	/// <summary>
	/// Gets or sets a value that indicates whether items in the view can be dragged as data payload.
	/// </summary>
	public bool CanDragItems
	{
		get => (bool)GetValue(CanDragItemsProperty);
		set => SetValue(CanDragItemsProperty, value);
	}

	/// <summary>
	/// Gets or sets a value that indicates whether items in the view can be reordered through user interaction.
	/// </summary>
	public bool CanReorderItems
	{
		get => (bool)GetValue(CanReorderItemsProperty);
		set => SetValue(CanReorderItemsProperty, value);
	}

	/// <summary>
	/// Gets or sets the style that is used when rendering the item containers.
	/// </summary>
	public Style ItemContainerStyle
	{
		get => (Style)GetValue(ItemContainerStyleProperty);
		set => SetValue(ItemContainerStyleProperty, value);
	}

	/// <summary>
	/// Gets or sets a reference to a custom StyleSelector logic class. The StyleSelector returns different
	/// Style values to use for the item container based on characteristics of the object being displayed.
	/// </summary>
	public StyleSelector ItemContainerStyleSelector
	{
		get => (StyleSelector)GetValue(ItemContainerStyleSelectorProperty);
		set => SetValue(ItemContainerStyleSelectorProperty, value);
	}

	/// <summary>
	/// Gets or sets the collection of Transition style elements that apply to the item containers of a TreeView.
	/// </summary>
	public TransitionCollection ItemContainerTransitions
	{
		get => (TransitionCollection)GetValue(ItemContainerTransitionsProperty);
		set => SetValue(ItemContainerTransitionsProperty, value);
	}

	/// <summary>
	/// Gets or sets an object source used to generate the content of the TreeView.
	/// </summary>
	public object ItemsSource
	{
		get => GetValue(ItemsSourceProperty);
		set => SetValue(ItemsSourceProperty, value);
	}

	/// <summary>
	/// Gets or sets the DataTemplate used to display each item.
	/// </summary>
	public DataTemplate ItemTemplate
	{
		get => (DataTemplate)GetValue(ItemTemplateProperty);
		set => SetValue(ItemTemplateProperty, value);
	}

	/// <summary>
	/// Gets or sets a reference to a custom DataTemplateSelector logic class.
	/// The DataTemplateSelector referenced by this property returns a template to apply to items.
	/// </summary>
	public DataTemplateSelector ItemTemplateSelector
	{
		get => (DataTemplateSelector)GetValue(ItemTemplateSelectorProperty);
		set => SetValue(ItemTemplateSelectorProperty, value);
	}

	/// <summary>
	/// Gets or sets the SelectedItem property of a TreeView.
	/// </summary>
	public object SelectedItem
	{
		get => GetValue(SelectedItemProperty);
		set => SetValue(SelectedItemProperty, value);
	}

	/// <summary>
	/// Gets or sets the selection behavior for a TreeView instance.
	/// </summary>
	public TreeViewSelectionMode SelectionMode
	{
		get => (TreeViewSelectionMode)GetValue(SelectionModeProperty);
		set => SetValue(SelectionModeProperty, value);
	}

	/// <summary>
	/// Identifies the CanDragItems dependency property.
	/// </summary>
	public static DependencyProperty CanDragItemsProperty { get; } =
		DependencyProperty.Register(nameof(CanDragItems), typeof(bool), typeof(TreeView), new FrameworkPropertyMetadata(true));

	/// <summary>
	/// Identifies the CanReorderItems dependency property.
	/// </summary>
	public static DependencyProperty CanReorderItemsProperty { get; } =
		DependencyProperty.Register(nameof(CanReorderItems), typeof(bool), typeof(TreeView), new FrameworkPropertyMetadata(true));

	/// <summary>
	/// Identifies the ItemContainerStyle dependency property.
	/// </summary>
	public static DependencyProperty ItemContainerStyleProperty { get; } =
		DependencyProperty.Register(nameof(ItemContainerStyle), typeof(Style), typeof(TreeView), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.ValueDoesNotInheritDataContext));

	/// <summary>
	/// Identifies the ItemContainerStyleSelector dependency property.
	/// </summary>
	public static DependencyProperty ItemContainerStyleSelectorProperty { get; } =
		DependencyProperty.Register(nameof(ItemContainerStyleSelector), typeof(StyleSelector), typeof(TreeView), new FrameworkPropertyMetadata(null));

	/// <summary>
	/// Identifies the ItemContainerTransitions dependency property.
	/// </summary>
	public static DependencyProperty ItemContainerTransitionsProperty { get; } =
		DependencyProperty.Register(nameof(ItemContainerTransitions), typeof(TransitionCollection), typeof(TreeView), new FrameworkPropertyMetadata(null));

	/// <summary>
	/// Identifies the ItemsSource dependency property.
	/// </summary>
	public static DependencyProperty ItemsSourceProperty { get; } =
		DependencyProperty.Register(nameof(ItemsSource), typeof(object), typeof(TreeView), new FrameworkPropertyMetadata(null, OnItemsSourcePropertyChanged));

	/// <summary>
	/// Identifies the ItemTemplate dependency property.
	/// </summary>
	public static DependencyProperty ItemTemplateProperty { get; } =
		DependencyProperty.Register(nameof(ItemTemplate), typeof(DataTemplate), typeof(TreeView), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.ValueDoesNotInheritDataContext));

	/// <summary>
	/// Identifies the ItemTemplateSelector dependency property.
	/// </summary>
	public static DependencyProperty ItemTemplateSelectorProperty { get; } =
		DependencyProperty.Register(nameof(ItemTemplateSelector), typeof(DataTemplateSelector), typeof(TreeView), new FrameworkPropertyMetadata(null));

	/// <summary>
	/// Identifies the SelectedItem dependency property.
	/// </summary>
	public static DependencyProperty SelectedItemProperty { get; } =
		DependencyProperty.Register(nameof(SelectedItem), typeof(object), typeof(TreeView), new FrameworkPropertyMetadata(null, OnSelectedItemPropertyChanged));

	/// <summary>
	/// Identifies the SelectionMode dependency property.
	/// </summary>
	public static DependencyProperty SelectionModeProperty { get; } =
		DependencyProperty.Register(nameof(SelectionMode), typeof(TreeViewSelectionMode), typeof(TreeView), new FrameworkPropertyMetadata(TreeViewSelectionMode.Single, OnSelectionModePropertyChanged));

	private static void OnItemsSourcePropertyChanged(
		DependencyObject sender,
		DependencyPropertyChangedEventArgs args)
	{
		var owner = (TreeView)sender;
		owner.OnPropertyChanged(args);
	}

	private static void OnSelectionModePropertyChanged(
		DependencyObject sender,
		DependencyPropertyChangedEventArgs args)
	{
		var owner = (TreeView)sender;
		owner.OnPropertyChanged(args);
	}

	private static void OnSelectedItemPropertyChanged(
		DependencyObject sender,
		DependencyPropertyChangedEventArgs args)
	{
		var owner = (TreeView)sender;
		owner.OnPropertyChanged(args);
	}
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// MUX reference 96244e6

using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class TreeView
	{
		public bool CanDragItems
		{
			get => (bool)GetValue(CanDragItemsProperty);
			set => SetValue(CanDragItemsProperty, value);
		}

		public bool CanReorderItems
		{
			get => (bool)GetValue(CanReorderItemsProperty);
			set => SetValue(CanReorderItemsProperty, value);
		}

		public Style ItemContainerStyle
		{
			get => (Style)GetValue(ItemContainerStyleProperty);
			set => SetValue(ItemContainerStyleProperty, value);
		}

		public StyleSelector ItemContainerStyleSelector
		{
			get => (StyleSelector)GetValue(ItemContainerStyleSelectorProperty);
			set => SetValue(ItemContainerStyleSelectorProperty, value);
		}

		public TransitionCollection ItemContainerTransitions
		{
			get => (TransitionCollection)GetValue(ItemContainerTransitionsProperty);
			set => SetValue(ItemContainerTransitionsProperty, value);
		}

		public object ItemsSource
		{
			get => (object)GetValue(ItemsSourceProperty);
			set => SetValue(ItemsSourceProperty, value);
		}

		public DataTemplate ItemTemplate
		{
			get => (DataTemplate)GetValue(ItemTemplateProperty);
			set => SetValue(ItemTemplateProperty, value);
		}

		public DataTemplateSelector ItemTemplateSelector
		{
			get => (DataTemplateSelector)GetValue(ItemTemplateSelectorProperty);
			set => SetValue(ItemTemplateSelectorProperty, value);
		}

		public object SelectedItem
		{
			get => (object)GetValue(SelectedItemProperty);
			set => SetValue(SelectedItemProperty, value);
		}

		public TreeViewSelectionMode SelectionMode
		{
			get => (TreeViewSelectionMode)GetValue(SelectionModeProperty);
			set => SetValue(SelectionModeProperty, value);
		}

		public static DependencyProperty CanDragItemsProperty { get; } =
			DependencyProperty.Register(nameof(CanDragItems), typeof(bool), typeof(TreeView), new PropertyMetadata(true));

		public static DependencyProperty CanReorderItemsProperty { get; } =
			DependencyProperty.Register(nameof(CanReorderItems), typeof(bool), typeof(TreeView), new PropertyMetadata(true));

		public static DependencyProperty ItemContainerStyleProperty { get; } =
			DependencyProperty.Register(nameof(ItemContainerStyle), typeof(Style), typeof(TreeView), new PropertyMetadata(null));

		public static DependencyProperty ItemContainerStyleSelectorProperty { get; } =
			DependencyProperty.Register(nameof(ItemContainerStyleSelector), typeof(StyleSelector), typeof(TreeView), new PropertyMetadata(null));

		public static DependencyProperty ItemContainerTransitionsProperty { get; } =
			DependencyProperty.Register(nameof(ItemContainerTransitions), typeof(TransitionCollection), typeof(TreeView), new PropertyMetadata(null));

		public static DependencyProperty ItemsSourceProperty { get; } =
			DependencyProperty.Register(nameof(ItemsSource), typeof(object), typeof(TreeView), new PropertyMetadata(null, OnItemsSourcePropertyChanged));

		public static DependencyProperty ItemTemplateProperty { get; } =
			DependencyProperty.Register(nameof(ItemTemplate), typeof(DataTemplate), typeof(TreeView), new PropertyMetadata(null));

		public static DependencyProperty ItemTemplateSelectorProperty { get; } =
			DependencyProperty.Register(nameof(ItemTemplateSelector), typeof(DataTemplateSelector), typeof(TreeView), new PropertyMetadata(null));

		public static DependencyProperty SelectedItemProperty { get; } =
			DependencyProperty.Register(nameof(SelectedItem), typeof(object), typeof(TreeView), new PropertyMetadata(null, OnSelectedItemPropertyChanged));

		public static DependencyProperty SelectionModeProperty { get; } =
			DependencyProperty.Register(nameof(SelectionMode), typeof(TreeViewSelectionMode), typeof(TreeView), new PropertyMetadata(TreeViewSelectionMode.Single, OnSelectionModePropertyChanged));

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
}

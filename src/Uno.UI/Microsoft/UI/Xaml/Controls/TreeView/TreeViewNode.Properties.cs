// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference TreeViewNode.properties.cpp, tag winui3/release/1.4.2

using Windows.UI.Xaml;

namespace Microsoft/* UWP don't rename */.UI.Xaml.Controls;

public partial class TreeViewNode
{
	public object Content
	{
		get => (object)GetValue(ContentProperty);
		set => SetValue(ContentProperty, value);
	}

	public int Depth
	{
		get => (int)GetValue(DepthProperty);
		private set => SetValue(DepthProperty, value);
	}

	public bool HasChildren
	{
		get => (bool)GetValue(HasChildrenProperty);
		private set => SetValue(HasChildrenProperty, value);
	}

	public bool IsExpanded
	{
		get => (bool)GetValue(IsExpandedProperty);
		set => SetValue(IsExpandedProperty, value);
	}

	public static DependencyProperty ContentProperty { get; } =
		DependencyProperty.Register(nameof(Content), typeof(object), typeof(TreeViewNode), new FrameworkPropertyMetadata(null));

	public static DependencyProperty DepthProperty { get; } =
		DependencyProperty.Register(nameof(Depth), typeof(int), typeof(TreeViewNode), new FrameworkPropertyMetadata(-1));

	public static DependencyProperty HasChildrenProperty { get; } =
		DependencyProperty.Register(nameof(HasChildren), typeof(bool), typeof(TreeViewNode), new FrameworkPropertyMetadata(false, OnHasChildrenPropertyChanged));

	public static DependencyProperty IsExpandedProperty { get; } =
		DependencyProperty.Register(nameof(IsExpanded), typeof(bool), typeof(TreeViewNode), new FrameworkPropertyMetadata(false, OnIsExpandedPropertyChanged));

	private static void OnHasChildrenPropertyChanged(
		DependencyObject sender,
		DependencyPropertyChangedEventArgs args)
	{
		var owner = (TreeViewNode)sender;
		owner.OnPropertyChanged(args);
	}

	private static void OnIsExpandedPropertyChanged(
		DependencyObject sender,
		DependencyPropertyChangedEventArgs args)
	{
		var owner = (TreeViewNode)sender;
		owner.OnPropertyChanged(args);
	}
}

// MUX Reference TreeViewNode.properties.cpp, commit de78834

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Windows.UI.Xaml;

namespace Microsoft.UI.Xaml.Controls
{
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
			DependencyProperty.Register(nameof(Content), typeof(object), typeof(TreeViewNode), new PropertyMetadata(null));

		public static DependencyProperty DepthProperty { get; } =
			DependencyProperty.Register(nameof(Depth), typeof(int), typeof(TreeViewNode), new PropertyMetadata(-1));

		public static DependencyProperty HasChildrenProperty { get; } =
			DependencyProperty.Register(nameof(HasChildren), typeof(bool), typeof(TreeViewNode), new PropertyMetadata(false, OnHasChildrenPropertyChanged));

		public static DependencyProperty IsExpandedProperty { get; } =
			DependencyProperty.Register(nameof(IsExpanded), typeof(bool), typeof(TreeViewNode), new PropertyMetadata(false, OnIsExpandedPropertyChanged));

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
}

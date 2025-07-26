// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference TreeView.cpp, tag winui3/release/1.4.2

using Windows.Foundation;
using Microsoft.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Controls;

public partial class TreeView
{
	/// <summary>
	/// Occurs when a node in the tree is collapsed.
	/// </summary>
	public event TypedEventHandler<TreeView, TreeViewCollapsedEventArgs> Collapsed;

	/// <summary>
	/// Occurs when a drag operation that involves one of the items in the view is ended.
	/// </summary>
	public event TypedEventHandler<TreeView, TreeViewDragItemsCompletedEventArgs> DragItemsCompleted;

	/// <summary>
	/// Occurs when a drag operation that involves one of the items in the view is initiated.
	/// </summary>
	public event TypedEventHandler<TreeView, TreeViewDragItemsStartingEventArgs> DragItemsStarting;

	/// <summary>
	/// Occurs when a node in the tree starts to expand.
	/// </summary>
	public event TypedEventHandler<TreeView, TreeViewExpandingEventArgs> Expanding;

	/// <summary>
	/// Occurs when an item in the tree is invoked.
	/// </summary>
	public event TypedEventHandler<TreeView, TreeViewItemInvokedEventArgs> ItemInvoked;

	/// <summary>
	/// Occurs when selection changes.
	/// </summary>
	public event TypedEventHandler<TreeView, TreeViewSelectionChangedEventArgs> SelectionChanged;

	private event TypedEventHandler<TreeView, ContainerContentChangingEventArgs> ContainerContentChanged;
}

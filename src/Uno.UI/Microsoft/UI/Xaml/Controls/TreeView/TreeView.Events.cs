// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// MUX reference de78834

using Windows.Foundation;
using Windows.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class TreeView
	{
		public event TypedEventHandler<TreeView, TreeViewCollapsedEventArgs> Collapsed;

		public event TypedEventHandler<TreeView, TreeViewDragItemsCompletedEventArgs> DragItemsCompleted;

		public event TypedEventHandler<TreeView, TreeViewDragItemsStartingEventArgs> DragItemsStarting;

		public event TypedEventHandler<TreeView, TreeViewExpandingEventArgs> Expanding;

		public event TypedEventHandler<TreeView, TreeViewItemInvokedEventArgs> ItemInvoked;

		public event TypedEventHandler<TreeView, TreeViewSelectionChangedEventArgs> SelectionChanged;

		private event TypedEventHandler<TreeView, ContainerContentChangingEventArgs> ContainerContentChanged;		
	}
}

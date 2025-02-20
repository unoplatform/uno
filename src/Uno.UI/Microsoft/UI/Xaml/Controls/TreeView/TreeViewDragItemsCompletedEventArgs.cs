// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference TreeView.idl, tag winui3/release/1.4.2

using System.Collections.Generic;
using Windows.ApplicationModel.DataTransfer;
using Windows.UI.Xaml.Controls;

namespace Microsoft/* UWP don't rename */.UI.Xaml.Controls;

/// <summary>
/// Provides event data for the TreeView.DragItemsCompleted event.
/// </summary>
public partial class TreeViewDragItemsCompletedEventArgs
{
	private readonly DragItemsCompletedEventArgs _dragItemsCompletedEventArgs;

	internal TreeViewDragItemsCompletedEventArgs(DragItemsCompletedEventArgs args, object newParentItem)
	{
		_dragItemsCompletedEventArgs = args;
		NewParentItem = newParentItem;
	}

	/// <summary>
	/// Gets a value that indicates what operation was performed on the dragged data, and whether it was successful.
	/// </summary>
	public DataPackageOperation DropResult => _dragItemsCompletedEventArgs.DropResult;

	/// <summary>
	/// Gets the loosely typed collection of objects that are selected for the item drag action.
	/// </summary>
	public IReadOnlyList<object> Items => _dragItemsCompletedEventArgs.Items;

	/// <summary>
	/// Gets the new parent item.
	/// </summary>
	public object NewParentItem { get; }
}

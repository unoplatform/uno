// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference TreeView.idl, tag winui3/release/1.4.2

using System.Collections.Generic;
using Windows.ApplicationModel.DataTransfer;
using Windows.UI.Xaml.Controls;

namespace Microsoft/* UWP don't rename */.UI.Xaml.Controls;

/// <summary>
/// Provides event data for the TreeView.DragItemsStarting event.
/// </summary>
public partial class TreeViewDragItemsStartingEventArgs
{
	private readonly DragItemsStartingEventArgs _dragItemsStartingEventArgs;

	public TreeViewDragItemsStartingEventArgs(DragItemsStartingEventArgs args)
	{
		_dragItemsStartingEventArgs = args;
	}

	/// <summary>
	/// Gets or sets a value that indicates whether the item drag action should be canceled.
	/// </summary>
	public bool Cancel { get; set; }

	/// <summary>
	/// Gets the data payload associated with an items drag action.
	/// </summary>
	public DataPackage Data => _dragItemsStartingEventArgs.Data;

	/// <summary>
	/// Gets the loosely typed collection of objects that are selected for the item drag action.
	/// </summary>
	public IList<object> Items => _dragItemsStartingEventArgs.Items;
}

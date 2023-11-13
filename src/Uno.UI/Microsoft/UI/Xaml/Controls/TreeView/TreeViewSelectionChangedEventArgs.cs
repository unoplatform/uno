// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference TreeView.idl, tag winui3/release/1.4.2

using System.Collections.Generic;

namespace Microsoft.UI.Xaml.Controls;

/// <summary>
/// Provides data for the TreeView.SelectionChanged event.
/// </summary>
public partial class TreeViewSelectionChangedEventArgs
{
	internal TreeViewSelectionChangedEventArgs(IList<object> addedItems, IList<object> removedItems)
	{
		AddedItems = addedItems;
		RemovedItems = removedItems;
	}

	/// <summary>
	/// Gets a list that contains the items that were selected.
	/// </summary>
	public IList<object> AddedItems { get; }

	/// <summary>
	/// Gets a list that contains the items that were unselected.
	/// </summary>
	public IList<object> RemovedItems { get; }
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference TreeView.idl, tag winui3/release/1.4.2

namespace Microsoft.UI.Xaml.Controls;

/// <summary>
/// Provides event data for the TreeView.Expanding event.
/// </summary>
public partial class TreeViewExpandingEventArgs
{
	internal TreeViewExpandingEventArgs(TreeViewNode node)
	{
		Node = node;
	}

	/// <summary>
	/// Gets the tree view node that is expanding.
	/// </summary>
	public TreeViewNode Node { get; }

	/// <summary>
	/// Gets the data item for the tree view node that is expanding.
	/// </summary>
	public object Item => Node.Content;
}

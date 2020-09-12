// MUX Reference TreeView.idl, commit 96244e6

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace Microsoft.UI.Xaml.Controls
{
	/// <summary>
	/// Provides event data for the TreeView.Collapsed event.
	/// </summary>
	public partial class TreeViewCollapsedEventArgs
    {
		internal TreeViewCollapsedEventArgs(TreeViewNode node)
		{
			Node = node;
		}
		
		/// <summary>
		/// Gets the TreeView node that is collapsed.
		/// </summary>
		public TreeViewNode Node { get; }

		/// <summary>
		/// Gets the TreeView item that is collapsed.
		/// </summary>
		public object Item => Node.Content;
	}
}

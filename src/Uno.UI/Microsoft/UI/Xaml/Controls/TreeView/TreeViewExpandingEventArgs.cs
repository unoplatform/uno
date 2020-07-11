// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// MUX reference 838a0cc

namespace Microsoft.UI.Xaml.Controls
{
	public partial class TreeViewExpandingEventArgs
    {
		internal TreeViewExpandingEventArgs(TreeViewNode node)
		{
			Node = node;
		}

		public TreeViewNode Node { get; }

		public object Item => Node.Content;
	}
}

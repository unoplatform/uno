// MUX Reference TreeView.idl, commit 96244e6

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace Microsoft.UI.Xaml.Controls
{
	public partial class TreeViewItemInvokedEventArgs
	{
		internal TreeViewItemInvokedEventArgs(object invokedItem)
		{
			InvokedItem = invokedItem;
		}

		public bool Handled { get; set; }

		public object InvokedItem { get; }
	}
}

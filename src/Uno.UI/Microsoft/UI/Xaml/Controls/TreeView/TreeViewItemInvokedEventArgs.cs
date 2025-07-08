// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference TreeView.idl, tag winui3/release/1.4.2

namespace Microsoft.UI.Xaml.Controls;

public partial class TreeViewItemInvokedEventArgs
{
	internal TreeViewItemInvokedEventArgs(object invokedItem)
	{
		InvokedItem = invokedItem;
	}

	public bool Handled { get; set; }

	public object InvokedItem { get; }
}

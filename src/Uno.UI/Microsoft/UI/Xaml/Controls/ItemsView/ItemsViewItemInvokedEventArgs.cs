// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// MUX Reference ItemsViewItemInvokedEventArgs.cpp, tag winui3/release/1.5.0

namespace Microsoft.UI.Xaml.Controls;

partial class ItemsViewItemInvokedEventArgs
{
	internal ItemsViewItemInvokedEventArgs(object invokedItem)
	{
		//ITEMSVIEW_TRACE_VERBOSE(nullptr, TRACE_MSG_METH_PTR, METH_NAME, this, invokedItem);

		InvokedItem = invokedItem;
	}

	#region IItemsViewItemInvokedEventArgs
	public object InvokedItem { get; }
	#endregion
}

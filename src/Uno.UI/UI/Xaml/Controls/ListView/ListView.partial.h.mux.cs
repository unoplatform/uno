// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// MUX Reference ListView_Partial.h, tag winui3/release/2.5.1, commit ba3a8d59e6d

//  Abstract:
//      ListView displays a a rich, interactive collection of vertically stacked
//      items.


#nullable enable

namespace Microsoft.UI.Xaml.Controls;

// ListView displays a a rich, interactive collection of vertically stacked
// items.
public partial class ListView : ListViewBase, IListView
{
	internal bool ItemAllowFocusFromUIA() => m_itemsAcceptFocusFromUIA;

	internal void SetAllowItemFocusFromUIA(bool itemsAcceptFocusFromUIA) => m_itemsAcceptFocusFromUIA = itemsAcceptFocusFromUIA;

	private bool m_itemsAcceptFocusFromUIA;
}


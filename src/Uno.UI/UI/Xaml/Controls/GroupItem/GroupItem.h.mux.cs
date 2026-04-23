// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference GroupItem_Partial.h, commit 5f9e85113

#nullable enable

using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;

namespace Microsoft.UI.Xaml.Controls;

// forward declarations
// class Selector;

/// <summary>
/// Represents a UI for group of items.
/// </summary>
partial class GroupItem
{
	// friend class ItemsControl;
	// friend class ItemContainerGenerator;

	private ItemContainerGenerator? m_tpGenerator;
	private ItemsControl? m_tpItemsControl;
	private WeakReference<ListViewBase>? m_wrParentListViewBaseWeakRef;
	private ICollectionViewGroup? m_tpCVG;
	// EventRegistrationToken m_itemsChangedToken{};
	private ItemsChangedEventHandler? m_itemsChangedToken;
	// EventRegistrationToken m_headerKeyDownToken{};
	private KeyEventHandler? m_headerKeyDownToken;
	private Visibility m_previousVisibility;
	private Control? m_tpHeaderControl;
	private bool m_bWasHidden;
}

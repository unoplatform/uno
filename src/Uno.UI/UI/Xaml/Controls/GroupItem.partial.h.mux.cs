// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference GroupItem_Partial.h, tag winui3/release/2.5.1, commit ba3a8d59e6d

#nullable enable

using System;
using Microsoft.UI.Xaml.Data;

namespace Microsoft.UI.Xaml.Controls;

/// <summary>
/// Represents a UI for group of items.
/// </summary>
partial class GroupItem
{
#if !HAS_UNO // TODO Uno: depends on the legacy ItemContainerGenerator, which Uno does not port.
	private IItemContainerGenerator? m_tpGenerator;
	private EventRegistrationToken m_itemsChangedToken;
	private Visibility m_previousVisibility;
	private bool m_bWasHidden;
#endif
	private ItemsControl? m_tpItemsControl;
#pragma warning disable CS0649 // Only assigned in #if !HAS_UNO code.
	private WeakReference<ListViewBase>? m_wrParentListViewBaseWeakRef;
#pragma warning restore CS0649
	private ICollectionViewGroup? m_tpCVG;
	private Control? m_tpHeaderControl;
}

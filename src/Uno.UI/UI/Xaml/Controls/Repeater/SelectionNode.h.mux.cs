// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference SelectionNode.h, commit 4b206bce3

using System.Collections.Generic;

namespace Microsoft.UI.Xaml.Controls;

internal enum SelectionState
{
	Selected,
	NotSelected,
	PartiallySelected
}

partial class SelectionNode
{
	private SelectionModel m_manager;

	// Note that a node can contain children who are leaf as well as
	// chlidren containing leaf entries.

	// For inner nodes (any node whose children are data sources)
	private List<SelectionNode> m_childrenNodes = new List<SelectionNode>();
	// Don't take a ref.
	private SelectionNode m_parent = null;

	// For parents of leaf nodes (any node whose children are not data sources)
	private List<IndexRange> m_selected = new List<IndexRange>();

	private object m_source;
	private ItemsSourceView m_dataSource;
	// TODO Uno: Original C++ uses ItemsSourceView::CollectionChanged_revoker m_itemsSourceViewChanged{};
	// We use direct subscribe/unsubscribe through HookupCollectionChangedHandler / UnhookCollectionChangedHandler.

	private int m_selectedCount = 0;
	private List<int> m_selectedIndicesCached = new List<int>();
	private bool m_selectedIndicesCacheIsValid = false;
	private int m_realizedChildrenNodeCount = 0;
}

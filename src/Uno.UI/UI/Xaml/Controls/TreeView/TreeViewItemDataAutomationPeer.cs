// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference TreeViewItemDataAutomationPeer.cpp, tag winui3/release/1.8.4

#nullable enable

using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Automation.Peers;

/// <summary>
/// Exposes TreeViewItem data types to Microsoft UI Automation.
/// </summary>
public partial class TreeViewItemDataAutomationPeer : ItemAutomationPeer, IExpandCollapseProvider
{
	public TreeViewItemDataAutomationPeer(object item, TreeViewListAutomationPeer parent)
		: base(item, parent)
	{
	}

	// IExpandCollapseProvider
	public ExpandCollapseState ExpandCollapseState
	{
		get
		{
			if (GetTreeViewItemAutomationPeer() is { } peer)
			{
				return peer.ExpandCollapseState;
			}

			throw new InvalidOperationException("UIA_E_ELEMENTNOTENABLED");
		}
	}

	public void Collapse()
	{
		if (GetTreeViewItemAutomationPeer() is { } peer)
		{
			peer.Collapse();
			return;
		}

		throw new InvalidOperationException("UIA_E_ELEMENTNOTENABLED");
	}

	public void Expand()
	{
		if (GetTreeViewItemAutomationPeer() is { } peer)
		{
			peer.Expand();
			return;
		}

		throw new InvalidOperationException("UIA_E_ELEMENTNOTENABLED");
	}

	// IAutomationPeerOverrides
	protected override object GetPatternCore(PatternInterface patternInterface)
	{
		if (patternInterface == PatternInterface.ExpandCollapse)
		{
			return this;
		}

		return base.GetPatternCore(patternInterface);
	}

	private TreeViewItemAutomationPeer? GetTreeViewItemAutomationPeer()
	{
		// ItemsAutomationPeer hold ItemsControlAutomationPeer and Item properties.
		// ItemsControlAutomationPeer -> ItemsControl by ItemsControlAutomationPeer.Owner -> ItemsControl Look up Item to get TreeViewItem -> Get TreeViewItemAutomationPeer
		if (ItemsControlAutomationPeer is { } itemsControlAutomationPeer)
		{
			if (itemsControlAutomationPeer.Owner is ItemsControl itemsControl)
			{
				if (itemsControl.ContainerFromItem(Item) is UIElement item)
				{
					if (FrameworkElementAutomationPeer.CreatePeerForElement(item) is TreeViewItemAutomationPeer treeViewItemAutomationPeer)
					{
						return treeViewItemAutomationPeer;
					}
				}
			}
		}

		return null;
	}
}

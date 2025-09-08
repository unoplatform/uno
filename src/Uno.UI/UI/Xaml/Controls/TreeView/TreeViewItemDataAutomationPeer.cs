// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference TreeViewItemDataAutomationPeer.cpp, tag winui3/release/1.4.2

using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Automation.Peers;

/// <summary>
/// Exposes TreeViewItem data types to Microsoft UI Automation.
/// </summary>
public partial class TreeViewItemDataAutomationPeer : ItemAutomationPeer, IExpandCollapseProvider
{
	private const string UIA_E_ELEMENTNOTENABLED = "Element not enabled";

	/// <summary>
	/// Initializes a new instance of the TreeViewItemDataAutomationPeer class.
	/// </summary>
	/// <param name="item">The TreeViewItem.</param>
	/// <param name="parent">The TreeViewList parent control instance for which to create the peer.</param>
	public TreeViewItemDataAutomationPeer(object item, ItemsControlAutomationPeer parent)
		: base(item, parent)
	{
	}

	// IExpandCollapseProvider

	/// <summary>
	/// Gets a value indicating the expanded or collapsed state of the associated TreeViewItemDataAutomationPeer.
	/// </summary>
	public ExpandCollapseState ExpandCollapseState
	{
		get
		{
			var peer = GetTreeViewItemAutomationPeer();
			if (peer != null)
			{
				return peer.ExpandCollapseState;
			}
			throw new InvalidOperationException(UIA_E_ELEMENTNOTENABLED);
		}
	}

	/// <summary>
	/// Collapses the associated Microsoft.UI.Xaml.Automation.Peers.TreeViewItemDataAutomationPeer.
	/// </summary>
	public void Collapse()
	{
		var peer = GetTreeViewItemAutomationPeer();
		if (peer != null)
		{
			peer.Collapse();
			return;
		}
		throw new InvalidOperationException(UIA_E_ELEMENTNOTENABLED);
	}

	/// <summary>
	/// Expands the associated Microsoft.UI.Xaml.Automation.Peers.TreeViewItemDataAutomationPeer.
	/// </summary>
	public void Expand()
	{
		var peer = GetTreeViewItemAutomationPeer();
		if (peer != null)
		{
			peer.Expand();
			return;
		}
		throw new InvalidOperationException(UIA_E_ELEMENTNOTENABLED);
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

	private TreeViewItemAutomationPeer GetTreeViewItemAutomationPeer()
	{
		// ItemsAutomationPeer hold ItemsControlAutomationPeer and Item properties.
		// ItemsControlAutomationPeer -> ItemsControl by ItemsControlAutomationPeer.Owner -> ItemsControl Look up Item to get TreeViewItem -> Get TreeViewItemAutomationPeer
		var itemsControlAutomationPeer = ItemsControlAutomationPeer;
		if (itemsControlAutomationPeer != null)
		{
			var itemsControl = itemsControlAutomationPeer.Owner as ItemsControl;
			if (itemsControl != null)
			{
				var item = itemsControl.ContainerFromItem(Item) as UIElement;
				if (item != null)
				{
					var treeViewItemAutomationPeer = FrameworkElementAutomationPeer.CreatePeerForElement(item) as TreeViewItemAutomationPeer;
					if (treeViewItemAutomationPeer != null)
					{
						return treeViewItemAutomationPeer;
					}
				}
			}
		}
		throw new InvalidOperationException(UIA_E_ELEMENTNOTENABLED);
	}
}

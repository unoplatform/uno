// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference TreeViewListAutomationPeer.cpp, tag winui3/release/1.4.2

using System;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using TreeViewList = Microsoft.UI.Xaml.Controls.TreeViewList;

namespace Microsoft.UI.Xaml.Automation.Peers;

/// <summary>
/// Exposes TreeViewList types to Microsoft UI Automation.
/// </summary>
public partial class TreeViewListAutomationPeer : ListViewAutomationPeer, ISelectionProvider, IDropTargetProvider
{
	/// <summary>
	/// Initializes a new instance of the TreeViewListAutomationPeer class.
	/// </summary>
	/// <param name="owner">The TreeViewList control instance to create the peer for.</param>
	public TreeViewListAutomationPeer(TreeViewList owner) : base(owner)
	{
	}

	// IItemsControlAutomationPeerOverrides2
	protected override ItemAutomationPeer OnCreateItemAutomationPeer(object item)
	{
		var itemPeer = new TreeViewItemDataAutomationPeer(item, this);
		return itemPeer;
	}

	// DropTargetProvider
	string IDropTargetProvider.DropEffect => ((TreeViewList)Owner).GetDropTargetDropEffect();

	string[] IDropTargetProvider.DropEffects => throw new NotImplementedException();

	protected override object GetPatternCore(PatternInterface patternInterface)
	{
		if (patternInterface == PatternInterface.DropTarget ||
		   (patternInterface == PatternInterface.Selection && IsMultiselect))
		{
			return this;
		}

		return base.GetPatternCore(patternInterface);
	}

	protected override AutomationControlType GetAutomationControlTypeCore()
	{
		return AutomationControlType.Tree;
	}

	// ISelectionProvider
	bool ISelectionProvider.CanSelectMultiple => IsMultiselect ? true : base.CanSelectMultiple;

	bool ISelectionProvider.IsSelectionRequired => IsMultiselect ? false : base.CanSelectMultiple;

	IRawElementProviderSimple[] ISelectionProvider.GetSelection()
	{
		// The selected items might be collapsed, virtualized, so getting an accurate list of selected items is not possible.
		return Array.Empty<IRawElementProviderSimple>();
	}

	private bool IsMultiselect => ((TreeViewList)Owner).IsMultiselect;
}

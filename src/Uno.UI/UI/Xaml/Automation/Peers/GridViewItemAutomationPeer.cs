// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference GridViewItemAutomationPeer_Partial.cpp, tag winui3/release/1.8.4

using Microsoft.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Automation.Peers;

/// <summary>
/// Exposes the data items in the collection of Items in GridView types to UI Automation.
/// </summary>
public partial class GridViewItemAutomationPeer : FrameworkElementAutomationPeer
{
	public GridViewItemAutomationPeer(GridViewItem owner) : base(owner)
	{

	}

	protected override string GetClassNameCore() => nameof(GridViewItem);

	// WinUI 3: ListViewBaseItemAutomationPeer (base for both ListViewItem and GridViewItem)
	// returns AutomationControlType_ListItem. Without this override, GridViewItem defaults
	// to Custom, causing screen readers to not announce it as a list item.
	protected override AutomationControlType GetAutomationControlTypeCore()
		=> AutomationControlType.ListItem;

	protected override int GetPositionInSetCore()
	{
		var returnValue = base.GetPositionInSetCore();
		if (returnValue == -1)
		{
			returnValue = GetPositionOrSizeOfSetHelper(isSetCount: false);
		}
		return returnValue;
	}

	protected override int GetSizeOfSetCore()
	{
		var returnValue = base.GetSizeOfSetCore();
		if (returnValue == -1)
		{
			returnValue = GetPositionOrSizeOfSetHelper(isSetCount: true);
		}
		return returnValue;
	}

	private int GetPositionOrSizeOfSetHelper(bool isSetCount)
	{
		if (Owner is not GridViewItem owner)
		{
			return -1;
		}

		var gridView = ItemsControl.ItemsControlFromItemContainer(owner) as ListViewBase;
		if (gridView?.GetOrCreateAutomationPeer() is not ItemsControlAutomationPeer itemsControlAutomationPeer)
		{
			return -1;
		}

		return isSetCount
			? itemsControlAutomationPeer.GetSizeOfSetHelper(owner)
			: itemsControlAutomationPeer.GetPositionInSetHelper(owner);
	}
}

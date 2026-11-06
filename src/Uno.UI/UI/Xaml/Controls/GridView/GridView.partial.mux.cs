// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// MUX Reference GridView_Partial.cpp, tag winui3/release/2.5.1, commit ba3a8d59e6d

//  Abstract:
//      GridView displays a a rich, interactive collection of items in a tabular
//      format.


#nullable enable

using DirectUI;
using Microsoft.UI.Xaml.Automation.Peers;

namespace Microsoft.UI.Xaml.Controls;

public partial class GridView : ListViewBase
{
	public GridView()
	{
		DefaultStyleKey = typeof(GridView);
	}

	// This method returns information regarding recent activities on GridView Control,
	// Activities are whether the items get added/removed/reordered or content changed.
	// ListViewBase does all calculation, in this class we just have to choose right enum
	// based on the base class calculation. We also have to special-case when animations
	// are disabled due to keyboard reordering.
	internal override ThemeTransitionContext GetCurrentTransitionContext(
		int id)
	{
		ThemeTransitionContext returnValue;

		returnValue = base.GetCurrentTransitionContext(id);

		switch (returnValue)
		{
			case ThemeTransitionContext.SingleAddList:
				returnValue = ThemeTransitionContext.SingleAddGrid;
				break;

			case ThemeTransitionContext.MultipleAddList:
				returnValue = ThemeTransitionContext.MultipleAddGrid;
				break;

			case ThemeTransitionContext.SingleDeleteList:
				returnValue = ThemeTransitionContext.SingleDeleteGrid;
				break;

			case ThemeTransitionContext.MultipleDeleteList:
				returnValue = ThemeTransitionContext.MultipleDeleteGrid;
				break;

			case ThemeTransitionContext.SingleReorderList:
				returnValue = ThemeTransitionContext.SingleReorderGrid;
				break;

			case ThemeTransitionContext.MultipleReorderList:
				returnValue = ThemeTransitionContext.MultipleReorderGrid;
				break;
		}

		return returnValue;
	}

	// Create GridViewAutomationPeer to represent the GridView.
	protected override AutomationPeer OnCreateAutomationPeer()
	{
		GridViewAutomationPeer spGridViewAutomationPeer;

		spGridViewAutomationPeer = new GridViewAutomationPeer(this);

		return spGridViewAutomationPeer;
	}

	// Determines if the specified item is (or is eligible to be) its own container.
	protected override bool IsItemItsOwnContainerOverride(
		object item)
	{
		bool returnValue = false;

		returnValue = item is GridViewItem;

		return returnValue;
	}

	// Creates or identifies the element that is used to display the given item.
	protected override DependencyObject GetContainerForItemOverride()
	{
		GridViewItem spItem;

		spItem = new GridViewItem() { IsGeneratedContainer = true };

		return spItem;
	}

	// Creates or identifies the element that is used to display the
	// given group. This is only valid for new style flat panel grouping
	internal override ContentControl GetGroupHeaderContainer(object groupHeader)
	{
		GridViewHeaderItem spContainer;

		spContainer = new GridViewHeaderItem() { IsGeneratedContainer = true };

		return spContainer;
	}
}


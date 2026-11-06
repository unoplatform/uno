// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// MUX Reference ListView_Partial.cpp, tag winui3/release/2.5.1, commit ba3a8d59e6d


#nullable enable

using Microsoft.UI.Xaml.Automation.Peers;

namespace Microsoft.UI.Xaml.Controls;

public partial class ListView
{
	public ListView()
	{
		m_itemsAcceptFocusFromUIA = true;

		DefaultStyleKey = typeof(ListView);
	}

	// Create ListViewAutomationPeer to represent the ListView.
	protected override AutomationPeer OnCreateAutomationPeer()
	{
		ListViewAutomationPeer spListViewAutomationPeer;

		spListViewAutomationPeer = new ListViewAutomationPeer(this);

		return spListViewAutomationPeer;
	}

	// Determines if the specified item is (or is eligible to be) its own container.
	protected override bool IsItemItsOwnContainerOverride(
		object item)
	{
		bool returnValue = false;

		returnValue = item is ListViewItem;

		return returnValue;
	}

	// Creates or identifies the element that is used to display the given item.
	protected override DependencyObject GetContainerForItemOverride()
	{
		ListViewItem spItem;

		spItem = new ListViewItem() { IsGeneratedContainer = true };

		return spItem;
	}

	internal override ContentControl GetGroupHeaderContainer(object groupHeader)
	{
		ListViewHeaderItem spContainer;

		spContainer = new ListViewHeaderItem() { IsGeneratedContainer = true };

		return spContainer;
	}
}


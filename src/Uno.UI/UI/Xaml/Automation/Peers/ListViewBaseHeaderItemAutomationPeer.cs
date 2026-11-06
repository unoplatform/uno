// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#nullable enable
#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Automation.Peers;

public partial class ListViewBaseHeaderItemAutomationPeer : FrameworkElementAutomationPeer
{
	protected ListViewBaseHeaderItemAutomationPeer(ListViewBaseHeaderItem owner) : base(owner)
	{
		ArgumentNullException.ThrowIfNull(owner);
	}

	protected override string GetClassNameCore() => nameof(ListViewBaseHeaderItem);

	protected override AutomationControlType GetAutomationControlTypeCore()
		=> AutomationControlType.Group;

	// TODO Uno: GetChildrenCore and GetPatternCore (ListViewBaseHeaderItemAutomationPeer_Partial.cpp) are not ported;
	// they need ItemsControlAutomationPeer's children cache accessors and the invoke adapter hookup.

	protected override Rect GetBoundingRectangleCore()
		=> ((ListViewBaseHeaderItem)Owner).GetGroupBounds();

	protected override int GetPositionInSetCore()
	{
		// First retrieve any valid value being directly set on the container, that value will get precedence.
		int returnValue = base.GetPositionInSetCore();

		// if it still is default value, calculate it ourselves.
		if (returnValue == -1)
		{
			var spItemsControlAutomationPeer = GetParentItemsControlAutomationPeer();

			if (spItemsControlAutomationPeer?.Owner is ItemsControl spItemsControl)
			{
				// Index of the header itself is position of the header within the list
				// Headers of empty groups are not counted when ModernCollectionBasePanel::m_hidesIfEmpty is True.
				int headerIndex = spItemsControl.IndexFromHeader(Owner, true /*excludeHiddenEmptyGroups*/);
				returnValue = headerIndex + 1;
			}
		}

		return returnValue;
	}

	// TODO Uno: GetSizeOfSetCore needs ModernCollectionBasePanel.GetTotalGroupCountImpl, which is not exposed on the panel yet.

	protected override int GetLevelCore()
	{
		// First retrieve any valid value being directly set on the container, that value will get precedence.
		int returnValue = base.GetLevelCore();

		// if it still is default value, calculate it ourselves.
		if (returnValue == -1)
		{
			// Header are always first level elements within list.
			returnValue = 1;
		}

		return returnValue;
	}

	internal ItemsControlAutomationPeer? GetParentItemsControlAutomationPeer()
	{
		if (Owner is not ListViewBaseHeaderItem header)
		{
			return null;
		}

		var parentControl = ItemsControl.ItemsControlFromItemContainer(header);
		if (parentControl is FrameworkElement element)
		{
			return FrameworkElementAutomationPeer.FromElement(element) as ItemsControlAutomationPeer
				?? FrameworkElementAutomationPeer.CreatePeerForElement(element) as ItemsControlAutomationPeer;
		}

		return null;
	}
}

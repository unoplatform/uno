// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// MUX Reference ItemsViewAutomationPeer.cpp, tag winui3/release/1.8.4

using System.Collections.Generic;
using Microsoft.UI.Private.Controls;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Automation.Peers;

partial class ItemsViewAutomationPeer : FrameworkElementAutomationPeer, ISelectionProvider
{

	public ItemsViewAutomationPeer(ItemsView owner) : base(owner)
	{
	}

	// IAutomationPeerOverrides
	protected override object GetPatternCore(PatternInterface patternInterface)
	{
		if (Owner is ItemsView itemsView)
		{
			if (patternInterface == PatternInterface.Selection && itemsView.SelectionMode != ItemsViewSelectionMode.None)
			{
				return this;
			}
		}

		return base.GetPatternCore(patternInterface);
	}

	protected override string GetClassNameCore()
	{
		return nameof(ItemsView);
	}

	protected override AutomationControlType GetAutomationControlTypeCore()
	{
		return AutomationControlType.List;
	}

	// ISelectionProvider
	public bool CanSelectMultiple
	{
		get
		{
			if (GetImpl() is { } itemsView)

			{
				var selectionMode = itemsView.SelectionMode;
				if (selectionMode == ItemsViewSelectionMode.Multiple ||
					selectionMode == ItemsViewSelectionMode.Extended)
				{
					return true;
				}
			}

			return false;
		}
	}

	public IRawElementProviderSimple[] GetSelection()
	{
		List<IRawElementProviderSimple> selectionList = null;

		if (GetImpl() is ItemsView itemsView)
		{
			if (itemsView.GetSelectionModel() is { } selectionModel)
			{
				if (selectionModel.SelectedIndices is { } selectedIndices)
				{
					if (selectedIndices.Count > 0)
					{
						if (ItemsViewTestHooks.GetItemsRepeaterPart(itemsView) is { } repeater)
						{
							foreach (var indexPath in selectedIndices)
							{
								// TODO: Update once ItemsView has grouping.
								var index = indexPath.GetAt(0);

								if (repeater.TryGetElement(index) is { } itemElement)
								{
									if (FrameworkElementAutomationPeer.CreatePeerForElement(itemElement) is { } peer)

									{
										(selectionList ??= new()).Add(ProviderFromPeer(peer));
									}
								}
							}
						}
					}
				}
			}
		}

		return selectionList?.ToArray();
	}

	internal void RaiseSelectionChanged(double oldIndex, double newIndex)
	{
		if (AutomationPeer.ListenerExists(AutomationEvents.SelectionPatternOnInvalidated))
		{
			if (Owner is ItemsView itemsView)
			{
				if (FrameworkElementAutomationPeer.CreatePeerForElement(itemsView) is { } peer)
				{
					peer.RaiseAutomationEvent(AutomationEvents.SelectionPatternOnInvalidated);
				}
			}

		}
	}

	ItemsView GetImpl()
	{
		return Owner as ItemsView;
	}
}

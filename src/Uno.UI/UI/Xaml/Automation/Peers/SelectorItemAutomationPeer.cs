// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference SelectorItemAutomationPeer_Partial.cpp, tag winui3/release/1.8.4

using System;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace Microsoft.UI.Xaml.Automation.Peers;

/// <summary>
/// Exposes the items in a Selector to Microsoft UI Automation.
/// </summary>
public partial class SelectorItemAutomationPeer : ItemAutomationPeer, Provider.ISelectionItemProvider
{
	public SelectorItemAutomationPeer(object item, SelectorAutomationPeer parent) : base(item, parent)
	{
	}

	protected override object GetPatternCore(PatternInterface patternInterface)
	{
		var spPatternProvider = base.GetPatternCore(patternInterface);

		if (spPatternProvider is not { })
		{
			return spPatternProvider;
		}

		if (patternInterface == PatternInterface.SelectionItem)
		{
			var selectionPatternApplicable = true;

			if (ItemsControlAutomationPeer is { } parent)
			{
				selectionPatternApplicable = (parent.Owner as Selector).IsSelectionPatternApplicable();
			}

			if (selectionPatternApplicable)
			{
				return this;
			}
		}
		else
		if (patternInterface == PatternInterface.ScrollItem)
		{
			return this;
		}

		return spPatternProvider;
	}

	/// <summary>
	/// Clears any existing selection and then selects the current element.
	/// </summary>
	public void Select()
	{
		if (!IsEnabled())
		{
			throw new Exception("Element is not enabled.");
		}

		if (ItemsControlAutomationPeer is { } parent)
		{
			if (parent.Owner is not ISelector selector)
			{
				throw new Exception("Operation cannot be performed.");
			}

			var index = (selector as Selector).Items.IndexOf(Item);
			(selector as Selector).MakeSingleSelection(index, false /*animateIfBringIntoView*/, Item, default);
		}
	}

	/// <summary>
	/// Adds the current element to the collection of selected items.
	/// </summary>
	public void AddToSelection()
	{
		if (!IsEnabled())
		{
			throw new Exception("Element is not enabled.");
		}

		if (ItemsControlAutomationPeer is { } parent)
		{
			if (parent.Owner is not ISelector selector)
			{
				throw new Exception("Operation cannot be performed.");
			}

			var index = (selector as Selector).Items.IndexOf(Item);
			(selector as Selector).AutomationPeerAddToSelection(index, Item);
		}
	}

	/// <summary>
	/// Removes the current element from the collection of selected items.
	/// </summary>
	public void RemoveFromSelection()
	{
		if (!IsEnabled())
		{
			throw new Exception("Element is not enabled.");
		}

		if (ItemsControlAutomationPeer is { } parent)
		{
			if (parent.Owner is not ISelector selector)
			{
				throw new Exception("Operation cannot be performed.");
			}

			var index = (selector as Selector).Items.IndexOf(Item);
			(selector as Selector).AutomationPeerRemoveFromSelection(index, Item);
		}
	}

	/// <summary>
	/// Gets a value that indicates whether an item is selected.
	/// </summary>
	public bool IsSelected
	{
		get
		{
			if (!IsEnabled())
			{
				throw new Exception("Element is not enabled.");
			}

			if (ItemsControlAutomationPeer is { } parent)
			{
				if (parent.Owner is ISelector selector)
				{
					return (selector as Selector).AutomationPeerIsSelected(Item);
				}
			}

			return false;
		}
	}

	/// <summary>
	/// Gets the UI Automation provider that implements ISelectionProvider and acts as container for the calling object.
	/// </summary>
	public Provider.IRawElementProviderSimple SelectionContainer
	{
		get
		{
			if (ItemsControlAutomationPeer is { } parent)
			{
				return ProviderFromPeer(parent);
			}

			return null;
		}
	}

	internal void ScrollIntoView()
	{
		if (ItemsControlAutomationPeer is { } parent)
		{
			(parent.Owner as Selector).ScrollIntoViewInternal(Item, /*isHeader*/ false, /*isFooter*/ false, /*isFromPublicAPI*/ true, ScrollIntoViewAlignment.Default, default, default);
		}
	}
}

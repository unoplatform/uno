// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#nullable enable
#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace Microsoft.UI.Xaml.Automation.Peers;

public partial class SelectorAutomationPeer : ItemsControlAutomationPeer, ISelectionProvider
{
	public SelectorAutomationPeer(Selector owner) : base(owner)
	{
	}

	protected override object GetPatternCore(PatternInterface patternInterface)
	{
		if (patternInterface == PatternInterface.Selection)
		{
			return SelectorOwner?.IsSelectionPatternApplicable() == true ? this : null;
		}

		return base.GetPatternCore(patternInterface);
	}

	public bool CanSelectMultiple => SelectorOwner is { } selector && !selector.IsSingleSelection;

	public virtual bool IsSelectionRequired => false;

	public IRawElementProviderSimple[] GetSelection()
	{
		var selectedItems = GetSelectedItems();
		if (selectedItems.Count == 0)
		{
			return Array.Empty<IRawElementProviderSimple>();
		}

		var providers = new List<IRawElementProviderSimple>(selectedItems.Count);
		foreach (var item in selectedItems)
		{
			var peer = CreateItemAutomationPeer(item);
			if (peer == null)
			{
				continue;
			}

			var provider = ProviderFromPeer(peer);
			if (provider != null)
			{
				providers.Add(provider);
			}
		}

		return providers.ToArray();
	}

	protected override ItemAutomationPeer OnCreateItemAutomationPeer(object item)
		=> new SelectorItemAutomationPeer(item, this);

	private IReadOnlyList<object> GetSelectedItems()
	{
		if (SelectorOwner is ListViewBase listViewBase)
		{
			var selectedItems = listViewBase.SelectedItems;
			if (selectedItems != null && selectedItems.Count > 0)
			{
				var copy = new object[selectedItems.Count];
				for (var i = 0; i < selectedItems.Count; i++)
				{
					copy[i] = selectedItems[i];
				}

				return copy;
			}

			return listViewBase.SelectedItem != null
				? new[] { listViewBase.SelectedItem }
				: Array.Empty<object>();
		}

		if (SelectorOwner?.SelectedItem != null)
		{
			return new[] { SelectorOwner.SelectedItem };
		}

		return Array.Empty<object>();
	}

	private Selector SelectorOwner => Owner as Selector;
}

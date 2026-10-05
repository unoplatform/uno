// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// MUX Reference ItemInvokeAdapter_Partial.cpp, commit 8463f45

#nullable enable

using System;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace Microsoft.UI.Xaml.Automation.Peers;

/// <summary>
/// Implements the Invoke UIA pattern for ListViewBase items, as they support ItemClick.
/// </summary>
internal sealed class ItemInvokeAdapter : IInvokeProvider
{
	// Automation Peer that owns this adapter
	private readonly WeakReference<FrameworkElementAutomationPeer> _automationPeerWeakRef;

	public ItemInvokeAdapter(FrameworkElementAutomationPeer automationPeer)
		=> _automationPeerWeakRef = new(automationPeer);

	// Defines behavior of when Invoke is called on an Item through UIA Client
	public void Invoke()
	{
		if (_automationPeerWeakRef.TryGetTarget(out var automationPeer) &&
			automationPeer.Owner is SelectorItem listViewBaseItem &&
			ItemsControl.ItemsControlFromItemContainer(listViewBaseItem) is ListViewBase parentListViewBase)
		{
			// Notify that UIAutomation has invoked action on ListViewBaseItem
			parentListViewBase.AutomationItemClick(listViewBaseItem);
		}

		// TODO Uno: ListViewBaseHeaderItem invocation (semantic zoom view toggle) is not supported.
	}
}

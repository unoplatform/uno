// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference RepeaterAutomationPeer.cpp, commit 4b206bce3

using System.Collections.Generic;
using Microsoft.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Automation.Peers;

partial class RepeaterAutomationPeer
{
	public RepeaterAutomationPeer(ItemsRepeater owner) : base(owner)
	{
	}

	// #pragma region IAutomationPeerOverrides

	protected override IList<AutomationPeer> GetChildrenCore()
	{
		var repeater = Owner as ItemsRepeater;
		var childrenPeers = base.GetChildrenCore();
		var peerCount = childrenPeers.Count;

		var realizedPeers = new List<KeyValuePair<int /* index */, AutomationPeer>>(peerCount);

		// Filter out unrealized peers.
		{
			for (var i = 0; i < peerCount; ++i)
			{
				var childPeer = childrenPeers[i];
				if (GetElement(childPeer, repeater) is { } childElement)
				{
					var virtInfo = ItemsRepeater.GetVirtualizationInfo(childElement);
					if (virtInfo.IsRealized)
					{
						realizedPeers.Add(new KeyValuePair<int, AutomationPeer>(virtInfo.Index, childPeer));
					}
				}
			}
		}

		// Sort peers by index.
		realizedPeers.Sort((lhs, rhs) => lhs.Key - rhs.Key);

		// Select peers.
		{
			var peers = new List<AutomationPeer>(realizedPeers.Count);
			foreach (var entry in realizedPeers)
			{
				peers.Add(entry.Value);
			}
			return peers;
		}
	}

	protected override AutomationControlType GetAutomationControlTypeCore()
	{
		return AutomationControlType.Group;
	}

	// #pragma endregion

	// Get the immediate child element of repeater under which this childPeer came from.
	private UIElement GetElement(AutomationPeer childPeer, ItemsRepeater repeater)
	{
		var childElement = (DependencyObject)((FrameworkElementAutomationPeer)childPeer).Owner;

		var parent = CachedVisualTreeHelpers.GetParent(childElement);
		// Child peer could have given a descendant of the repeater's child. We
		// want to get to the immediate child.
		while (parent != null && parent as ItemsRepeater != repeater)
		{
			childElement = parent;
			parent = CachedVisualTreeHelpers.GetParent(childElement);
		}

		return childElement as UIElement;
	}
}

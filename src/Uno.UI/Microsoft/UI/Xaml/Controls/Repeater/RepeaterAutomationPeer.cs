// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Generic;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Automation.Peers;
using Microsoft/* UWP don't rename */.UI.Xaml.Controls;

namespace Microsoft/* UWP don't rename */.UI.Xaml.Automation.Peers
{
	public partial class RepeaterAutomationPeer : FrameworkElementAutomationPeer
	{
		public RepeaterAutomationPeer(ItemsRepeater owner) : base(owner)
		{
		}

		#region IAutomationPeerOverrides
		protected override IList<AutomationPeer> GetChildrenCore()
		{
			var repeater = Owner as ItemsRepeater;
			var childrenPeers = base.GetChildrenCore();
			var peerCount = childrenPeers.Count;

			List<KeyValuePair<int /* index */, AutomationPeer>> realizedPeers = new List<KeyValuePair<int /* index */, AutomationPeer>>(peerCount);

			// Filter out unrealized peers.
			{
				for (var i = 0; i < peerCount; ++i)
				{
					var childPeer = childrenPeers[i];
					var childElement = GetElement(childPeer, repeater);
					if (childElement != null)
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

		#endregion

		// Get the immediate child element of repeater under which this childPeer came from. 
		private UIElement GetElement(AutomationPeer childPeer, ItemsRepeater repeater)
		{
			var childElement = (DependencyObject)(childPeer as FrameworkElementAutomationPeer).Owner;

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
}

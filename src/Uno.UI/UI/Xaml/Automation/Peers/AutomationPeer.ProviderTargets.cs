#nullable enable

using System.Diagnostics.CodeAnalysis;
using Microsoft.UI.Xaml;

namespace Microsoft.UI.Xaml.Automation.Peers;

partial class AutomationPeer
{
	internal AutomationPeer ResolveProviderPeer(bool resolveEventsSource)
	{
		if (resolveEventsSource && GetAPEventsSource() is { } eventsSource)
		{
			return eventsSource;
		}

		return this;
	}

	internal bool TryGetProviderOwner([NotNullWhen(true)] out UIElement? owner)
	{
		switch (this)
		{
			case FrameworkElementAutomationPeer { Owner: { } element }:
				owner = element;
				return true;

			case ItemAutomationPeer itemPeer when itemPeer.GetContainer() is { } container:
				owner = container;
				return true;

			default:
				// Data/virtual peers (e.g. LoopingSelectorItemDataAutomationPeer) don't
				// own a UIElement directly. Walk the parent peer chain to find the
				// nearest ancestor that has one, so the provider infrastructure can
				// create a virtual provider keyed by this peer.
				var parentPeer = GetParent();
				var depth = 0;
				while (parentPeer is not null && depth++ < 50)
				{
					if (parentPeer is FrameworkElementAutomationPeer { Owner: { } parentElement })
					{
						owner = parentElement;
						return true;
					}

					if (parentPeer is ItemAutomationPeer parentItemPeer
						&& parentItemPeer.GetContainer() is { } parentContainer)
					{
						owner = parentContainer;
						return true;
					}

					parentPeer = parentPeer.GetParent();
				}

				owner = null;
				return false;
		}
	}
}

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
				owner = null;
				return false;
		}
	}
}

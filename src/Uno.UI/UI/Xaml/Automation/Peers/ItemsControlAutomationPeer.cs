#pragma warning disable 108 // new keyword hiding
using System;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Automation.Peers;

public partial class ItemsControlAutomationPeer : FrameworkElementAutomationPeer, IItemContainerProvider
{
	public ItemsControlAutomationPeer(ItemsControl owner) : base(owner)
	{
	}

	public ItemsControlAutomationPeer(FrameworkElement e)
	{
	}

	public ItemsControlAutomationPeer(object e)
	{
	}
}

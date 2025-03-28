#pragma warning disable 108 // new keyword hiding
using System;
using Windows.UI.Xaml.Automation.Provider;
using Windows.UI.Xaml.Controls;

namespace Windows.UI.Xaml.Automation.Peers;

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

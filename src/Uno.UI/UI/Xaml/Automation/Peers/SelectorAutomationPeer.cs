#pragma warning disable 108 // new keyword hiding
using System;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Automation.Provider;

namespace Windows.UI.Xaml.Automation.Peers;

public partial class SelectorAutomationPeer : ItemsControlAutomationPeer, ISelectionProvider
{
	public SelectorAutomationPeer(Selector owner) : base(owner)
	{
	}

	public SelectorAutomationPeer(object o) : base(null)
	{
	}
}

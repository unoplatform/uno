#pragma warning disable 108 // new keyword hiding
using System;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Automation.Provider;

namespace Microsoft.UI.Xaml.Automation.Peers;

public partial class SelectorAutomationPeer : ItemsControlAutomationPeer, ISelectionProvider
{
	public SelectorAutomationPeer(Selector owner) : base(owner)
	{
	}
}

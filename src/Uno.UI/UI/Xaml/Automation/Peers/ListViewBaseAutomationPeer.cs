#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
using System;

namespace Microsoft.UI.Xaml.Automation.Peers
{
	public partial class ListViewBaseAutomationPeer : global::Microsoft.UI.Xaml.Automation.Peers.SelectorAutomationPeer, global::Microsoft.UI.Xaml.Automation.Provider.IDropTargetProvider
	{
		[Uno.NotImplemented]
		public ListViewBaseAutomationPeer(object e) : base(null)
		{
			throw new NotImplementedException();
		}
	}
}

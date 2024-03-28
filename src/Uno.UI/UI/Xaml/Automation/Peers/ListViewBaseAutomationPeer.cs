#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
using System;

namespace Windows.UI.Xaml.Automation.Peers
{
	public partial class ListViewBaseAutomationPeer : global::Windows.UI.Xaml.Automation.Peers.SelectorAutomationPeer, global::Windows.UI.Xaml.Automation.Provider.IDropTargetProvider
	{
		[Uno.NotImplemented]
		public ListViewBaseAutomationPeer(object e) : base(null)
		{
			throw new NotImplementedException();
		}
	}
}

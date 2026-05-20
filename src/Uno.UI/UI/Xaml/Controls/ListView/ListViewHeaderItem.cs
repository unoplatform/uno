using System;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class ListViewHeaderItem : ListViewBaseHeaderItem
	{
		public ListViewHeaderItem()
		{
			DefaultStyleKey = typeof(ListViewHeaderItem);
		}

		protected override Automation.Peers.AutomationPeer OnCreateAutomationPeer()
			=> new Automation.Peers.ListViewHeaderItemAutomationPeer(this);
	}
}

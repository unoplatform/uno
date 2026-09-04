using System;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class GridViewHeaderItem : ListViewBaseHeaderItem
	{
		public GridViewHeaderItem()
		{
			DefaultStyleKey = typeof(GridViewHeaderItem);
		}

		protected override Automation.Peers.AutomationPeer OnCreateAutomationPeer()
			=> new Automation.Peers.GridViewHeaderItemAutomationPeer(this);
	}
}

using System;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class GridViewHeaderItem : ListViewBaseHeaderItem
	{
#if !__WASM__
		public GridViewHeaderItem()
		{
			DefaultStyleKey = typeof(GridViewHeaderItem);
		}
#endif

		protected override Automation.Peers.AutomationPeer OnCreateAutomationPeer()
			=> new Automation.Peers.GridViewHeaderItemAutomationPeer(this);
	}
}

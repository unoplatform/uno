using NavigationViewItem = Windows.UI.Xaml.Controls.NavigationViewItem;

namespace Windows.UI.Xaml.Automation.Peers
{
	public partial class NavigationViewItemAutomationPeer : ListViewItemAutomationPeer
	{
		public NavigationViewItemAutomationPeer(NavigationViewItem owner) : base(owner)
		{
		}
	}
}

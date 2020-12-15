#if HAS_UNO_WINUI
using Microsoft.UI.Xaml.Automation.Peers;
#else
using Windows.UI.Xaml.Automation.Peers;
using Windows.UI.Xaml.Controls;
#endif
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

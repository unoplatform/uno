using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Automation.Peers;

namespace Microsoft.UI.Xaml.Automation.Peers
{
	public partial class MenuBarItemAutomationPeer : FrameworkElementAutomationPeer
	{
		private MenuBarItem MenuBarItemOwner => Owner as MenuBarItem;

		public MenuBarItemAutomationPeer(MenuBarItem owner) : base(owner)
		{
		}

		public void Invoke() => MenuBarItemOwner?.Invoke();

		public void Expand() => MenuBarItemOwner?.ShowMenuFlyout();

		public void Collapse() => MenuBarItemOwner?.CloseMenuFlyout();
	}
}

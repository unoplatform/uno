using Microsoft.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Automation.Peers
{
	public partial class MenuBarItemAutomationPeer : FrameworkElementAutomationPeer
	{
		private MenuBarItem MenuBarItemOwner => Owner as MenuBarItem;

		public MenuBarItemAutomationPeer(global::Microsoft.UI.Xaml.Controls.MenuBarItem owner) : base(owner)
		{
		}

		public void Invoke() => MenuBarItemOwner?.Invoke();

		public void Expand() => MenuBarItemOwner?.ShowMenuFlyout();

		public void Collapse() => MenuBarItemOwner?.CloseMenuFlyout();
	}
}

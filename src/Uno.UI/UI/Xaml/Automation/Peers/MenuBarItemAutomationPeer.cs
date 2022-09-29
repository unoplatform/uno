#nullable disable

using Windows.UI.Xaml.Controls;

namespace Windows.UI.Xaml.Automation.Peers
{
	public partial class MenuBarItemAutomationPeer : FrameworkElementAutomationPeer
	{
		private MenuBarItem MenuBarItemOwner => Owner as MenuBarItem;

		public MenuBarItemAutomationPeer(global::Windows.UI.Xaml.Controls.MenuBarItem owner) : base(owner)
		{
		}

		public void Invoke() => MenuBarItemOwner?.Invoke();

		public void Expand() => MenuBarItemOwner?.ShowMenuFlyout();

		public void Collapse() => MenuBarItemOwner?.CloseMenuFlyout();
	}
}

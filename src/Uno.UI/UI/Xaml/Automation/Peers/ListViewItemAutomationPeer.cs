using Windows.UI.Xaml.Controls;

namespace Windows.UI.Xaml.Automation.Peers
{
	public partial class ListViewItemAutomationPeer : FrameworkElementAutomationPeer
	{
		public ListViewItemAutomationPeer(ListViewItem owner) : base(owner)
		{
		}

		protected override string GetClassNameCore()
		{
			return "ListViewItem";
		}

		protected override AutomationControlType GetAutomationControlTypeCore()
		{
			return AutomationControlType.ListItem;
		}
	}
}

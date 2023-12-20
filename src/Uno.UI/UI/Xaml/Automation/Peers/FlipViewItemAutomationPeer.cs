using Microsoft.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Automation.Peers
{
	public partial class FlipViewItemAutomationPeer : FrameworkElementAutomationPeer
	{
		public FlipViewItemAutomationPeer(FlipViewItem owner) : base(owner)
		{
		}

		protected override string GetClassNameCore()
		{
			return "FlipViewItem";
		}

		protected override AutomationControlType GetAutomationControlTypeCore()
		{
			return AutomationControlType.ListItem;
		}
	}
}

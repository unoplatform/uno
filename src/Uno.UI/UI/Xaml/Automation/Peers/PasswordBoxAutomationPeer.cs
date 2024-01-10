using Microsoft.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Automation.Peers
{
	public partial class PasswordBoxAutomationPeer : FrameworkElementAutomationPeer
	{
		public PasswordBoxAutomationPeer(PasswordBox owner) : base(owner)
		{
		}

		protected override string GetClassNameCore()
		{
			return "PasswordBox";
		}

		protected override AutomationControlType GetAutomationControlTypeCore()
		{
			return AutomationControlType.Edit;
		}

		protected override bool IsPasswordCore()
		{
			return true;
		}
	}
}

namespace Microsoft.UI.Xaml.Automation.Peers
{
	public partial class CheckBoxAutomationPeer : ToggleButtonAutomationPeer
	{
		public CheckBoxAutomationPeer(Controls.CheckBox owner) : base(owner)
		{
		}

		protected override string GetClassNameCore()
		{
			return "CheckBox";
		}

		protected override AutomationControlType GetAutomationControlTypeCore()
		{
			return AutomationControlType.CheckBox;
		}
	}
}

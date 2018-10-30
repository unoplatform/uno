using Windows.UI.Xaml.Controls.Primitives;

namespace Windows.UI.Xaml.Automation.Peers
{
	public partial class ToggleButtonAutomationPeer : ButtonBaseAutomationPeer, Provider.IToggleProvider
	{
		public ToggleButtonAutomationPeer(ToggleButton element) : base(element)
		{
		}

		protected override string GetClassNameCore()
		{
			return "ToggleButton";
		}

		protected override AutomationControlType GetAutomationControlTypeCore()
		{
			return AutomationControlType.Button;
		}

		public ToggleState ToggleState
		{
			get
			{
				switch (((ToggleButton)Owner).IsChecked)
				{
					case (true):
						return ToggleState.On;
					case (false):
						return ToggleState.Off;
					default:
						return ToggleState.Indeterminate;
				}
			}
		}

		public void Toggle()
		{
			if (IsEnabled())
			{
				((ToggleButton)Owner).AutomationPeerToggle();
			}
		}
	}
}

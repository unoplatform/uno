using Windows.UI.Xaml.Controls;
using static Microsoft.UI.Xaml.Controls._Tracing;

namespace Windows.UI.Xaml.Automation.Peers
{
	public partial class ToggleSwitchAutomationPeer : FrameworkElementAutomationPeer, Provider.IToggleProvider
	{
		public ToggleSwitchAutomationPeer(ToggleSwitch owner) : base(owner)
		{
		}

		protected override string GetClassNameCore()
		{
			return "ToggleSwitch";
		}

		protected override AutomationControlType GetAutomationControlTypeCore()
		{
			return AutomationControlType.Button;
		}

		public ToggleState ToggleState => ((ToggleSwitch)Owner).IsOn ? ToggleState.On : ToggleState.Off;

		public void Toggle()
		{
			if (IsEnabled())
			{
				((ToggleSwitch)Owner).AutomationPeerToggle();
			}
		}

		internal void RaiseToggleStatePropertyChangedEvent(object pOldValue, object pNewValue)
		{
			var oldValue = ToggleButtonAutomationPeer.ConvertToToggleState(pOldValue);
			var newValue = ToggleButtonAutomationPeer.ConvertToToggleState(pNewValue);

			MUX_ASSERT(oldValue != ToggleState.Indeterminate);
			MUX_ASSERT(newValue != ToggleState.Indeterminate);

			if (oldValue != newValue)
			{
				RaisePropertyChangedEvent(TogglePatternIdentifiers.ToggleStateProperty, oldValue, newValue);
			}
		}
	}
}

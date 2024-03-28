using Windows.UI.Xaml.Controls;
using static Microsoft/* UWP don't rename */.UI.Xaml.Controls._Tracing;

namespace Windows.UI.Xaml.Automation.Peers
{
	/// <summary>
	/// Exposes ToggleSwitch types to Microsoft UI Automation.
	/// </summary>
	public partial class ToggleSwitchAutomationPeer : FrameworkElementAutomationPeer, Provider.IToggleProvider
	{
		/// <summary>
		/// Initializes a new instance of the ToggleSwitchAutomationPeer class.
		/// </summary>
		/// <param name="owner"></param>
		public ToggleSwitchAutomationPeer(ToggleSwitch owner) : base(owner)
		{
		}

		protected override string GetClassNameCore() => "ToggleSwitch";

		protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Button;

		/// <summary>
		/// Gets the toggle state of the control.
		/// </summary>
		public ToggleState ToggleState => ((ToggleSwitch)Owner).IsOn ? ToggleState.On : ToggleState.Off;

		/// <summary>
		/// Cycles through the toggle states of a control.
		/// </summary>
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

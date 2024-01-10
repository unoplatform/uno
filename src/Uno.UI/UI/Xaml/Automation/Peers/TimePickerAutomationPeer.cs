using Microsoft.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Automation.Peers
{
	public partial class TimePickerAutomationPeer : FrameworkElementAutomationPeer
	{
		public TimePickerAutomationPeer(TimePicker owner) : base(owner)
		{
		}

		protected override string GetClassNameCore()
		{
			return "TimePicker";
		}

		protected override AutomationControlType GetAutomationControlTypeCore()
		{
			return AutomationControlType.Group;
		}
	}
}

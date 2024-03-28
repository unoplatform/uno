using Windows.UI.Xaml.Controls;

namespace Windows.UI.Xaml.Automation.Peers
{
	public partial class DatePickerAutomationPeer : FrameworkElementAutomationPeer
	{
		public DatePickerAutomationPeer(DatePicker owner) : base(owner)
		{
		}

		protected override string GetClassNameCore()
		{
			return "DatePicker";
		}

		protected override AutomationControlType GetAutomationControlTypeCore()
		{
			return AutomationControlType.Group;
		}
	}
}

using Uno.UI.Helpers.WinUI;
using Microsoft.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Automation.Peers;

public partial class TimePickerAutomationPeer : FrameworkElementAutomationPeer
{
	private string UIA_AP_TIMEPICKER = nameof(UIA_AP_TIMEPICKER);

	public TimePickerAutomationPeer(TimePicker owner) : base(owner)
	{
	}

	protected override string GetClassNameCore() => nameof(TimePicker);

	protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Group;

	protected override string GetNameCore()
	{
		var returnValue = base.GetNameCore();

		if (string.IsNullOrEmpty(returnValue))
		{
			var owner = (TimePicker)Owner;

			var spHeaderAsInspectable = owner.Header;
			if (spHeaderAsInspectable is not null)
			{
				returnValue = FrameworkElement.GetStringFromObject(spHeaderAsInspectable);
			}

			if (string.IsNullOrEmpty(returnValue))
			{
				returnValue = ResourceAccessor.GetLocalizedStringResource(UIA_AP_TIMEPICKER);
			}
		}

		return returnValue;
	}
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference ToggleMenuFlyoutItemAutomationPeer_Partial.cpp, tag winui3/release/1.8.4
using Uno.UI.Helpers.WinUI;
using Microsoft.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Automation.Peers;

public partial class TimePickerAutomationPeer : FrameworkElementAutomationPeer
{
	private const string UIA_AP_TIMEPICKER = nameof(UIA_AP_TIMEPICKER);

	public TimePickerAutomationPeer(TimePicker owner) : base(owner)
	{
	}

	protected override string GetClassNameCore() => nameof(TimePicker);

	protected override AutomationControlType GetAutomationControlTypeCore()
		=> AutomationControlType.Group;

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

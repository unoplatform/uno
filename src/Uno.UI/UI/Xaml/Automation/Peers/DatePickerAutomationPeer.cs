// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX reference DatePickerAutomationPeer_Partial.cpp, tag winui3/release/1.8.4

using DirectUI;
using Microsoft.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Automation.Peers;

/// <summary>
/// Exposes DatePicker types to Microsoft UI Automation.
/// </summary>
public partial class DatePickerAutomationPeer : FrameworkElementAutomationPeer
{
	public DatePickerAutomationPeer(DatePicker owner) : base(owner)
	{
	}

	protected override string GetClassNameCore() => nameof(DatePicker);

	protected override AutomationControlType GetAutomationControlTypeCore()
		=> AutomationControlType.Group;

	protected override string GetNameCore()
	{
		var baseName = base.GetNameCore();
		if (!string.IsNullOrEmpty(baseName))
		{
			return baseName;
		}

		// WinUI3 uses Header as the accessible name when no Name/LabeledBy is set,
		// falling back to the localized "DatePicker" string for narrator parity.
		if (Owner is DatePicker { Header: { } header })
		{
			var headerText = FrameworkElement.GetStringFromObject(header);
			if (!string.IsNullOrEmpty(headerText))
			{
				return headerText;
			}
		}

		return DXamlCore.GetCurrentNoCreate()?.GetLocalizedResourceString("UIA_AP_DATEPICKER") ?? "DatePicker";
	}
}

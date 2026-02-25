// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX reference AutomationPeerAnnotation_Partial.cpp, tag winui3/release/1.8.4

using System;
using DirectUI;
using Microsoft.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Automation.Peers;

/// <summary>
/// Exposes CalendarDatePicker types to Microsoft UI Automation.
/// </summary>
partial class CalendarDatePickerAutomationPeer : FrameworkElementAutomationPeer
{
	private const string UIA_AP_CALENDARDATEPICKER = nameof(UIA_AP_CALENDARDATEPICKER);

	public CalendarDatePickerAutomationPeer(CalendarDatePicker owner) : base(owner)
	{

	}

	/// <summary>
	/// Gets a value that specifies whether the value of a control is read-only.
	/// </summary>
	public bool IsReadOnly => true;

	/// <summary>
	/// Gets the value of the control.
	/// </summary>
	public string Value
	{
		get
		{
			if (Owner is not CalendarDatePicker ownerItem)
			{
				throw new ArgumentNullException();
			}

			ownerItem.GetCurrentFormattedDate(out var value);

			return value;
		}
	}

	protected override object GetPatternCore(PatternInterface patternInterface)
	{
		if (patternInterface == PatternInterface.Invoke
			|| patternInterface == PatternInterface.Value)
		{
			return this;
		}

		return base.GetPatternCore(patternInterface);
	}

	protected override string GetClassNameCore() => nameof(CalendarDatePicker);

	protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Button;

	protected override string GetLocalizedControlTypeCore()
		=> DXamlCore.GetCurrentNoCreate().GetLocalizedResourceString(UIA_AP_CALENDARDATEPICKER);

	/// <summary>
	/// Sends a request to open the CalendarDatePicker associated with the automation peer.
	/// </summary>
	/// <exception cref="ElementNotEnabledException"></exception>
	public void Invoke()
	{
		bool bIsEnabled;

		bIsEnabled = IsEnabled();
		if (!bIsEnabled)
		{
			//UIA_E_ELEMENTNOTENABLED;
			throw new ElementNotEnabledException();
		}

		var pOwner = Owner;
		((CalendarDatePicker)pOwner).IsCalendarOpen = true;
	}

	/// <summary>
	/// Sets the value of a control, as an implementation of the IValueProvider pattern.
	/// </summary>
	/// <param name="value"></param>
	/// <exception cref="NotImplementedException"></exception>
	public void SetValue(string value) => throw new NotImplementedException();
}

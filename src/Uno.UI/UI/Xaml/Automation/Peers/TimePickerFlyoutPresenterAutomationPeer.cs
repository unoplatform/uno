// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference TimePickerFlyoutPresenterAutomationPeer_Partial.cpp, tag winui3/release/1.8.4
using Uno.UI.Helpers.WinUI;
using Microsoft.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Automation.Peers;

/// <summary>
/// Exposes TimePickerFlyoutPresenter types to Microsoft UI Automation.
/// </summary>
public sealed partial class TimePickerFlyoutPresenterAutomationPeer : FrameworkElementAutomationPeer
{
	private string UIA_AP_TIMEPICKER_NAME = nameof(UIA_AP_TIMEPICKER_NAME);

	internal TimePickerFlyoutPresenterAutomationPeer(TimePickerFlyoutPresenter owner) : base(owner)
	{
	}

	protected override AutomationControlType GetAutomationControlTypeCore()
		=> AutomationControlType.Pane;

	protected override string GetClassNameCore() => nameof(TimePickerFlyoutPresenter);

	protected override string GetNameCore()
		=> ResourceAccessor.GetLocalizedStringResource(UIA_AP_TIMEPICKER_NAME);
}

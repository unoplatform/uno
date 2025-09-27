// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX reference InfoBarAutomationPeer.cpp, tag winui3/release/1.4.2

using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;

namespace Microsoft.UI.Xaml.Automation.Peers;

/// <summary>
/// Exposes InfoBar types to Microsoft UI Automation.
/// </summary>
public partial class InfoBarAutomationPeer : FrameworkElementAutomationPeer
{
	/// <summary>
	/// Initializes a new instance of the InfoBarAutomationPeer class.
	/// </summary>
	/// <param name="owner">The InfoBar control instance to create the peer for.</param>
	public InfoBarAutomationPeer(InfoBar owner) : base(owner)
	{
	}

	protected override AutomationControlType GetAutomationControlTypeCore()
	{
		return AutomationControlType.StatusBar;
	}

	protected override string GetClassNameCore()
	{
		return nameof(InfoBar);
	}

	internal void RaiseOpenedEvent(InfoBarSeverity severity, string displayString)
	{
		this.RaiseNotificationEvent(
			AutomationNotificationKind.Other,
			GetProcessingForSeverity(severity),
			displayString,
			"InfoBarOpenedActivityId");
	}

	internal void RaiseClosedEvent(InfoBarSeverity severity, string displayString)
	{
		//AutomationNotificationProcessing processing = AutomationNotificationProcessing.CurrentThenMostRecent;

		this.RaiseNotificationEvent(
			AutomationNotificationKind.Other,
			GetProcessingForSeverity(severity),
			displayString,
			"InfoBarClosedActivityId");
	}


	private AutomationNotificationProcessing GetProcessingForSeverity(InfoBarSeverity severity)
	{
		AutomationNotificationProcessing processing = AutomationNotificationProcessing.CurrentThenMostRecent;

		if (severity == InfoBarSeverity.Error
			|| severity == InfoBarSeverity.Warning)
		{
			processing = AutomationNotificationProcessing.ImportantAll;
		}

		return processing;
	}
}

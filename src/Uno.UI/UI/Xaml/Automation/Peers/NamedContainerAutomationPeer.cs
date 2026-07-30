// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference NamedContainerAutomationPeer_Partial.cpp

namespace Microsoft.UI.Xaml.Automation.Peers;

/// <summary>
/// Automation peer used for FrameworkElements that aren't themselves a Control but set
/// AutomationProperties.Name or LabeledBy. WinUI auto-promotes such elements to this peer
/// so they appear in the UIA tree as a Group.
/// </summary>
public partial class NamedContainerAutomationPeer : FrameworkElementAutomationPeer
{
	public NamedContainerAutomationPeer(FrameworkElement owner) : base(owner)
	{
	}

	protected override string GetClassNameCore() => nameof(NamedContainerAutomationPeer);

	protected override AutomationControlType GetAutomationControlTypeCore()
		=> AutomationControlType.Group;
}

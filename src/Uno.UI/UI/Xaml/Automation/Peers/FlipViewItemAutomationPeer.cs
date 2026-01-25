// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX reference FlipViewItemAutomationPeer_Partial.cpp, tag winui3/release/1.8.4

using Microsoft.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Automation.Peers;

/// <summary>
/// Exposes a FlipViewItem to Microsoft UI Automation.
/// </summary>
public partial class FlipViewItemAutomationPeer : FrameworkElementAutomationPeer
{
	public FlipViewItemAutomationPeer(FlipViewItem owner) : base(owner)
	{
	}

	protected override string GetClassNameCore() => nameof(FlipViewItem);

	protected override AutomationControlType GetAutomationControlTypeCore()
		=> AutomationControlType.ListItem;
}

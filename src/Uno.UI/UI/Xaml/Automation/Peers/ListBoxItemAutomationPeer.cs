// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference ListBoxItemAutomationPeer_Partial.cpp, tag winui3/release/1.8.4
namespace Microsoft.UI.Xaml.Automation.Peers;

/// <summary>
/// Exposes the items in the Items collection of a ListBox to Microsoft UI Automation.
/// </summary>
public partial class ListBoxItemAutomationPeer : FrameworkElementAutomationPeer
{
	public ListBoxItemAutomationPeer(Controls.ListBoxItem owner) : base(owner)
	{

	}

	protected override string GetClassNameCore() => nameof(Controls.ListBoxItem);

	protected override AutomationControlType GetAutomationControlTypeCore()
		=> AutomationControlType.ListItem;
}

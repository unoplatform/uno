// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference MenuBarAutomationPeer.cpp, tag winui3/release/1.8.4

#nullable enable

using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Automation.Peers;

/// <summary>
/// Exposes MenuBar types to Microsoft UI Automation.
/// </summary>
public partial class MenuBarAutomationPeer : FrameworkElementAutomationPeer
{
	public MenuBarAutomationPeer(MenuBar owner) : base(owner)
	{
	}

	protected override string GetClassNameCore()
		=> nameof(MenuBar);

	protected override AutomationControlType GetAutomationControlTypeCore()
		=> AutomationControlType.MenuBar;
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference MenuBarItemAutomationPeer.cpp, tag winui3/release/1.8.4

#nullable enable

using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Automation.Peers;

/// <summary>
/// Exposes MenuBarItem types to Microsoft UI Automation.
/// </summary>
public partial class MenuBarItemAutomationPeer : FrameworkElementAutomationPeer, IExpandCollapseProvider, IInvokeProvider
{
	public MenuBarItemAutomationPeer(MenuBarItem owner) : base(owner)
	{
	}

	protected override AutomationControlType GetAutomationControlTypeCore()
		=> AutomationControlType.MenuItem;

	protected override object? GetPatternCore(PatternInterface patternInterface)
	{
		if (patternInterface == PatternInterface.Invoke || patternInterface == PatternInterface.ExpandCollapse)
		{
			return this;
		}

		return base.GetPatternCore(patternInterface);
	}

	protected override string GetNameCore()
	{
		// Check to see if the item has a defined AutomationProperties.Name
		var name = base.GetNameCore();

		if (string.IsNullOrEmpty(name))
		{
			var owner = Owner as MenuBarItem;
			name = owner?.Title ?? string.Empty;
		}

		return name;
	}

	protected override string GetClassNameCore()
		=> nameof(MenuBarItem);

	// IInvokeProvider
	public void Invoke()
	{
		var owner = Owner as MenuBarItem;
		owner?.Invoke();
	}

	// IExpandCollapseProvider
	public ExpandCollapseState ExpandCollapseState
	{
		get
		{
			var owner = Owner as MenuBarItem;
			var menuBarItem = owner;
			if (menuBarItem?.IsFlyoutOpen() == true)
			{
				return ExpandCollapseState.Expanded;
			}
			else
			{
				return ExpandCollapseState.Collapsed;
			}
		}
	}

	public void Collapse()
	{
		var owner = Owner as MenuBarItem;
		owner?.CloseMenuFlyout();
	}

	public void Expand()
	{
		var owner = Owner as MenuBarItem;
		owner?.ShowMenuFlyout();
	}
}

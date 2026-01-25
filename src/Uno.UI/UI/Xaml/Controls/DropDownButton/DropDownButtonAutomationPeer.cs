// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference DropDownButtonAutomationPeer.cpp, tag winui3/release/1.8.4

#nullable enable

using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Automation.Peers;

/// <summary>
/// Exposes DropDownButton types to Microsoft UI Automation.
/// </summary>
public partial class DropDownButtonAutomationPeer : ButtonAutomationPeer, IExpandCollapseProvider
{
	public DropDownButtonAutomationPeer(DropDownButton owner) : base(owner)
	{
	}

	// IAutomationPeerOverrides
	protected override object? GetPatternCore(PatternInterface patternInterface)
	{
		if (patternInterface == PatternInterface.ExpandCollapse)
		{
			return this;
		}

		return base.GetPatternCore(patternInterface);
	}

	protected override string GetClassNameCore()
		=> nameof(DropDownButton);

	private DropDownButton? GetImpl() => Owner as DropDownButton;

	// IExpandCollapseProvider
	public ExpandCollapseState ExpandCollapseState
	{
		get
		{
			var currentState = ExpandCollapseState.Collapsed;

			if (GetImpl() is { } dropDownButton)
			{
				if (dropDownButton.IsFlyoutOpen())
				{
					currentState = ExpandCollapseState.Expanded;
				}
			}

			return currentState;
		}
	}

	public void Expand()
	{
		GetImpl()?.OpenFlyout();
	}

	public void Collapse()
	{
		GetImpl()?.CloseFlyout();
	}
}

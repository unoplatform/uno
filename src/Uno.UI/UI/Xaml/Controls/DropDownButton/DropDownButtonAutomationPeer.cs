// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX reference DropDownButtonAutomationPeer.cpp, tag winui3/release/1.4.2

using Windows.UI.Xaml.Automation;
using Windows.UI.Xaml.Automation.Peers;
using Windows.UI.Xaml.Automation.Provider;

namespace Windows.UI.Xaml.Controls;

public partial class DropDownButtonAutomationPeer : ButtonAutomationPeer, IExpandCollapseProvider
{
	public DropDownButtonAutomationPeer(DropDownButton owner) : base(owner)
	{
	}

	protected override object GetPatternCore(PatternInterface patternInterface)
	{
		if (patternInterface == PatternInterface.ExpandCollapse)
		{
			return this;
		}
		return base.GetPatternCore(patternInterface);
	}

	protected override string GetClassNameCore()
	{
		return nameof(DropDownButton);
	}

	private DropDownButton GetImpl()
	{
		return Owner as DropDownButton;
	}

	public ExpandCollapseState ExpandCollapseState
	{
		get
		{
			var dropDownButton = GetImpl();
			return dropDownButton?.IsFlyoutOpen() == true ?
				ExpandCollapseState.Expanded :
				ExpandCollapseState.Collapsed;
		}
	}

	public void Expand()
	{
		if (GetImpl() is DropDownButton dropDownButton)
		{
			dropDownButton.OpenFlyout();
		}
	}

	public void Collapse()
	{
		if (GetImpl() is DropDownButton dropDownButton)
		{
			dropDownButton.CloseFlyout();
		}
	}
}

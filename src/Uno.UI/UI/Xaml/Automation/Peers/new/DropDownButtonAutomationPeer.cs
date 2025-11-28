using System;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Automation.Peers;

public partial class DropDownButtonAutomationPeer : ButtonAutomationPeer, IExpandCollapseProvider
{
	public DropDownButtonAutomationPeer(DropDownButton owner) : base(owner)
	{
		ArgumentNullException.ThrowIfNull(owner);
	}

	protected override object GetPatternCore(PatternInterface patternInterface)
		=> patternInterface == PatternInterface.ExpandCollapse ? this : base.GetPatternCore(patternInterface);

	protected override string GetClassNameCore()
		=> nameof(DropDownButton);

	private DropDownButton? DropDownButtonOwner => Owner as DropDownButton;

	public ExpandCollapseState ExpandCollapseState
		=> DropDownButtonOwner?.IsFlyoutOpen() == true
			? ExpandCollapseState.Expanded
			: ExpandCollapseState.Collapsed;

	public void Expand()
	{
		DropDownButtonOwner?.OpenFlyout();
	}

	public void Collapse()
	{
		DropDownButtonOwner?.CloseFlyout();
	}
}

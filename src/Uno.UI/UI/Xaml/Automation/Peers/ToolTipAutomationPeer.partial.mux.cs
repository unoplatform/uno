// MUX Reference dxaml\xcp\dxaml\lib\ToolTipAutomationPeer_Partial.cpp, tag 5f9e85113

#nullable enable

using Microsoft.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Automation.Peers;

public partial class ToolTipAutomationPeer
{
	// Initializes a new instance of the ToolTipAutomationPeer class.
	public ToolTipAutomationPeer(ToolTip owner) : base(owner)
	{
	}

	// Deconstructor — C# uses GC; the C++ destructor is a no-op.

	protected override string GetClassNameCore() => "ToolTip";

	protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.ToolTip;
}
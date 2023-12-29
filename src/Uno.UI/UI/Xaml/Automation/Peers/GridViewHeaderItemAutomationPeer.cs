using Microsoft.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Automation.Peers;

/// <summary>
/// Exposes GridViewHeaderItem types to Microsoft UI Automation.
/// </summary>
public partial class GridViewHeaderItemAutomationPeer : ListViewBaseHeaderItemAutomationPeer
{
	public GridViewHeaderItemAutomationPeer(GridViewHeaderItem owner) : base(owner)
	{

	}

	protected override string GetClassNameCore() => nameof(GridViewHeaderItem);
}

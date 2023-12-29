using Microsoft.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Automation.Peers;

/// <summary>
/// Exposes the data items in the collection of Items in GridView types to UI Automation.
/// </summary>
public partial class GridViewItemAutomationPeer : FrameworkElementAutomationPeer
{
	public GridViewItemAutomationPeer(GridViewItem owner) : base(owner)
	{

	}

	protected override string GetClassNameCore() => nameof(GridViewItem);
}

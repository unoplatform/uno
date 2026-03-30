// MUX Reference GridViewItem_Partial.cpp, tag winui3/release/1.8.4

using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace Microsoft.UI.Xaml.Controls;

partial class GridViewItem
{
	/// <summary>
	/// Creates a GridViewItemAutomationPeer to represent the GridViewItem.
	/// </summary>
	/// <returns>A new GridViewItemAutomationPeer for this GridViewItem.</returns>
	protected override AutomationPeer OnCreateAutomationPeer()
	{
		return new GridViewItemAutomationPeer(this);
	}

	/// <summary>
	/// Sets the value to display as the dragged item count.
	/// </summary>
	/// <param name="dragItemsCount">The number of items being dragged.</param>
	internal override void SetDragItemsCountDisplay(uint dragItemsCount)
	{
		base.SetDragItemsCountDisplay(dragItemsCount);
		TemplateSettings.DragItemsCount = (int)dragItemsCount;
	}
}

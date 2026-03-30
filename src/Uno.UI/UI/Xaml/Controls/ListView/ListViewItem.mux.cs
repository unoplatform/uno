// MUX Reference ListViewItem_Partial.cpp, tag winui3/release/1.8.4

using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace Microsoft.UI.Xaml.Controls;

partial class ListViewItem
{
	/// <summary>
	/// Creates a ListViewItemAutomationPeer to represent the ListViewItem.
	/// </summary>
	/// <returns>A new ListViewItemAutomationPeer for this ListViewItem.</returns>
	protected override AutomationPeer OnCreateAutomationPeer()
	{
		return new ListViewItemAutomationPeer(this);
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

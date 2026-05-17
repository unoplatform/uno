// MUX Reference GridViewItemDataAutomationPeer_Partial.cpp, tag winui3/release/1.8.4

using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Automation.Peers;

/// <summary>
/// Exposes GridViewItem data items to Microsoft UI Automation.
/// </summary>
public partial class GridViewItemDataAutomationPeer : SelectorItemAutomationPeer, IScrollItemProvider
{
	public GridViewItemDataAutomationPeer(object item, GridViewAutomationPeer parent) : base(item, parent)
	{
	}

	protected override string GetClassNameCore() => "GridViewItem";

	/// <summary>
	/// Scrolls the content area of a container object in order to display
	/// the control within the visible region (viewport) of the container.
	/// </summary>
	public new void ScrollIntoView()
	{
		var itemsControlPeer = ItemsControlAutomationPeer;
		if (itemsControlPeer is null)
		{
			return;
		}

		var gridView = itemsControlPeer.Owner as ListViewBase;
		if (gridView is null)
		{
			return;
		}

		// Try to get the container for this item
		var container = GetContainer() as UIElement;
		if (container is not null)
		{
			container.StartBringIntoView(new BringIntoViewOptions
			{
				AnimationDesired = false,
			});
		}
	}

	private new UIElement GetContainer()
	{
		var itemsControlPeer = ItemsControlAutomationPeer;
		if (itemsControlPeer?.Owner is ItemsControl itemsControl)
		{
			return itemsControl.ContainerFromItem(Item) as UIElement;
		}

		return null;
	}
}

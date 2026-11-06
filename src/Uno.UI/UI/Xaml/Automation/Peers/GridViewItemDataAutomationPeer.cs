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

		// If the ListView's ScrollViewer can scroll, a scroll into view is sufficient to bring it into view. If
		// it is not (nested scroll viewer case), then a bring into view which bubbles up the tree
		// is required to bring the item into view.
		if (gridView.IsScrollable())
		{
			base.ScrollIntoView();
			return;
		}

		// bring into view
		var container = GetContainer() as UIElement;
		if (container is not null)
		{
			var size = container.RenderSize;
			container.StartBringIntoView(new BringIntoViewOptions
			{
				AnimationDesired = false,
				TargetRect = new global::Windows.Foundation.Rect(0, 0, size.Width, size.Height),
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

// MUX Reference ListViewItemDataAutomationPeer_Partial.cpp, tag winui3/release/1.8.4

using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Automation.Peers;

/// <summary>
/// Exposes ListViewItem data items to Microsoft UI Automation.
/// </summary>
public partial class ListViewItemDataAutomationPeer : SelectorItemAutomationPeer, IScrollItemProvider
{
	public ListViewItemDataAutomationPeer(object item, ListViewBaseAutomationPeer parent) : base(item, parent)
	{
	}

	protected override string GetClassNameCore() => "ListViewItem";

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

		var listView = itemsControlPeer.Owner as ListViewBase;
		if (listView is null)
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

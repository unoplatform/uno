// MUX Reference ListViewBaseItemAutomationPeer_Partial.cpp, tag winui3/release/1.8.4

using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace Microsoft.UI.Xaml.Automation.Peers;

/// <summary>
/// Base automation peer for ListViewItem and GridViewItem controls in a
/// ListView or GridView. Provides Invoke and Drag pattern support for
/// list view base items.
/// </summary>
internal partial class ListViewBaseItemAutomationPeer : FrameworkElementAutomationPeer
{
	public ListViewBaseItemAutomationPeer(FrameworkElement owner) : base(owner)
	{
	}

	protected override AutomationControlType GetAutomationControlTypeCore()
		=> AutomationControlType.ListItem;

	protected override object GetPatternCore(PatternInterface patternInterface)
	{
		if (patternInterface == PatternInterface.Invoke)
		{
			if (ShouldSupportInvokePattern(Owner))
			{
				// Create adapter to perfom Invoke action, if needed
				return _invokeAdapter ??= new ItemInvokeAdapter(this);
			}
		}

		// TODO Uno: this class is not the base of the public peers. ListViewItemAutomationPeer and
		// GridViewItemAutomationPeer implement IDragProvider with the helpers below.
		return base.GetPatternCore(patternInterface);
	}

	// IDragProvider -----------------------------------------------------------------------------------------------------------------
	internal static bool ShouldSupportDragPattern(UIElement owner)
		=> (owner as ListViewBaseItem)?.GetParentListView() is { CanDragItems: true };

	internal static bool GetIsGrabbed(UIElement owner)
		=> owner is ListViewBaseItem item && item.GetDragIsGrabbed();

	// In the future the effect text should be updated based on what is possible to do with this item.
	internal static string GetDropEffect()
		=> DirectUI.DXamlCore.GetCurrentNoCreate().GetLocalizedResourceString("UIA_DRAG_MOVE");

	// In the future the list of effect should be updated based on what is possible to do with this item.
	internal static string[] GetDropEffects()
		=> new[] { GetDropEffect() };

	internal static Provider.IRawElementProviderSimple[] GetGrabbedItems(UIElement owner)
		=> (owner as ListViewBaseItem)?.GetParentListView()?.GetDragGrabbedItems() ?? global::System.Array.Empty<Provider.IRawElementProviderSimple>();

	// Shared with ListViewItemAutomationPeer and GridViewItemAutomationPeer, whose public base can't be this internal class.
	internal static bool ShouldSupportInvokePattern(UIElement owner)
	{
		// TODO Uno: WinUI also supports Invoke in a SemanticZoom's zoomed-out view, to switch to the zoomed-in view.
		return owner is SelectorItem listViewBaseItem
			&& ItemsControl.ItemsControlFromItemContainer(listViewBaseItem) is ListViewBase { IsItemClickEnabled: true };
	}
}

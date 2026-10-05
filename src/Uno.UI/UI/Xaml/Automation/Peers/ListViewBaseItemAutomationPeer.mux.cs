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

		// TODO Uno: IDragProvider support is not yet implemented.
		// Original C++ also supports PatternInterface_Drag when CanDragItems is true
		// on the parent ListViewBase.

		return base.GetPatternCore(patternInterface);
	}

	// Shared with ListViewItemAutomationPeer and GridViewItemAutomationPeer, whose public base can't be this internal class.
	internal static bool ShouldSupportInvokePattern(UIElement owner)
	{
		// TODO Uno: WinUI also supports Invoke in a SemanticZoom's zoomed-out view, to switch to the zoomed-in view.
		return owner is SelectorItem listViewBaseItem
			&& ItemsControl.ItemsControlFromItemContainer(listViewBaseItem) is ListViewBase { IsItemClickEnabled: true };
	}
}

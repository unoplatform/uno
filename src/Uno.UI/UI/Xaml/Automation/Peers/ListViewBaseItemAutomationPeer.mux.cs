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
			if (Owner is SelectorItem selectorItem)
			{
				var listViewBase = ItemsControl.ItemsControlFromItemContainer(selectorItem) as ListViewBase;
				if (listViewBase is not null && listViewBase.IsItemClickEnabled)
				{
					return this;
				}
			}
		}

		// TODO Uno: IDragProvider support is not yet implemented.
		// Original C++ also supports PatternInterface_Drag when CanDragItems is true
		// on the parent ListViewBase.

		return base.GetPatternCore(patternInterface);
	}
}

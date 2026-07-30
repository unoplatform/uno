// MUX Reference PivotItemDataAutomationPeer_Partial.cpp, tag winui3/release/1.8.4

using System;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Automation.Peers;

/// <summary>
/// Exposes PivotItem data to Microsoft UI Automation, providing
/// selection, scroll-into-view, and virtualization support.
/// </summary>
public partial class PivotItemDataAutomationPeer : ItemAutomationPeer,
	ISelectionItemProvider, IScrollItemProvider, IVirtualizedItemProvider
{
	public PivotItemDataAutomationPeer(object item, PivotAutomationPeer parent) : base(item, parent)
	{
	}

	protected override object GetPatternCore(PatternInterface patternInterface)
	{
		if (patternInterface == PatternInterface.SelectionItem ||
			patternInterface == PatternInterface.ScrollItem)
		{
			return this;
		}

		return base.GetPatternCore(patternInterface);
	}

	protected override Rect GetBoundingRectangleCore()
	{
		if (IsOffscreenCore())
		{
			return default;
		}

		return base.GetBoundingRectangleCore();
	}

	#region ISelectionItemProvider

	/// <summary>
	/// Gets whether this pivot item is currently selected.
	/// </summary>
	public bool IsSelected
	{
		get
		{
			var parentPeer = ItemsControlAutomationPeer;
			if (parentPeer?.Owner is Pivot pivot)
			{
				var selectedItem = pivot.SelectedItem;
				return ReferenceEquals(Item, selectedItem) || Equals(Item, selectedItem);
			}

			return false;
		}
	}

	/// <summary>
	/// Gets the selection container provider.
	/// </summary>
	public IRawElementProviderSimple SelectionContainer
	{
		get
		{
			var parentPeer = ItemsControlAutomationPeer;
			if (parentPeer is not null)
			{
				return ProviderFromPeer(parentPeer);
			}

			return null;
		}
	}

	/// <summary>
	/// Pivot only allows single selection; adding to a multi-selection is not supported.
	/// </summary>
	public void AddToSelection()
		=> throw new InvalidOperationException("Pivot does not support multi-selection.");

	/// <summary>
	/// Pivot does not support removing from selection.
	/// </summary>
	public void RemoveFromSelection()
		=> throw new InvalidOperationException("Pivot does not support removing from selection.");

	/// <summary>
	/// Selects this pivot item.
	/// </summary>
	public void Select()
	{
		var parentPeer = ItemsControlAutomationPeer;
		if (parentPeer?.Owner is Pivot pivot)
		{
			if (pivot.IsLocked)
			{
				var selectedItem = pivot.SelectedItem;
				if (!ReferenceEquals(Item, selectedItem) && !Equals(Item, selectedItem))
				{
					throw new InvalidOperationException("Cannot select a different item when Pivot is locked.");
				}
			}

			pivot.SelectedItem = Item;
		}
	}

	#endregion

	#region IScrollItemProvider

	/// <summary>
	/// Scrolls to this pivot item by selecting it.
	/// </summary>
	public void ScrollIntoView() => Select();

	#endregion

	#region IVirtualizedItemProvider

	/// <summary>
	/// Realizes this pivot item by selecting it.
	/// </summary>
	public new void Realize() => Select();

	#endregion
}

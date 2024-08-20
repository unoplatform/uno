using System.Linq;
using Microsoft.UI.Xaml.Input;
using Uno.UI.Extensions;
using Uno.UI.Xaml.Input;
using Windows.System;

namespace Microsoft.UI.Xaml.Controls.Primitives;

partial class Selector
{
	// Selects the next item in the list.
	private protected void SelectNext(ref int index)
	{
		var count = Items.Count;
		if (count > 0)
		{
			int internalSelectedIndex = index + 1;
			if (internalSelectedIndex <= count - 1)
			{
				SelectItemHelper(ref internalSelectedIndex, 1);
				if (internalSelectedIndex != -1)
				{
					index = internalSelectedIndex;
				}
			}
		}
	}

	// Selects the previous item in the list.
	private protected void SelectPrev(ref int index)
	{
		var count = Items.Count;
		if (count > 0)
		{
			int internalSelectedIndex = index - 1;
			if (internalSelectedIndex >= 0)
			{
				SelectItemHelper(ref internalSelectedIndex, -1);
				if (internalSelectedIndex != -1)
				{
					index = internalSelectedIndex;
				}
			}
		}
	}

	// Given a direction, searches through list for next available item to select.
	private void SelectItemHelper(ref int index, int increment)
	{
		var items = Items;
		var count = items.Count;
		bool isSelectable = false;

		for (; index > -1 && index < count; index += increment)
		{
			var item = items[index];
			isSelectable = IsSelectableHelper(item);
			if (isSelectable)
			{
				var container = ContainerFromIndex(index);
				isSelectable = IsSelectableHelper(container);
				if (isSelectable)
				{
					break;
				}
			}
		}

		if (!isSelectable)
		{
			// If no selectable item was found, set index to -1 so selection will not be updated.
			index = -1;
		}
	}

	internal void SetAllowCustomValues(bool allow)
	{
		m_customValuesAllowed = allow;
	}

	internal void HandleNavigationKey(
		VirtualKey key,
		bool scrollViewport,
		ref int newFocusedIndex)
	{
		// TODO Uno: Not implemented yet.
	}

	private void OnSelectionChanged(
		int oldSelectedIndex,
		int newSelectedIndex,
		object pOldSelectedItem,
		object pNewSelectedItem,
		bool animateIfBringIntoView = false,
		FocusNavigationDirection focusNavigationDirection = FocusNavigationDirection.None)
	{
		if (newSelectedIndex != -1)
		{
			// Only change the focus if there is a selected item.
			// Use InputActivationBehavior.NoActivate because just changing selected item by default shouldn't steal activation from another window/island.
			SetFocusedItem(newSelectedIndex, true /*shouldScrollIntoView*/, animateIfBringIntoView, focusNavigationDirection, InputActivationBehavior.NoActivate);
		}
	}
}

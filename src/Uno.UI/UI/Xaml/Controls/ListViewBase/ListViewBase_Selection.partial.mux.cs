// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// MUX Reference ListViewBase_Partial_Selection.cpp, tag winui3/release/2.5.1, commit ba3a8d59e6d


#nullable enable

using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Windows.Foundation.Collections;

namespace Microsoft.UI.Xaml.Controls;

partial class ListViewBase
{
	// Gets a collection containing the currently selected items in the ListView.
	public IList<object> SelectedItems
	{
		get
		{
			// Use the Selector::get_SelectedItemsInternal implementation
			var pValue = (IList<object>)SelectedItemsInternal!;

			// TODO: We'll probably need to optimize this behavior for users that call
			// SelectAll on a large, virtualized collection.  The current plan is that
			// we'll provide an implementation of IVector that forwards along to an
			// ItemCollection, but copies over all the indices as soon as the original
			// collection grows or replaces an item.

			return pValue;
		}
	}

	// Gets a collection containing the currently selected ranges in the ListView.
	public IReadOnlyList<ItemIndexRange> SelectedRanges
	{
		get
		{
			// Use the Selector::get_SelectedRangesInternal implementation
			return SelectedRangesInternal;
		}
	}

	// Handles changes to the SelectionMode property by updating or clearing
	// selection.
	private void OnSelectionModeChangedImpl(
		ListViewSelectionMode oldMode,
		ListViewSelectionMode newMode)
	{
		var selectedIndex = -1;

		m_isInOnSelectionModeChanged = true;

		try
		{
			switch (newMode)
			{
				case ListViewSelectionMode.None:
				{
					// Clear any selection when we're switching to None
					selectedIndex = SelectedIndex;
					if (selectedIndex >= 0)
					{
						ClearSelection();
					}
					break;
				}
				case ListViewSelectionMode.Single:
				{
					// Restrict selection to a single item when we're changing to Single
					selectedIndex = SelectedIndex;
					if (selectedIndex >= 0)
					{
						MakeSingleSelection((uint)selectedIndex, false /*animateIfBringIntoView*/, null);
					}
					break;
				}
			}

			// Update the CollectionView synchronization state now that the
			// selection mode has changed
			UpdateCVSynchronizationState();
		}
		finally
		{
			m_isInOnSelectionModeChanged = false;
		}
	}

	// Gets a value indicating whether the ListView can select multiple items at
	// once.
	internal override bool CanSelectMultiple
	{
		get
		{
			// We can select multiple items as long as we're not in either Single or
			// None selection modes.
			var mode = SelectionMode;
			var pValue =
				mode != ListViewSelectionMode.Single &&
				mode != ListViewSelectionMode.None;

			// TODO: We should look to see if there are any customer scenarios for this
			// method and possibly add another version that would factor in a primary
			// input device (i.e., we can't always multi-select with touch input if
			// we've disabled Swipe and have an ItemClick event.

			return pValue;
		}
	}

	// Called after the Selector has changed the selected item.  We determine
	// whether or not we want to call the base implementation which will focus the
	// newSelectedIndex for us.
	private protected override void OnSelectionChanged(
		int oldSelectedIndex,
		int newSelectedIndex,
		object? pOldSelectedItem,
		object? pNewSelectedItem,
		bool animateIfBringIntoView = false,
		FocusNavigationDirection focusNavigationDirection = FocusNavigationDirection.None)
	{
		// Don't focus any new item during a reorder.
		if (!m_isInItemReorderAfterDrop)
		{
			// Prevent selection from changing when SelectionMode is None
			var mode = SelectionMode;
			if (mode != ListViewSelectionMode.None)
			{
				var movedExistingSelection = (newSelectedIndex != -1) && (oldSelectedIndex == -1);
				var selectionChanged = oldSelectedIndex != newSelectedIndex;

				// Only call base to focus the item if we actually moved
				// the selection or if there was a change when SelectionMode is Single
				if (movedExistingSelection ||
					(selectionChanged && (mode == ListViewSelectionMode.Single)))
				{
					// Focus the item.
					base.OnSelectionChanged(
						oldSelectedIndex,
						newSelectedIndex,
						pOldSelectedItem,
						pNewSelectedItem,
						animateIfBringIntoView,
						focusNavigationDirection);
				}
			}
		}
	}

	// Handles changes to the Items collection.  We should adjust m_pAnchorIndex if
	// any item before that index was added or removed. Additionally, we should
	// cancel a drag/drop if the item we're dragging is removed, and we should cancel
	// any swipe in progress.
	protected override void OnItemsChanged(object e)
	{
		var pArgs = (IVectorChangedEventArgs)e;
		var action = CollectionChange.Reset;
		uint index = 0;
		var shouldConsiderDragItemIndexForCancelDrag = false;
		var allowCancelDrag = !m_isInItemReorderAfterDrop;  // Default to always cancel drag, unless we're currently in the middle
		                                                    // of reordering items after a drop.

		base.OnItemsChanged(pArgs);

		action = pArgs.CollectionChange;
		switch (action)
		{
			case CollectionChange.Reset:
			{
				// Clear the anchor index if the entire collection was cleared
				ReleaseAnchorIndex();
				break;
			}
			case CollectionChange.ItemInserted:
			{
				// If an item was inserted before the anchored item, move the anchor
				// index forward an element
				if (m_pAnchorIndex.HasValue)
				{
					index = pArgs.Index;

					if (index <= m_pAnchorIndex.Value)
					{
						m_pAnchorIndex++;
					}
				}

				// Insertion is not a problem for an ongoing drag - we'll never
				// lose our currently dragged item.
				allowCancelDrag = false;
				break;
			}
			case CollectionChange.ItemRemoved:
			{
				index = pArgs.Index;

				if (m_pAnchorIndex.HasValue)
				{
					if (index == m_pAnchorIndex.Value)
					{
						// If the anchored item was removed, clear the index
						ReleaseAnchorIndex();
					}
					else if (index < m_pAnchorIndex.Value)
					{
						// If the removed item was before the anchored item, move the
						// anchor index back an element
						m_pAnchorIndex--;
					}
				}

				shouldConsiderDragItemIndexForCancelDrag = true;
				break;
			}
			case CollectionChange.ItemChanged:
			{
				index = pArgs.Index;

				if (m_pAnchorIndex.HasValue)
				{
					// If the anchored item was swapped out, clear the anchor index
					if (index == m_pAnchorIndex.Value)
					{
						ReleaseAnchorIndex();
					}
				}

				shouldConsiderDragItemIndexForCancelDrag = true;
				break;
			}
		}

		if (allowCancelDrag)
		{
			// Possibly continue the drag if the removed/changed index is not the primary dragged item.
			if (shouldConsiderDragItemIndexForCancelDrag && m_tpPrimaryDraggedContainer is not null)
			{
				var draggedItemIndex = IndexFromContainer(m_tpPrimaryDraggedContainer);

				// Cancel the drag if the dragged item was removed/changed, or if the dragged item is no longer
				// recognized by the ICG
				allowCancelDrag = (draggedItemIndex == -1) || (draggedItemIndex == index);
			}

			if (allowCancelDrag)
			{
				CancelDrag();
				global::System.Diagnostics.Debug.Assert(!IsInDragDrop());
			}
		}

		// Recalculate the reorder hints, etc if there is (still) a drag drop in progress.
		if (IsInDragDrop() && !m_isInItemReorderAfterDrop)
		{
			ResetAllItemsForLiveReorder();
			ProcessDragOverAt(m_lastDragOverPoint);
		}

		// Note: We're just returning an HRESULT as an HRESULT here.  It would be
		// best to change the return type on the original method.
	}

	// Handles selection of single item, and only that item
	internal override void MakeSingleSelection(
		uint index,
		bool animateIfBringIntoView,
		object? pSelectedItem,
		FocusNavigationDirection focusNavigationDirection = FocusNavigationDirection.None)
	{
		var mode = SelectionMode;
		if (mode == ListViewSelectionMode.Extended ||
			mode == ListViewSelectionMode.Multiple)
		{
			SetAnchorIndex(index);
		}

		base.MakeSingleSelection(index, animateIfBringIntoView, pSelectedItem, focusNavigationDirection);
	}

	// Handles selection of single item, and only that item
	private void MakeSingleSelection(
		ListViewBaseItem pItem,
		FocusNavigationDirection focusNavigationDirection = FocusNavigationDirection.None)
	{
		var index = IndexFromContainer(pItem);
		// in the ItemClick event (which precedes this code) we have found multiple partners
		// to actually clear their collection. After talking to them, the feature team is convinced
		// that that code should be able to be written and thus we have decided to ignore the selection in this case.

		// we can only select an item if we can find it in the collection
		if (index >= 0)
		{
			var spSelectedItem = ItemFromContainer(pItem);
			MakeSingleSelection((uint)index, false /*animateIfBringIntoView*/, spSelectedItem, focusNavigationDirection);
		}
	}

	// Toggle IsSelected of item
	private void MakeToggleSelection(ListViewBaseItem pItem)
	{
		var mode = SelectionMode;
		if (mode == ListViewSelectionMode.Extended ||
			mode == ListViewSelectionMode.Multiple)
		{
			var index = IndexFromContainer(pItem);


			// in the ItemClick event (which precedes this code) we have found multiple partners
			// to actually clear their collection. After talking to them, the feature team is convinced
			// that that code should be able to be written and thus we have decided to ignore the selection in this case.

			// we can only select an item if we can find it in the collection
			if (index >= 0)
			{
				SetAnchorIndex((uint)index);
			}
		}

		var isSelected = pItem.IsSelected;
		pItem.IsSelected = !isSelected;
	}

	// Select a range of items beginning with current anchor
	private void MakeRangeSelection(
		uint index,
		bool clearOldSelection)
	{
		uint firstIndex = 0;
		var lastIndex = index;
		uint length = 0;

		var mode = SelectionMode;

		global::System.Diagnostics.Debug.Assert(
			mode != ListViewSelectionMode.Single &&
			mode != ListViewSelectionMode.None,
			"Invalid selection mode in range selection");

		// if the anchor index exists then use its value
		// otherwise, we check if a SelectedIndex is set
		// SelectedIndex can be either set using the property itself directly
		// or by using SelectRange... in both cases the value is set programmatically
		// and not by user interaction
		if (m_pAnchorIndex.HasValue)
		{
			firstIndex = m_pAnchorIndex.Value;
		}
		else
		{
			var selectedIndex = SelectedIndex;

			if (selectedIndex == -1)
			{
				firstIndex = 0;
			}
			else
			{
				firstIndex = (uint)selectedIndex;
				SetAnchorIndex(firstIndex);
			}
		}

		if (firstIndex > index)
		{
			lastIndex = firstIndex;
			firstIndex = index;
		}

		// compute the length of the range for selection/deselection
		length = lastIndex - firstIndex + 1;

		// In threshold, Shift+Click with Multiple selection mode invokes a selection that depends on the anchor item
		if (mode == ListViewSelectionMode.Multiple)
		{
			// MakeRangeSelection will only be called in the case where the Selection mode is not None or Single
			// and when there is at least one item, hence it is safe to set the anchor index to 0 in our fallback case
			var isAnchorSelected = IsIndexSelected((int)(m_pAnchorIndex ?? 0));

			if (isAnchorSelected)
			{
				// call the Selector::SelectRangeInternal
				SelectRangeInternal((int)firstIndex /*firstIndex*/, length /*length*/, clearOldSelection);
			}
			else
			{
				// call the Selector::DeselectRangeInternal
				DeselectRangeInternal((int)firstIndex /*firstIndex*/, length /*length*/);
			}
		}
		else
		{
			// call the Selector::SelectRangeInternal
			SelectRangeInternal((int)firstIndex /*firstIndex*/, length /*length*/, clearOldSelection);
		}
	}

	// Select a range of items beginning with current anchor
	private void MakeRangeSelection(
		ListViewBaseItem pItem,
		bool clearOldSelection)
	{
		var index = IndexFromContainer(pItem);

		if (index < 0)
		{
			throw new InvalidOperationException();
		}

		MakeRangeSelection((uint)index, clearOldSelection);
	}

	// Selects all the items.
	public void SelectAll()
	{
		// call the Selector::SelectAllInternal
		SelectAllInternal();
	}

	// Selects a range of items in the ListView.
	public void SelectRange(ItemIndexRange itemIndexRange)
	{
		var rangeFirstIndex = itemIndexRange.FirstIndex;
		var rangeLength = itemIndexRange.Length;

		// call the Selector::SelectRangeInternal
		SelectRangeInternal(rangeFirstIndex /* startIndex */, rangeLength /* length */, false /* clearOldSelection */);
	}

	// Deselects a range of items in the ListView.
	public void DeselectRange(ItemIndexRange itemIndexRange)
	{
		var rangeFirstIndex = itemIndexRange.FirstIndex;
		var rangeLength = itemIndexRange.Length;

		// call the Selector::DeselectRangeInternal
		DeselectRangeInternal(rangeFirstIndex /* firstIndex */, rangeLength /* length */);
	}

	// Deselect all the items.
	internal void DeselectAll()
	{
		// call the Selector::ClearSelection
		ClearSelection();
	}

	private void InitializeDataSourceSelectionInfo()
	{
		// TODO Uno: DataSourceAsSelectionInfo also resets the previous source's selection info when ItemsSource changes; the C++ form below does not.
#if false
		var spItemsSourceAsIInspectable = ItemsSource;
		if (spItemsSourceAsIInspectable is not null)
		{
			var spItemsSourceAsSI = spItemsSourceAsIInspectable as ISelectionInfo;
			if (spItemsSourceAsSI is not null)
			{
				// call the Selector::SetDataSourceAsSelectionInfo
				SetDataSourceAsSelectionInfo(spItemsSourceAsSI);
			}
		}
#endif
		DataSourceAsSelectionInfo = ItemsSource as ISelectionInfo;
	}

	internal override bool IsSelectionPatternApplicable()
	{
		var selectionMode = SelectionMode;
		var selectionPatternApplicable = (selectionMode != ListViewSelectionMode.None);

		return selectionPatternApplicable;
	}
}


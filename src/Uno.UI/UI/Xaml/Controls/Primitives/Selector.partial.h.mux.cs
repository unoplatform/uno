// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// MUX Reference Selector_Partial.h, tag winui3/release/2.5.1, commit ba3a8d59e6d

//  Abstract:
//      Represents a Selector abstract class.
//      Management of the selected items list is done through a Selection instance. If we need to modify the selected items list,
//      we request a SelectionChanger from the Selection (via BeginChange) and perform the needed operations. This is done to prevent
//      reentrancy (and infinite loops) arising from user event handlers changing the selection during selection processing.
//      The Selector class itself is primarily responsible for managing the UI side of things such as notifying SelectorItems
//      when their selection state changes. To this end, Selector provides a callback (in the form of SelectionChangeApplier)
//      to the Selection instance.

#nullable enable

using System;
using DirectUI;
using Microsoft.UI.Xaml.Data;
using Windows.Foundation.Collections;

namespace Microsoft.UI.Xaml.Controls.Primitives;

// Represents a Selector abstract class.
//
partial class Selector : ISelectionChangeApplier
{
	// Prevent reentrancy even if m_selection.IsChangeActive() is false.
	private protected void PreventSelectionReentrancy() => m_isSelectionReentrancyLocked = true;

	// Undo the effects of PreventSelectionReentrancy to allow reentrancy
	// when m_selection.IsChangeActive() is false.
	private protected void AllowSelectionReentrancy() => m_isSelectionReentrancyLocked = false;

	// Updating selection causes reentrancy due to our event handler
	// layout. We prevent reentrancy into selection-related functions
	// if there is an active SelectionChanger or someone explicitly prevents
	// reentrancy via PreventSelectionReentrancy.
	private protected bool IsSelectionReentrancyAllowed() => !m_isSelectionReentrancyLocked && !m_selection.IsChangeActive();

	private protected virtual bool IsInSingleSelectionMode() => false;

	// nullable int. keeps old selected index
	private int? m_pOldSelectedIndexToReport;
	private int m_selectedIndexValueSetBeforeItemsAvailable = -1;

	private bool AreCustomValuesAllowed() => m_customValuesAllowed;

	private ISelectionInfo? m_tpDataSourceAsSelectionInfo;

	// Allows the insertion of custom values by not reverting values outside the item source.
	private bool m_customValuesAllowed;

	// Can be negative. (-1) means nothing focused.
	private int m_focusedIndex = -1;

	// Holds the last focused index just before focusing out of the selector.
	private uint m_lastFocusedIndex;

	private protected bool IsInit() => m_pInitializingData is not null;

	// GetFocusedIndex and SetFocusedIndex are consistently used instead of
	// m_focusedIndex to make it easier to track when this field is read & written.
	private protected int GetFocusedIndex() => m_focusedIndex;

	internal void SetFocusedIndex(int focusedIndex)
	{
		if (m_focusedIndex != focusedIndex)
		{
			m_focusedIndex = focusedIndex;
		}
	}

	// GetLastFocusedIndex and SetLastFocusedIndex are consistently used instead of
	// m_lastFocusedIndex to make it easier to track when this field is read & written.
	private protected uint GetLastFocusedIndex() => m_lastFocusedIndex;

	private protected void SetLastFocusedIndex(uint lastFocusedIndex)
	{
		if (m_lastFocusedIndex != lastFocusedIndex)
		{
			m_lastFocusedIndex = lastFocusedIndex;
		}
	}

	// The Selection instance responsible for managing our selected items list and
	// providing the transactional API required for modifying that list.
	private protected readonly Selection m_selection = new();

	// If true, do not scroll Selector Item into view. We will use this flag to prevent Selector from scrolling
	// into view the item we will be animating in FlipView.
	private protected bool m_skipScrollIntoView;
	private protected bool m_inCollectionChange;
	private protected bool m_bSelectionChangeCausedBySelectedValuePathPropertyChange;
	private protected bool m_bCoercingSelectedeValueToNull;
	private protected bool m_skipFocusSuggestion;
	// If true, prevent reentrancy by short-circuiting selection-related functions.
	private protected bool m_isSelectionReentrancyLocked;
	private protected bool m_fUpdatingCurrentItemInCollectionView;
	private protected bool m_fSynchronizeCurrentItem;

	private protected sealed class InitializingData
	{
		// Some data we need to track when in Initializing mode.  We don't want to keep it around all the time.
		internal int m_nInitialIndex = -1;
		internal object? m_pInitialItem;
		internal object? m_pInitialValue;
	}

	private protected ObservableTrackerCollection<object?> m_tpSelectedItemsImpl = null!;

	private protected TrackerCollection<ItemIndexRange> m_tpSelectedRangesImpl = null!;

	private protected ScrollViewer? m_tpScrollViewer;

	private protected ICollectionView? m_tpMonitoredCV;
	private IDisposable? m_epCVCurrentChanged;
	private protected InitializingData? m_pInitializingData;
	private protected PropertyPathListener? m_tpSelectedValuePropertyPathListener;

	// The order at which the XAML properties are set on Selector should not matter.
	// SelectedItem might be set before ItemsSource is set, in which case the SelectedItem
	// value in m_itemPendingSelection until ItemsSource is set.
	private protected object? m_itemPendingSelection;

	// MUX Reference VirtualizingPanel_Partial.cpp: the offset at which off screen elements are arranged.
	private const float ExtraContainerArrangeOffset = 50000.0f;
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// MUX Reference Selection.h, tag winui3/release/2.5.1, commit ba3a8d59e6d

//  Abstract:
//      A transaction-based selection tracker.

#nullable enable

using System;
using System.Collections.Generic;

namespace DirectUI;

// Class for transaction-based selection changes
internal abstract class SelectionChanger
{
	// End selection change.
	// This SelectionChanger instance should not be used again after this method completes.
	public abstract void End(
		IList<object?> pUnselectedItems,
		IList<object?> pSelectedItems,
		bool canSelectMultiple);

	// Select the given item.
	public abstract void Select(
		int index,
		object? pItem,
		bool canSelectMultiple);

	// Unselect a specific item.
	public abstract void Unselect(
		uint index,
		object? pItem);

	// Unselect all selected items.
	public abstract void UnselectAll();

	// Cancels all changes and invalidates this SelectionChanger.
	// This SelectionChanger instance should not be used again after this method completes.
	public abstract void Cancel();
}

// Callback, used to notify interested parties of indexes being selected or unselected.
// For example, used by Selector to notify SelectorItems of their Selected state changing.
internal interface ISelectionChangeApplier
{
	void SelectIndex(uint index);

	void UnselectIndex(uint index);
}

internal sealed partial class Selection
{
	// A collection of selected items with their associated indexes in the items collection.
	// Conceptually, a List<Pair<IInspectable, UINT>>, where the UINT is the
	// index of the respective selected item. The Collection superclass is used for its ability
	// to properly handle IInspectable references.
	// Ideally, this would be replaced with a single list of IInspectable, UINT pairs for greater
	// performance.
	private sealed partial class InternalSelectedItemsStorage : TrackerCollection<object?>
	{
		public override object? this[int index]
		{
			get => base[index];
			set => throw new InvalidOperationException();
		}

		public override void Insert(int index, object? item) => throw new InvalidOperationException();

		public override void Add(object? item) => throw new InvalidOperationException();

		public override void RemoveAtEnd() => throw new InvalidOperationException();

		private readonly List<uint> m_indexlist = new();
	}

	private sealed partial class SelectionChangerImpl : SelectionChanger
	{
		// weak reference to owner Selection that this SelectionChanger belongs to.
		private Selection? m_pOwnerNoRef;

		// List of items selected in current selection change
		private InternalSelectedItemsStorage? m_spItemsToSelect;

		// List of items unselected in current selection change
		private InternalSelectedItemsStorage? m_spItemsToUnselect;

		// Whether or not we're in the middle of a selection change
		private bool m_bIsActive;

		// Whether or not this transaction's only action is to clear
		// all the selected items. This is a perf optimization.
		private bool m_unselectAllRequested;

		// We need to know which items have been removed during this change, so when the change is applied
		// we can correctly differentiate removed items from items which simply ended up with the removed item's index.
		private struct IndexDeletedDuringThisChange
		{
			public uint index;
			public bool empty;
		}

		private IndexDeletedDuringThisChange m_indexDeletedDuringThisChange;

		private ISelectionChangeApplier? m_pChangeApplier;
	}

	private readonly SelectionChangerImpl m_selectionChanger;

	// The current Selection.
	private InternalSelectedItemsStorage? m_spSelectedItems;
}

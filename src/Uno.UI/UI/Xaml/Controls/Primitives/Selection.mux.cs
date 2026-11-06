// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// MUX Reference Selection.cpp, tag winui3/release/2.5.1, commit ba3a8d59e6d

//  Abstract:
//      A transaction-based selection tracker.

#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Windows.Foundation;

namespace DirectUI;

partial class Selection
{
	public Selection()
	{
		m_selectionChanger = new();
	}

	public void Initialize(ISelectionChangeApplier pChangeApplier)
	{
		m_spSelectedItems = new();
		m_selectionChanger.Initialize(this, pChangeApplier);
	}

	// Returns a copy of the list of selected indexes.
	public List<uint> GetSelectedIndexes()
	{
		Debug.Assert(m_spSelectedItems is not null);
		return m_spSelectedItems!.GetSelectedIndexes();
	}

	// Obtains the index of the selected item at the given position in the selection.
	public uint GetIndexAt(uint position)
	{
		if (m_spSelectedItems is null)
		{
			throw new InvalidOperationException();
		}

		return m_spSelectedItems.GetIndexAt(position);
	}

	// Obtains the the selected item at the given position in the selection.
	public object? GetAt(uint position) => m_spSelectedItems![(int)position];

	public void AddSelectedIndex(uint itemIndex) => m_spSelectedItems!.Inserted(itemIndex);

	public void RemoveSelectedIndex(uint itemIndex)
	{
		m_spSelectedItems!.Removed(itemIndex);
		m_selectionChanger.AccountForRemovedItem(itemIndex);
	}

	public void Has(uint itemIndex, out uint position, out bool hasItem) => m_spSelectedItems!.Has(itemIndex, out position, out hasItem);

	// Whether or not we're in the middle of a selection change.
	public bool IsChangeActive() => m_selectionChanger.IsActive();

	// Obtains the number of selected items.
	public uint GetNumItemsSelected()
	{
		if (m_spSelectedItems is null)
		{
			throw new InvalidOperationException();
		}

		return (uint)m_spSelectedItems.Count;
	}

	// Start selection change.
	public SelectionChanger BeginChange()
	{
		m_selectionChanger.BeginInternal();

		return m_selectionChanger;
	}

	#region SelectionChangerImpl implementation

	partial class SelectionChangerImpl
	{
		public SelectionChangerImpl()
		{
			m_pOwnerNoRef = null;
			m_pChangeApplier = null;
			m_bIsActive = false;
			m_unselectAllRequested = false;
			m_indexDeletedDuringThisChange.index = 0;
			m_indexDeletedDuringThisChange.empty = true;
		}

		// Prepares object's state
		public void Initialize(
			Selection pOwner,
			ISelectionChangeApplier pChangeApplier)
		{
			m_pOwnerNoRef = pOwner ?? throw new ArgumentNullException(nameof(pOwner));
			m_pChangeApplier = pChangeApplier ?? throw new ArgumentNullException(nameof(pChangeApplier));
			m_spItemsToSelect = new();
			m_spItemsToUnselect = new();
			Cleanup();
		}

		// Start selection change
		public void BeginInternal()
		{
			Debug.Assert(!m_bIsActive, "SelectionChanger already active");
			m_bIsActive = true;
			m_unselectAllRequested = false;
		}

		// End selection change
		public override void End(
			IList<object?> pUnselectedItems,
			IList<object?> pSelectedItems,
			bool canSelectMultiple)
		{
			Debug.Assert(m_bIsActive, "Selection change must be active before calling SelectionChanger.End()");

			try
			{
				// Make sure that we're not selecting too many items
				// Skip this if we're optimizing for a UnselectAll-only transaction.
				if (!m_unselectAllRequested)
				{
					ApplyCanSelectMultiple(canSelectMultiple);
				}

				// Dispatch any pending UnselectAll requests.
				UnrollUnselectAllRequest();

				// Create lists of what was actually selected and unselected
				CreateDeltaSelectionChange(pUnselectedItems, pSelectedItems);
			}
			finally
			{
				Cleanup();
			}
		}

		public override void Select(
			int itemIndex,
			object? pItem,
			bool canSelectMultiple)
		{
			object? spSelectedItem;

			// If we were optimizing for the UnselectAll-only case, turn off the optimization here.
			UnrollUnselectAllRequest();

			m_spItemsToUnselect!.Has((uint)itemIndex, out var position, out var bHasUnselected);
			if (bHasUnselected)
			{
				var spUnselectedItem = m_spItemsToUnselect[(int)position];
				var areEqual = PropertyValue.AreEqualImpl(pItem, spUnselectedItem);
				if (areEqual)
				{
					// We were going to unselect it, so don't
					m_spItemsToUnselect.RemoveAt((int)position);
					return;
				}
			}

			m_pOwnerNoRef!.Has((uint)itemIndex, out position, out var bHasSelected);
			if (bHasSelected)
			{
				spSelectedItem = m_pOwnerNoRef.GetAt(position);
				var areEqual = PropertyValue.AreEqualImpl(pItem, spSelectedItem);
				if (areEqual)
				{
					// It's already selected
					return;
				}
			}

			m_spItemsToSelect!.Has((uint)itemIndex, out position, out bHasSelected);
			if (bHasSelected)
			{
				spSelectedItem = m_pOwnerNoRef.GetAt(position);
				var areEqual = PropertyValue.AreEqualImpl(pItem, spSelectedItem);
				if (areEqual)
				{
					// We're already going to select it
					return;
				}
			}

			// Too many items would be selected for the mode, deselect some
			if (!canSelectMultiple)
			{
				var nSelectedCount = (uint)m_spItemsToSelect.Count;

				for (uint i = 0; i < nSelectedCount; ++i)
				{
					spSelectedItem = m_spItemsToSelect[(int)i];
					var itemIndexToUnselect = m_spItemsToSelect.GetIndexAt(i);
					m_spItemsToUnselect.Add(itemIndexToUnselect, spSelectedItem);
				}

				m_spItemsToSelect.Clear();
			}

			m_spItemsToSelect.Add((uint)itemIndex, pItem);
		}

		public override void Unselect(
			uint itemIndex,
			object? pItem)
		{
			if (m_unselectAllRequested)
			{
				// We are going to clear the entire selection anyway.
				return;
			}

			m_spItemsToSelect!.Has(itemIndex, out var position, out var bHasSelected);
			if (bHasSelected)
			{
				var spSelectedItem = m_spItemsToSelect[(int)position];
				var areEqual = PropertyValue.AreEqualImpl(pItem, spSelectedItem);
				if (areEqual)
				{
					// If we were going to select it, don't
					m_spItemsToSelect.RemoveAt((int)position);
					return;
				}
			}

			m_pOwnerNoRef!.Has(itemIndex, out position, out bHasSelected);
			if (!bHasSelected)
			{
				// If it's not selected, nothing to do
				return;
			}

			m_spItemsToUnselect!.Has(itemIndex, out position, out var bHasUnselected);
			if (bHasUnselected)
			{
				var spUnselectedItem = m_spItemsToUnselect[(int)position];
				var areEqual = PropertyValue.AreEqualImpl(pItem, spUnselectedItem);
				if (areEqual)
				{
					// We're already going to unselect it
					return;
				}
			}

			m_spItemsToUnselect.Add(itemIndex, pItem);
		}

		// Unselect all selected items.
		public override void UnselectAll() => m_unselectAllRequested = true;

		// If m_unselectAllRequested is true, clear it and
		// remove all items by adding them to m_spItemsToUnselect.
		// This is part of a perf optimization to rapidly handle
		// the UnselectAll case.
		private void UnrollUnselectAllRequest()
		{
			if (m_unselectAllRequested)
			{
				// Very important we clear this here, because
				// Unselect short-circuits if we don't.
				m_unselectAllRequested = false;

				var nSelectedCount = m_pOwnerNoRef!.GetNumItemsSelected();
				for (uint i = 0; i < nSelectedCount; ++i)
				{
					var spSelectedItem = m_pOwnerNoRef.GetAt(i);
					var itemIndex = m_pOwnerNoRef.GetIndexAt(i);
					Unselect(itemIndex, spSelectedItem);
				}
			}
		}

		public override void Cancel()
		{
			Debug.Assert(m_bIsActive, "SelectionChanger not active");

			Cleanup();
		}

		// We need to know if an item was removed, so we can correctly differentiate
		// removed items from items which simply ended up with the removed item's index.
		public void AccountForRemovedItem(uint removedItemIndex)
		{
			m_indexDeletedDuringThisChange.index = removedItemIndex;
			m_indexDeletedDuringThisChange.empty = false;
		}

		public bool IsActive() => m_bIsActive;

		private void Cleanup()
		{
			m_bIsActive = false;
			m_unselectAllRequested = false;
			m_spItemsToSelect?.Clear();
			m_spItemsToUnselect?.Clear();

			m_indexDeletedDuringThisChange.empty = true;
		}

		// Ensures that the selection change is valid for the current mode
		private void ApplyCanSelectMultiple(bool canSelectMultiple)
		{
			if (canSelectMultiple)
			{
				return;
			}

			var nToSelectCount = (uint)m_spItemsToSelect!.Count;
			Debug.Assert(nToSelectCount <= 1, "Too many items selected for a single select selector");

			if (nToSelectCount == 1)
			{
				var nSelectedCount = (uint)m_pOwnerNoRef!.m_spSelectedItems!.Count;
				if (nSelectedCount > 0)
				{
					m_spItemsToUnselect!.Clear();
					for (uint nIndex = 0; nIndex < nSelectedCount; ++nIndex)
					{
						var spItem = m_pOwnerNoRef.GetAt(nIndex);
						var itemIndex = m_pOwnerNoRef.GetIndexAt(nIndex);

						m_spItemsToUnselect.Add(itemIndex, spItem);
					}
				}
			}
			else
			{
				m_spItemsToSelect.Clear();
				var nSelectedCount = (uint)m_pOwnerNoRef!.m_spSelectedItems!.Count;
				if (nSelectedCount > 1)
				{
					m_spItemsToUnselect!.Clear();
					for (uint nIndex = 1; nIndex < nSelectedCount; ++nIndex)
					{
						var spItem = m_pOwnerNoRef.GetAt(nIndex);
						var itemIndex = m_pOwnerNoRef.GetIndexAt(nIndex);

						m_spItemsToUnselect.Add(itemIndex, spItem);
					}
				}
			}
		}

		private void CreateDeltaSelectionChange(
			IList<object?> pUnselectedItems,
			IList<object?> pSelectedItems)
		{
			var nUnselectedCount = (uint)m_spItemsToUnselect!.Count;
			var nSelectedCount = (uint)m_spItemsToSelect!.Count;

			// Make sure that selection change is consistent
			// Create list of items that are actually to be selected/deselected
			if (nUnselectedCount == 0 && nSelectedCount == 0)
			{
				return;
			}

			// We currently don't support removing items and selecting items in one operation.
			// The way our code is architected we shouldn't ever try this. This is here in case
			// there's a side-path we don't know about.
			Debug.Assert(nSelectedCount == 0 || m_indexDeletedDuringThisChange.empty);

			for (uint nIndex = 0; nIndex < nUnselectedCount; ++nIndex)
			{
				var spItem = m_spItemsToUnselect[(int)nIndex];
				var itemIndex = m_spItemsToUnselect.GetIndexAt(nIndex);
				// Note that itemIndex may well be an old index if items were added or removed.
				// This is fine but we need to be careful because the removed item's index
				// may now belong an item that used to be just after the removed item.
				// This will confuse the change applier, which isn't capable of understanding
				// removed items.
				// If the item was removed, we don't do any extra processing as the item
				// was already removed from m_spSelectedItems by RemoveSelectedIndex.
				if (m_indexDeletedDuringThisChange.empty || m_indexDeletedDuringThisChange.index != itemIndex)
				{
					// itemIdx can be -1 for Custom values, these values don't inherit from ISelectorItem so don't try to unselect them.
					if ((int)itemIndex >= 0)
					{
						m_pChangeApplier!.UnselectIndex(itemIndex);
					}

					m_pOwnerNoRef!.Has(itemIndex, out var position, out var hasSelected);
					if (!hasSelected)
					{
						throw new InvalidOperationException();
					}

					m_pOwnerNoRef.m_spSelectedItems!.RemoveAt((int)position);
				}

				pUnselectedItems.Add(spItem);
			}

			// Select all selected items
			for (uint nIndex = 0; nIndex < nSelectedCount; ++nIndex)
			{
				var spItem = m_spItemsToSelect[(int)nIndex];
				var itemIndex = m_spItemsToSelect.GetIndexAt(nIndex);

				// itemIdx can be -1 for Custom values, these values don't inherit from ISelectorItem so don't try to select them.
				if ((int)itemIndex >= 0)
				{
					m_pChangeApplier!.SelectIndex(itemIndex);
				}

#if DEBUG
				m_pOwnerNoRef!.Has(itemIndex, out _, out var hasSelected);
				Debug.Assert(!hasSelected);
#endif

				m_pOwnerNoRef!.m_spSelectedItems!.Add(itemIndex, spItem);
				pSelectedItems.Add(spItem);
			}
		}
	}

	#endregion

	#region InternalSelectedItemsStorage implementation

	partial class InternalSelectedItemsStorage
	{
		public void Add(uint itemIndex, object? pItem)
		{
			m_indexlist.Add(itemIndex);
			base.Add(pItem);
		}

		public void Has(uint itemIndex, out uint position, out bool hasItem)
		{
			hasItem = false;
			position = 0;

			for (var nPosition = 0; nPosition < m_indexlist.Count; ++nPosition)
			{
				if (m_indexlist[nPosition] == itemIndex)
				{
					position = (uint)nPosition;
					hasItem = true;
					break;
				}
			}
		}

		public uint GetIndexAt(uint position) => m_indexlist[(int)position];

		public void Inserted(uint itemIndex)
		{
			// An item was inserted. Update the selected index
			// list so our indexes keep matching up.
			for (var it = 0; it < m_indexlist.Count; ++it)
			{
				if (itemIndex <= m_indexlist[it])
				{
					++m_indexlist[it];
				}
			}
		}

		public void Removed(uint itemIndex)
		{
			var indexInIndexList = 0;

			// An item was removed. Update the selected index
			// list so our indexes keep matching up.
			// We also need to remove the item which has been removed.
			for (var it = 0; it < m_indexlist.Count;)
			{
				if (itemIndex == m_indexlist[it])
				{
					m_indexlist.RemoveAt(it);
					base.RemoveAt(indexInIndexList);
				}
				else
				{
					if (itemIndex < m_indexlist[it])
					{
						--m_indexlist[it];
					}
					++it;
					++indexInIndexList;
				}
			}
		}

		// Returns a copy of the list of selected indexes.
		public List<uint> GetSelectedIndexes() =>
			// Clone the selected index list.
			new(m_indexlist);

		public override void RemoveAt(int position)
		{
			if ((uint)position >= (uint)m_indexlist.Count)
			{
				throw new ArgumentOutOfRangeException(nameof(position));
			}

			m_indexlist.RemoveAt(position);
			base.RemoveAt(position);
		}

		public override void Clear()
		{
			m_indexlist.Clear();
			base.Clear();
		}
	}

	#endregion
}

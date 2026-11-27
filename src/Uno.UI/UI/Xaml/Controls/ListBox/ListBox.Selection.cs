using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using Microsoft.UI.Xaml.Controls.Primitives;
using Uno.Foundation.Logging;

namespace Microsoft.UI.Xaml.Controls;

// Keeps SelectedItems, SelectedIndex/SelectedItem and the containers' IsSelected in sync. In Single mode Selector drives
// the selection and SelectedItems mirrors it; in Multiple/Extended mode SelectedItems is the source of truth and
// SelectedIndex/SelectedItem point at its first item.
partial class ListBox
{
	internal override bool IsSelected(int index)
		=> IsSingleSelection
			? base.IsSelected(index)
			: index >= 0 && _selectedItems.Contains(ItemFromIndex(index));

	/// <summary>
	/// Replaces the multi-selection, raising a single SelectionChanged for the difference.
	/// </summary>
	/// <param name="newSelection">The new selection, in selection order.</param>
	/// <param name="inScope">Runs with the selection events muted, before the selection is replaced.</param>
	private void SetSelection(IList<object> newSelection, Action inScope = null)
	{
		var newSet = new HashSet<object>(newSelection);
		var removed = _selectedItems.Where(item => !newSet.Contains(item)).ToArray();
		var added = newSelection.Distinct().Where(item => !_selectedItems.Contains(item)).ToArray();

		CommitSelection(removed, added, inScope, mutate: () =>
		{
			if (removed.Length == _selectedItems.Count)
			{
				_selectedItems.Clear();
			}
			else if (removed.Length > 0)
			{
				// From the end, so that removing a tail doesn't shift what's left.
				for (var i = _selectedItems.Count - 1; i >= 0; i--)
				{
					if (!newSet.Contains(_selectedItems[i]))
					{
						_selectedItems.RemoveAt(i);
					}
				}
			}

			foreach (var item in added)
			{
				_selectedItems.Add(item);
			}
		});
	}

	private void CommitSelection(object[] removed, object[] added, Action inScope, Action mutate)
	{
		var wasUpdatingSelection = _isUpdatingSelection;
		try
		{
			_modifyingSelectionInternally = true;
			_isUpdatingSelection = true; // Selector doesn't raise SelectionChanged while set, it's raised once below.

			inScope?.Invoke();
			mutate?.Invoke();

			var hasSelection = _selectedItems.Count > 0;
			var first = hasSelection ? _selectedItems[0] : null;
			var firstIndex = hasSelection ? IndexFromItem(first) : -1;

			if (SelectedIndex != firstIndex)
			{
				SelectedIndex = firstIndex;
			}

			if (!Equals(SelectedItem, first))
			{
				SelectedItem = first;
			}

			if (removed.Length > 0 || added.Length > 0)
			{
				UpdateContainersIsSelected();
			}
		}
		finally
		{
			_isUpdatingSelection = wasUpdatingSelection;
			_modifyingSelectionInternally = false;
		}

		_selectionSnapshot = _selectedItems.ToArray();

		if (removed.Length > 0 || added.Length > 0)
		{
			InvokeSelectionChanged(removed, added);
		}
	}

	// SelectedItems was modified by the app. SelectedItemCollection only lets selectable items in, so it already holds the
	// new selection.
	private void OnSelectedItemsCollectionChanged()
	{
		if (_modifyingSelectionInternally)
		{
			return;
		}

		var snapshot = new HashSet<object>(_selectionSnapshot);
		var removed = _selectionSnapshot.Where(item => !_selectedItems.Contains(item)).ToArray();
		var added = _selectedItems.Where(item => !snapshot.Contains(item)).ToArray();

		CommitSelection(removed, added, inScope: null, mutate: null);
	}

	// Syncs the realized containers' IsSelected with the selection, in one pass rather than a container lookup per item.
	private void UpdateContainersIsSelected()
	{
		foreach (var container in MaterializedContainers)
		{
			if (container is SelectorItem selectorItem && IndexFromContainer(container) is var index and >= 0)
			{
				selectorItem.IsSelected = _selectedItems.Contains(ItemFromIndex(index));
			}
		}
	}

	// Uno specific: filters the app's changes before they go in, so that OnSelectedItemsCollectionChanged never has to
	// correct the collection from its own CollectionChanged, which ObservableCollection rejects once another handler is attached.
	// Also keeps a set of the items, so that a lookup doesn't scan the selection.
	// Like WinUI, the app can't change it in Single mode (E_UNEXPECTED), and an addition which isn't in the items or is
	// already selected selects nothing. (WinUI still shows such an addition in SelectedItems until the next selection
	// change; here it's dropped right away.)
	private sealed class SelectedItemCollection : ObservableCollection<object>
	{
		private readonly ListBox _owner;
		private readonly HashSet<object> _set = new();

		public SelectedItemCollection(ListBox owner) => _owner = owner;

		public new bool Contains(object item) => _set.Contains(item);

		// The ListBox syncs the selection and raises SelectionChanged after the CollectionChanged handlers, outside of
		// ObservableCollection's reentrancy block, so that a SelectionChanged handler can change SelectedItems, as on WinUI.
		protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
		{
			base.OnCollectionChanged(e);
			_owner.OnSelectedItemsCollectionChanged();
		}

		protected override void InsertItem(int index, object item)
		{
			if (_owner._modifyingSelectionInternally)
			{
				InsertTracked(index, item);
			}
			else if (!IsSelectableAddition(item))
			{
				// Not in the items, or already selected: ignored.
				LogIgnored(item);
			}
			else
			{
				InsertTracked(index, item);
			}
		}

		protected override void SetItem(int index, object item)
		{
			ThrowIfSingleSelection();

			if (_owner._modifyingSelectionInternally)
			{
				ReplaceTracked(index, item);
			}
			else if (Equals(this[index], item))
			{
				// Unchanged.
			}
			else if (IsSelectableAddition(item))
			{
				ReplaceTracked(index, item);
			}
			else
			{
				LogIgnored(item);
			}
		}

		protected override void RemoveItem(int index)
		{
			ThrowIfSingleSelection();
			CheckReentrancy();
			_set.Remove(this[index]);
			base.RemoveItem(index);
		}

		protected override void ClearItems()
		{
			ThrowIfSingleSelection();
			CheckReentrancy();
			_set.Clear();
			base.ClearItems();
		}

		private bool IsSelectableAddition(object item)
		{
			ThrowIfSingleSelection();
			return _owner.IndexFromItem(item) >= 0 && !Contains(item);
		}

		private void LogIgnored(object item)
		{
			if (_owner.Log().IsEnabled(LogLevel.Debug))
			{
				_owner.Log().Debug($"SelectedItems ignored '{item}': it isn't in the items, or it is already selected.");
			}
		}

		private void ThrowIfSingleSelection()
		{
			if (!_owner._modifyingSelectionInternally && _owner.IsSingleSelection)
			{
				throw new InvalidOperationException("SelectedItems can't be modified when the SelectionMode is Single.");
			}
		}

		// The set is updated before the base raises CollectionChanged, so that the handlers see it in sync.
		private void InsertTracked(int index, object item)
		{
			CheckReentrancy();
			_set.Add(item);
			base.InsertItem(index, item);
		}

		private void ReplaceTracked(int index, object item)
		{
			CheckReentrancy();
			_set.Remove(this[index]);
			_set.Add(item);
			base.SetItem(index, item);
		}
	}

	internal override void OnSelectedIndexChanged(int oldSelectedIndex, int newSelectedIndex)
	{
		if (_modifyingSelectionInternally)
		{
			base.OnSelectedIndexChanged(oldSelectedIndex, newSelectedIndex);
			return;
		}

		if (IsSingleSelection)
		{
			base.OnSelectedIndexChanged(oldSelectedIndex, newSelectedIndex);

			// Selector doesn't update the containers when the change comes from SelectedIndex.
			try
			{
				_modifyingSelectionInternally = true;
				SetContainerIsSelected(oldSelectedIndex, false);
				SetContainerIsSelected(SelectedIndex, true);
			}
			finally
			{
				_modifyingSelectionInternally = false;
			}
			return;
		}

		// The app set SelectedIndex in a multi-selection mode: select just that item, like WinUI's SelectJustThisItemInternal.
		SetSelection(
			newSelectedIndex == -1 ? Array.Empty<object>() : new[] { ItemFromIndex(newSelectedIndex) },
			inScope: () => base.OnSelectedIndexChanged(oldSelectedIndex, newSelectedIndex));
	}

	internal override void OnSelectedItemChanged(object oldSelectedItem, object selectedItem, bool updateItemSelectedState)
	{
		if (_modifyingSelectionInternally)
		{
			// The containers are updated by CommitSelection.
			base.OnSelectedItemChanged(oldSelectedItem, selectedItem, updateItemSelectedState: IsSingleSelection && updateItemSelectedState);
			return;
		}

		var isInItems = IndexFromItem(selectedItem) >= 0;

		if (IsSingleSelection)
		{
			// Mirror the selection before Selector raises SelectionChanged.
			if (selectedItem is null || isInItems)
			{
				MirrorSingleSelection(isSelected: isInItems, selectedItem);
			}

			base.OnSelectedItemChanged(oldSelectedItem, selectedItem, updateItemSelectedState);

			// Selector may have rejected or adjusted the value.
			MirrorSingleSelection(isSelected: SelectedIndex != -1, SelectedItem);
			return;
		}

		if (selectedItem is not null && !isInItems)
		{
			// Let Selector reject the value. It does so by writing SelectedItem back, which isn't the app selecting that item.
			var wasModifyingSelectionInternally = _modifyingSelectionInternally;
			try
			{
				_modifyingSelectionInternally = true;
				base.OnSelectedItemChanged(oldSelectedItem, selectedItem, updateItemSelectedState);
			}
			finally
			{
				_modifyingSelectionInternally = wasModifyingSelectionInternally;
			}

			// Point SelectedIndex/SelectedItem back at the first selected item.
			SetSelection(_selectedItems.ToList());
			return;
		}

		// The app set SelectedItem in a multi-selection mode: select just that item.
		SetSelection(
			isInItems ? new[] { selectedItem } : Array.Empty<object>(),
			inScope: () => base.OnSelectedItemChanged(oldSelectedItem, selectedItem, updateItemSelectedState: false));
	}

	// A container's IsSelected was changed, by the app or the template.
	internal override void ChangeSelectedItem(object item, bool oldIsSelected, bool newIsSelected)
	{
		if (_modifyingSelectionInternally)
		{
			return;
		}

		if (IsSingleSelection)
		{
			base.ChangeSelectedItem(item, oldIsSelected, newIsSelected);
			return;
		}

		if (newIsSelected && !_selectedItems.Contains(item))
		{
			SetSelection(_selectedItems.Append(item).ToList());
		}
		else if (!newIsSelected && _selectedItems.Contains(item))
		{
			SetSelection(_selectedItems.Where(i => !Equals(i, item)).ToList());
		}
	}

	private void SetContainerIsSelected(int index, bool isSelected)
	{
		if (index >= 0 && ContainerFromIndex(index) is SelectorItem container)
		{
			container.IsSelected = isSelected;
		}
	}

	private void MirrorSingleSelection(bool isSelected, object item)
	{
		if (isSelected ? _selectedItems.Count == 1 && Equals(_selectedItems[0], item) : _selectedItems.Count == 0)
		{
			return;
		}

		try
		{
			_modifyingSelectionInternally = true;

			_selectedItems.Clear();
			if (isSelected)
			{
				_selectedItems.Add(item);
			}
		}
		finally
		{
			_modifyingSelectionInternally = false;
		}

		_selectionSnapshot = _selectedItems.ToArray();
	}
}

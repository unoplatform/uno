// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// MUX Reference ListBox_Partial.cpp, tag winui3/release/1.8.2, commit b1db157

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Windows.System;

namespace Microsoft.UI.Xaml.Controls;

partial class ListBox
{
	// Uno specific: WinUI keeps the multi-selection in Selector (m_selection, SelectRangeInternal), which Uno's Selector
	// doesn't have: it only tracks SelectedIndex/SelectedItem. Like ListViewBase, ListBox keeps it in SelectedItems.
	private readonly SelectedItemCollection _selectedItems;
	private object[] _selectionSnapshot = Array.Empty<object>();
	private bool _modifyingSelectionInternally;

	private int? m_pAnchorIndex;

	public ListBox()
	{
		DefaultStyleKey = typeof(ListBox);

		_selectedItems = new SelectedItemCollection(this);
		Loaded += OnLoaded;
	}

	/// <summary>
	/// Gets the currently selected items.
	/// </summary>
	public IList<object> SelectedItems => _selectedItems;

	internal override bool IsSingleSelection => SelectionMode == SelectionMode.Single;

	internal override IList<object> MultiSelectionItems => IsSingleSelection ? null : SelectedItems;

	private protected override bool ScrollsSelectionIntoView => true;

	private protected override void ScrollItemIntoView(object item, ScrollIntoViewAlignment alignment, bool animateIfBringIntoView)
		=> ScrollIntoViewManaged(item, alignment);

	internal override void OnPropertyChanged2(DependencyPropertyChangedEventArgs args)
	{
		base.OnPropertyChanged2(args);

		if (args.Property == IsSelectionActiveProperty)
		{
			OnIsSelectionActiveChanged();
		}
	}

	// Handler for selection mode changed event.
	private void OnSelectionModeChanged(DependencyPropertyChangedEventArgs args)
	{
		if ((SelectionMode)args.NewValue == SelectionMode.Single)
		{
			var selectedIndex = SelectedIndex;

			if (selectedIndex > -1)
			{
				MakeSingleSelection(selectedIndex);
			}
		}

		// Update the selection mode
		UpdateCVSynchronizationState();
	}

	// Uno specific: stand-in for Selector::UpdateCVSynchronizationState, which isn't ported. Unless the selection follows
	// a collection view's current item, WinUI resets SelectedItem, which clears the selection.
	private void UpdateCVSynchronizationState()
	{
		if (IsSingleSelection && (IsSynchronizedWithCurrentItem ?? true) && ItemsSource is ICollectionView collectionView)
		{
			SelectedIndex = collectionView.CurrentPosition;
		}
		else
		{
			SetSelection(Array.Empty<object>());
		}
	}

	// Called when the IsEnabled property changes.
	private protected override void OnIsEnabledChanged(IsEnabledChangedEventArgs e)
	{
		base.OnIsEnabledChanged(e);

		UpdateVisualState(true);
	}

	// Change to the correct visual state for the ListBox.
	private protected override void ChangeVisualState(bool useTransitions)
	{
		var hasFocus = HasFocus();

		if (!IsEnabled)
		{
			GoToState(useTransitions, "Disabled");
		}
		else
		{
			GoToState(useTransitions, "Normal");
		}

		if (hasFocus)
		{
			GoToState(useTransitions, "Focused");
		}
		else
		{
			GoToState(useTransitions, "Unfocused");
		}
	}

	// Determines if the specified item is (or is eligible to be) its own container.
	protected override bool IsItemItsOwnContainerOverride(object item) => item is ListBoxItem;

	// Creates or identifies the element that is used to display the given item.
	protected override DependencyObject GetContainerForItemOverride() => new ListBoxItem { IsGeneratedContainer = true };

	// Called when the control got focus.
	protected override void OnGotFocus(RoutedEventArgs e)
	{
		base.OnGotFocus(e);

		if (!IsFocusEngagementEnabled || IsFocusEngaged)
		{
			FocusChanged(HasFocus(), ReferenceEquals(e.OriginalSource, this));
		}
	}

	// Called when the control lost focus.
	protected override void OnLostFocus(RoutedEventArgs e)
	{
		base.OnLostFocus(e);

		FocusChanged(HasFocus(), ReferenceEquals(e.OriginalSource, this));
	}

	// Called when the focus has changed.
	private void FocusChanged(bool hasFocus, bool self)
	{
		if (self)
		{
			IsSelectionActive = hasFocus;

			if (hasFocus)
			{
				SetFocusedItem(GetFocusedIndex() < 0 ? 0 : GetFocusedIndex(), true);
			}
		}

		UpdateVisualState(true);
	}

	// Responds to the KeyDown event.
	protected override void OnKeyDown(KeyRoutedEventArgs args)
	{
		base.OnKeyDown(args);

		if (args.Handled)
		{
			return;
		}

		var handled = false;
		var newFocusedIndex = -1;
		// Uno specific: WinUI reads the global keyboard state (CoreImports::Input_GetKeyboardModifiers).
		var modifiers = args.KeyboardModifiers;

		switch (args.Key)
		{
			case VirtualKey.Enter:
			case VirtualKey.Space:
				if (!modifiers.HasFlag(VirtualKeyModifiers.Menu)
					&& args.OriginalSource is ListBoxItem listBoxItem
					&& IndexFromContainer(listBoxItem) is var itemIndex and >= 0)
				{
					HandleItemSelection(itemIndex, modifiers, isMouseSelection: false);
					handled = true;
				}
				break;
			case VirtualKey.Home:
			case VirtualKey.End:
			case VirtualKey.PageUp:
			case VirtualKey.GamepadLeftTrigger:
			case VirtualKey.GamepadLeftShoulder:
			case VirtualKey.PageDown:
			case VirtualKey.GamepadRightTrigger:
			case VirtualKey.GamepadRightShoulder:
			case VirtualKey.Left:
			case VirtualKey.Up:
			case VirtualKey.Right:
			case VirtualKey.Down:
				newFocusedIndex = GetFocusedIndex();
				HandleNavigationKey(args.Key, scrollViewport: true, ref newFocusedIndex);
				break;
			case VirtualKey.A:
				if (SelectionMode != SelectionMode.Single && modifiers.HasFlag(VirtualKeyModifiers.Control))
				{
					var itemCount = Items.Count;

					if (itemCount != 0)
					{
						// Uno specific: WinUI compares the counts, but equal items share one SelectedItems entry here.
						if (AreAllItemsSelected())
						{
							DeselectAllItems();
						}
						else
						{
							SelectAllItems();
						}

						handled = true;
					}
				}
				break;
		}

		if (newFocusedIndex != -1 && newFocusedIndex != GetFocusedIndex())
		{
			handled = true;

			newFocusedIndex = Math.Min(newFocusedIndex, Items.Count - 1);
			if (0 <= newFocusedIndex)
			{
				var mode = SelectionMode;

				if (mode == SelectionMode.Extended && modifiers.HasFlag(VirtualKeyModifiers.Shift))
				{
					// if keyboard Modifier is Shift key and mode is Extended we need to reselect range
					// and set focus to focused index and bring it into the view if necessary.
					MakeRangeSelection(newFocusedIndex, clearOldSelection: !modifiers.HasFlag(VirtualKeyModifiers.Control));
				}
				else if (modifiers.HasFlag(VirtualKeyModifiers.Control) || mode == SelectionMode.Multiple)
				{
					// if keyboard Modifier is Ctrl key or mode is Multiple we just want to move the focus
					// to focused index and bring it into the view if necessary.
				}
				else if ((mode == SelectionMode.Single && SingleSelectionFollowsFocus) || mode != SelectionMode.Single)
				{
					// If mode is single selection, select only if SingleSelectionFollowsFocus is true.
					// For all other cases we should select just this item and move focus
					// to focused index and bring it into the view if necessary.
					MakeSingleSelection(newFocusedIndex);
				}

				SetFocusedItem(newFocusedIndex, shouldScrollIntoView: true, forceFocus: false, FocusState.Keyboard, animateIfBringIntoView: false);
			}
		}

		if (handled)
		{
			args.Handled = true;
		}
	}

	// Handler for when a ListBoxItem is clicked: allows different behavior based on the current SelectionMode.
	// Uno specific: SelectorItem focuses itself on pointer release before calling this (WinUI's OnSelectorItemClicked does both).
	internal override void OnItemClicked(int clickedIndex, VirtualKeyModifiers modifiers)
	{
		if (clickedIndex < 0)
		{
			return;
		}

		HandleItemSelection(clickedIndex, modifiers, isMouseSelection: true);
	}

	// Select item(s) based on current selection mode
	private void HandleItemSelection(int index, VirtualKeyModifiers modifiers, bool isMouseSelection)
	{
		var ctrl = modifiers.HasFlag(VirtualKeyModifiers.Control);
		var shift = modifiers.HasFlag(VirtualKeyModifiers.Shift);

		switch (SelectionMode)
		{
			case SelectionMode.Single:
				if (ctrl)
				{
					MakeToggleSelection(index);
				}
				else
				{
					MakeSingleSelection(index);
				}
				break;

			case SelectionMode.Multiple:
				MakeToggleSelection(index);
				break;

			case SelectionMode.Extended:
				if (shift && ctrl)
				{
					if (isMouseSelection)
					{
						MakeRangeSelection(index, clearOldSelection: false);
					}
				}
				else if (shift)
				{
					MakeRangeSelection(index, clearOldSelection: true);
				}
				else if (ctrl)
				{
					MakeToggleSelection(index);
				}
				else
				{
					MakeSingleSelection(index);
				}
				break;
		}

		// TODO Uno: ElementSoundPlayerService.RequestInteractionSoundForElement(ElementSoundKind.Invoke, this)
	}

	// Called when the IsSelectionActive property has changed.
	private void OnIsSelectionActiveChanged()
	{
		// Uno specific: walks the realized containers rather than looking up the container of each selected item.
		foreach (var container in MaterializedContainers)
		{
			if (container is ListBoxItem { IsSelected: true } listBoxItem)
			{
				listBoxItem.UpdateVisualState(true);
			}
		}
	}

	// Additional processing that occurs after Selector has changed the selected item.
	private protected override void OnSelectionChanged(
		int oldSelectedIndex,
		int newSelectedIndex,
		object pOldSelectedItem,
		object pNewSelectedItem,
		bool animateIfBringIntoView = false,
		FocusNavigationDirection focusNavigationDirection = FocusNavigationDirection.None)
	{
		var shouldCallBase = newSelectedIndex != -1 && oldSelectedIndex == -1;
		if (!shouldCallBase && oldSelectedIndex != newSelectedIndex)
		{
			shouldCallBase = SelectionMode == SelectionMode.Single;
		}

		if (shouldCallBase)
		{
			base.OnSelectionChanged(oldSelectedIndex, newSelectedIndex, pOldSelectedItem, pNewSelectedItem, animateIfBringIntoView, focusNavigationDirection);
		}
	}

	// Uno specific: on a virtualizing panel the base doesn't update the containers (ShouldItemsControlManageChildren is
	// false), so the panel's layout is refreshed instead, like ComboBox does.
	private protected override void UpdateItems(NotifyCollectionChangedEventArgs args)
	{
		base.UpdateItems(args);

		if (VirtualizingPanel is not null)
		{
			Refresh();
		}
	}

	// Uno specific: replacing ItemsSource clears a multi-selection, as WinUI does, even the items that the new source shares.
	// Without this, the multi-mode pruning of OnItemsSourceSingleCollectionChanged would keep those.
	protected override void OnItemsSourceChanged(DependencyPropertyChangedEventArgs e)
	{
		if (!IsSingleSelection && _selectedItems.Count > 0)
		{
			SetSelection(Array.Empty<object>());
		}

		base.OnItemsSourceChanged(e);
	}

	// Handler for when the Items collection is changed. We should adjust the anchor index
	// if any item before that index were added or removed.
	// Uno specific: WinUI's OnItemsChanged only sees the Items vector; this also sees ItemsSource changes.
	internal override void OnItemsSourceSingleCollectionChanged(object sender, NotifyCollectionChangedEventArgs args, int section)
	{
		if (IsSingleSelection)
		{
			base.OnItemsSourceSingleCollectionChanged(sender, args, section);
		}
		else
		{
			// Uno specific: Selector moves SelectedIndex to follow the items, which would otherwise read as the app
			// selecting just that item. The multi-selection lives in SelectedItems, which is synced back below.
			var wasModifyingSelectionInternally = _modifyingSelectionInternally;
			var wasUpdatingSelection = _isUpdatingSelection;
			try
			{
				_modifyingSelectionInternally = true;
				_isUpdatingSelection = true;

				base.OnItemsSourceSingleCollectionChanged(sender, args, section);
			}
			finally
			{
				_isUpdatingSelection = wasUpdatingSelection;
				_modifyingSelectionInternally = wasModifyingSelectionInternally;
			}
		}

		if (m_pAnchorIndex is { } anchorIndex)
		{
			switch (args.Action)
			{
				case NotifyCollectionChangedAction.Reset:
					m_pAnchorIndex = null;
					break;

				case NotifyCollectionChangedAction.Add:
					if (args.NewStartingIndex <= anchorIndex)
					{
						m_pAnchorIndex = anchorIndex + (args.NewItems?.Count ?? 1);
					}
					break;

				case NotifyCollectionChangedAction.Remove:
					var removedCount = args.OldItems?.Count ?? 1;
					if (args.OldStartingIndex <= anchorIndex && anchorIndex < args.OldStartingIndex + removedCount)
					{
						m_pAnchorIndex = null;
					}
					else if (args.OldStartingIndex < anchorIndex)
					{
						m_pAnchorIndex = anchorIndex - removedCount;
					}
					break;

				case NotifyCollectionChangedAction.Replace:
				case NotifyCollectionChangedAction.Move:
					m_pAnchorIndex = null;
					break;
			}
		}

		// Uno specific: drop the items which left the collection from the multi-selection, and point
		// SelectedIndex/SelectedItem back at its first item.
		if (!IsSingleSelection && _selectedItems.Count > 0)
		{
			if (args.Action == NotifyCollectionChangedAction.Add)
			{
				SetSelection(_selectedItems.ToList());
			}
			else
			{
				var items = new HashSet<object>(Items);
				SetSelection(_selectedItems.Where(items.Contains).ToList());
			}
		}
		else if (!IsSingleSelection && SelectedIndex >= 0)
		{
			// Uno specific: with nothing selected, Selector only moves SelectedIndex to apply an index set before there were
			// items (e.g. SelectedIndex before ItemsSource in markup, which WinUI also applies once the items are available).
			SetSelection(new[] { ItemFromIndex(SelectedIndex) });
		}
	}

	/// <summary>
	/// Causes the object to scroll into view.
	/// </summary>
	/// <param name="item">The object to scroll to.</param>
	public void ScrollIntoView(object item)
		=> ScrollIntoViewInternal(item, isHeader: false, isFooter: false, isFromPublicAPI: true, ScrollIntoViewAlignment.Default, offset: 0, animateIfBringIntoView: false);

	// Handles selection of single item, and only that item
	private void MakeSingleSelection(int index)
	{
		if (SelectionMode == SelectionMode.Extended)
		{
			m_pAnchorIndex = index;
		}

		if (IsSingleSelection && _selectedItems.Count <= 1)
		{
			MakeSingleSelection(index, animateIfBringIntoView: false, selectedItem: null, FocusNavigationDirection.None);
		}
		else
		{
			// Uno specific: Selector.MakeSingleSelection only moves SelectedIndex, which would leave the rest of a multi-selection.
			SetSelection(new[] { ItemFromIndex(index) });
		}
	}

	// Toggle IsSelected of item
	private void MakeToggleSelection(int index)
	{
		if (SelectionMode == SelectionMode.Extended)
		{
			m_pAnchorIndex = index;
		}

		// Uno specific: works on the selection rather than the container's IsSelected, as the container may not be realized.
		if (IsSingleSelection)
		{
			SelectedIndex = SelectedIndex == index ? -1 : index;
		}
		else
		{
			var item = ItemFromIndex(index);
			SetSelection(_selectedItems.Contains(item)
				? _selectedItems.Where(i => !Equals(i, item)).ToList()
				: _selectedItems.Append(item).ToList());
		}
	}

	// Select a range of items beginning with current anchor
	private void MakeRangeSelection(int index, bool clearOldSelection)
	{
		global::System.Diagnostics.Debug.Assert(SelectionMode != SelectionMode.Single, "Invalid selection mode in range selection");

		var anchorIndex = m_pAnchorIndex ?? 0;
		var newIndex = index;

		if (anchorIndex > index)
		{
			newIndex = anchorIndex;
			anchorIndex = index;
		}

		SelectRangeInternal(anchorIndex, newIndex - anchorIndex + 1, clearOldSelection);
	}

	/// <summary>
	/// Selects all the items in the ListBox.
	/// </summary>
	/// <exception cref="NotSupportedException">The SelectionMode is Single.</exception>
	public void SelectAll()
	{
		if (SelectionMode == SelectionMode.Single)
		{
			// IFC(ErrorHelper::OriginateErrorUsingResourceID(E_NOT_SUPPORTED, ERROR_INVALID_MULTIPLE_SELECT));
			throw new NotSupportedException("SelectAll is not supported when the SelectionMode is Single.");
		}

		SelectAllItems();
	}

	// Selects all the items.
	private void SelectAllItems()
	{
		var count = Items.Count;

		if (count > 0)
		{
			SelectRangeInternal(0, count, clearOldSelection: false);
		}
	}

	// Uno specific: whether every item is selected.
	private bool AreAllItemsSelected()
	{
		foreach (var item in Items)
		{
			if (!_selectedItems.Contains(item))
			{
				return false;
			}
		}

		return true;
	}

	// Deselect all the items.
	private void DeselectAllItems() => SetSelection(Array.Empty<object>());

	// Loaded event handler.
	private void OnLoaded(object sender, RoutedEventArgs e) => UpdateVisualState(false);

	// Uno specific: stand-in for WinUI's Selector::SelectRangeInternal.
	private void SelectRangeInternal(int firstIndex, int length, bool clearOldSelection)
	{
		var range = Enumerable.Range(firstIndex, length).Select(ItemFromIndex);
		var newSelection = clearOldSelection
			? range.ToList()
			: _selectedItems.Concat(range.Where(item => !_selectedItems.Contains(item))).ToList();

		SetSelection(newSelection);
	}
}

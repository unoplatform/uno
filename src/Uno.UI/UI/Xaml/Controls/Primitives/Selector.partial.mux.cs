// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// MUX Reference Selector_Partial.cpp, tag winui3/release/2.5.1, commit ba3a8d59e6d

#nullable enable

using System;
using System.Collections.Generic;
using DirectUI;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Uno.Disposables;
using Uno.Foundation.Logging;
using Uno.UI.Extensions;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.System;

namespace Microsoft.UI.Xaml.Controls.Primitives;

partial class Selector
{
	public Selector()
	{
		// Prepares object's state

		// Provide our Selection instance a SelectionChangeApplier so we can forward selection change
		// notifications to our SelectorItems.
		m_selection.Initialize(this);

		m_tpSelectedItemsImpl = new();

		m_tpSelectedItemsImpl.VectorChanged += OnSelectedItemsCollectionChanged;

		FocusEngaged += OnFocusEngaged;

		m_tpSelectedRangesImpl = new();
	}

#if !HAS_UNO // TODO Uno: nothing to release, the selected items, the focus handler and the CV subscription die with the Selector.
	~Selector()
	{
		spSelectedItems.remove_VectorChanged(m_SelectedItemsVectorChangedToken);
		remove_FocusEngaged(m_focusEnagedToken);
		DisconnectFromMonitoredCVCore(spMonitoredCV);
		delete m_pInitializingData;
	}
#endif

	// Invoked whenever application code or internal processes call
	// ApplyTemplate.
	protected override void OnApplyTemplate()
	{
		base.OnApplyTemplate();

		// Get the parts
		m_tpScrollViewer = GetTemplateChild<ScrollViewer>("ScrollViewer");
		if (m_tpScrollViewer is not null)
		{
			m_tpScrollViewer.TemplatedParentHandlesScrolling = true;
		}
	}

	internal override void OnPropertyChanged2(DependencyPropertyChangedEventArgs args)
	{
		base.OnPropertyChanged2(args);

		if (args.Property == SelectedIndexProperty)
		{
			OnSelectedIndexChanged((int)args.OldValue, (int)args.NewValue);
		}
		else if (args.Property == SelectedItemProperty)
		{
			OnSelectedItemChanged(args.OldValue, args.NewValue, updateItemSelectedState: true);
		}
		else if (args.Property == IsSynchronizedWithCurrentItemProperty)
		{
			OnIsSynchronizedWithCurrentItemChanged(args.OldValue, args.NewValue);
		}
		else if (args.Property == SelectedValueProperty)
		{
			OnSelectedValueChanged(args.OldValue, args.NewValue);
		}
		else if (args.Property == SelectedValuePathProperty)
		{
			OnSelectedValuePathChanged(args.OldValue, args.NewValue);
		}
#if !HAS_UNO // TODO Uno: ItemsHost isn't a dependency property, and ScrollContentPresenter has no HookupScrollingComponents.
		else if (args.Property == ItemsHostProperty)
		{
			// If Items host changes we need to make sure scrolling components are re-hooked.
			// For example if the ItemsPanelTemplate changes from horizontally aligned VSP to vertically aligned VSP,
			// we need to hook the new VSP as ScrollInfo.
			if (m_tpScrollViewer?.Presenter is not null)
			{
				GetXamlDispatcherNoRef().RunAsync(OnItemsHostChanged);
			}
		}
#endif
	}

	protected override void OnKeyDown(KeyRoutedEventArgs e)
	{
		// Ignore already handled events
		if (e is null)
		{
			throw new ArgumentNullException(nameof(e));
		}

		base.OnKeyDown(e);
		var isHandled = e.Handled;
		if (isHandled)
		{
			return;
		}

#if !HAS_UNO // TODO Uno: ScrollViewer has no keyboard zoom (GetKeyboardMessageZoomAction, ProcessPureInertiaInputMessage).
		// If this is a zoom hotkey, forward it to our ScrollViewer.
		if (m_tpScrollViewer is not null)
		{
			var modifiers = GetKeyboardModifiers();
			var key = e.Key;
			var messageZoomDirection = ScrollViewer.GetKeyboardMessageZoomAction(modifiers, key);
			if (messageZoomDirection != ZoomDirection_None)
			{
				isHandled = m_tpScrollViewer.ProcessPureInertiaInputMessage(messageZoomDirection);
			}
		}
#endif

		// Update when we've handled the event
		if (isHandled)
		{
			e.Handled = isHandled;
		}
	}

	private void OnSelectedValueChanged(
		object? pOldValue,
		object? pNewValue)
	{
		if (!m_bCoercingSelectedeValueToNull)
		{
			// Avoid recursion.  If we are updating item in response to a change in the index then don't do any additional processing.  Also side-exit when parsing XAML because we defer processing the selection until after all the content has been loaded.
			if (!IsSelectionReentrancyAllowed() || IsInit())
			{
				return;
			}

			FindIndexOfItemWithValue(pNewValue, out var indexOfItemWithValue, out var spItemWithValue);
			var selectedIndex = SelectedIndex;

			SelectJustThisItemInternal(selectedIndex, indexOfItemWithValue, spItemWithValue, false /*animateIfBringIntoView*/, out _);

			var bHasItems = GetHasItems();

			// If setting SelectedValue does not result in item selection
			// then set coerce SelectedValue to null if there is an ItemsSource set
			if (pNewValue is not null && indexOfItemWithValue == -1 && bHasItems)
			{
				CoerceSelectedValueToNull();
			}
		}
	}

	private void CoerceSelectedValueToNull()
	{
		m_bCoercingSelectedeValueToNull = true;
		try
		{
			SelectedValue = null;
		}
		finally
		{
			m_bCoercingSelectedeValueToNull = false;
		}
	}

	private void OnItemsHostChanged()
	{
#if !HAS_UNO // TODO Uno: ScrollContentPresenter has no HookupScrollingComponents.
		// If Items host changes we need to make sure scrolling components are re-hooked.
		// For example if the ItemsPanelTemplate changes from horizontally aligned VSP to vertically aligned VSP,
		// we need to hook the new VSP as ScrollInfo.
		m_tpScrollViewer?.Presenter?.HookupScrollingComponents();
#endif
	}

	private void OnSelectedValuePathChanged(
		object? pOldValue,
		object? pNewValue)
	{
		try
		{
			var strNewPath = pNewValue as string;
			m_tpSelectedValuePropertyPathListener?.Dispose();
			m_tpSelectedValuePropertyPathListener = null;

			if (!string.IsNullOrEmpty(strNewPath))
			{
				PropertyPathParser pPropertyPathParser = new();
				pPropertyPathParser.SetSource(strNewPath, null!);
				PropertyPathListener spPropertyPathListener = new();
				spPropertyPathListener.Initialize(null!, pPropertyPathParser, false, false);
				m_tpSelectedValuePropertyPathListener = spPropertyPathListener;
			}

			// If we are updating item in response to a change in the index then don't do any additional processing.  Also side-exit when parsing XAML because we defer processing the selection until after all the content has been loaded.
			if (IsInit())
			{
				return;
			}

			var selectedIndex = SelectedIndex;
			var spSelectedValue = SelectedValue;
			m_bSelectionChangeCausedBySelectedValuePathPropertyChange = true;
			FindIndexOfItemWithValue(spSelectedValue, out var newSelectedIndex, out var spNewSelectedItem);

			SelectJustThisItemInternal(selectedIndex, newSelectedIndex, spNewSelectedItem, false /*animateIfBringIntoView*/, out _);
		}
		finally
		{
			m_bSelectionChangeCausedBySelectedValuePathPropertyChange = false;
		}
	}

	/// Returns the selected value of an item using a path.
	private object? GetSelectedValue(object? pItem)
	{
		if (m_tpSelectedValuePropertyPathListener is null
			|| pItem is null)
		{
			return pItem;
		}

		m_tpSelectedValuePropertyPathListener.SetSource(pItem);
		return m_tpSelectedValuePropertyPathListener.GetValue();
	}

	/// Finds the index of the first item with a given property path value.
	private void FindIndexOfItemWithValue(
		object? pValue,
		out int index,
		out object? pItemWithValue)
	{
		index = -1;
		pItemWithValue = null;

		var spItems = Items;
		var nCount = spItems.Size;

		for (uint cnt = 0; cnt < nCount; cnt++)
		{
			var spItem = spItems[(int)cnt];
			var spItemValue = GetSelectedValue(spItem);
			var areValuesEqual = PropertyValue.AreEqualImpl(spItemValue, pValue);
			if (areValuesEqual)
			{
				index = (int)cnt;
				pItemWithValue = spItem;
				return;
			}
		}
	}

	// Reprocesses any SelectedItem or SelectedValue that was set prior to the ItemsSource being set
	protected override void OnItemsSourceChanged(DependencyPropertyChangedEventArgs e)
	{
		var pNewValue = e.NewValue;
		SelectionChanger? pSelectionChanger = null;

		// Evaluate before OnItemsSourceChanged since the value can be modified during the call.
		var hasPendingSelectedIndex = m_selectedIndexValueSetBeforeItemsAvailable >= 0;

		base.OnItemsSourceChanged(e);

#if HAS_UNO
		Refresh();
#endif

		try
		{
			pSelectionChanger = BeginChange();

			// If we do not have a pending selected index, clear selection
			if (!hasPendingSelectedIndex)
			{
				var nSelectedCount = m_selection.GetNumItemsSelected();
				for (uint i = 0; i < nSelectedCount; ++i)
				{
					var spItem = m_selection.GetAt(i);
					var itemIndex = m_selection.GetIndexAt(i);
					pSelectionChanger.Unselect(itemIndex, spItem);
				}
			}

			// If we're monitoring a CV and we're synchronizing the selected item
			// with the current item then select it here
			if (m_tpMonitoredCV is not null && m_fSynchronizeCurrentItem)
			{
				var spItem = m_tpMonitoredCV.CurrentItem;
				var iCurrentPosition = m_tpMonitoredCV.CurrentPosition;

				var spItems = Items;
				var nCount = spItems.Size;

				// we should add current to the selection only if it is within collection range
				if (iCurrentPosition > -1 && iCurrentPosition < (int)nCount)
				{
					pSelectionChanger.Select(iCurrentPosition, spItem, false /* canSelectMultiple */);
				}
#if DEBUG
				else
				{
					// if current index out of range then we shouldn't get any item from currency.
					global::System.Diagnostics.Debug.Assert(spItem is null);
				}
#endif
			}

			SelectAllSelectedSelectorItems(pSelectionChanger);
			EndChange(pSelectionChanger);
			pSelectionChanger = null;

			if (pNewValue is not null && m_itemPendingSelection is not null)
			{
				var items = Items;

				// Check if we're trying to restore a value that's not in the collection.
				var itemFound = items.IndexOf(m_itemPendingSelection) >= 0;
				if (itemFound)
				{
					SelectedItem = m_itemPendingSelection;
				}

				m_itemPendingSelection = null;
			}
		}
		finally
		{
			pSelectionChanger?.Cancel();
		}
	}

	private void SelectAllSelectedSelectorItems(SelectionChanger pSelectionChanger)
	{
		var isSelected = false;
		var canSelectMultiple = CanSelectMultiple;

		var spItems = Items;
		var pItemCollectionNoRef = spItems;

		// Items source is set from ItemSource, thus if the ItemSource is active
		// we skip this old behavior.
		if (ItemsSource is not null)
		{
			return;
		}

		var spItemTemplate = ItemTemplate;
		if (spItemTemplate is null)
		{
			var nCount = pItemCollectionNoRef.Size;
			if (nCount > 0)
			{
				for (uint i = 0; i < nCount; ++i)
				{
					var spItem = spItems[(int)i];
					// Upstream queries the collection here, not the item.
					var spSelectorItem = (object)spItems as SelectorItem;
					if (spSelectorItem is not null)
					{
						isSelected = spSelectorItem.IsSelected;
						if (isSelected)
						{
							pSelectionChanger.Select((int)i, spItem, canSelectMultiple);
						}
					}
				}
			}
		}
	}

	// Uno's FlipView and ListViewBase override this to react to the property change, see Selector.cs.
	internal virtual void OnSelectedIndexChanged(
		int oldValue,
		int newValue)
	{
		SelectionChanger? pSelectionChanger = null;

		var undoChange = false;
		var outOfRange = false;

		// Avoid recursion.  If we are updating index in response to a change in the item then don't do any additional processing.
		if (!IsSelectionReentrancyAllowed() || IsInit())
		{
			return;
		}

		try
		{
			var spItems = Items;
			var nCount = spItems.Size;

			if (newValue >= -1 && newValue < (int)nCount)
			{
				// See if the change can actually take place, if not we need
				// to undo it
				SelectJustThisItemInternal(oldValue, newValue, null, false /*animateIfBringIntoView*/, out undoChange);

				// We got a selected index that is valid, so throw away the
				// stored pending value that we wanted to apply.
				m_selectedIndexValueSetBeforeItemsAvailable = -1;
			}
			else
			{
				// The index is invalid so we need to undo the change
				undoChange = true;
				outOfRange = true;
			}

			// Undo the change if required
			if (undoChange)
			{
				// Don't modify SelectedItems
				pSelectionChanger = BeginChange();
				try
				{
					SelectedIndex = oldValue;
				}
				finally
				{
					pSelectionChanger.Cancel();
					pSelectionChanger = null;
				}

				if (outOfRange)
				{
					if (nCount == 0)
					{
						// No Items have been added yet, so store the index and
						// set the selection once we get the items.
						m_selectedIndexValueSetBeforeItemsAvailable = newValue;
					}
					else
					{
						// Only throw if the value was actually out of range and there are already existing items.
						// Un the case of the collection view cancelling the change
						// this is perfectly fine.
						throw new ArgumentException("SelectedIndex cannot be set to invalid value when items are present", nameof(newValue));
					}
				}
			}
		}
		finally
		{
			pSelectionChanger?.Cancel();
		}
	}

	private void OnSelectedItemChanged(
		object? pOldValue,
		object? pNewValue)
	{
		var newSelectedIndex = -1;
		var undoChange = false;
		SelectionChanger? pSelectionChanger = null;

		// Avoid recursion.  If we are updating index in response to a change in the item then don't do any additional processing.
		if (!IsSelectionReentrancyAllowed() || IsInit())
		{
			return;
		}

		try
		{
			var spItems = Items;

			// Check if we're trying to set this to a value that's not in the collection
			var newIndex = spItems.IndexOf(pNewValue!);
			var bItemFound = newIndex >= 0;
			if (bItemFound)
			{
				newSelectedIndex = newIndex;
			}

			// Custom values will not be found in the item collection prevent undoing the change in these cases.
			if (!bItemFound && pNewValue is not null && !AreCustomValuesAllowed())
			{
				undoChange = true;

				var itemsSource = ItemsSource;
				if (itemsSource is null)
				{
					m_itemPendingSelection = pNewValue;
				}
			}
			else
			{
				var oldSelectedIndex = SelectedIndex;

				// If we couldn't change the selection that means that we need
				// to undo the change
				SelectJustThisItemInternal(oldSelectedIndex, newSelectedIndex, pNewValue, false /*animateIfBringIntoView*/, out undoChange);
			}

			// If we need to undo the change do so here
			if (undoChange)
			{
				// Don't modify SelectedItems
				pSelectionChanger = BeginChange();
				try
				{
					// Restore old value
					SelectedItem = pOldValue;
				}
				finally
				{
					pSelectionChanger.Cancel();
					pSelectionChanger = null;
				}
			}
		}
		finally
		{
			pSelectionChanger?.Cancel();
		}
	}

	// Called to update the synchronization state due to changes on properties
	private protected void UpdateCVSynchronizationState()
	{
		var fOldSync = m_fSynchronizeCurrentItem;

		UpdateIsSynchronized();

		// If we didn't change state then we're done
		if (fOldSync == m_fSynchronizeCurrentItem)
		{
			// make sure that the SelectedItem is not synchronized
			if (!m_fSynchronizeCurrentItem)
			{
				SelectedItem = null;
			}
			return;
		}

		// We're starting to synchronize
		if (!fOldSync && m_fSynchronizeCurrentItem)
		{
			global::System.Diagnostics.Debug.Assert(m_tpMonitoredCV is not null); // In order for m_fSynchronizeCurrentItem to be true we must have a CV

			var spCurrentItem = m_tpMonitoredCV!.CurrentItem;
			SelectedItem = spCurrentItem;
		}
		// We're stopping the synchronization
		else
		{
			SelectedItem = null;
		}
	}

	private void OnIsSynchronizedWithCurrentItemChanged(
		object? pOldValue,
		object? pNewValue)
	{
		// Validate that the value is what we expect
		if (pNewValue is not null)
		{
			// This must be an IPropertyValue and it must be a BOOLEAN
			var fValue = (bool)pNewValue;
			if (fValue != false)
			{
				// We only accept FALSE or NULL as values for this property
				throw new ArgumentException("True is not a supported value for this property", nameof(pNewValue));
			}
		}

		UpdateCVSynchronizationState();
	}

	// List of all items that are currently selected
	private protected IList<object?>? SelectedItemsInternal => m_tpDataSourceAsSelectionInfo is null ? m_tpSelectedItemsImpl : null;

	// List of all ranges that are currently selected
	private protected IReadOnlyList<ItemIndexRange> SelectedRangesInternal
	{
		get
		{
			if (m_tpDataSourceAsSelectionInfo is not null)
			{
				return m_tpDataSourceAsSelectionInfo.GetSelectedRanges();
			}
			else
			{
				TrackerView<ItemIndexRange> view = new();
				view.SetCollection(m_tpSelectedRangesImpl);
				return view;
			}
		}
	}

	private void OnSelectedItemsCollectionChanged(
		IObservableVector<object?> pSender,
		IVectorChangedEventArgs e)
	{
		SelectionChanger? pSelectionChanger = null;

		if (!IsSelectionReentrancyAllowed() || IsInit() || m_tpDataSourceAsSelectionInfo is not null)
		{
			return;
		}

		var canSelectMultiple = CanSelectMultiple;

		// TODO: Report Error Resx.Selector_CannotModifySelectedItems
		// Bug#95997
		if (!canSelectMultiple)
		{
			throw new InvalidOperationException();
		}

		var action = e.CollectionChange;

		try
		{
			pSelectionChanger = BeginChange();

			uint index;
			uint itemIndex;
			object? spSelectedItem;
			object? spReplacedItem;
			ItemCollection spItems;
			int foundIndex;

			switch (action)
			{
				case CollectionChange.Reset:
					{
						var selectedIndex = SelectedIndex;
						SelectJustThisItem(selectedIndex, -1, pSelectionChanger, null, out _);
						break;
					}

				case CollectionChange.ItemInserted:
					{
						index = e.Index;
						spSelectedItem = m_tpSelectedItemsImpl[(int)index];
						spItems = Items;
						foundIndex = spItems.IndexOf(spSelectedItem!);

						// we have to start the selection process even if new item is not in Items
						// at the end "non-selectable" item will be removed from SelectedItems collection.
						if (foundIndex >= 0)
						{
							pSelectionChanger.Select(foundIndex, spSelectedItem, canSelectMultiple);
						}
						break;
					}

				case CollectionChange.ItemRemoved:
					{
						index = e.Index;
						spReplacedItem = m_selection.GetAt(index);
						itemIndex = m_selection.GetIndexAt(index);

						pSelectionChanger.Unselect(itemIndex, spReplacedItem);
						break;
					}

				case CollectionChange.ItemChanged:
					{
						index = e.Index;
						spReplacedItem = m_selection.GetAt(index);
						itemIndex = m_selection.GetIndexAt(index);

						pSelectionChanger.Unselect(itemIndex, spReplacedItem);

						spSelectedItem = m_tpSelectedItemsImpl[(int)index];
						spItems = Items;
						foundIndex = spItems.IndexOf(spSelectedItem!);
						if (foundIndex >= 0)
						{
							pSelectionChanger.Select(foundIndex, spSelectedItem, canSelectMultiple);
						}
						break;
					}
			}

			EndChange(pSelectionChanger);
			pSelectionChanger = null;
		}
		finally
		{
			pSelectionChanger?.Cancel();
		}
	}

	internal void InvokeSelectionChanged(
		IList<object?>? pUnselectedItems,
		IList<object?>? pSelectedItems)
	{
		// Create the args
		SelectionChangedEventArgs spArgs = new(this, pUnselectedItems!, pSelectedItems!);

		OnSelectionChanged(spArgs);

#if !HAS_UNO // TODO Uno: SelectorAutomationPeer has no RaiseSelectionEvents.
		var bAutomationListener = AutomationPeer.ListenerExistsHelper(AutomationEvents.SelectionPatternOnInvalidated);
		if (!bAutomationListener)
		{
			bAutomationListener = AutomationPeer.ListenerExistsHelper(AutomationEvents.SelectionItemPatternOnElementSelected);
		}
		if (!bAutomationListener)
		{
			bAutomationListener = AutomationPeer.ListenerExistsHelper(AutomationEvents.SelectionItemPatternOnElementAddedToSelection);
		}
		if (!bAutomationListener)
		{
			bAutomationListener = AutomationPeer.ListenerExistsHelper(AutomationEvents.SelectionItemPatternOnElementRemovedFromSelection);
		}
		if (bAutomationListener)
		{
			var spAutomationPeer = GetOrCreateAutomationPeer();
			if (spAutomationPeer is not null)
			{
				((SelectorAutomationPeer)spAutomationPeer).RaiseSelectionEvents(spArgs);
			}
		}
#endif
	}

	private protected virtual void OnSelectionChanged(SelectionChangedEventArgs pSelectionChangedEventArgs) =>
		RaiseSelectionChanged(pSelectionChangedEventArgs);

	private protected virtual void OnSelectionChanged(
		int oldSelectedIndex,
		int newSelectedIndex,
		object? pOldSelectedItem,
		object? pNewSelectedItem,
		bool animateIfBringIntoView = false,
		FocusNavigationDirection focusNavigationDirection = FocusNavigationDirection.None)
	{
		if (newSelectedIndex != -1)
		{
			// Only change the focus if there is a selected item.
			// Use InputActivationBehavior::NoActivate because just changing selected item by default shouldn't steal activation from another window/island.
			SetFocusedItem(newSelectedIndex, true /*shouldScrollIntoView*/, animateIfBringIntoView, focusNavigationDirection, Uno.UI.Xaml.Input.InputActivationBehavior.NoActivate);
		}
	}

	// Raise SelectionChanged event
	private void RaiseSelectionChanged(SelectionChangedEventArgs pSelectionChangedEventArgs) =>
		SelectionChanged?.Invoke(this, pSelectionChangedEventArgs);

	// Handler for when the Items collection is changed
	// TODO Uno: Selector.cs drives this per item from OnItemsSourceSingleCollectionChanged. The items host is told through
	// notifyItemsHost, in one call: WinUI's ItemsControl::NotifyOfSourceChanged only sends the NotifyOfItemsChanging half.
	private void NotifyOfSourceChanged(
		IVectorChangedEventArgs e,
		Action notifyItemsHost)
	{
		SelectionChanger? pSelectionChanger = null;

		// If a collection change lead to a selection change, we should not bring into view the selected item.
		m_inCollectionChange = true;

		try
		{
			var action = e.CollectionChange;

#if !HAS_UNO // TODO Uno: there is no parent selector to clear, SelectorItem resolves it from its ItemsControl.
			// in case of Remove we can show animation where container will be around and uncleared.
			// all pointer actions shouldn't go to its parent selector. Clear it here instead of in ClearContainerForItem.
			if (action == CollectionChange.ItemRemoved)
			{
				var itemIndex = e.Index;
				var spContainer = ContainerFromIndex((int)itemIndex);

				if (spContainer is not null)
				{
					((SelectorItem)spContainer).SetParentSelector(null);
				}
			}

			if (action != CollectionChange.Reset)
			{
				// to limit impact, for all but reset change notifications do not change invocation order
				base.NotifyOfSourceChanged(pSender, e);
			}
#endif

			pSelectionChanger = BeginChange();

			switch (action)
			{
				case CollectionChange.Reset:
					{
						var arePropertyValuesEqual = false;
						DependencyObject? spFocusedContainer = null;

						if (GetFocusedIndex() > -1)
						{
							// before we tell the panel about the reset, let's get the focused container
							spFocusedContainer = ContainerFromIndex(GetFocusedIndex());
							// set m_focusedIndex to -1 so that we don't get confused during prepare
							SetFocusedIndex(-1); // notice: in a reset, the index is not guaranteed anymore
						}

						var nSelectedCount = m_selection.GetNumItemsSelected();
						var spItems = Items;
						var nNewItemCount = spItems.Size;

						for (uint i = 0; i < nSelectedCount; ++i)
						{
							var spItem = m_selection.GetAt(i);
							var itemIndex = m_selection.GetIndexAt(i);
							arePropertyValuesEqual = false;
							if (itemIndex < nNewItemCount)
							{
								var spNewItem = spItems[(int)itemIndex];
								arePropertyValuesEqual = PropertyValue.AreEqualImpl(spItem, spNewItem);
							}

							if (!arePropertyValuesEqual) // Only unselect if the values are not equal.
							{
								pSelectionChanger.Unselect(itemIndex, spItem);
							}
						}

						SelectAllSelectedSelectorItems(pSelectionChanger);
						EndChange(pSelectionChanger);
						pSelectionChanger = null;

						// for reset, call into super-class after selection was updated.
						// modern panels need to have selection in sync with collection, so selection is updated above
						notifyItemsHost();

						// focus is interesting. There are resets that are benign. They recreate everything and we can still maintain focus
						var shouldResetFocus = true;
						if (spFocusedContainer is not null)
						{
							// we used to have focus, maybe after this reset we can restore it somehow?
							var newFocusedIndex = IndexFromContainer(spFocusedContainer);
							if (newFocusedIndex > -1)
							{
								// well, the old container still has focus, it just might have a different index
								SetFocusedIndex(newFocusedIndex);
								shouldResetFocus = false;
							}
						}
						if (shouldResetFocus)
						{
							SetFocusedItem(-1, false /*shouldScrollIntoView*/);
						}
					}
					break;

				case CollectionChange.ItemInserted:
					{
						var isSelected = false;
						var focusedIndex = GetFocusedIndex();

						var itemIndex = e.Index;

						// An item was inserted before the focused item, we need
						// to update our focused item's index to reflect that.
						if (focusedIndex >= (int)itemIndex)
						{
							global::System.Diagnostics.Debug.Assert(focusedIndex != -1);

							focusedIndex++;
							SetFocusedIndex(focusedIndex);
							SetLastFocusedIndex((uint)focusedIndex);
						}

						m_selection.AddSelectedIndex(itemIndex);

						// At the time an item is inserted, the only way for the item to already be
						// IsSelected == true is for the item to implement ISelectorItem itself, as
						// the item doesn't have a container yet.

						var spItems = Items;
						var spItem = spItems[(int)itemIndex];
						var spSelectorItem = spItem as SelectorItem;

						if (spSelectorItem is not null)
						{
							isSelected = spSelectorItem.IsSelected;
							if (isSelected)
							{
								var canSelectMultiple = CanSelectMultiple;
								pSelectionChanger.Select((int)itemIndex, spItem, canSelectMultiple);
							}
						}

						if (isSelected)
						{
							EndChange(pSelectionChanger);
							pSelectionChanger = null;
						}
						else if (!IsSelectionReentrancyAllowed())
						{
							var nSize = spItems.Size;

							// If the item wasn't added at the end, then check if we need to update the
							// existing selection properties.
							if (itemIndex < (nSize - 1))
							{
								var oldSelectedIndex = SelectedIndex;
								if ((int)itemIndex <= oldSelectedIndex && !IsInit())
								{
									var spOldSelectedItem = SelectedItem;
									UpdatePublicSelectionProperties(oldSelectedIndex, oldSelectedIndex + 1, spOldSelectedItem, spOldSelectedItem);
								}
							}
							pSelectionChanger.Cancel();
							pSelectionChanger = null;
						}
					}
					break;

				case CollectionChange.ItemChanged:
					{
						var isSelected = false;
						var itemIndex = e.Index;

						m_selection.Has(itemIndex, out var collectionIndex, out var hasItem);

						object? spItem;
						if (hasItem)
						{
							spItem = m_selection.GetAt(collectionIndex);
							pSelectionChanger.Unselect(itemIndex, spItem);
						}

						var spItems = Items;
						spItem = spItems[(int)itemIndex];

						var spSelectorItem = spItem as SelectorItem;

						if (spSelectorItem is not null)
						{
							isSelected = spSelectorItem.IsSelected;
							if (isSelected)
							{
								var canSelectMultiple = CanSelectMultiple;
								pSelectionChanger.Select((int)itemIndex, spItem, canSelectMultiple);
							}
						}

						if (hasItem || isSelected)
						{
							EndChange(pSelectionChanger);
						}
						else
						{
							pSelectionChanger.Cancel();
						}
						pSelectionChanger = null;
					}
					break;
			}
		}
		finally
		{
			pSelectionChanger?.Cancel();
			m_inCollectionChange = false;
		}
	}

	// Handler for when the Items collection is changed
	private void OnItemsChanged(
		IVectorChangedEventArgs e,
		Action notifyItemsHost)
	{
		SelectionChanger? pSelectionChanger = null;

		// If a collection change lead to a selection change, we should not bring into view the selected item.
		m_inCollectionChange = true;

		try
		{
			var action = e.CollectionChange;
			uint index;

			switch (action)
			{
				case CollectionChange.ItemRemoved:
					{
						index = e.Index;
						// need to cast unsigned to signed
						var itemIndex = (int)index; // Index in Items.

						pSelectionChanger = BeginChange();

						// collectionIndex is the index in selected indexes.
						m_selection.Has((uint)itemIndex, out var collectionIndex, out var hasItem);
						if (hasItem)
						{
							var spItem = m_selection.GetAt(collectionIndex);
							pSelectionChanger.Unselect((uint)itemIndex, spItem);
						}

						m_selection.RemoveSelectedIndex((uint)itemIndex);

						if (hasItem)
						{
							EndChange(pSelectionChanger);
							pSelectionChanger = null;
						}
						else if (pSelectionChanger is not null && !IsSelectionReentrancyAllowed())
						{
							var oldSelectedIndex = SelectedIndex;
							if (itemIndex <= oldSelectedIndex && !IsInit())
							{
								var spOldSelectedItem = SelectedItem;
								UpdatePublicSelectionProperties(oldSelectedIndex, oldSelectedIndex - 1, spOldSelectedItem, spOldSelectedItem);
							}
							pSelectionChanger.Cancel();
							pSelectionChanger = null;
						}
					}
					break;
				case CollectionChange.ItemChanged:
				case CollectionChange.ItemInserted:
					// Check if the newly inserted item is actually a data virtualized
					// placeholder that hasn't been realized yet
					index = e.Index;
					ShowPlaceholderIfVirtualized(index);
					break;

				case CollectionChange.Reset:
					{
						var spCV = UnwrapItemsSource() as ICollectionView;
						if (m_tpMonitoredCV != spCV)
						{
							DisconnectFromMonitoredCV();

							MonitorCV(spCV);

							UpdateIsSynchronized();
						}
					}
					break;
			}

			notifyItemsHost();

			switch (action)
			{
				case CollectionChange.ItemRemoved:
					{
						var itemIndex = e.Index;

						var itemIndexInt = (int)itemIndex;
						var focusedIndex = GetFocusedIndex();

						var items = Items;
						var itemsCount = items.Size;

						// Our focused item was deleted. We will give focus to the item with the same index
						// unless, of course, it's not possible because of the reduced items count.
						// We had to wait until SelectorGenerated::OnItemsChanged to give the items host
						// panel the chance to handle the collection change and update its container mapping.
						// Using InputActivationBehavior::NoActivate instead of the default InputActivationBehavior::RequestActivation
						// to avoid the window/island stealing focus.
						if (focusedIndex == itemIndexInt)
						{
							SetFocusedItem(
								Math.Min((int)itemsCount - 1, itemIndexInt),
								false /*shouldScrollIntoView*/,
								false /*animateIfBringIntoView*/,
								FocusNavigationDirection.None /*focusNavigationDirection*/,
								Uno.UI.Xaml.Input.InputActivationBehavior.NoActivate /*inputActivationBehavior*/);
						}
						// An item was removed before our focused item, we need to update
						// the focused item's index to reflect that.
						else if (focusedIndex > itemIndexInt)
						{
							global::System.Diagnostics.Debug.Assert(focusedIndex > 0);
							focusedIndex--;

							SetFocusedIndex(focusedIndex);
							SetLastFocusedIndex((uint)focusedIndex);
						}
						// If focus moved outside the selector, m_focusedIndex will be -1 and will not match the removed item index.
						// However, m_lastFocusedIndex could end up going outside the data range now - so make sure that we clamp it back.
						// This can happen if there is a context menu on the item, that deletes the item.
						else if (focusedIndex == -1 && itemsCount > 0 && GetLastFocusedIndex() >= itemsCount)
						{
							SetLastFocusedIndex(0);
						}
					}
					break;
			}

			if (m_selectedIndexValueSetBeforeItemsAvailable >= 0)
			{
				// There was a selected index set before Items was available. Now
				// that items is being reset, see if the index is in range and if so
				// set the selected index now.
				var spItems = Items;
				var nCount = spItems.Size;
				if (m_selectedIndexValueSetBeforeItemsAvailable < (int)nCount)
				{
					SelectedIndex = m_selectedIndexValueSetBeforeItemsAvailable;
				}
			}
		}
		finally
		{
			pSelectionChanger?.Cancel();
			m_inCollectionChange = false;
		}
	}

	protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
	{
		var newIndex = -1;
		var hasItem = false;

		if (element is null)
		{
			throw new ArgumentNullException(nameof(element));
		}

		try
		{
			base.PrepareContainerForItemOverride(element, item);

			// TODO Uno: a plain Selector generates ContentPresenter containers, which have no selection state.
			if (element is not SelectorItem pSelectorItemNoRef)
			{
				return;
			}

			// Clear the mouse-over state, as the item may still have this set.
			pSelectorItemNoRef.IsPointerOver = false;

#if !HAS_UNO // TODO Uno: there is no parent selector to store, SelectorItem resolves it from its ItemsControl.
			// Store a weak reference to the parent Selector
			pSelectorItemNoRef.SetParentSelector(this);
#endif

			newIndex = IndexFromContainer(pSelectorItemNoRef);

			if (newIndex != -1)
			{
				// If the ISelectionInfo is implemented by the DataSource,
				// we let the DataSource handle selection
				if (m_tpDataSourceAsSelectionInfo is not null)
				{
					// In this case, we only want to update the SelectorItem.IsSelected property
					// We do not want to update selection (or call any of the selection interfaces to select or deselect)
					// Changing IsSelected will call Selector::NotifyListItemSelected
					// In NotifyListItemSelected, we will not call selection interface functions when the Selection is prevented
					PreventSelectionReentrancy();

					hasItem = m_tpDataSourceAsSelectionInfo.IsSelected(newIndex);
				}
				else
				{
#if HAS_UNO
					hasItem = IsSelected(newIndex);
#else
					m_selection.Has(newIndex, out var collectionIndex, out hasItem);
#endif
				}

				if (hasItem)
				{
					pSelectorItemNoRef.IsSelected = true;
				}
				else
				{
					pSelectorItemNoRef.IsSelected = false;
				}

				// we cannot check for focus on every container here, since that is very expensive
				if (newIndex == GetFocusedIndex())
				{
					var spPanel = ItemsPanelRoot;

					SetFocusedItem(newIndex,
						/*shouldScrollIntoView*/
						spPanel is not VirtualizingPanel
						&& spPanel is not ModernCollectionBasePanel);
				}
			}

			// Check if the SelectorItem is a data virtualized placeholder
			// We do not call ShowPlaceholderIfVirtualized because it queries data we already have and updates the visual state which we do too
			pSelectorItemNoRef.m_isPlaceholder = item is null;

			pSelectorItemNoRef.UpdateVisualState(false);
		}
		finally
		{
			AllowSelectionReentrancy();
		}
	}

	protected override void ClearContainerForItemOverride(DependencyObject element, object item)
	{
		base.ClearContainerForItemOverride(element, item);

		// TODO Uno: a plain Selector generates ContentPresenter containers, which have no selection state.
		if (element is not SelectorItem spSelectorItem)
		{
			return;
		}

		var spStoredItem = item as DependencyObject;

		if (spStoredItem != element)
		{
			var virtualizationInformation = spSelectorItem.GetVirtualizationInformationOrNull();
			var isContainerFromTemplateRoot = virtualizationInformation is not null && virtualizationInformation.IsContainerFromTemplateRoot;

			// Do not clear content if we got the container from ItemTemplate
			if (!isContainerFromTemplateRoot)
			{
				spSelectorItem.ClearValue(ContentControl.ContentProperty);
			}
		}

		// Clear whether or not the SelectorItem is a data virtualized placeholder
		spSelectorItem.m_isPlaceholder = false;
	}

	// Called when a SelectorItem is clicked
	// Allows subclasses (e.g. ListBox) to change behavior
	private protected virtual bool OnSelectorItemClicked(SelectorItem pSelectorItem)
	{
		var bIsSelected = true;

		var pFocused = pSelectorItem.Focus(FocusState.Pointer);

		if (pFocused)
		{
			var nModifiersKey = CoreImports.Input_GetKeyboardModifiers();
			if (VirtualKeyModifiers.Control ==
				(nModifiersKey & VirtualKeyModifiers.Control))
			{
				bIsSelected = pSelectorItem.IsSelected;
				bIsSelected = !bIsSelected;
			}

			pSelectorItem.IsSelected = bIsSelected;
		}

		return pSelectorItem.HasFocus();
	}

	private protected void OnFocusEngaged(
		Control pSender,
		FocusEngagedEventArgs pArgs)
	{
		var hasItems = GetHasItems();

		if (hasItems)
		{
			SetFocusedItem((int)GetLastFocusedIndex(), true /*shouldScrollIntoView*/, true /*forceFocus*/, FocusState.Keyboard);
		}

		pArgs.Handled = true;
	}

	// Called to detect whether we can scroll to the View or not.
	private protected virtual bool CanScrollIntoView()
	{
#if HAS_UNO
		// TODO Uno: FlipView scrolls itself from OnSelectedIndexChanged, after the base call. It raises m_skipScrollIntoView
		// too late for this path: remove once its OnSelectionChanged override is ported.
		if (this is FlipView)
		{
			return false;
		}
#endif

		var isItemsHostInvalid = false;
		var isInLiveTree = false;

		var spPanel = ItemsPanelRoot;
		if (spPanel is not null)
		{
			isItemsHostInvalid = IsItemsHostInvalid;
			if (!isItemsHostInvalid)
			{
				isInLiveTree = IsInLiveTree;
			}
		}

		return !isItemsHostInvalid && isInLiveTree && !m_skipScrollIntoView && !m_inCollectionChange;
	}

	// Handles changing selection properties when a SelectorItem has IsSelected change
	internal void NotifyListItemSelected(
		SelectorItem pSelectorItem,
		bool bIsSelected)
	{
		SelectionChanger? pSelectionChanger = null;

		try
		{
			var newIndex = IndexFromContainer(pSelectorItem);

			if (m_tpDataSourceAsSelectionInfo is not null)
			{
				InvokeDataSourceRangeSelection(bIsSelected /* select */, newIndex, 1);
			}
			else
			{
				if (!IsSelectionReentrancyAllowed())
				{
					return;
				}

				var spItems = Items;
				var nCount = spItems.Size;

				if (newIndex >= 0 && newIndex < (int)nCount)
				{
					var spSelectedItem = ItemFromContainer(pSelectorItem);

					pSelectionChanger = BeginChange();

					if (bIsSelected)
					{
						var canSelectMultiple = CanSelectMultiple;
						pSelectionChanger.Select(newIndex, spSelectedItem, canSelectMultiple);
					}
					else
					{
						pSelectionChanger.Unselect((uint)newIndex, spSelectedItem);
					}

					EndChange(pSelectionChanger);
					pSelectionChanger = null;
				}
			}
		}
		catch (Exception e)
		{
			// returning S_OK to maintain compat with blue - bug 927905
			// In blue we swallowed any exception thrown in the app's
			// SelectionChanged event handler
			if (this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().Error("Failed to process the IsSelected change of a SelectorItem.", e);
			}
		}
		finally
		{
			pSelectionChanger?.Cancel();
		}
	}

	// Requests a SelectionChanger from our Selection instance.
	// This is preferred over requesting the Selection directly.
	private protected SelectionChanger BeginChange() => m_selection.BeginChange();

	// Applies the changes in the given SelectionChanger to this
	// Selector, ensuring the state of public properties.
	private protected void EndChange(
		SelectionChanger pChanger,
		bool animateIfBringIntoView = false,
		FocusNavigationDirection focusNavigationDirection = FocusNavigationDirection.None)
	{
		object? spNewSelectedItem = null;
		var currentSelectedIndex = -1;
		var newSelectedIndex = -1;

		global::System.Diagnostics.Debug.Assert(!IsSelectionReentrancyAllowed(), "Reentrancy lock must be engaged when EndChange is called.");

		if (pChanger is null)
		{
			throw new ArgumentNullException(nameof(pChanger));
		}

		TrackerCollection<object?> spUnselectedItems = new();
		TrackerCollection<object?> spSelectedItems = new();

		var canSelectMultiple = CanSelectMultiple;

		try
		{
			// We need to prevent reentrancy until we finish calling UpdatePublicSelectionProperties.
			PreventSelectionReentrancy();

			pChanger.End(spUnselectedItems, spSelectedItems, canSelectMultiple);

			if (m_pOldSelectedIndexToReport is null)
			{
				currentSelectedIndex = SelectedIndex;
			}

			// Update SelectedItem, SelectedIndex
			var nSelectedCount = m_selection.GetNumItemsSelected();
			if (nSelectedCount > 0)
			{
				spNewSelectedItem = m_selection.GetAt(0);
				var itemIndex = m_selection.GetIndexAt(0);
				newSelectedIndex = (int)itemIndex;
			}

			var spCurrentSelectedItem = SelectedItem;
			UpdatePublicSelectionProperties(m_pOldSelectedIndexToReport ?? currentSelectedIndex, newSelectedIndex, spCurrentSelectedItem, spNewSelectedItem, animateIfBringIntoView, focusNavigationDirection);

			AllowSelectionReentrancy();

			var nUnselectedCount = spUnselectedItems.Count;
			var nSelectedDeltaCount = spSelectedItems.Count;

			var didChange = nUnselectedCount > 0 || nSelectedDeltaCount > 0;
			if (didChange)
			{
				UpdateSelectedItems();
				UpdateSelectedRanges();
				InvokeSelectionChanged(spUnselectedItems, spSelectedItems);
			}

			ClearOldIndexToReport();
		}
		finally
		{
			AllowSelectionReentrancy();
		}
	}

	// Sets the SelectorItem for item to be selected
	private void SetItemIsSelected(
		uint index,
		bool isSelected)
	{
		var spContainer = ContainerFromIndex((int)index);
		var spSelectorItem = spContainer as SelectorItem;

		if (spSelectorItem is not null)
		{
			spSelectorItem.IsSelected = isSelected;
		}
	}

	// Handles selection of single item, and only that item
	internal virtual void MakeSingleSelection(
		uint index,
		bool animateIfBringIntoView,
		object? pSelectedItem,
		FocusNavigationDirection focusNavigationDirection = FocusNavigationDirection.None)
	{
		var selectedIndex = SelectedIndex;

		SelectJustThisItemInternal(selectedIndex, (int)index, pSelectedItem, animateIfBringIntoView, out _, focusNavigationDirection);
	}

	// Raises IsSelected property change Automation event on Item got selected.
	internal void RaiseIsSelectedChangedAutomationEvent(DependencyObject pContainer, bool isSelected)
	{
		var bAutomationListener = AutomationPeer.ListenerExistsHelper(AutomationEvents.PropertyChanged);
		if (bAutomationListener)
		{
			if (pContainer is null)
			{
				throw new ArgumentNullException(nameof(pContainer));
			}

			var spItem = ItemFromContainer(pContainer);
			// We do not create item automation peer when item is null (for cases like placeholder containers)
			if (spItem is not null)
			{
				var isUnsetValue = spItem == DependencyProperty.UnsetValue;
				if (!isUnsetValue)
				{
					var spAutomationPeer = GetOrCreateAutomationPeer();
					if (spAutomationPeer is not null)
					{
						var spItemsControlAutomationPeer = spAutomationPeer as ItemsControlAutomationPeer;
						if (spItemsControlAutomationPeer is not null)
						{
#if !HAS_UNO // TODO Uno: SelectorItem raises the IsSelected change itself, ItemAutomationPeer has no RaiseAutomationIsSelectedChanged.
							var spItemAutomationPeer = spItemsControlAutomationPeer.CreateItemAutomationPeer(spItem);
							if (spItemAutomationPeer is not null)
							{
								spItemAutomationPeer.RaiseAutomationIsSelectedChanged(isSelected);
							}
#endif
						}
					}
				}
			}
		}
	}

	// Updates m_tpSelectedItemsImpl
	private void UpdateSelectedItems()
	{
		// prevent reentrancy or
		// other properties from being updated.
		PreventSelectionReentrancy();
		try
		{
			m_tpSelectedItemsImpl.Clear();
			var nSelectedCount = m_selection.GetNumItemsSelected();
			for (uint i = 0; i < nSelectedCount; ++i)
			{
				var spCurrentItem = m_selection.GetAt(i);
				m_tpSelectedItemsImpl.Add(spCurrentItem);
			}
		}
		finally
		{
			AllowSelectionReentrancy();
		}
	}

	// Updates m_tpSelectedRangesImpl
	private void UpdateSelectedRanges()
	{
		// prevent reentrancy or
		// other properties from being updated.
		PreventSelectionReentrancy();

		try
		{
			var spSelectedRanges = m_tpSelectedRangesImpl;

			spSelectedRanges.Clear();

			var selectedIndexes = m_selection.GetSelectedIndexes();

			// make sure indexes are sorted
			selectedIndexes.Sort();

			// TODO Uno: AppendItemIndexRangesFromSortedVectorToItemIndexRangeCollection is an instance member returning a new list.
			foreach (var range in new ItemIndexRange(0, 0).AppendItemIndexRangesFromSortedVectorToItemIndexRangeCollection(selectedIndexes))
			{
				spSelectedRanges.Add(range);
			}
		}
		finally
		{
			AllowSelectionReentrancy();
		}
	}

	// Updates all properties that are publicly visible after a selection change
	private void UpdatePublicSelectionProperties(
		int oldSelectedIndex,
		int newSelectedIndex,
		object? pOldSelectedItem,
		object? pNewSelectedItem,
		bool animateIfBringIntoView = false,
		FocusNavigationDirection focusNavigationDirection = FocusNavigationDirection.None)
	{
		// if the selection interface (ISelectionInfo) is implemented, we let the developer handle selection with collection changes
		if (m_tpDataSourceAsSelectionInfo is not null)
		{
			return;
		}

		global::System.Diagnostics.Debug.Assert(!IsSelectionReentrancyAllowed(), "Reentrancy lock must be active to call UpdatePublicSelectionProperties");

		bool areEqual;

		// Custom Values have always an index of -1, avoid doing this check to update the selected value correctly.
		if (!AreCustomValuesAllowed())
		{
			if (oldSelectedIndex == newSelectedIndex)
			{
				areEqual = PropertyValue.AreEqualImpl(pOldSelectedItem, pNewSelectedItem);
				if (areEqual)
				{
					// nothing changed.
					return;
				}
			}
		}

		var currentSelectedIndex = SelectedIndex;
		if (newSelectedIndex != currentSelectedIndex)
		{
			SelectedIndex = newSelectedIndex;
		}

		var spCurrentSelectedItem = SelectedItem;
		areEqual = PropertyValue.AreEqualImpl(spCurrentSelectedItem, pNewSelectedItem);
		if (!areEqual)
		{
			SelectedItem = pNewSelectedItem;
		}

		var spInternalSelectedValue = GetSelectedValue(pNewSelectedItem);
		// Don't clear the SelectedValue just because a change to the SelectedValuePath has cleared the selection.
		// This behavior ensures that these two properties can be changed together without the SelectedValue property
		// changing in between the two property sets.
		var spSelectedValue = SelectedValue;
		var areValuesEqual = PropertyValue.AreEqualImpl(spInternalSelectedValue, spSelectedValue);
		if (!areValuesEqual && !m_bSelectionChangeCausedBySelectedValuePathPropertyChange)
		{
			SelectedValue = spInternalSelectedValue;
		}

		// If the ItemCollection contains an ICollectionView sync the selected index
		UpdateCurrentItemInCollectionView(pNewSelectedItem);

		currentSelectedIndex = SelectedIndex;

		spCurrentSelectedItem = SelectedItem;
		OnSelectionChanged(oldSelectedIndex, currentSelectedIndex, pOldSelectedItem, spCurrentSelectedItem, animateIfBringIntoView, focusNavigationDirection);
	}

	// Helper function to select one item in Selector
	private protected void SelectOneItemInternal(
		int index,
		object? item,
		bool clearOldSelection)
	{
		SelectionChanger? pSelectionChanger = null;

		try
		{
			if (m_tpDataSourceAsSelectionInfo is not null)
			{
				if (clearOldSelection)
				{
					InvokeDataSourceClearSelection();
				}

				InvokeDataSourceRangeSelection(true /* select */, index, 1);
			}
			else
			{
				var canSelectMultiple = CanSelectMultiple;

				pSelectionChanger = BeginChange();

				if (clearOldSelection)
				{
					pSelectionChanger.UnselectAll();
				}

				pSelectionChanger.Select(index, item, canSelectMultiple);

				EndChange(pSelectionChanger);
				pSelectionChanger = null;
			}
		}
		finally
		{
			pSelectionChanger?.Cancel();
		}
	}

	// Helper function to select range of items in Selector
	private protected void SelectAllInternal()
	{
		SelectionChanger? pSelectionChanger = null;

		try
		{
			var spItems = Items;
			var itemCount = spItems.Size;

			if (m_tpDataSourceAsSelectionInfo is not null)
			{
				InvokeDataSourceRangeSelection(true /* select */, 0, itemCount);
			}
			else
			{
				var canSelectMultiple = CanSelectMultiple;

				if (!canSelectMultiple)
				{
					throw new NotSupportedException("Multiple selection is not supported in the current selection mode.");
				}
				else
				{
					pSelectionChanger = BeginChange();

					for (uint i = 0; i < itemCount; ++i)
					{
						var spSelectedItem = spItems[(int)i];
						pSelectionChanger.Select((int)i, spSelectedItem, canSelectMultiple);
					}

					EndChange(pSelectionChanger);
					pSelectionChanger = null;
				}
			}
		}
		finally
		{
			pSelectionChanger?.Cancel();
		}
	}

	// Helper function to select range of items in Selector
	private protected void SelectRangeInternal(
		int firstIndex,
		uint length,
		bool clearOldSelection)
	{
		SelectionChanger? pSelectionChanger = null;

		try
		{
			if (m_tpDataSourceAsSelectionInfo is not null)
			{
				InvokeDataSourceRangeSelection(true /* select */, firstIndex, length);
			}
			else
			{
				var canSelectMultiple = CanSelectMultiple;

				if (!canSelectMultiple)
				{
					throw new NotSupportedException("Multiple selection is not supported in the current selection mode.");
				}
				else
				{
					pSelectionChanger = BeginChange();

					if (clearOldSelection)
					{
						pSelectionChanger.UnselectAll();
					}

					var spItems = Items;
					var itemCount = spItems.Size;

					if ((uint)firstIndex < itemCount)
					{
						var modifiedLength = Math.Min(length, itemCount - (uint)firstIndex);

						if (modifiedLength > 0)
						{
							var lastIndex = firstIndex + (int)modifiedLength - 1;

							for (var i = firstIndex; i <= lastIndex; ++i)
							{
								var spSelectedItem = spItems[i];
								pSelectionChanger.Select(i, spSelectedItem, canSelectMultiple);
							}
						}
					}

					EndChange(pSelectionChanger);
					pSelectionChanger = null;
				}
			}
		}
		finally
		{
			pSelectionChanger?.Cancel();
		}
	}

	// Helper function to deselect range of items in Selector
	private protected void DeselectRangeInternal(
		int firstIndex,
		uint length)
	{
		SelectionChanger? pSelectionChanger = null;

		try
		{
			if (m_tpDataSourceAsSelectionInfo is not null)
			{
				InvokeDataSourceRangeSelection(false /* select */, firstIndex, length);
			}
			else
			{
				var spItems = Items;
				var itemCount = spItems.Size;

				if ((uint)firstIndex < itemCount)
				{
					var modifiedLength = Math.Min(length, itemCount - (uint)firstIndex);

					if (modifiedLength > 0)
					{
						var lastIndex = firstIndex + (int)modifiedLength - 1;

						pSelectionChanger = BeginChange();

						for (var i = firstIndex; i <= lastIndex; ++i)
						{
							var spSelectedItem = spItems[i];
							pSelectionChanger.Unselect((uint)i, spSelectedItem);
						}

						EndChange(pSelectionChanger);
						pSelectionChanger = null;
					}
				}
			}
		}
		finally
		{
			pSelectionChanger?.Cancel();
		}
	}

	// Helper function to select range of items in Selector
	private protected void ClearSelection()
	{
		SelectionChanger? pSelectionChanger = null;

		try
		{
			if (m_tpDataSourceAsSelectionInfo is not null)
			{
				var spItems = Items;
				var itemCount = spItems.Size;

				InvokeDataSourceRangeSelection(false /* select */, 0, itemCount);
			}
			else
			{
				pSelectionChanger = BeginChange();

				pSelectionChanger.UnselectAll();

				EndChange(pSelectionChanger);

				pSelectionChanger = null;
			}
		}
		finally
		{
			pSelectionChanger?.Cancel();
		}
	}

	// Ask the CollectionView if it is appropriate to select a single item,
	// then select just that item (in the context of the given SelectionChanger).
	private protected void SelectJustThisItem(
		int oldIndex,
		int newIndex,
		SelectionChanger pChanger,
		object? pSelectedItem,
		out bool pShouldUndoChange)
	{
		var spSelectedItemCalculated = pSelectedItem;

		if (pChanger is null)
		{
			throw new ArgumentNullException(nameof(pChanger));
		}

		pShouldUndoChange = true;

		if (spSelectedItemCalculated is null && newIndex >= 0)
		{
			var spItems = Items;
			spSelectedItemCalculated = spItems[newIndex];
		}

		// First tell the owner that it needs to update its collection view
		var fChangeDone = UpdateCurrentItemInCollectionView(spSelectedItemCalculated);
		if (!fChangeDone)
		{
			// The change was cancelled by the CV nothing else to do here
			return;
		}

		pChanger.UnselectAll();

		// Custom values have index -1
		if (newIndex >= 0 || (AreCustomValuesAllowed() && spSelectedItemCalculated is not null))
		{
			var canSelectMultiple = CanSelectMultiple;
			pChanger.Select(newIndex, spSelectedItemCalculated, canSelectMultiple);
		}

		ReportOldSelectedIndexAs(oldIndex);

		pShouldUndoChange = false;
	}

	// Set the SelectorItem at index to be focused.
	// If HasFocus() returns true, we propagate previous focused item's FocusState to the new focused item.
	private protected virtual void SetFocusedItem(
		int index,
		bool shouldScrollIntoView,
		bool animateIfBringIntoView = false,
		FocusNavigationDirection focusNavigationDirection = FocusNavigationDirection.None,
		Uno.UI.Xaml.Input.InputActivationBehavior inputActivationBehavior = Uno.UI.Xaml.Input.InputActivationBehavior.RequestActivation) // default to request activation to match legacy behavior
	{
		var focusState = FocusState.Programmatic;

		var hasFocus = HasFocus();
		if (hasFocus)
		{
			var spFocused = this.GetFocusedElement();
			var spFocusedAsElement = spFocused as UIElement;
			if (spFocusedAsElement is not null)
			{
				focusState = spFocusedAsElement.FocusState;
				global::System.Diagnostics.Debug.Assert(FocusState.Unfocused != focusState, "FocusState_Unfocused unexpected since spFocusedAsElement is focused");
			}
		}

		SetFocusedItem(index, shouldScrollIntoView, false /*forceFocus*/, focusState, animateIfBringIntoView, focusNavigationDirection, inputActivationBehavior);
	}

	// Set the SelectorItem at index to be focused using an explicit FocusState
	// The focus is only set if forceFocus is TRUE or the Selector already has focus.
	// ScrollIntoView is always called if shouldScrollIntoView is set (regardless of focus).
	private protected virtual void SetFocusedItem(
		int index,
		bool shouldScrollIntoView,
		bool forceFocus,
		FocusState focusState,
		bool animateIfBringIntoView = false,
		FocusNavigationDirection focusNavigationDirection = FocusNavigationDirection.None,
		Uno.UI.Xaml.Input.InputActivationBehavior inputActivationBehavior = Uno.UI.Xaml.Input.InputActivationBehavior.RequestActivation) // default to request activation to match legacy behavior
	{
		bool shouldFocus;

		var spItems = Items;
		var nCount = spItems.Size;
		if (index < 0 || (int)nCount <= index)
		{
			index = -1;
		}

		if (index >= 0)
		{
			SetLastFocusedIndex((uint)index);
		}

		if (!forceFocus)
		{
			shouldFocus = HasFocus();
		}
		else
		{
			shouldFocus = true;
		}

		if (shouldFocus || GetFocusedIndex() >= (int)nCount)
		{
			// Always update m_focusedIndex when we have focus. Also correct it when the Selector
			// doesn't currently have focus (e.g. the focused container was recycled during an item
			// removal) but m_focusedIndex is stale and points beyond the valid item range.
			// In that case, if current focused index goes out of range, we should set the focused
			// index to value asked by caller. Note that we have already verified that index is valid above.
			SetFocusedIndex(index);
		}

		if (GetFocusedIndex() == -1)
		{
			if (shouldFocus)
			{
				// Since none of our child items have the focus, put the focus back on the main list box.
				//
				// This will happen e.g. when the focused item is being removed but is still in the visual tree at the time of this call.
				// Note that this call may fail e.g. if IsTabStop is FALSE, which is OK; it will just set focus
				// to the next focusable element (or clear focus if none is found).
				Focus(focusState); // FUTURE: should handle inputActivationBehavior
			}
			return;
		}

		if (shouldScrollIntoView)
		{
			shouldScrollIntoView = CanScrollIntoView();
		}

		if (shouldScrollIntoView)
		{
			ScrollIntoView(
				(uint)index,
				false /*isGroupItemIndex*/,
				false /*isHeader*/,
				false /*isFooter*/,
				false /*isFromPublicAPI*/,
				true  /*ensureContainerRealized*/,
				animateIfBringIntoView,
				ScrollIntoViewAlignment.Default);
		}

		if (shouldFocus)
		{
			var spContainer = ContainerFromIndex(index);
			var spSelectorItem = spContainer as SelectorItem;
			if (spSelectorItem is not null)
			{
				spSelectorItem.FocusSelfOrChild(focusState, animateIfBringIntoView, out _, focusNavigationDirection, inputActivationBehavior);
			}
		}
	}

	// Handler for when a SelectorItem received focus
	internal virtual void ItemFocused(SelectorItem pSelectorItem)
	{
		// Set the focused index correctly
		var focusedIndex = IndexFromContainer(pSelectorItem);
		SetFocusedIndex(focusedIndex);

		if (focusedIndex >= 0)
		{
			SetLastFocusedIndex((uint)focusedIndex);
		}
	}

	// Handler for when a SelectorItem lost focus
	internal virtual void ItemUnfocused(SelectorItem pSelectorItem)
	{
		// Set the focused index correctly
		var itemIndex = IndexFromContainer(pSelectorItem);

		if (GetFocusedIndex() == itemIndex)
		{
			SetFocusedIndex(-1);
		}
	}

	// Selects the next item in the list.
	private protected void SelectNext(ref int index)
	{
		var spItems = Items;
		var nCount = spItems.Size;

		if (nCount > 0)
		{
			var internalSelectedIndex = index + 1;
			if (internalSelectedIndex <= (int)nCount - 1)
			{
				SelectItemHelper(ref internalSelectedIndex, +1);
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
		var spItems = Items;
		var nCount = spItems.Size;

		if (nCount > 0)
		{
			var internalSelectedIndex = index - 1;
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
	private void SelectItemHelper(
		ref int index,
		int increment)
	{
		var isFocusable = false;

		var spItems = Items;
		var nCount = spItems.Size;

		for (; index > -1 && index < (int)nCount; index += increment)
		{
			var spItem = spItems[index];
			isFocusable = IsFocusableHelper(spItem);

			if (isFocusable)
			{
				var spContainer = ContainerFromIndex(index);
				isFocusable = IsFocusableHelper(spContainer);

				if (isFocusable)
				{
					break;
				}
			}
		}

		if (!isFocusable)
		{
			// If no focusable item was found, set index to -1 so selection will not be updated.
			index = -1;
		}
	}

	// Get the ItemsHost's orientations. This is a stopgap while
	// StackPanel doesn't implement IOrientedPanel. See IOrientedPanel
	// for more information on what these two orientations mean.
	// If the panel is neither a StackPanel nor a IOrientedPanel,
	// both orientations are returned as Vertical.
	internal void GetItemsHostOrientations(
		out Orientation pPhysicalOrientation,
		out Orientation pLogicalOrientation) =>
		(pPhysicalOrientation, pLogicalOrientation) = GetItemsHostOrientations();

	internal bool IsIndexSelected(int index)
	{
		var result = false;

		if (index >= 0)
		{
			bool isSelected;
			if (m_tpDataSourceAsSelectionInfo is not null)
			{
				isSelected = m_tpDataSourceAsSelectionInfo.IsSelected(index);
			}
			else
			{
				m_selection.Has((uint)index, out _, out isSelected);
			}

			result = isSelected;
		}

		return result;
	}

	// Get the given panel's orientations. This is a stopgap while
	// StackPanel doesn't implement IOrientedPanel. See IOrientedPanel
	// for more information on what these two orientations mean.
	// If the panel is neither a StackPanel nor a IOrientedPanel,
	// both orientations are returned as Vertical.
	private protected void GetPanelOrientations(
		Panel? pPanel,
		out Orientation pPhysicalOrientation,
		out Orientation pLogicalOrientation)
	{
		var logicalOrientation = Orientation.Vertical;
		var physicalOrientation = Orientation.Vertical;

		var spStackPanel = pPanel as StackPanel;
		if (spStackPanel is not null)
		{
			logicalOrientation = spStackPanel.Orientation;
			physicalOrientation = logicalOrientation;
		}
		else
		{
			var spOrientedPanel = pPanel as IOrientedPanel;
			if (spOrientedPanel is not null)
			{
				physicalOrientation = spOrientedPanel.PhysicalOrientation;
				logicalOrientation = spOrientedPanel.LogicalOrientation;
			}
		}

		// TODO: Add support for arbitrary panels to provide their Orientation

		pPhysicalOrientation = physicalOrientation;
		pLogicalOrientation = logicalOrientation;
	}

	// Indicate whether the specified item is currently visible.
	internal void IsOnCurrentPage(
		int index,
		bool isGroupItemIndex,
		bool allowPartialVisibility,
		out Rect pScrollHostRect,
		out Rect pContainerRect,
		out bool isOnCurrentPage)
	{
		pScrollHostRect = default;
		pContainerRect = default;

		DependencyObject? spDOContainer;
		if (isGroupItemIndex)
		{
			spDOContainer = HeaderFromIndex(index);
		}
		else
		{
			spDOContainer = ContainerFromIndex(index);
		}

		if (spDOContainer is null)
		{
			// container is not realized
			isOnCurrentPage = false;
			return;
		}

		var spContainer = (FrameworkElement)spDOContainer;
		IsOnCurrentPage(spContainer, allowPartialVisibility, out pScrollHostRect, out pContainerRect, out isOnCurrentPage);
	}

	// Indicate whether the specified item is currently visible.
	internal void IsOnCurrentPage(
		FrameworkElement? pContainer,
		bool allowPartialVisibility,
		out Rect pScrollHostRect,
		out Rect pContainerRect,
		out bool isOnCurrentPage)
	{
		Rect scrollHostRect = default;
		Rect containerRect = default;
		Point containerCornerPoint = default;
		Point containerSizePoint = default;
		isOnCurrentPage = true;

		pScrollHostRect = default;
		pContainerRect = default;

		if (m_tpScrollViewer is null)
		{
			return;
		}

		FrameworkElement? spScrollHost = m_tpScrollViewer.Presenter;
		if (spScrollHost is null)
		{
			spScrollHost = m_tpScrollViewer;
		}

		var actualWidth = spScrollHost.ActualWidth;
		var actualHeight = spScrollHost.ActualHeight;

		scrollHostRect.X = scrollHostRect.Y = 0;
		scrollHostRect.Width = (float)actualWidth;
		scrollHostRect.Height = (float)actualHeight;

		pScrollHostRect = scrollHostRect;

		if (pContainer is null)
		{
			// container is not realized
			isOnCurrentPage = false;
			return;
		}

		if (!pContainer.IsInLiveTree)
		{
			// container is not in live tree
			isOnCurrentPage = false;
			return;
		}

		var spTransform = pContainer.TransformToVisual(spScrollHost);

		actualWidth = pContainer.ActualWidth;
		actualHeight = pContainer.ActualHeight;

		if (pContainer.IsMeasureDirty ||
			actualWidth == 0 && actualHeight == 0)
		{
			// container has not been measured or was measured while outside the visible area, this can lead us to believe the container
			// is on current page when it is not. Set isOnCurrentPage to false to ensure we try to scroll if necessary.
			isOnCurrentPage = false;
			return;
		}

		containerSizePoint.X = (float)actualWidth;
		containerSizePoint.Y = (float)actualHeight;

		// Get relative Rect for Container
		var containerCornerTransformedPoint = spTransform.TransformPoint(containerCornerPoint);
		var containerSizeTransformedPoint = spTransform.TransformPoint(containerSizePoint);

		containerRect.X = (float)DoubleUtil.Min(containerCornerTransformedPoint.X, containerSizeTransformedPoint.X);
		containerRect.Y = (float)DoubleUtil.Min(containerCornerTransformedPoint.Y, containerSizeTransformedPoint.Y);

		containerRect.Width = (float)DoubleUtil.Max(DoubleUtil.Max(containerCornerTransformedPoint.X, containerSizeTransformedPoint.X) - containerRect.X, 0);
		containerRect.Height = (float)DoubleUtil.Max(DoubleUtil.Max(containerCornerTransformedPoint.Y, containerSizeTransformedPoint.Y) - containerRect.Y, 0);

		GetItemsHostOrientations(out var physicalOrientation, out _ /*pLogicalOrientation*/);

		// Return result
		if (allowPartialVisibility)
		{
			// A partially visible item on the page is considered to be on it.
			if (physicalOrientation == Orientation.Vertical)
			{
				isOnCurrentPage = !(containerRect.Y >= scrollHostRect.Y + scrollHostRect.Height) &&
					!(scrollHostRect.Y >= containerRect.Y + containerRect.Height);
			}
			else
			{
				isOnCurrentPage = !(containerRect.X >= scrollHostRect.X + scrollHostRect.Width) &&
					!(scrollHostRect.X >= containerRect.X + containerRect.Width);
			}
		}
		else
		{
			// Check for total visibility on the page.
			isOnCurrentPage = physicalOrientation == Orientation.Vertical ?
				(scrollHostRect.Y <= containerRect.Y) && (containerRect.Y + containerRect.Height <= scrollHostRect.Y + scrollHostRect.Height) :
			(scrollHostRect.X <= containerRect.X) && (containerRect.X + containerRect.Width <= scrollHostRect.X + scrollHostRect.Width);
		}

		pContainerRect = containerRect;
	}

	// Get the first visible item. If possible, return an item that's completely visible on the page.
	// If no item is completely contained on the page, get the first item that is partially visible in the search
	// direction.
	private void GetFirstItemOnCurrentPage(
		ref int startingIndex,
		bool forward)
	{
		var delta = forward ? 1 : -1;
		var probeIndex = startingIndex;
		var isOnCurrentPage = false;

		var spItems = Items;
		var nCount = spItems.Size;

		// Scan looking for the first visible element. Allow partial visibility here,
		// we want to know when we've hit the page.
		while ((0 <= probeIndex) && (probeIndex < (int)nCount))
		{
			IsOnCurrentPage(
				probeIndex,
				false, // isGroupItemIndex
				true, // allowPartialVisibility
				out _, // pScrollHostRect
				out _, // pContainerRect
				out isOnCurrentPage);
			if (isOnCurrentPage)
			{
				break;
			}

			startingIndex = probeIndex;
			probeIndex += delta;
		}

		if (isOnCurrentPage)
		{
			// This match on the current page may be a partial match. Advance and check if there's at least one full match after it.
			isOnCurrentPage = false;
			startingIndex = probeIndex;
			probeIndex += delta;
			if ((0 <= probeIndex) && (probeIndex < (int)nCount))
			{
				IsOnCurrentPage(
					probeIndex,
					false, // isGroupItemIndex
					false, // allowPartialVisibility
					out _,  // pScrollHostRect
					out _,  // pContainerRect
					out isOnCurrentPage);
			}

			// Since there's at least one fully visible, keep searching for more in the search direction. Stop
			// as soon as an item is encountered that is not completely visible, and return the item before it.
			// If no item is completely visible, we won't enter this loop and will just return the first partially
			// visible item found.
			if (isOnCurrentPage)
			{
				startingIndex = probeIndex;
				probeIndex += delta;

				// Then scan looking for the last fully visible element.
				while ((0 <= probeIndex) && (probeIndex < (int)nCount))
				{
					isOnCurrentPage = false;

					IsOnCurrentPage(
						probeIndex,
						false, // isGroupItemIndex
						false, // allowPartialVisibility
						out _,  // pScrollHostRect
						out _,  // pContainerRect
						out isOnCurrentPage);

					if (!isOnCurrentPage)
					{
						break;
					}

					startingIndex = probeIndex;
					probeIndex += delta;
				}
			}
		}
	}

	// Move the focus forward/backward one page.
	private int NavigateByPage(bool forward)
	{
		object? spItem = null;

		var newFocusedIndex = GetFocusedIndex();
		var spItems = Items;
		if (newFocusedIndex >= 0)
		{
			spItem = spItems[newFocusedIndex];
		}

		// Get it visible to start with
		if (spItem is not null)
		{
			IsOnCurrentPage(
				newFocusedIndex,
				false, // isGroupItemIndex
				true, // allowPartialVisibility - in this case, we will not scroll selected item into view if it's already partially visible, it does not have to completely visible.
				out _, // pScrollHostRect
				out _, // pContainerRect
				out var isOnCurrentPage);
			if (!isOnCurrentPage)
			{
				ScrollIntoView(
					(uint)newFocusedIndex,
					false /*isGroupItemIndex*/,
					false /*isHeader*/,
					false /*isFooter*/,
					false /*isFromPublicAPI*/,
					true  /*ensureContainerRealized*/,
					false /*animateIfBringIntoView*/,
					ScrollIntoViewAlignment.Default);

				m_tpScrollViewer?.UpdateLayout();
			}
		}
		// Inlined implementation of NavigateByPageInternal
		if (spItem is null)
		{
			// Select something
			GetFirstItemOnCurrentPage(ref newFocusedIndex, forward);
		}
		else
		{
			GetFirstItemOnCurrentPage(ref newFocusedIndex, forward);
			if (newFocusedIndex == GetFocusedIndex())
			{
				var focusedIndex = newFocusedIndex;
				if (m_tpScrollViewer is not null)
				{
					GetItemsHostOrientations(out var physicalOrientation, out _ /*pLogicalOrientation*/);
					// Scroll a page in the relevant direction
					if (physicalOrientation == Orientation.Vertical)
					{
						if (forward)
						{
							m_tpScrollViewer.PageDown();
						}
						else
						{
							m_tpScrollViewer.PageUp();
						}
					}
					else
					{
						if (forward)
						{
							m_tpScrollViewer.PageRight();
						}
						else
						{
							m_tpScrollViewer.PageLeft();
						}
					}
					m_tpScrollViewer.UpdateLayout();
				}

				// Select the "edge" element
				GetFirstItemOnCurrentPage(ref newFocusedIndex, forward);

				if (newFocusedIndex == focusedIndex)
				{
					var nCount = spItems.Size;

					// Page navigation has to advance by at least 1 item. If items are > control's size,
					// even after a PageUp/Down this may not happen.
					if (forward &&
						nCount > 0 &&
						(newFocusedIndex < (int)(nCount - 1)))
					{
						newFocusedIndex++;
					}
					else if (!forward && newFocusedIndex > 0)
					{
						newFocusedIndex--;
					}
				}
			}
		}

		return newFocusedIndex;
	}

	internal void ScrollIntoViewInternal(
		object? item,
		bool isHeader,
		bool isFooter,
		bool isFromPublicAPI,
		ScrollIntoViewAlignment alignment,
		double offset = 0.0,
		bool animateIfBringIntoView = false)
	{
		if (isHeader || isFooter)
		{
			// Index is not used when isHeader or isFooter is set to true.
			ScrollIntoView(
				1 /*index*/,
				false /*isGroupItemIndex*/,
				isHeader,
				isFooter,
				isFromPublicAPI,
				false /*ensureContainerRealized*/,
				animateIfBringIntoView,
				alignment,
				offset);
		}
		else
		{
			// First search in group items
			GetGroupItemIndex(item, out var index, out var found);
			if (found)
			{
				ScrollIntoView(index,
					true  /*isGroupItemIndex*/,
					false /*isHeader*/,
					false /*isFooter*/,
					isFromPublicAPI,
					false /*ensureContainerRealized*/,
					animateIfBringIntoView,
					alignment,
					offset);
			}
			else
			{
				// if not found, search in the ItemCollection
				var spItems = Items;
				var foundIndex = spItems.IndexOf(item!);
				if (foundIndex >= 0)
				{
					ScrollIntoView((uint)foundIndex,
						false /*isGroupItemIndex*/,
						false /*isHeader*/,
						false /*isFooter*/,
						isFromPublicAPI,
						false /*ensureContainerRealized*/,
						animateIfBringIntoView,
						alignment,
						offset);
				}
			}
		}
	}

	// if isGroupItemIndex is TRUE, the given index is groupItemIndex
	// currentGroupIndex is only valid when isGroupItemIndex and animateIfBringIntoView are TRUE.
	private protected override void ScrollIntoView(
		uint index,
		bool isGroupItemIndex,
		bool isHeader,
		bool isFooter,
		bool isFromPublicAPI,
		bool ensureContainerRealized,
		bool animateIfBringIntoView,
		ScrollIntoViewAlignment alignment,
		double offset = 0.0,
		uint currentGroupIndex = 0)
	{
		Rect scrollHostRect = default;
		Rect containerRect = default;
		var isOnCurrentPage = false;
		var isThinkingInLogicalUnits = false;
		var isVertical = false;

		if (m_tpScrollViewer is null)
		{
			return;
		}

		var spContent = m_tpScrollViewer.Content;

		var spItemsPresenter = spContent as ItemsPresenter;

		if (spItemsPresenter is null)
		{
			return;
		}

		// If we have a ModernCollectionPanel, we're going to delegate the work there
		var spPanel = ItemsPanelRoot;
		var spModernPanel = spPanel as ModernCollectionBasePanel;
		if (spModernPanel is not null && !isHeader && !isFooter)
		{
			// our panel can deal with group headers and items, not with the ListView.Header property
			if (isGroupItemIndex)
			{
				uint neighboringItemIndex = 0;
				if (animateIfBringIntoView)
				{
					neighboringItemIndex = spModernPanel.GetNeighboringItemIndex(currentGroupIndex, index /*newGroupIndex*/);
				}

				spModernPanel.ScrollGroupHeaderIntoView(
					index,
					alignment,
					offset,
					ensureContainerRealized /*forceSynchronous*/,
					animateIfBringIntoView /*animate*/,
					neighboringItemIndex);
			}
			else
			{
				spModernPanel.ScrollItemIntoView(
					index,
					alignment,
					offset,
					ensureContainerRealized /*forceSynchronous*/,
					animateIfBringIntoView /*animate*/);
			}
			return;
		}

		bool reevaluateContainerRect;
		var canReevaluateContainerRect = true;

		do
		{
			reevaluateContainerRect = false;

			// For ScrollIntoView, if the item is not completely on the current page, it's not considered visible. pass allowPartialVisibility = FALSE to
			// IsOnCurrentPage.
			if (isHeader)
			{
				var spHeaderContainer = spItemsPresenter.HeaderContentControl;

				IsOnCurrentPage(spHeaderContainer, /*allowPartialVisibility*/false, out scrollHostRect, out containerRect, out isOnCurrentPage);
			}
			else if (isFooter)
			{
				// Force footer to load.
				spItemsPresenter.LoadFooter(true /* updateLayout */);

				var spFooterContainer = spItemsPresenter.FooterContentControl;

				IsOnCurrentPage(spFooterContainer, /*allowPartialVisibility*/false, out scrollHostRect, out containerRect, out isOnCurrentPage);
			}
			else
			{
				IsOnCurrentPage((int)index, isGroupItemIndex, /*allowPartialVisibility*/false, out scrollHostRect, out containerRect, out isOnCurrentPage);
			}

			if (isOnCurrentPage && ScrollIntoViewAlignment.Default == alignment && offset == 0.0)
			{
				return;
			}

			GetItemsHostOrientations(out var physicalOrientation, out _ /*pLogicalOrientation*/);
			isVertical = physicalOrientation == Orientation.Vertical;

			// special case. If first item is not on current page it might be scrolled by huge header (or header + leading padding).
			// in this case containerRect will have positive offset. But for CarouselPanel we will have item0 after itemN and item0 will have positive offset too.
			// We will ignore ScrollIntoView request for non-public calls when the offset are positive and our ItemsHost panel is not CarouselPanel.
			// we probably need the same special case for the trailing padding.
			if (!isFromPublicAPI && index == 0 &&
				!isOnCurrentPage &&
				ScrollIntoViewAlignment.Default == alignment &&
				spPanel is not CarouselPanel)
			{
				if (isVertical)
				{
					if (containerRect.Y > 0)
					{
						if (canReevaluateContainerRect && spModernPanel is null && containerRect.X == ExtraContainerArrangeOffset)
						{
							// This situation may be encountered when processing the Home key while the ItemsPanel is a VirtualizingStackPanel. Some elements
							// are rendered off-screen temporarily. Forcing a layout and re-evaluating containerRect to avoid failure of the keystroke processing.
							canReevaluateContainerRect = false;
							reevaluateContainerRect = true;
							m_tpScrollViewer.UpdateLayout();
						}
						else
						{
							return;
						}
					}
				}
				else
				{
					if (containerRect.X > 0)
					{
						if (canReevaluateContainerRect && spModernPanel is null && containerRect.Y == ExtraContainerArrangeOffset)
						{
							// This situation may be encountered when processing the Home key while the ItemsPanel is a VirtualizingStackPanel. Some elements
							// are rendered off-screen temporarily. Forcing a layout and re-evaluating containerRect to avoid failure of the keystroke processing.
							canReevaluateContainerRect = false;
							reevaluateContainerRect = true;
							m_tpScrollViewer.UpdateLayout();
						}
						else
						{
							return;
						}
					}
				}
			}
		}
		while (reevaluateContainerRect);

#if !HAS_UNO // TODO Uno: no panel thinks in logical units: VirtualizingStackPanel is not implemented, and modern panels returned above.
		isThinkingInLogicalUnits = VirtualizingStackPanel.GetIsVirtualizing(this) && spPanel is VirtualizingPanel;
#endif

		// offset is not supported for legacy panels.
		global::System.Diagnostics.Debug.Assert(offset == 0.0 || !isThinkingInLogicalUnits);

		// If it is Virtualizing, let it decide the ScrollIntoView
		// Note: we don't support footers for virtualizing panels.
		if (isThinkingInLogicalUnits && !isFooter)
		{
			double pixelOffset = 0;

			if (isVertical)
			{
				// if container exists
				if (containerRect.Y != 0 || containerRect.Height != 0)
				{
					pixelOffset = containerRect.Y + containerRect.Height - scrollHostRect.Height;
				}
			}
			else
			{
				if (containerRect.X != 0 || containerRect.Width != 0)
				{
					pixelOffset = containerRect.X + containerRect.Width - scrollHostRect.Width;
				}
			}

#if !HAS_UNO // TODO Uno: ItemsPresenter has no ScrollIntoView.
			spItemsPresenter.ScrollIntoView(index, isGroupItemIndex, isHeader, ensureContainerRealized /*forceSynchronous*/, pixelOffset, alignment);
#endif
		}
		else if (!isThinkingInLogicalUnits)
		{
			ScrollRectToViewport(containerRect, scrollHostRect, animateIfBringIntoView && spModernPanel is not null /*animateIfBringIntoView*/, alignment, offset, spItemsPresenter);
		}
	}

	private protected void ScrollRectToViewport(
		Rect containerRect,
		Rect scrollHostRect,
		bool animateIfBringIntoView,
		ScrollIntoViewAlignment alignment,
		double offset,
		ItemsPresenter pItemsPresenter)
	{
		GetItemsHostOrientations(out var physicalOrientation, out _ /*pLogicalOrientation*/);

		global::System.Diagnostics.Debug.Assert(pItemsPresenter is not null);

		if (physicalOrientation == Orientation.Vertical)
		{
			// Scroll into view vertically (first make the right bound visible, then the left)
			var verticalOffset = m_tpScrollViewer!.VerticalOffset;

			switch (alignment)
			{
				case ScrollIntoViewAlignment.Leading:
					{
						var viewportWidth = m_tpScrollViewer.ViewportWidth;
						var viewportHeight = m_tpScrollViewer.ViewportHeight;

						Rect futureViewportRect = new(
							/* X = */ 0.0f,
							/* Y = */ (float)(verticalOffset + containerRect.Y),
							/* Width = */ (float)viewportWidth,
							/* Height = */ (float)viewportHeight
						);

						pItemsPresenter!.DelayLoadFooter(futureViewportRect, true /* updateLayout */);
						verticalOffset += containerRect.Y + offset;
						break;
					}
				case ScrollIntoViewAlignment.Default:
				default:
					{
						var verticalDelta = 0.0;
						var scrollHostRectBottom = scrollHostRect.Y + scrollHostRect.Height;
						var containerRectBottom = containerRect.Y + containerRect.Height;
						if (scrollHostRectBottom < containerRectBottom)
						{
							verticalDelta = containerRectBottom - scrollHostRectBottom;
							verticalOffset += verticalDelta;
						}
						if (containerRect.Y - verticalDelta < scrollHostRect.Y)
						{
							verticalOffset -= scrollHostRect.Y - (containerRect.Y - verticalDelta);
						}
						verticalOffset += offset;
						break;
					}
			}

			if (animateIfBringIntoView)
			{
				m_tpScrollViewer.ChangeViewWithOptionalAnimation(
					null,           // horizontalOffset
					verticalOffset, // verticalOffset
					null,           // zoomFactor
					false);         // disableAnimation
			}
			else
			{
				// TODO Uno: ScrollViewer has no ScrollToVerticalOffsetInternal.
				m_tpScrollViewer.ScrollToVerticalOffset(verticalOffset);
			}
		}
		else
		{
			// Scroll into view horizontally (first make the bottom bound visible, then the top)
			var horizontalOffset = m_tpScrollViewer!.HorizontalOffset;

			switch (alignment)
			{
				case ScrollIntoViewAlignment.Leading:
					{
						var viewportWidth = m_tpScrollViewer.ViewportWidth;
						var viewportHeight = m_tpScrollViewer.ViewportHeight;

						Rect futureViewportRect = new(
							/* X = */ (float)(horizontalOffset + containerRect.X),
							/* Y = */ 0.0f,
							/* Width = */ (float)viewportWidth,
							/* Height = */ (float)viewportHeight
						);

						pItemsPresenter!.DelayLoadFooter(futureViewportRect, true /* updateLayout */);
						horizontalOffset += containerRect.X + offset;
						break;
					}
				case ScrollIntoViewAlignment.Default:
				default:
					{
						var horizontalDelta = 0.0;
						var scrollHostRectRight = scrollHostRect.X + scrollHostRect.Width;
						var containerRectRight = containerRect.X + containerRect.Width;
						if (scrollHostRectRight < containerRectRight)
						{
							horizontalDelta = containerRectRight - scrollHostRectRight;
							horizontalOffset += horizontalDelta;
						}
						if (containerRect.X - horizontalDelta < scrollHostRect.X)
						{
							horizontalOffset -= scrollHostRect.X - (containerRect.X - horizontalDelta);
						}
						horizontalOffset += offset;
						break;
					}
			}

			if (animateIfBringIntoView)
			{
				m_tpScrollViewer.ChangeViewWithOptionalAnimation(
					horizontalOffset, // horizontalOffset
					null,             // verticalOffset
					null,             // zoomFactor
					false);           // disableAnimation
			}
			else
			{
				// TODO Uno: ScrollViewer has no ScrollToHorizontalOffsetInternal.
				m_tpScrollViewer.ScrollToHorizontalOffset(horizontalOffset);
			}
		}
	}

	// Call ElementScrollViewer.ScrollInDirection if possible.
	private protected void ElementScrollViewerScrollInDirection(
		VirtualKey key,
		bool animate = false)
	{
		// When moving to C++20 this probably needs to change to the following:
		// if (nullptr != m_tpScrollViewer)
		// There is ambiguity as to which comparison operator should be used and that warning is treated as a break.
		if (null != m_tpScrollViewer)
		{
			if (animate)
			{
				// This is a move request within a header or footer. Only perform an animated move when the hosting panel is a modern panel. Moves from item to item
				// are only animated for modern panels. So for consistency, moves within headers are only animated for modern panels as well.

				var spPanel = ItemsPanelRoot;
				var spModernPanel = spPanel as ModernCollectionBasePanel;

				animate = spModernPanel is not null;
			}

			if (animate)
			{
#if !HAS_UNO // TODO Uno: ScrollViewer has no ScrollInDirection.
				m_tpScrollViewer.ScrollInDirection(key, true /*animate*/);
#endif
			}
			else
			{
				GetItemsHostOrientations(out var physicalOrientation, out _ /*pLogicalOrientation*/);
				var isVertical = physicalOrientation == Orientation.Vertical;

				var direction = FlowDirection;
				var invert = direction == FlowDirection.RightToLeft;

				switch (key)
				{
					case VirtualKey.PageUp:
						if (isVertical)
						{
							m_tpScrollViewer.PageUp();
						}
						else
						{
							if (invert)
							{
								m_tpScrollViewer.PageRight();
							}
							else
							{
								m_tpScrollViewer.PageLeft();
							}
						}
						break;
					case VirtualKey.PageDown:
						if (isVertical)
						{
							m_tpScrollViewer.PageDown();
						}
						else
						{
							if (invert)
							{
								m_tpScrollViewer.PageLeft();
							}
							else
							{
								m_tpScrollViewer.PageRight();
							}
						}
						break;
					case VirtualKey.Home:
						if (isVertical)
						{
							m_tpScrollViewer.HandleVerticalScroll(ScrollEventType.First);
						}
						else
						{
							m_tpScrollViewer.HandleHorizontalScroll(ScrollEventType.First);
						}
						break;
					case VirtualKey.End:
						if (isVertical)
						{
							m_tpScrollViewer.HandleVerticalScroll(ScrollEventType.Last);
						}
						else
						{
							m_tpScrollViewer.HandleHorizontalScroll(ScrollEventType.Last);
						}
						break;
					default:
#if !HAS_UNO // TODO Uno: ScrollViewer has no ScrollInDirection.
						m_tpScrollViewer.ScrollInDirection(key, false /*animate*/);
#endif
						break;
				}
			}
		}
	}

	internal void HandleNavigationKey(
		VirtualKey key,
		bool scrollViewport,
		ref int newFocusedIndex)
	{
		var spItems = Items;
		var nCount = spItems.Size;

		var direction = FlowDirection;
		var bInvertForRTL = direction == FlowDirection.RightToLeft;
		GetItemsHostOrientations(out var physicalOrientation, out _ /*pLogicalOrientation*/);
		var isVertical = physicalOrientation == Orientation.Vertical;
		switch (key)
		{
			case VirtualKey.Left:
				if (isVertical && scrollViewport)
				{
					ElementScrollViewerScrollInDirection(VirtualKey.Left);
				}
				else
				{
					if (bInvertForRTL)
					{
						SelectNext(ref newFocusedIndex);
					}
					else
					{
						SelectPrev(ref newFocusedIndex);
					}

					if (GetFocusedIndex() == newFocusedIndex && scrollViewport)
					{
						ElementScrollViewerScrollInDirection(VirtualKey.Left);
					}
				}
				break;
			case VirtualKey.Up:
				if (!isVertical && scrollViewport)
				{
					ElementScrollViewerScrollInDirection(VirtualKey.Up);
				}
				else
				{
					SelectPrev(ref newFocusedIndex);
				}

				if (GetFocusedIndex() == newFocusedIndex && scrollViewport)
				{
					ElementScrollViewerScrollInDirection(VirtualKey.Up);
				}
				break;
			case VirtualKey.Right:
				if (isVertical && scrollViewport)
				{
					ElementScrollViewerScrollInDirection(VirtualKey.Right);
				}
				else
				{
					if (bInvertForRTL)
					{
						SelectPrev(ref newFocusedIndex);
					}
					else
					{
						SelectNext(ref newFocusedIndex);
					}

					if (GetFocusedIndex() == newFocusedIndex && scrollViewport)
					{
						ElementScrollViewerScrollInDirection(VirtualKey.Right);
					}
				}
				break;
			case VirtualKey.Down:
				if (!isVertical && scrollViewport)
				{
					ElementScrollViewerScrollInDirection(VirtualKey.Down);
				}
				else
				{
					SelectNext(ref newFocusedIndex);
				}

				if (GetFocusedIndex() == newFocusedIndex && scrollViewport)
				{
					ElementScrollViewerScrollInDirection(VirtualKey.Down);
				}
				break;
			case VirtualKey.Home:
				newFocusedIndex = 0;
				break;
			case VirtualKey.End:
				newFocusedIndex = (int)nCount - 1;
				break;
			case VirtualKey.PageUp:
				newFocusedIndex = NavigateByPage(/*forward*/false);
				break;
			case VirtualKey.GamepadLeftTrigger:
				if (isVertical)
				{
					newFocusedIndex = NavigateByPage(/*forward*/false);
				}
				break;
			case VirtualKey.GamepadLeftShoulder:
				if (!isVertical)
				{
					newFocusedIndex = NavigateByPage(/*forward*/false);
				}
				break;
			case VirtualKey.PageDown:
				newFocusedIndex = NavigateByPage(/*forward*/true);
				break;
			case VirtualKey.GamepadRightTrigger:
				if (isVertical)
				{
					newFocusedIndex = NavigateByPage(/*forward*/true);
				}
				break;
			case VirtualKey.GamepadRightShoulder:
				if (!isVertical)
				{
					newFocusedIndex = NavigateByPage(/*forward*/true);
				}
				break;
		}
		newFocusedIndex = Math.Min(newFocusedIndex, (int)nCount - 1);
		newFocusedIndex = Math.Max(newFocusedIndex, -1);
	}

	// Sets the ListBoxItem for item to be selected
	internal void AutomationPeerAddToSelection(
		uint index,
		object? pItem)
	{
		SelectionChanger? pSelectionChanger = null;

		try
		{
			if (m_tpDataSourceAsSelectionInfo is not null)
			{
				InvokeDataSourceRangeSelection(true /* select */, (int)index, 1);
			}
			else
			{
				var canSelectMultiple = CanSelectMultiple;

				// Use a SelectionChanger instance to prevent reentrancy or
				// other properties from being updated.
				pSelectionChanger = BeginChange();

				pSelectionChanger.Select((int)index, pItem, canSelectMultiple);

				EndChange(pSelectionChanger);
				pSelectionChanger = null;
			}
		}
		finally
		{
			pSelectionChanger?.Cancel();
		}
	}

	// Sets the ListBoxItem for item to be selected
	internal void AutomationPeerRemoveFromSelection(
		uint index,
		object? pItem)
	{
		SelectionChanger? pSelectionChanger = null;

		try
		{
			if (m_tpDataSourceAsSelectionInfo is not null)
			{
				InvokeDataSourceRangeSelection(false /* select */, (int)index, 1);
			}
			else
			{
				// Use a SelectionChanger instance to prevent reentrancy or
				// other properties from being updated.
				pSelectionChanger = BeginChange();

				pSelectionChanger.Unselect(index, pItem);

				EndChange(pSelectionChanger);
				pSelectionChanger = null;
			}
		}
		finally
		{
			pSelectionChanger?.Cancel();
		}
	}

	// Checks to see if an item is selected
	internal bool AutomationPeerIsSelected(object? item)
	{
		var isSelected = false;

		if (m_tpDataSourceAsSelectionInfo is not null)
		{
			var spItems = Items;

			var index = spItems.IndexOf(item!);
			if (index >= 0)
			{
				isSelected = m_tpDataSourceAsSelectionInfo.IsSelected(index);
			}
		}
		else
		{
			var selectedItems = SelectedItemsInternal;
			if (selectedItems is not null)
			{
				isSelected = selectedItems.IndexOf(item) >= 0;
			}
		}

		return isSelected;
	}

	// Gets first element that should take focus after Tab.
	// Return NULL to default to FocusManager logic.
	// Selector returns previous focused element.
	internal override DependencyObject? GetFirstFocusableElementOverride()
	{
		if (!m_skipFocusSuggestion)
		{
			// For ListView it is important to check if the focus is coming from outside due to issues in scenarios with header and group header.
			// Since we do not support those scenarios with the rest of the selectors, every time we are asked for first focusable element
			// we can give out the last focused element.
#if HAS_UNO // ContainerFromIndex also returns items that are their own container, realized or not (e.g. in a closed ComboBox popup).
			return ContainerFromIndex((int)GetLastFocusedIndex()) is UIElement { IsInLiveTree: true } container ? container : null;
#else
			return ContainerFromIndex((int)GetLastFocusedIndex());
#endif
		}

		return null;
	}

	// Gets last element that should take focus after Shift+Tab.
	internal override DependencyObject? GetLastFocusableElementOverride() =>
		// Logic for GetFirst/LastFocusable element is invoked on Tab/Shift+Tab. The logic here is the same because
		// we want to go to the currently or previously focused item if nothing within the
		// items is currently focused, otherwise default to FocusManager logic.
		GetFirstFocusableElementOverride();

	// ISupportInitialize
	internal void BeginInit()
	{
		m_pInitializingData = new()
		{
			m_pInitialValue = SelectedValue,
			m_pInitialItem = SelectedItem,
			m_nInitialIndex = SelectedIndex,
		};
	}

	internal void EndInit()
	{
		// You can not call EndInit before (or without first) calling BeginInit.
		var initializingData = m_pInitializingData ?? throw new InvalidOperationException();

		// Now that XAML parsing is over we will reset and play back any interesting property values.
		var finalIndex = SelectedIndex;
		var spFinalItem = SelectedItem;
		var spFinalValue = SelectedValue;

		var selectedCount = m_selection.GetNumItemsSelected();

		if (selectedCount == 0)
		{
			// No items had IsSelected set, so we can play back our values
			var areEqual = PropertyValue.AreEqualImpl(initializingData.m_pInitialValue, spFinalValue);
			if (!areEqual)
			{
				SelectedValue = initializingData.m_pInitialValue;

				m_pInitializingData = null;

				SelectedValue = spFinalValue;
			}
			else if (initializingData.m_nInitialIndex != finalIndex)
			{
				SelectedIndex = initializingData.m_nInitialIndex;

				m_pInitializingData = null;

				SelectedIndex = finalIndex;
			}
			else if (initializingData.m_pInitialItem != spFinalItem)
			{
				SelectedItem = initializingData.m_pInitialItem;

				m_pInitializingData = null;

				SelectedItem = spFinalItem;
			}
		}

		m_pInitializingData = null;
	}

	// SelectionChangeApplier implementation.

	// Calls SetItemIsSelected(TRUE) on the given item.
	void ISelectionChangeApplier.SelectIndex(uint index) => SetItemIsSelected(index, true);

	// Calls SetItemIsSelected(FALSE) on the given item.
	void ISelectionChangeApplier.UnselectIndex(uint index) => SetItemIsSelected(index, false);

	// TODO Uno: SelectorFactory::GetIsSelectionActiveImpl belongs to Selector.GetIsSelectionActive, still a generated stub.

	// TODO Uno: named HasItems upstream, which here would hide the ItemsControl.HasItems property.
	private protected bool GetHasItems()
	{
		var spItemsSource = ItemsSource;
		var spItems = Items;
		var itemCount = spItems.Size;

		return spItemsSource is not null || itemCount > 0;
	}

	// Check whether the item at a given index is a data virtualized placeholder,
	// and update its visuals if that's the case.
	private protected void ShowPlaceholderIfVirtualized(uint index)
	{
		// If the value of the new item is a placeholder, then we want to make sure
		// the SelectorItem will correctly represent it.
		var spItemsSource = ItemsSource;
		if (spItemsSource is not null)
		{
			// Check if the item being changed is a placeholder
			var spContainer = ContainerFromIndex((int)index);
			if (spContainer is not null)
			{
				var spValue = ItemFromContainer(spContainer);

				// NULL is now the placeholder
				var isPlaceholder = spValue is null;

				// Update the SelectorItem with whether or not it's a placeholder
				var spItem = spContainer as SelectorItem;
				if (spItem is not null && spItem.m_isPlaceholder != isPlaceholder)
				{
					spItem.m_isPlaceholder = isPlaceholder;
					spItem.UpdateVisualState(true);
				}
			}
		}
	}

	// Check whether the given SelectorItem is a data virtualized placeholder, and
	// update its visuals if that's the case.
	internal void ShowPlaceholderIfVirtualized(SelectorItem pItem)
	{
		if (pItem is null)
		{
			throw new ArgumentNullException(nameof(pItem));
		}

		// If the value of the new item is a placeholder, then we want to make sure
		// the SelectorItem will correctly represent it.
		var spItemsSource = ItemsSource;
		if (spItemsSource is not null)
		{
			// Check if the item being changed is a placeholder
			var spValue = ItemFromContainer(pItem);

			// note: do not go through get_Content here, that may not be set at all
			// because fast panning feature allows app code to decide whether it ever wants
			// to set the content

			// NULL is now the placeholder
			var isPlaceholder = spValue is null;

			// Update the SelectorItem with whether or not it's a placeholder
			if (pItem.m_isPlaceholder != isPlaceholder)
			{
				pItem.m_isPlaceholder = isPlaceholder;
				pItem.UpdateVisualState(true);
			}
		}
	}

	// CV synchronization methods
	private void DisconnectFromMonitoredCV() => DisconnectFromMonitoredCVCore(m_tpMonitoredCV);

	private void DisconnectFromMonitoredCVCore(object? pMonitoredCV)
	{
		if (m_epCVCurrentChanged is not null)
		{
			if (pMonitoredCV is not null)
			{
				m_epCVCurrentChanged.Dispose();
				m_epCVCurrentChanged = null;
			}
			m_tpMonitoredCV = null;
		}
	}

	private void MonitorCV(ICollectionView? pCV)
	{
		global::System.Diagnostics.Debug.Assert(m_tpMonitoredCV is null);

		if (pCV is null)
		{
			return;
		}

		m_tpMonitoredCV = pCV;

		EventHandler<object> currentChangedHandler = (pSender, pArgs) => OnCurrentChanged();
		pCV.CurrentChanged += currentChangedHandler;
		m_epCVCurrentChanged = Disposable.Create(() => pCV.CurrentChanged -= currentChangedHandler);
	}

	private void OnCurrentChanged()
	{
		// Avoid re-entrancy
		if (m_fUpdatingCurrentItemInCollectionView || !m_fSynchronizeCurrentItem)
		{
			return;
		}

		var pCurrentItem = m_tpMonitoredCV!.CurrentItem;
		var pSelectedItem = SelectedItem;

		var areEqual = PropertyValue.AreEqualImpl(pCurrentItem, pSelectedItem);

		if (!areEqual)
		{
			SelectedItem = pCurrentItem;
		}
	}

	private bool UpdateCurrentItemInCollectionView(object? pItem)
	{
		try
		{
			if (!m_fSynchronizeCurrentItem || m_tpMonitoredCV is null)
			{
				return true;
			}

			// Need to know if the new item is the same as it is already on the
			// collection view, if so we're done
			var pCurrentItem = m_tpMonitoredCV.CurrentItem;
			var areEqual = PropertyValue.AreEqualImpl(pCurrentItem, pItem);
			if (areEqual)
			{
				return true;
			}

			m_fUpdatingCurrentItemInCollectionView = true;

			m_tpMonitoredCV.MoveCurrentTo(pItem);

			// Now that we have modified the collection view, ensure that it accepted the new value
			pCurrentItem = m_tpMonitoredCV.CurrentItem;
			areEqual = PropertyValue.AreEqualImpl(pItem, pCurrentItem);
			return areEqual;
		}
		finally
		{
			m_fUpdatingCurrentItemInCollectionView = false;
		}
	}

	private void UpdateIsSynchronized()
	{
		var spIsSynchronizedReference = IsSynchronizedWithCurrentItem;
		if (spIsSynchronizedReference is not null)
		{
			// We only allow FALSE as the value of this property
			// so we can assume here that it is
			m_fSynchronizeCurrentItem = false;
		}
		else
		{
			var fCanSelectMultiple = CanSelectMultiple;
			m_fSynchronizeCurrentItem = m_tpMonitoredCV is not null && !fCanSelectMultiple;
		}
	}

	// Report the old selected index as the given index.
	private void ReportOldSelectedIndexAs(int index)
	{
		ClearOldIndexToReport();
		m_pOldSelectedIndexToReport = index;
	}

	// Stop reporting the old selected index.
	private void ClearOldIndexToReport() => m_pOldSelectedIndexToReport = null;

	internal static bool IsNavigationKey(VirtualKey key) =>
		VirtualKey.Left == key ||
		VirtualKey.Up == key ||
		VirtualKey.Right == key ||
		VirtualKey.Down == key ||
		VirtualKey.Home == key ||
		VirtualKey.End == key ||
		VirtualKey.PageUp == key ||
		VirtualKey.PageDown == key;

	internal void DataSourceGetIsSelected(
		SelectorItem pSelectorItem,
		out bool pIsSelected,
		out bool pIsValueSet)
	{
		pIsSelected = false;
		pIsValueSet = false;

		if (m_tpDataSourceAsSelectionInfo is not null)
		{
			var itemIndex = IndexFromContainer(pSelectorItem);

			pIsSelected = m_tpDataSourceAsSelectionInfo.IsSelected(itemIndex);

			pIsValueSet = true;
		}
	}

	private void SelectJustThisItemInternal(
		int oldIndex,
		int newIndex,
		object? pSelectedItem,
		bool animateIfBringIntoView,
		out bool pShouldUndoChange,
		FocusNavigationDirection focusNavigationDirection = FocusNavigationDirection.None)
	{
		SelectionChanger? pSelectionChanger = null;

		pShouldUndoChange = false;

		try
		{
			if (m_tpDataSourceAsSelectionInfo is not null)
			{
				InvokeDataSourceClearSelection();
				InvokeDataSourceRangeSelection(true /* select */, newIndex, 1);
			}
			else
			{
				pSelectionChanger = BeginChange();

				SelectJustThisItem(oldIndex, newIndex, pSelectionChanger, pSelectedItem, out pShouldUndoChange);

				EndChange(pSelectionChanger, animateIfBringIntoView, focusNavigationDirection);
				pSelectionChanger = null;
			}
		}
		finally
		{
			pSelectionChanger?.Cancel();
		}
	}

	private protected ISelectionInfo? GetDataSourceAsSelectionInfo() => m_tpDataSourceAsSelectionInfo;

	private protected void SetDataSourceAsSelectionInfo(ISelectionInfo? pDataSourceAsSelectionInfo) =>
		m_tpDataSourceAsSelectionInfo = pDataSourceAsSelectionInfo;

	// Used to inform the data source of a range selection/deselection
	// bool select indicates whether it's a select or deselect
	private void InvokeDataSourceRangeSelection(
		bool select,
		int firstIndex,
		uint length)
	{
		// Before the end of the function, we call UpdateVisibleAndCachedItemsSelectionAndVisualState
		// This function updates the IsSelected property of the SelectorItem
		// That triggers OnPropertyChanged2
		// OnPropertyChanged2 triggers OnIsSelectedChanged
		// OnIsSelectedChanged triggers NotifyListIsItemSelected
		// NotifyListIsItemSelected triggers InvokeDataSourceRangeSelection again
		if (!IsSelectionReentrancyAllowed())
		{
			return;
		}

		PreventSelectionReentrancy();

		try
		{
			// when SelectedIndex < 0, it means we have no selection (cleared selection)
			// SelectJustThisItemInternal handles the call to invoke clear selection for ISelectionInfo interfaces
			if (firstIndex >= 0)
			{
				ItemIndexRange spItemIndexRange = new(firstIndex, length);

				if (select)
				{
					m_tpDataSourceAsSelectionInfo!.SelectRange(spItemIndexRange);
				}
				else
				{
					m_tpDataSourceAsSelectionInfo!.DeselectRange(spItemIndexRange);
				}
			}

			// call the Selector::InvokeSelectionChanged with AddedItems and RemovedItems being null
			// when the SelectionInterface is implemented, we let the developer handle SelectedItems and SelectedRanges
			// in here, we simply invoke a selection changed event
			InvokeSelectionChanged(null /* pUnselectedItems */, null /* pSelectedItems */);

			// updates SelectedIndex
			UpdatePublicSelectionPropertiesAfterDataSourceSelectionInfo();

			// updates the selection and visual state of visible and cached items
			UpdateVisibleAndCachedItemsSelectionAndVisualState(true /* updateIsSelected */);
		}
		finally
		{
			AllowSelectionReentrancy();
		}
	}

	// helper function to invoke a DeselectAll for the data source without invoking a SelectionChangedEvent
	private void InvokeDataSourceClearSelection()
	{
		var spSelectedRanges = m_tpDataSourceAsSelectionInfo!.GetSelectedRanges();

		var selectedRangesCount = spSelectedRanges.Count;

		for (var i = 0; i < selectedRangesCount; ++i)
		{
			var spItemIndexRange = spSelectedRanges[i];

			m_tpDataSourceAsSelectionInfo.DeselectRange(spItemIndexRange);
		}
	}

	// updates SelectedIndex, SelectedItem and SelectedValue after a selection using SelectionInfo occurs
	private void UpdatePublicSelectionPropertiesAfterDataSourceSelectionInfo()
	{
		var selectedPropertiesUpdated = false;

		var spSelectedRanges = m_tpDataSourceAsSelectionInfo!.GetSelectedRanges();

		var selectedRangesCount = spSelectedRanges.Count;

		if (selectedRangesCount > 0)
		{
			var spItems = Items;
			var itemsCount = spItems.Size;

			if (itemsCount > 0)
			{
				var spFirstRange = spSelectedRanges[0];

				// update SelectedIndex
				var currentSelectedIndex = SelectedIndex;
				var newSelectedIndex = spFirstRange.FirstIndex;
				if (currentSelectedIndex != newSelectedIndex)
				{
					SelectedIndex = newSelectedIndex;
				}

				if (newSelectedIndex >= 0 && (uint)newSelectedIndex < itemsCount)
				{
					// update SelectedItem
					var spCurrentSelectedItem = SelectedItem;
					var spNewSelectedItem = spItems[newSelectedIndex];
					var areEqual = PropertyValue.AreEqualImpl(spCurrentSelectedItem, spNewSelectedItem);
					if (!areEqual)
					{
						SelectedItem = spNewSelectedItem;

						// update SelectedValue
						var spCurrentSelectedValue = SelectedValue;
						var spNewSelectedValue = GetSelectedValue(spNewSelectedItem);
						areEqual = PropertyValue.AreEqualImpl(spCurrentSelectedValue, spNewSelectedValue);
						if (!areEqual)
						{
							SelectedValue = spNewSelectedValue;
						}
					}

					// If the ItemCollection contains an ICollectionView sync the selected index
					UpdateCurrentItemInCollectionView(spNewSelectedItem);

					newSelectedIndex = SelectedIndex;

					spNewSelectedItem = SelectedItem;
					OnSelectionChanged(currentSelectedIndex, newSelectedIndex, spCurrentSelectedItem, spNewSelectedItem);

					selectedPropertiesUpdated = true;
				}
			}
		}

		if (!selectedPropertiesUpdated)
		{
			SelectedIndex = -1;
			SelectedItem = null;
			SelectedValue = null;
		}
	}

	// goes through the visible and cached items and updates their IsSelected property and their visual state
	private protected void UpdateVisibleAndCachedItemsSelectionAndVisualState(bool updateIsSelected)
	{
		var spItemsPanelRoot = ItemsPanelRoot;
		if (spItemsPanelRoot is not null)
		{
			var spIModernCollectionBasePanel = spItemsPanelRoot as ModernCollectionBasePanel;
			if (spIModernCollectionBasePanel is not null)
			{
				// get the data from the panel
				var firstCachedIndex = spIModernCollectionBasePanel.FirstCacheIndexBase;
				var lastCachedIndex = spIModernCollectionBasePanel.LastCacheIndexBase;
				var pinnedElementsIndices = spIModernCollectionBasePanel.GetPinnedElementsIndexVector(ElementType.ItemContainer);

				void updateVisualState(int itemIndex)
				{
					var spContainer = ContainerFromIndex(itemIndex);
					if (spContainer is not null)
					{
						var spISelectorItem = spContainer as SelectorItem;
						if (spISelectorItem is not null)
						{
							var pSelectorItem = spISelectorItem;

							// querying for the selection state from the ISelectionInfo interface
							if (updateIsSelected && m_tpDataSourceAsSelectionInfo is not null)
							{
								var isSelected = m_tpDataSourceAsSelectionInfo.IsSelected(itemIndex);

								// this will call ChangeVisualState internally so there is no point calling the below ChangeVisualState
								pSelectorItem.IsSelected = isSelected;
							}
							else
							{
								pSelectorItem.UpdateVisualState(true /* bUseTransitions */);
							}
						}
					}
				}

				for (var i = firstCachedIndex; i <= lastCachedIndex; ++i)
				{
					updateVisualState(i);
				}

				foreach (int pinnedIndex in pinnedElementsIndices)
				{
					// Ignore containers for which we already updated the visual state.
					if (pinnedIndex < firstCachedIndex || pinnedIndex > lastCachedIndex)
					{
						updateVisualState(pinnedIndex);
					}
				}
			}
		}
	}

	internal virtual bool IsSelectionPatternApplicable() => true;

	internal void SetAllowCustomValues(bool allow) => m_customValuesAllowed = allow;
}

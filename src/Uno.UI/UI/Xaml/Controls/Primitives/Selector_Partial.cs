// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DirectUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml.Data;
using Uno.Disposables;
using Microsoft.UI.Xaml.Input;

namespace Microsoft.UI.Xaml.Controls.Primitives;

partial class Selector
{
	private bool m_isSelectionReentrancyLocked;

	private ISelectionInfo m_tpDataSourceAsSelectionInfo;

	internal ISelectionInfo DataSourceAsSelectionInfo
	{
		get => m_tpDataSourceAsSelectionInfo;
		set => m_tpDataSourceAsSelectionInfo = value;
	}

	/// <summary>
	/// Used to inform the data source of a range selection/deselection
	/// </summary>
	/// <param name="select">bool select indicates whether it's a select or deselect</param>
	/// <param name="firstIndex"></param>
	/// <param name="length"></param>
	/// <remarks>Caller must ensure m_tpDataSourceAsSelectionInfo is not null.</remarks>
	private void InvokeDataSourceRangeSelection(bool select, int firstIndex, uint length)
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

		using var allowSelectionReentrancy = Disposable.Create(AllowSelectionReentrancy);

		// when SelectedIndex < 0, it means we have no selection (cleared selection)
		// SelectJustThisItemInternal handles the call to invoke clear selection for ISelectionInfo interfaces
		if (firstIndex >= 0)
		{
			var spItemIndexRange = new ItemIndexRange(firstIndex, length);
			if (select)
			{
				m_tpDataSourceAsSelectionInfo.SelectRange(spItemIndexRange);
			}
			else
			{
				m_tpDataSourceAsSelectionInfo.DeselectRange(spItemIndexRange);
			}
		}

		// call the Selector::InvokeSelectionChanged with AddedItems and RemovedItems being null
		// when the SelectionInterface is implemented, we let the developer handle selecteditems and selectedranges
		// in here, we simply invoke a selection changed event
		InvokeSelectionChanged(removedItems: null /* pUnselectedItems */, addedItems: null /* pSelectedItems */);

		// updates SelectedIndex
		UpdatePublicSelectionPropertiesAfterDataSourceSelectionInfo();

		// updates the selection and visual state of visible and cached items
		UpdateVisibleAndCachedItemsSelectionAndVisualState(true /* updateIsSelected */);
	}

	/// <summary>
	/// Clears the current selection for data sources implementing ISelectionInfo.
	/// </summary>
	private void InvokeDataSourceClearSelection()
	{
		if (m_tpDataSourceAsSelectionInfo is null)
		{
			return;
		}

		var selectedRanges = m_tpDataSourceAsSelectionInfo.GetSelectedRanges();
		for (int i = 0; i < selectedRanges.Count; i++)
		{
			m_tpDataSourceAsSelectionInfo.DeselectRange(selectedRanges[i]);
		}
	}

	/// <summary>
	/// Prevent reentrancy even if m_selection.IsChangeActive() is false.
	/// </summary>
	private void PreventSelectionReentrancy()
	{
		m_isSelectionReentrancyLocked = true;
	}

	/// <summary>
	/// Undo the effects of PreventSelectionReentrancy to allow reentrancy
	/// when m_selection.IsChangeActive() is false.
	/// </summary>
	private void AllowSelectionReentrancy()
	{
		m_isSelectionReentrancyLocked = false;
	}

	/// <summary>
	/// Updating selection causes reentrancy due to our event handler
	/// layout.We prevent reentrancy into selection-related functions
	/// if there is an active SelectionChanger or someone explicitly prevents
	/// reentrancy via PreventSelectionReentrancy.
	/// </summary>
	/// <returns></returns>
	private bool IsSelectionReentrancyAllowed()
	{
		return !m_isSelectionReentrancyLocked
			//&& !m_selection.IsChangeActive()
			;
	}

	/// <summary>
	/// updates SelectedIndex, SelectedItem and SelectedValue after a selection using SelectionInfo occurs
	/// </summary>
	/// <remarks>Caller must ensure m_tpDataSourceAsSelectionInfo is not null.</remarks>
	private void UpdatePublicSelectionPropertiesAfterDataSourceSelectionInfo()
	{
		bool selectedPropertiesUpdated = false;
		var spSelectedRanges = m_tpDataSourceAsSelectionInfo.GetSelectedRanges();
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

				if (newSelectedIndex >= 0 && newSelectedIndex < itemsCount)
				{
					// update SelectedItem
					var spCurrentSelectedItem = SelectedItem;
					var spNewSelectedItem = spItems[newSelectedIndex];
					if (!PropertyValue.AreEqualImpl(spCurrentSelectedItem, spNewSelectedItem))
					{
						SelectedItem = spNewSelectedItem;

						// update SelectedValue
						var spCurrentSelectedValue = SelectedValue;
						var spNewSelectedValue = GetSelectedValue(spNewSelectedItem);
						if (!PropertyValue.AreEqualImpl(spCurrentSelectedValue, spNewSelectedValue))
						{
							SelectedValue = spNewSelectedValue;
						}
					}

#if false // Uno specific: should be invoked by SelectedIndex/Item/Value
					// If the ItemCollection contains an ICollectionView sync the selected index
					//IFC(UpdateCurrentItemInCollectionView(spNewSelectedItem.Get(), &fDone));

					//IFC(get_SelectedIndex(&newSelectedIndex));

					//IFC(get_SelectedItem(&spNewSelectedItem));
					//IFC(OnSelectionChanged(currentSelectedIndex, newSelectedIndex, spCurrentSelectedItem.Get(), spNewSelectedItem.Get()));
#endif

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

	/// <summary>
	/// goes through the visible and cached items and updates their IsSelected property and their visual state
	/// </summary>
	/// <param name="updateIsSelected"></param>
	private void UpdateVisibleAndCachedItemsSelectionAndVisualState(bool updateIsSelected)
	{
		var spItemsPanelRoot = (IPanel)ItemsPanelRoot;
		if (spItemsPanelRoot is { })
		{
			//ctl::ComPtr<IModernCollectionBasePanel> spIModernCollectionBasePanel;

			//spIModernCollectionBasePanel = spItemsPanelRoot.AsOrNull<IModernCollectionBasePanel>();
			//if (spIModernCollectionBasePanel)
			//{
			//	int firstCachedIndex = -1;
			//	int lastCachedIndex = -1;
			//	std::vector <unsigned int> pinnedElementsIndices;

			//	// get the data from the panel
			//	IFC_RETURN(spIModernCollectionBasePanel.Cast<ModernCollectionBasePanel>()->get_FirstCacheIndexBase(&firstCachedIndex));
			//	IFC_RETURN(spIModernCollectionBasePanel.Cast<ModernCollectionBasePanel>()->get_LastCacheIndexBase(&lastCachedIndex));
			//	IFC_RETURN(spIModernCollectionBasePanel.Cast<ModernCollectionBasePanel>()->GetPinnedElementsIndexVector(xaml_controls::ElementType_ItemContainer, &pinnedElementsIndices));

			//void UpdateVisualState(int itemIndex)
			//{
			//	if (ContainerFromIndex(itemIndex) is SelectorItem pSelectorItem)
			//	{
			//		// querying for the selection state from the ISelectionInfo interface
			//		if (updateIsSelected && m_tpDataSourceAsSelectionInfo is { })
			//		{
			//			var isSelected = m_tpDataSourceAsSelectionInfo.IsSelected(itemIndex);

			//			// this will call ChangeVisualState internally so there is no point calling the below ChangeVisualState
			//			pSelectorItem.IsSelected = isSelected;
			//		}
			//		else
			//		{
			//			pSelectorItem.UpdateVisualState(useTransitions: true);
			//		}
			//	}
			//}

			//	for (int i = firstCachedIndex; i <= lastCachedIndex; ++i)
			//	{
			//		IFC_RETURN(updateVisualState(i));
			//	}

			//	for (int pinnedIndex : pinnedElementsIndices)
			//	{
			//		// Ignore containers for which we already updated the visual state.
			//		if (pinnedIndex < firstCachedIndex || pinnedIndex > lastCachedIndex)
			//		{
			//			IFC_RETURN(updateVisualState(pinnedIndex));
			//		}
			//	}
			//}
		}

		//return S_OK;
	}

	/// <summary>
	/// Handles changing selection properties when a SelectorItem has IsSelected change
	/// </summary>
	/// <param name="pSelectorItem"></param>
	/// <param name="oldValue"></param>
	/// <param name="bIsSelected"></param>
	internal virtual void NotifyListItemSelected(SelectorItem pSelectorItem, bool oldValue, bool bIsSelected)
	{
		//SelectionChanger* pSelectionChanger = nullptr;

		if (m_tpDataSourceAsSelectionInfo is { })
		{
			var newIndex = IndexFromContainer(pSelectorItem);

			InvokeDataSourceRangeSelection(select: bIsSelected, newIndex, 1);
		}
		else
		{
#if false // Uno specific: invoke existing non-ISelectionInfo implementation
			//UINT nCount = 0;
			//ctl::ComPtr<wfc::IObservableVector<IInspectable*>> spItems;
			//ctl::ComPtr<IInspectable> spSelectedItem;

			//if (!IsSelectionReentrancyAllowed())
			//{
			//	goto Cleanup;
			//}


			//IFC(get_Items(&spItems));
			//IFC(spItems.Cast<ItemCollection>()->get_Size(&nCount));

			//if (newIndex >= 0 && newIndex < static_cast<INT>(nCount))
			//{
			//	IFC(ItemFromContainer(pSelectorItem, &spSelectedItem));

			//	IFC(BeginChange(&pSelectionChanger));

			//	if (bIsSelected)
			//	{
			//		BOOLEAN canSelectMultiple = FALSE;
			//		IFC(get_CanSelectMultiple(&canSelectMultiple));
			//		IFC(pSelectionChanger->Select(newIndex, spSelectedItem.Get(), canSelectMultiple));
			//	}
			//	else
			//	{
			//		IFC(pSelectionChanger->Unselect(newIndex, spSelectedItem.Get()));
			//	}

			//	IFC(EndChange(pSelectionChanger));
			//	pSelectionChanger = nullptr;
			//}
#else
			OnSelectorItemIsSelectedChanged(pSelectorItem, oldValue, bIsSelected);
#endif
		}

		//Cleanup:
		//	if (pSelectionChanger != nullptr)
		//	{
		//		VERIFYRETURNHR(pSelectionChanger->Cancel());
		//	}

		//	// returning S_OK to maintain compat with blue - bug 927905
		//	// In blue we swallowed any exception thrown in the app's
		//	// SelectionChanged event handler
		//	return S_OK;
	}

	/// <summary>
	/// Handles selection of single item, and only that item
	/// </summary>
	internal void MakeSingleSelection(int index, bool animateIfBringIntoView, object selectedItem, FocusNavigationDirection focusNavigationDirection)
	{
		if (m_tpDataSourceAsSelectionInfo is { })
		{
			InvokeDataSourceClearSelection();
			if (index >= 0)
			{
				InvokeDataSourceRangeSelection(select: true, index, 1);
			}
			else
			{
				UpdatePublicSelectionPropertiesAfterDataSourceSelectionInfo();
			}
			return;
		}

		var resolvedItem = selectedItem ?? TryGetItemAt(index);
		if (index >= 0)
		{
			SelectedIndex = index;
		}
		else
		{
			SelectedIndex = -1;
		}

		if (resolvedItem is not null && !ReferenceEquals(SelectedItem, resolvedItem))
		{
			SelectedItem = resolvedItem;
		}

		if (animateIfBringIntoView && index >= 0)
		{
			SetFocusedItem(index, shouldScrollIntoView: true, animateIfBringIntoView, focusNavigationDirection, InputActivationBehavior.NoActivate);
		}
	}

	internal void ScrollIntoViewInternal(object item, bool isHeader, bool isFooter, bool isFromPublicAPI, ScrollIntoViewAlignment alignment, double offset, bool animateIfBringIntoView)
	{
		switch (this)
		{
			case ListViewBase listViewBase:
				if (alignment != ScrollIntoViewAlignment.Default)
				{
					listViewBase.ScrollIntoView(item, alignment);
				}
				else
				{
					listViewBase.ScrollIntoView(item);
				}
				break;
			case ComboBox comboBox:
				comboBox.ScrollIntoView(item, alignment);
				break;
			default:
				if (ContainerFromItem(item) is FrameworkElement element)
				{
					var options = new BringIntoViewOptions
					{
						AnimationDesired = animateIfBringIntoView
					};
					element.StartBringIntoView(options);
				}
				break;
		}
	}

	internal void AutomationPeerAddToSelection(int index, object item)
	{
		item ??= TryGetItemAt(index);

		if (m_tpDataSourceAsSelectionInfo is { } && index >= 0)
		{
			InvokeDataSourceRangeSelection(select: true, index, 1);
			return;
		}

		if (this is ListViewBase listViewBase && listViewBase.IsSelectionMultiple)
		{
			if (item is { } && !listViewBase.SelectedItems.Contains(item))
			{
				listViewBase.SelectedItems.Add(item);
			}
			return;
		}

		MakeSingleSelection(index, animateIfBringIntoView: false, item, FocusNavigationDirection.None);
	}

	internal void AutomationPeerRemoveFromSelection(int index, object item)
	{
		item ??= TryGetItemAt(index);

		if (m_tpDataSourceAsSelectionInfo is { } && index >= 0)
		{
			InvokeDataSourceRangeSelection(select: false, index, 1);
			return;
		}

		if (this is ListViewBase listViewBase && listViewBase.IsSelectionMultiple)
		{
			if (item is { } && listViewBase.SelectedItems.Contains(item))
			{
				listViewBase.SelectedItems.Remove(item);
			}
			return;
		}

		if (SelectedIndex == index || Equals(SelectedItem, item))
		{
			SelectedIndex = -1;
		}
	}

	internal bool AutomationPeerIsSelected(object item)
	{
		if (m_tpDataSourceAsSelectionInfo is { } && item is { })
		{
			var index = Items.IndexOf(item);
			if (index >= 0)
			{
				return m_tpDataSourceAsSelectionInfo.IsSelected(index);
			}
		}

		if (this is ListViewBase listViewBase && listViewBase.IsSelectionMultiple)
		{
			return listViewBase.SelectedItems.Contains(item);
		}

		return Equals(SelectedItem, item);
	}

	internal bool IsSelectionPatternApplicable() => true;

	private object TryGetItemAt(int index)
	{
		var items = Items;
		if (items is null || index < 0 || index >= items.Count)
		{
			return null;
		}

		return items[index];
	}
}

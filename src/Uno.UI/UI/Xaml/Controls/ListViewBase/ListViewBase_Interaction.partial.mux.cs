// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// MUX Reference ListViewBase_Partial_Interaction.cpp, tag winui3/release/2.5.1, commit ba3a8d59e6d


#nullable enable

using System;
using DirectUI;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Uno.UI.Extensions;
using Uno.UI.Xaml.Core;
using Uno.UI.Xaml.Input;
using Windows.Foundation;
using Windows.System;

namespace Microsoft.UI.Xaml.Controls;

partial class ListViewBase
{
	// Uncomment to output ListViewBase debugging information
	//#define LVB_DEBUG

	// Handler for when a GroupItem receives focus.
	internal void GroupItemFocused(GroupItem pItem)
	{
		var focusedGroupIndex = pItem.GetGroupIndex();
		SetFocusedGroupIndex(focusedGroupIndex);
		SetLastFocusedGroupIndex(focusedGroupIndex);
	}

	// Handler for when a GroupItem loses focus.
	internal void GroupItemUnfocused(GroupItem pItem)
	{
		var groupIndex = pItem.GetGroupIndex();

		if (GetFocusedGroupIndex() == groupIndex)
		{
			SetFocusedGroupIndex(-1);
		}
	}

	// Handler called by ListViewBaseHeaderItem when it receives focus.
	internal void GroupHeaderItemFocused(ListViewBaseHeaderItem pItem)
	{
		var focusedGroupIndex = IndexFromHeader(pItem, false /*excludeHiddenEmptyGroups*/);
		SetFocusedGroupIndex(focusedGroupIndex);

		if (focusedGroupIndex >= 0)
		{
			SetLastFocusedGroupIndex(focusedGroupIndex);
			m_lastFocusedElementType = ElementType.GroupHeader;
			m_wasItemOrGroupHeaderFocused = true;

			var groupHasElements = GroupHasElements((uint)GetLastFocusedGroupIndex());

			if (groupHasElements)
			{
				// Make sure the last focused index falls within range applicable for this group.
				var lastFocusedIndex = ValidateItemIndexForGroup(
					GetLastFocusedIndex(),
					(uint)GetLastFocusedGroupIndex(),
					true,
					false);
				SetLastFocusedIndex(lastFocusedIndex);
			}
		}
	}

	// Handler called by ListViewBaseHeaderItem when it loses focus.
	internal void GroupHeaderItemUnfocused(ListViewBaseHeaderItem pItem)
	{
		var groupIndex = IndexFromHeader(pItem, false /*excludeHiddenEmptyGroups*/);

		if (GetFocusedGroupIndex() == groupIndex)
		{
			SetFocusedGroupIndex(-1);
		}
	}

	// Handler for when a SelectorItem received focus
	internal override void ItemFocused(SelectorItem pSelectorItem)
	{
#if !HAS_UNO // LVB_DEBUG
		// TODO Uno: gps->DebugTrace has no equivalent.
		IGNOREHR(gps->DebugTrace(XCP_TRACE_OUTPUT_MSG /*traceType*/, L"LVB(%s)[0x%p]: ItemFocused.", ctl::is<xaml_controls::IGridView>(this) ? L"GV" : L"LV", this));
#endif

		base.ItemFocused(pSelectorItem);

		m_lastFocusedElementType = ElementType.Item;
		m_wasItemOrGroupHeaderFocused = true;

		var shouldCustomizeTabNavigation = ShouldCustomizeTabNavigation();

		if (shouldCustomizeTabNavigation)
		{
			var isGrouping = IsGrouping;

			if (isGrouping)
			{
				GetGroupFromItem(
					(uint)GetFocusedIndex(),
					out _,
					out _,
					out var lastFocusedGroupIndex,
					out _);
				SetLastFocusedGroupIndex(lastFocusedGroupIndex);
			}

			// Sometimes when focus moves from a header to an item (disconnected)
			// the header does not get the GroupHeaderItemUnfocused call. In order to
			// workaround that, if an item gets focus, we set the focused group index to -1.
			SetFocusedGroupIndex(-1);
		}

		// When SelectionMode is 'Single', SingleSelectionFollowsFocus is True, TabNavigation is 'Cycle' or 'Local',
		// the focused item needs to be selected.
		var navigationMode = TabNavigation;

		if (navigationMode != KeyboardNavigationMode.Once)
		{
			var singleSelectionFollowsFocus = SingleSelectionFollowsFocus;

			if (singleSelectionFollowsFocus)
			{
				var selectionMode = SelectionMode;

				if (selectionMode == ListViewSelectionMode.Single)
				{
					MakeSingleSelection(GetLastFocusedIndex(), false /*animateIfBringIntoView*/, null /*pSelectedItem*/);
				}
			}
		}
	}

	// Automation Helper for ItemClick
	internal void AutomationItemClick(ListViewBaseItem pItem)
	{
		// We only use FocusState_Pointer here as one of the primary scenario is touch narrator which is mocking a tap.
		// FocusState_Keyboard is not really required here.
		OnItemPrimaryInteractionGesture(pItem, false /*isKeyboardInput*/);
	}

	// Automation Helper for HeaderItemClick
	internal void AutomationHeaderItemClick(ListViewBaseHeaderItem headerItem)
	{
		ToggleSemanticZoomActiveView(headerItem);
	}

	// Handles the primary interaction gesture for a ListViewBaseItem (Tap, Click, or
	// Space) and updates selection or raises the ItemClick event.
	internal bool OnItemPrimaryInteractionGesture(
		ListViewBaseItem pItem,
		bool isKeyboardInput)
	{
		var pIsHandled = false;

		try
		{
			// Get the SemanticZoom owner
			var spSemanticZoom = SemanticZoomOwner;

			var isZoomedInView = IsZoomedInView;

			// store the container as a suggestion so that any call to ContainerFromItem that is called from user code
			// during any of the below callouts, will be able to be more intelligent.
			global::System.Diagnostics.Debug.Assert(m_spContainerBeingClicked is null);
			m_spContainerBeingClicked = pItem;

			// If we're not the ZoomedOutView of a SemanticZoom,
			// do a regular primary interaction.
			if (spSemanticZoom is null || isZoomedInView)
			{
				var mode = SelectionMode;
				var shouldSetFocus = true;

				// We interpret the primary interaction gesture as an ItemClick if it's
				// enabled or as a selection otherwise.
				var isItemClickEnabled = IsItemClickEnabled;
				if (isItemClickEnabled)
				{
					// Focus the item before ItemClick
					FocusItem(isKeyboardInput ? FocusState.Keyboard : FocusState.Pointer, pItem);

					// After setting focus to the item, we don't want to set it again.
					// When we call out to user-code in OnItemClick, user-code could move focus (directly or indirectly). We don't want to
					// stomp on their change.
					shouldSetFocus = false;

					// Raise the ItemClick event
					OnItemClick(pItem);
					pIsHandled = true;
				}

				if (mode != ListViewSelectionMode.None)
				{
					// Update the selection accordingly
					pIsHandled = OnSelectItemPrimary(pItem, isKeyboardInput, shouldSetFocus) || pIsHandled;
				}

				if (!pIsHandled && shouldSetFocus)
				{
					// Selection is disabled. Just focus the item.
					FocusItem(isKeyboardInput ? FocusState.Keyboard : FocusState.Pointer, pItem);
					pIsHandled = true;
				}
			}
			// If we're in a SemanticZoom, the primary interaction gesture in the ZoomedOutView
			// should be used to jump back into the ZoomedInView
			else
			{
				if (spSemanticZoom is not null)
				{
					// Make the item the target of our jump
					FocusItem(isKeyboardInput ? FocusState.Keyboard : FocusState.Pointer, pItem);
					var focusedIndex = IndexFromContainer(pItem);
					SetFocusedIndex(focusedIndex);

					// Jump to this item
#if !HAS_UNO
					// TODO Uno: SemanticZoom.ToggleActiveViewWithFocusState does not exist.
					spSemanticZoom.ToggleActiveViewWithFocusState(
						isKeyboardInput ? FocusState.Keyboard : FocusState.Pointer);
#endif
					pIsHandled = true;
				}
			}

			var soundPlayerService = DXamlCore.Current.GetElementSoundPlayerServiceNoRef();
			soundPlayerService.RequestInteractionSoundForElement(ElementSoundKind.Invoke, this);

			if (isKeyboardInput)
			{
				KeyPress.StartFocusPress(this);
			}

			return pIsHandled;
		}
		finally
		{
			m_spContainerBeingClicked = null;
		}
	}

	// Handles the secondary interaction gesture for a ListViewBaseItem
	// (Ctrl+Click or Ctrl+Space) and updates selection accordingly.
	internal bool OnItemSecondaryInteractionGesture(
		ListViewBaseItem pItem,
		bool isKeyboardInput)
	{
		var pIsHandled = false;

		try
		{
			// store the container as a suggestion so that any call to ContainerFromItem that is called from user code
			// during any of the below callouts, will be able to be more intelligent.
			global::System.Diagnostics.Debug.Assert(m_spContainerBeingClicked is null);
			m_spContainerBeingClicked = pItem;

			var isItemClickEnabled = IsItemClickEnabled;

			if (isItemClickEnabled)
			{
				// Raise the ItemClick event
				OnItemClick(pItem);
				pIsHandled = true;
			}

			// We ignore all secondary input gestures if the SelectionMode is None.
			var mode = SelectionMode;
			if (mode != ListViewSelectionMode.None)
			{
				// Update the selection
				pIsHandled = OnSelectItemSecondary(pItem, isKeyboardInput) || pIsHandled;
			}

			var soundPlayerService = DXamlCore.Current.GetElementSoundPlayerServiceNoRef();
			soundPlayerService.RequestInteractionSoundForElement(ElementSoundKind.Invoke, this);

			return pIsHandled;
		}
		finally
		{
			m_spContainerBeingClicked = null;
		}
	}

	// Selects item(s) based on the current SelectionMode, given a primary interaction gesture.
	private protected bool OnSelectItemPrimary(
		ListViewBaseItem pItem,
		bool isKeyboardInput,
		bool shouldFocusItem)
	{
		var pIsHandled = false;

		if (shouldFocusItem)
		{
			// Focus the item
			FocusItem(isKeyboardInput ? FocusState.Keyboard : FocusState.Pointer, pItem);
		}

		var mode = SelectionMode;
		switch (mode)
		{
			// There's nothing to select when SelectionMode is None
			// case xaml_controls::ListViewSelectionMode_None:
			//     break;
			case ListViewSelectionMode.Single:
			{
				MakeSingleSelection(pItem);
				pIsHandled = true;
				break;
			}
			case ListViewSelectionMode.Multiple:
			{
				var modifiers = GetKeyboardModifiers();

				if ((modifiers & VirtualKeyModifiers.Shift) != 0)
				{
					// Create a range selection from the current anchor to the
					// current item, keeping all items outside this change as is.
					MakeRangeSelection(pItem, /*clearOldSelection*/ false);
					pIsHandled = true;
				}
				else
				{
					// Add or remove the current item to the selection depending on
					// whether it was already selected or not.
					MakeToggleSelection(pItem);
					pIsHandled = true;
				}
				break;
			}
			case ListViewSelectionMode.Extended:
			{
				var modifiers = GetKeyboardModifiers();
				if ((modifiers & VirtualKeyModifiers.Shift) != 0)
				{
					// Create a range selection from the current anchor to the
					// current item, unselecting all items outside of this range.
					MakeRangeSelection(pItem, /*clearOldSelection*/ true);
					pIsHandled = true;
				}
				else
				{
					MakeSingleSelection(pItem);
					pIsHandled = true;
				}

				break;
			}
		}

		return pIsHandled;
	}

	// Selects item(s) based on the current SelectionMode, given a secondary interaction gesture.
	private protected bool OnSelectItemSecondary(
		ListViewBaseItem pItem,
		bool isKeyboardInput)
	{
		var pIsHandled = false;

		// Focus the item
		FocusItem(isKeyboardInput ? FocusState.Keyboard : FocusState.Pointer, pItem);

		var mode = SelectionMode;
		switch (mode)
		{
			// There's nothing to select when SelectionMode is None
			// case xaml_controls::ListViewSelectionMode_None:
			//     break;
			case ListViewSelectionMode.Single:
			{
				MakeToggleSelection(pItem);
				pIsHandled = true;
				break;
			}
			case ListViewSelectionMode.Multiple:
			{
				var modifiers = GetKeyboardModifiers();

				if ((modifiers & VirtualKeyModifiers.Shift) != 0)
				{
					// Multiple selection will now support Ctrl+Shift+Click to create a
					// range selection from the current anchor to the current item,
					// leaving all previously selected items selected.
					MakeRangeSelection(pItem, /*clearOldSelection*/ false);
					pIsHandled = true;
				}
				else
				{
					MakeToggleSelection(pItem);
					pIsHandled = true;
				}
				break;
			}
			case ListViewSelectionMode.Extended:
			{
				var modifiers = GetKeyboardModifiers();
				if ((modifiers & VirtualKeyModifiers.Shift) != 0)
				{
					// Extended selection supports Ctrl+Shift+Click to create a
					// range selection from the current anchor to the current item,
					// leaving all previously selected items selected.
					MakeRangeSelection(pItem, /*clearOldSelection*/ false);
					pIsHandled = true;
				}
				else
				{
					// Toggle the selection of the current item (effectively adding
					// or removing it to the current selection)
					MakeToggleSelection(pItem);
					pIsHandled = true;
				}
				break;
			}
		}

		return pIsHandled;
	}

	// Raises the ItemClick event.
	private protected void OnItemClick(ListViewBaseItem pContainer)
	{
		object? spItemData;

		// Create the args
		ItemClickEventArgs spArgs = new();
		spArgs.OriginalSource = this;

		// we assign content to the container, but that is not necessarily
		// the correct item, nor are the new panels guaranteeing that
		// they set content
		var pVirtualizationInformation = ((UIElement)pContainer).GetVirtualizationInformationOrNull();
		if (pVirtualizationInformation is not null)
		{
			spItemData = pVirtualizationInformation.Item;

			// Do a quick check to see if the item is its own container before
			// proceeding.
			var isItemAContainer = IsItemItsOwnContainer(spItemData);

			if (isItemAContainer)
			{
				// Item was a container. We need to get its content.
				var spTempItemAsICC = spItemData as ContentControl;
				global::System.Diagnostics.Debug.Assert(spTempItemAsICC is not null);

				if (spTempItemAsICC is not null)
				{
					// Get the data from the container.
					// This is safe, because if you are your own container, you have been defined with content in it.
					// The CCC work does not truly apply, nor do the placeholders.
					spItemData = spTempItemAsICC.Content;
				}
			}
		}
		else
		{
			// fallback to old behavior
			spItemData = pContainer.Content;
		}

		spArgs.ClickedItem = spItemData!;

		// Raise the event
		ItemClick?.Invoke(this, spArgs);
	}

	// Tells our KeyDown handler that the key down args came from an item so we should handle them.
	// Otherwise we should pass them to the scroll view.
	internal void SetHandleKeyDownArgsFromItem(bool fFromItem)
	{
		m_bKeyDownArgsFromItem = fFromItem;
	}

	// Makes use of ModernCollectionBasePane's get_FirstCacheGroupIndexBase/get_LastCacheGroupIndexBase,
	// ModernCollectionBasePane's get_FirstCacheIndexBase/get_LastCacheIndexBase, OrientedVirtualizingPanel's GetFirstCacheIndex/GetLastCacheIndex
	// to retrieve the index range of realized items or groups. Falls back to the entire collection range.
	private void GetCacheStartAndEnd(
		bool isForGroupIndexes,
		out int cacheIndexStart,
		out int cacheIndexEnd)
	{
		cacheIndexStart = -1;
		cacheIndexEnd = -1;

		var itemsPanel = ItemsPanelRoot;

		if (itemsPanel is null)
		{
			return;
		}

		var modernPanel = itemsPanel as ModernCollectionBasePanel;

		if (modernPanel is not null)
		{
			if (isForGroupIndexes)
			{
				cacheIndexStart = modernPanel.FirstCacheGroupIndexBase;
				cacheIndexEnd = modernPanel.LastCacheGroupIndexBase;
			}
			else
			{
				cacheIndexStart = modernPanel.FirstCacheIndexBase;
				cacheIndexEnd = modernPanel.LastCacheIndexBase;
			}
		}
		else
		{
			var isGrouping = IsGrouping;

			global::System.Diagnostics.Debug.Assert(!(isForGroupIndexes && !isGrouping));

#if !HAS_UNO
			// TODO Uno: OrientedVirtualizingPanel has no GetFirstCacheIndex/GetLastCacheIndex.
			ctl::ComPtr<IOrientedVirtualizingPanel> orientedVirtualizingPanel;

			if (!isGrouping)
			{
				orientedVirtualizingPanel = itemsPanel.AsOrNull<IOrientedVirtualizingPanel>();
			}

			if (orientedVirtualizingPanel)
			{
				*cacheIndexStart = orientedVirtualizingPanel.Cast<OrientedVirtualizingPanel>()->GetFirstCacheIndex();
				*cacheIndexEnd = orientedVirtualizingPanel.Cast<OrientedVirtualizingPanel>()->GetLastCacheIndex();
			}
			else
#endif
			if (isForGroupIndexes)
			{
				// This is encountered in the grouped VirtualizingStackPanel case, when the TabNavigation is 'Local' or 'Cycle'
				// and the group count is expected to be small.
				var groupCount = GetGroupCount();

				cacheIndexStart = 0;
				cacheIndexEnd = groupCount - 1;
			}
			else
			{
				var itemCount = (uint)GetItemCount();

				if (itemCount > 0)
				{
					// Non-virtualizing StackPanel or grouped VirtualizingStackPanel for instance.
					// In the grouped VirtualizingStackPanel case, this is encountered when the TabNavigation is 'Local' or 'Cycle'
					// and the total item count is expected to be small.
					cacheIndexStart = 0;
					cacheIndexEnd = (int)itemCount - 1;
				}
			}
		}
	}

	// Handles when a key is pressed down on the ListViewBase.
	protected override void OnKeyDown(KeyRoutedEventArgs pArgs)
	{
		using var modifiersScope = SetCurrentInputModifiers(pArgs.KeyboardModifiers);

#if !HAS_UNO // LVB_DEBUG
		// TODO Uno: gps->DebugTrace has no equivalent.
		IGNOREHR(gps->DebugTrace(XCP_TRACE_OUTPUT_MSG /*traceType*/, L"LVB(%s)[0x%p]: OnKeyDown.", ctl::is<xaml_controls::IGridView>(this) ? L"GV" : L"LV", this));
#endif

		var isHandled = false;
		var animateIfBringIntoView = false;
		var originalKey = VirtualKey.None;
		var key = VirtualKey.None;
		var modifiers = VirtualKeyModifiers.None;
		var mode = ListViewSelectionMode.Single;
		var newFocusedIndex = -1;
		var newFocusedType = Controls.ElementType.ItemContainer;
		object? spOriginalSource;
		var gamepadDirection = FocusNavigationDirection.None;
		var handleEvent = m_bKeyDownArgsFromItem;
		var is2DPageNavigation = false;
		var verticalViewportPadding = 0.0f;
		var wasGroupInitiallyFocused = HasFocusedGroup();

		// reset the m_bKeyDownArgsFromItem variable
		SetHandleKeyDownArgsFromItem(false /* fFromItem */);

		// Ignore already handled events
		base.OnKeyDown(pArgs);
		isHandled = pArgs.Handled;
		if (isHandled)
		{
			return;
		}

		if (IsInExclusiveInteraction())
		{
			// During an exclusive interaction (drag/drop), disable all keyboard interaction.
			// Handle the event, since other controls don't have the necessary context around drag/drop interactions.
			pArgs.Handled = true;
			return;
		}

		originalKey = pArgs.OriginalKey;
		key = pArgs.Key;

		// If gamepad keys, we want to handle keys that come from items, headers, listview etc.
		// For Keyboard home/end/page keys, if the key came from something other than an item, we scroll the ScrollViewer.
		if (!handleEvent && !FocusHelper.IsGamepadNavigationDirection(originalKey) && !FocusHelper.IsGamepadPageNavigationDirection(originalKey))
		{
			if (key == VirtualKey.End ||
				key == VirtualKey.Home ||
				key == VirtualKey.PageUp ||
				key == VirtualKey.PageDown)
			{
				ElementScrollViewerScrollInDirection(key);
				pArgs.Handled = true;
			}

			return;
		}

		// TODO: Ignore keyboard input if input has already been "captured" by
		// another device.

		// Most keyboard input (except navigation if we're in a mode where
		// navigation moves focus rather than selection) is ignored if the
		// SelectionMode is None.
		mode = SelectionMode;

		switch (originalKey)
		{
			// TODO: Should we move Enter/Space down to ListViewBaseItem instead so that
			// the interaction processing split between ListView and ListViewBaseItem is
			// symmetrical for keyboard and pointer input?
			case VirtualKey.Enter:
			{
				spOriginalSource = pArgs.OriginalSource;

				if (spOriginalSource is not null && IsListViewBaseItem(spOriginalSource))
				{
					// Enter is considered a primary input gesture
					isHandled = OnItemPrimaryInteractionGesture((ListViewBaseItem)spOriginalSource, true);
				}
				break;
			}

			case VirtualKey.Space:
			{
				// Ignore Alt+Space key presses which bring up the current window's context menu.
				modifiers = GetKeyboardModifiers();

				if ((modifiers & VirtualKeyModifiers.Menu) == 0)
				{
					spOriginalSource = pArgs.OriginalSource;

					if (spOriginalSource is not null && IsListViewBaseItem(spOriginalSource))
					{
						var shouldInvokeSecondary = (modifiers & VirtualKeyModifiers.Control) != 0;

						if (shouldInvokeSecondary)
						{
							// Space is considered a secondary input gesture
							isHandled = OnItemSecondaryInteractionGesture((ListViewBaseItem)spOriginalSource, true /* isKeyboardInput */);
						}
						else
						{
							isHandled = OnItemPrimaryInteractionGesture((ListViewBaseItem)spOriginalSource, true /* isKeyboardInput */);
						}
					}
				}
				break;
			}

			case VirtualKey.GamepadDPadUp:
			case VirtualKey.GamepadLeftThumbstickUp:
			case VirtualKey.NavigationUp:
				gamepadDirection = FocusNavigationDirection.Up;
				animateIfBringIntoView = true;
				break;
			case VirtualKey.GamepadDPadDown:
			case VirtualKey.GamepadLeftThumbstickDown:
			case VirtualKey.NavigationDown:
				gamepadDirection = FocusNavigationDirection.Down;
				animateIfBringIntoView = true;
				break;
			case VirtualKey.GamepadDPadLeft:
			case VirtualKey.GamepadLeftThumbstickLeft:
			case VirtualKey.NavigationLeft:
				gamepadDirection = FocusNavigationDirection.Left;
				animateIfBringIntoView = true;
				break;
			case VirtualKey.GamepadDPadRight:
			case VirtualKey.GamepadLeftThumbstickRight:
			case VirtualKey.NavigationRight:
				gamepadDirection = FocusNavigationDirection.Right;
				animateIfBringIntoView = true;
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
			{
				var canNavigate = CanPerformKeyboardNavigation(key);

				if (!canNavigate)
				{
					break;
				}

#if !HAS_UNO
				// TODO Uno: RuntimeEnabledFeature.EnableAnimatedListViewBaseScrolling has no equivalent.
#if DEBUG
				static auto runtimeEnabledFeatureDetector = RuntimeFeatureBehavior::GetRuntimeEnabledFeatureDetector();
				// Turn on animated navigations for regular keyboard keys in debug builds when the EnableAnimatedListViewBaseScrolling feature is enabled for convenient testing purposes.
				animateIfBringIntoView = animateIfBringIntoView || runtimeEnabledFeatureDetector->IsFeatureEnabled(RuntimeEnabledFeature::EnableAnimatedListViewBaseScrolling);
#endif
#endif
				if (animateIfBringIntoView && m_tpScrollViewer is not null && m_tpScrollViewer.IsInManipulation)
				{
#if !HAS_UNO
					// TODO Uno: ScrollViewer.StopInertialManipulation does not exist.
					m_tpScrollViewer.StopInertialManipulation();
#endif
				}

				{
					// Map gamepad trigger/shoulder keys to page navigation
					if (key == VirtualKey.GamepadLeftTrigger || key == VirtualKey.GamepadLeftShoulder)
					{
						key = VirtualKey.PageUp;
					}

					if (key == VirtualKey.GamepadRightTrigger || key == VirtualKey.GamepadRightShoulder)
					{
						key = VirtualKey.PageDown;
					}

					OnKeyboardNavigation(key, originalKey, animateIfBringIntoView, out newFocusedIndex, out newFocusedType);

					// perform 2D page navigation if we are using a modern panel and we got a paging key (trigger/bumper)
					var spItemsPanel = ItemsPanelRoot;
					if (spItemsPanel is ModernCollectionBasePanel spItemsHostPanelModernCollection &&
						FocusHelper.IsGamepadPageNavigationDirection(originalKey))
					{
						// if we are doing gamepad paging, the above call to OnKeyboardNavigation ensures that the
						// next page is realized, now we can let 2D focus figure out the candidate for us.
						// pad the viewport by the amount the header is sticky.
						verticalViewportPadding = spItemsHostPanelModernCollection.GetVerticalViewportPadding();

						// update layout is required here because the modern panel has already scrolled by a page
						// to realize the content and set its window back to original but not run layout yet.
						UpdateLayout();
						gamepadDirection = FocusHelper.GetPageNavigationDirection(originalKey);
						animateIfBringIntoView = true;
						newFocusedIndex = -1;
						newFocusedType = Controls.ElementType.ItemContainer;
						is2DPageNavigation = true;
					}
				}
				break;
			}
			case VirtualKey.A:
			{
				// Select all items on Ctrl+A
				if (mode != ListViewSelectionMode.Single &&
					mode != ListViewSelectionMode.None)
				{
					modifiers = GetKeyboardModifiers();
					if (modifiers == VirtualKeyModifiers.Control)
					{
						var itemCount = Items.Size;

						if (itemCount != 0)
						{
							uint itemCountSelected = 0;

							// call the Selector::GetDataSourceAsSelectionInfo
							var spDataSourceAsSelectionInfo = GetDataSourceAsSelectionInfo();

							// if the DataSource implemented SelectionInfo, we get the selected ranges and merge them if needed
							// in the end, if all items were selected, we should end up with one range with its lenght being the itemcount
							if (spDataSourceAsSelectionInfo is not null)
							{
								var spSelectedRanges = spDataSourceAsSelectionInfo.GetSelectedRanges();

								var selectedRangesCount = (uint)spSelectedRanges.Count;

								if (selectedRangesCount > 0)
								{
#if !HAS_UNO
									// TODO Uno: ItemIndexRangeHelper.Range/RangeSelection are not ported (commented out in ItemIndexRangeHelper.cs).
									std::vector<DirectUI::Components::ItemIndexRangeHelper::Range> addedRanges;
									DirectUI::Components::ItemIndexRangeHelper::RangeSelection rangeSelection;

									for (unsigned int i = 0; i < selectedRangesCount; ++i)
									{
										int firstIndex = -1;
										unsigned int length = 0;
										ctl::ComPtr<xaml_data::IItemIndexRange> spRange;

										IFC_RETURN(spSelectedRanges->GetAt(i, &spRange));
										IFC_RETURN(spRange->get_FirstIndex(&firstIndex));
										IFC_RETURN(spRange->get_Length(&length));

										rangeSelection.SelectRange(firstIndex, length, addedRanges);
									}

									if (rangeSelection.size() == static_cast<unsigned int>(1))
									{
										itemCountSelected = (*rangeSelection.begin()).length;
									}
#endif
								}
							}
							else
							{
								itemCountSelected = (uint)SelectedItems.Count;
							}

							if (itemCount == itemCountSelected)
							{
								DeselectAll();
							}
							else
							{
								SelectAll();
							}
							isHandled = true;
						}
					}
				}
				break;
			}
			// TODO: Support Ctrl+C, Ctrl+X, Ctrl+Z
		}

		// If it was a game pad navigation key, use auto focus to figure out the next focused index
		if (gamepadDirection != FocusNavigationDirection.None)
		{
			// ask auto focus for next item, scoping for items only under the listview
			// check if that element is a ListViewBaseItem - if it is, then find its index and
			// use that as newFocusedIndex
			object? spCandidate = null;
			ListViewBaseItem? spCurrentAsLVBI = null;
			var isCandidateAlreadyFocused = false;

			if (is2DPageNavigation)
			{
				if (m_tpScrollViewer is not null)
				{
					var scrollViewer = m_tpScrollViewer;

#if !HAS_UNO
					// TODO Uno: ScrollViewer.GetGamepadNavigationCandidate does not exist.
					scrollViewer->GetGamepadNavigationCandidate(
						XboxUtility::GetPageNavigationDirection(originalKey),
						true /* isPageNavigation */,
						2 /* numPagesLookAhead */,
						verticalViewportPadding /* verticalViewportPadding */,
						&spCandidate);
#endif

					// if the candidate we get is the currently focused element, we do not
					// want to set focus/scroll again and handle the key. We should let the key
					// bubble up so that the next control can handle it.
					var spCandidateAsFE = spCandidate as FrameworkElement;
					if (spCandidateAsFE is not null)
					{
						isCandidateAlreadyFocused = spCandidateAsFE.HasFocus();
						if (isCandidateAlreadyFocused)
						{
							spCandidate = null;
						}
					}
				}
			}
			else
			{
				spCandidate = VisualTree.GetFocusManagerForElement(this)?.FindNextFocusWithSearchRootIgnoreEngagementImpl(gamepadDirection,
					this /* searchRoot */);

				// If no candidate was found and the currently focused element
				// is outside the visible range of items. Bring back focus to
				// something within the visible range. This can happen when flipping
				// through at a fast clip and the focused element pans out of the visible range.
				if (spCandidate is null)
				{
					var currentFocusedElement = this.GetFocusedElement();
					if (currentFocusedElement is not null)
					{
						if (currentFocusedElement is ListViewBaseItem currentFocusAsLVBI)
						{
							var itemsHost = ItemsPanelRoot;
							var panel = itemsHost as ModernCollectionBasePanel;
							if (panel is not null)
							{
								var focusIndex = IndexFromContainer(currentFocusAsLVBI);

								var firstVisibleIndex = panel.FirstVisibleIndexBase;
								var lastVisibleIndex = panel.LastVisibleIndexBase;

								if (focusIndex < firstVisibleIndex || focusIndex > lastVisibleIndex)
								{
									var targetIndex = -1;
									if (gamepadDirection == FocusNavigationDirection.Left ||
										gamepadDirection == FocusNavigationDirection.Up)
									{
										targetIndex = firstVisibleIndex;
									}
									else if (gamepadDirection == FocusNavigationDirection.Down ||
											 gamepadDirection == FocusNavigationDirection.Right)
									{
										targetIndex = lastVisibleIndex;
									}

									if (targetIndex > 0)
									{
										var container = ContainerFromIndex(targetIndex);
										spCandidate = container;
									}
								}
							}
						}
					}
				}
			}

			var focusMoved = false;
			if (spCandidate is not null)
			{
				var spCandidateAsLVBI = spCandidate as ListViewBaseItem;
				spCurrentAsLVBI = spCandidateAsLVBI;
				// Is the candidate list item this listview's item. we could have nested listviews.
				var isCandidateNotOwnListItem = spCandidateAsLVBI is null || (spCandidateAsLVBI.GetParentListView() != this);

				// candidate is a list item but it is not this listview's list item (or)
				// candidate is not a list item or header item
				if (isCandidateNotOwnListItem || (spCandidateAsLVBI is null && spCandidate is not ListViewBaseHeaderItem))
				{
					// we got a candidate but it is not a ListViewBaseItem or a ListViewBaseHeaderItem
					// walk up the tree until the ListViewBase to see if the candidate is within an item
					// weird for auto focus to give something inside focus - but this is to catch that case.
					var spCandidateAsDO = spCandidate as DependencyObject;
					var spCurrentItem = spCandidateAsDO;
					var header = Header;
					var footer = Footer;
					while (spCurrentItem is not null)
					{
						var spCurrentItemAsI = (object)spCurrentItem;
						if (ReferenceEquals(spCurrentItemAsI, header) || ReferenceEquals(spCurrentItemAsI, footer))
						{
							// if we reached the header or footer, set focus on the item
							// since the ScrollViewer within the ListViewBase will not handle scrolling, ListViewBase
							// will need to do this explicitly.
							var spCandidateAsUI = spCandidate as UIElement;
							if (spCandidateAsUI is null)
							{
								var spParent = VisualTreeHelper.GetParent(spCandidateAsDO);
								spCandidateAsUI = spParent as UIElement;
							}

							global::System.Diagnostics.Debug.Assert(spCandidateAsUI is not null);

							if (spCandidateAsUI is not null && m_tpScrollViewer is not null)
							{
								var overlapsViewport = false;
								var focusUpdated = false;

								var svAsControl = (Control)m_tpScrollViewer;
#if !HAS_UNO
								// TODO Uno: Control.DoesElementOverlapViewport does not exist.
								svAsControl->DoesElementOverlapViewport(spCandidateAsUI.Get(), overlapsViewport);
#endif

								if (overlapsViewport)
								{
									focusUpdated = this.SetFocusedElement(spCandidateAsDO, FocusState.Keyboard, true /* animateBringIntoView */);
									isHandled = focusUpdated;
								}
							}

							break;
						}

						spCurrentAsLVBI = spCurrentItem as ListViewBaseItem;
						var isCurrentOwnListItem = false;
						if (spCurrentAsLVBI is not null)
						{
							var spParentListView = spCurrentAsLVBI.GetParentListView();
							isCurrentOwnListItem = spParentListView is not null && (spParentListView == this);

							if (!isCurrentOwnListItem && spParentListView is not null)
							{
								// we are going through a listview who is the parent of this item but it is not
								// this listview. Make the parent listview bring it into view and set the correct
								// focused item.
								var index = spParentListView.IndexFromContainer(spCurrentAsLVBI);
								if (index > -1)
								{
									spParentListView.SetFocusedItem(index,
										true /* shouldScrollIntoView*/,
										true /* forcefocus*/,
										FocusState.Keyboard,
										true /* animateIfBringIntoView */);
									focusMoved = true;
								}
							}
						}

						if (isCurrentOwnListItem ||
							spCurrentItem is ListViewBaseHeaderItem ||
							ReferenceEquals(spCurrentItem, this))
						{
							// we reached this listview's item (or) a GroupHeader (or) this listview.
							break;
						}

						var spNextItem = VisualTreeHelper.GetParent(spCurrentItem);
						spCurrentItem = spNextItem;
					}
				}
			}

			if (spCurrentAsLVBI is not null)
			{
				var hasFocus = spCurrentAsLVBI.HasFocus();

				if (!hasFocus || focusMoved)
				{
					// if the new candidate item is the currently focused item, it means we likely
					// have focus within the item and moved it to another element within the item. In that case don't handle
					// the key. This will let auto focus move focus within the item and keep
					// the currently focused list item the same. If the candidate item is not the currently
					// focused item, handle the key and set focus on the new candidate.
					newFocusedIndex = IndexFromContainer(spCurrentAsLVBI);
				}
			}

			if (newFocusedIndex != -1)
			{
				// we have a ListViewItem we want to set focus on
				if (spCandidate is not ListViewBaseItem)
				{
					// we are going to handle and set focus. If the candidate is not a listviewitem, then
					// lets set focus on the candidate explicitly so that focus does not go within the listviewitem
					// to the default element (1st focusable candidate)
					var spCandidateAsDO = (DependencyObject?)spCandidate;
					var focusUpdated = false;

					focusUpdated = this.SetFocusedElement(spCandidateAsDO,
						FocusState.Keyboard,
						true /* animateIfBringIntoView */);
				}
			}
			else if (!isHandled && m_tpScrollViewer is not null && !isCandidateAlreadyFocused)
			{
				// we did not find an item to scroll to, fallback to ScrollViewer's logic
				// to pick the next item and move to it. This will make sure that the ScrollViewer scrolls
				// or moves focus based on the focusable content in that direction if it is scrollable.
				if (spCandidate is not null)
				{
#if !HAS_UNO
					// TODO Uno: ScrollViewer.ProcessGamepadNavigation does not exist.
					m_tpScrollViewer->ProcessGamepadNavigation(key, originalKey, isHandled);
#endif
				}
				else
				{
					var viewportScrollPercent = is2DPageNavigation ? 1.0f : 0.5f;

#if !HAS_UNO
					// TODO Uno: ScrollViewer.ScrollForFocusNavigation does not exist.
					//If the Candidate returned was null, the list is likely blank, we need to process this key for Scroll
					m_tpScrollViewer->ScrollForFocusNavigation(key, gamepadDirection, viewportScrollPercent, &isHandled);
#endif
				}
			}
		}

		if (newFocusedIndex != -1)
		{
			// If a new item received focus, update the selection accordingly
			if (newFocusedType == Controls.ElementType.ItemContainer)
			{
				m_itemIndexHintForHeaderNavigation = -1;

				isHandled = true;

				// Coerce the new index under the upper bound of the ItemCollection
				var itemCount = Items.Size;
				newFocusedIndex = Math.Min(newFocusedIndex, (int)itemCount - 1);

				// TODO: We'll need to update the following behavior which moves
				// selection to be compliant with input gestures that only move focus
				// (we need to resolve the desired behavior of Shift+Arrows when
				// keyboard navigation only moves focus)
				if (newFocusedIndex >= 0)
				{
					modifiers = GetKeyboardModifiers();

					if (mode == ListViewSelectionMode.Extended &&
						(modifiers & VirtualKeyModifiers.Shift) != 0)
					{
						// If the KeyboardModifier is the Shift key and SelectionMode is
						// Extended, we need to reselect the range and set focus to the
						// focused index (including bringing it into view if necessary).
						// We clear the old selection unless the Control modifier is
						// also pressed.
						MakeRangeSelection(
							(uint)newFocusedIndex,
							(modifiers & VirtualKeyModifiers.Control) == 0);
					}
					else if (mode == ListViewSelectionMode.Multiple)
					{
						// Holding shift key will do a range selection or deselection (depending on anchor item)
						if ((modifiers & VirtualKeyModifiers.Shift) != 0)
						{
							MakeRangeSelection((uint)newFocusedIndex, /*clearOldSelection*/ false);
						}

						SetAnchorIndex((uint)newFocusedIndex);
					}
					else if ((modifiers & VirtualKeyModifiers.Control) != 0)
					{
						// If the KeyboardModifier is the Ctrl key, we just want to move the focus to the focused index
						// (and bring it into view if necessary)
					}
					else
					{
						// For all other cases, we select just this item and move focus
						// to the focused index (and bring it into view if necessary)
						if (mode != ListViewSelectionMode.None)
						{
							var singleSelectionFollowsFocus = SingleSelectionFollowsFocus;

							// If mode is single selection, select only if SingleSelectionFollowsFocus is true
							if ((mode == ListViewSelectionMode.Single && singleSelectionFollowsFocus) ||
								mode != ListViewSelectionMode.Single)
							{
								MakeSingleSelection((uint)newFocusedIndex, animateIfBringIntoView, null, gamepadDirection);
							}
						}
					}

					SetFocusedItem(newFocusedIndex, true /*shouldScrollIntoView*/, false /*forceFocus*/, FocusState.Keyboard, animateIfBringIntoView, gamepadDirection);
				}
			}
			else
			{
				// Focus the group headers.
				var groupHeader = HeaderFromIndex(newFocusedIndex);
				if (groupHeader is not null)
				{
					isHandled = this.SetFocusedElement(groupHeader, FocusState.Keyboard, animateIfBringIntoView);
					if (isHandled && !wasGroupInitiallyFocused)
					{
						// Only update the item index hint if we transitioned from an item to a group header.
						// We need to preserve it while moving between group headers, which could happen
						// if we are displaying group headers for empty groups.
						m_itemIndexHintForHeaderNavigation = (int)GetLastFocusedIndex();
					}
				}
				else
				{
					// If we got here, we should have a group header.
					global::System.Diagnostics.Debug.Assert(false);
				}
			}
		}

		if (isHandled)
		{
			pArgs.Handled = true;
		}
	}

	// Handles when a key is released on the ListViewBase.
	protected override void OnKeyUp(KeyRoutedEventArgs pArgs)
	{
#if !HAS_UNO // LVB_DEBUG
		// TODO Uno: gps->DebugTrace has no equivalent.
		IGNOREHR(gps->DebugTrace(XCP_TRACE_OUTPUT_MSG /*traceType*/, L"LVB(%s)[0x%p]: OnKeyUp.", ctl::is<xaml_controls::IGridView>(this) ? L"GV" : L"LV", this));
#endif

		base.OnKeyUp(pArgs);
		if (IsInExclusiveInteraction())
		{
			// During an exclusive interaction (drag/drop), disable all keyboard interaction.
			// Handle the event, since other controls don't have the necessary context around drag/drop interactions.
			pArgs.Handled = true;
		}
		else
		{
			KeyPress.EndFocusPress(this);
		}
	}

	// Handles a key press on a group header.
	internal bool OnGroupHeaderKeyDown(
		object? pItem,
		VirtualKey originalKey,
		VirtualKey key)
	{
		var pHandled = false;

		// Ignore already handled events
		if (IsInExclusiveInteraction())
		{
			// During an exclusive interaction (drag/drop), disable all keyboard interaction.
			// Handle the event, since other controls don't have the necessary context around drag/drop interactions.
			pHandled = true;
		}
		else
		{
			switch (key)
			{
				// Enter, Space (gamepad 'A' maps to space)
				case VirtualKey.Enter:
				case VirtualKey.Space:
					pHandled = ToggleSemanticZoomActiveView(pItem);
					break;
			}
		}

		return pHandled;
	}

	// Handles a key press on a group header.
	internal bool OnGroupHeaderKeyUp(
		object? pItem,
		VirtualKey originalKey,
		VirtualKey key)
	{
		var pHandled = false;

		// Ignore already handled events
		if (IsInExclusiveInteraction())
		{
			// During an exclusive interaction (drag/drop), disable all  interaction.
			// Handle the event, since other controls don't have the necessary context around drag/drop interactions.
			pHandled = true;
		}
		else
		{
			switch (key)
			{
				// Enter, Space (gamepad 'A' maps to space)
				case VirtualKey.Enter:
				case VirtualKey.Space:
					pHandled = ToggleSemanticZoomActiveView(pItem);
					break;
			}
		}

		return pHandled;
	}

	// Handles the tap gesture for a ListViewHeaderBaseItem
	internal bool OnHeaderItemTap(object? pItem)
	{
		var pIsHandled = ToggleSemanticZoomActiveView(pItem);

		return pIsHandled;
	}

	// Toggles the SemanticZoom active view
	// Can be called either through KeyDown (Enter) or Tap
	private protected bool ToggleSemanticZoomActiveView(object? pItem)
	{
		var pHandled = false;

		// Get the SemanticZoom owner
		var spSemanticZoom = SemanticZoomOwner;

		if (spSemanticZoom is not null)
		{
#if !HAS_UNO
			// TODO Uno: SemanticZoom.CanChangeViews and ToggleActiveViewFromHeaderItem are not ported.
			// If we cannot change views, doing a view change will
			// result in an IFCEXPECT - When doing keydown or Tap we
			// do not want to crash the app, so we just don't change views
			// in that case.
			var canChangeViews = spSemanticZoom.CanChangeViews;
			if (canChangeViews)
			{
				var isZoomedInView = IsZoomedInView;
				if (spSemanticZoom is not null && isZoomedInView)
				{
					m_tpSZRequestingItem = pItem;

					// Call ToggleActiveViewFromHeaderItem
					pHandled = spSemanticZoom.ToggleActiveViewFromHeaderItem();
				}
			}
#endif
		}

		return pHandled;
	}

	// Handles keyboard navigation on a group header.
	private protected bool OnGroupKeyboardNavigation(
		VirtualKey key,
		VirtualKey originalKey,
		bool animateIfBringIntoView)
	{
		var pHandled = false;

		TranslateKeyToKeyNavigationAction(key, out var action, out var isValidKey);
		global::System.Diagnostics.Debug.Assert(isValidKey);

		var groupCount = GetGroupCount();

		var spOuterPanel = ItemsPanelRoot;

		var spKeyboardHeaderNavigationPanel = spOuterPanel as IKeyboardHeaderNavigationPanel;

		if (spKeyboardHeaderNavigationPanel is not null)
		{
			var currentGroupIndex = unchecked((uint)GetFocusedGroupIndex());
			uint newGroupIndex = 0;
			var isActionHandled = false;
			DependencyObject? spHeaderDO = null;

			for (var groupsSeen = 0;
				 groupsSeen < groupCount;
				 ++groupsSeen)
			{
				spKeyboardHeaderNavigationPanel.GetTargetHeaderIndexFromNavigationAction(
					currentGroupIndex,
					action,
					out newGroupIndex,
					out isActionHandled);

				if (isActionHandled)
				{
					if (currentGroupIndex != newGroupIndex)
					{
						// Handled, new index
						base.ScrollIntoView(
							newGroupIndex,
							true  /*isGroupItemIndex*/,
							false /*isHeader*/,
							false /*isFooter*/,
							false /*isFromPublicAPI*/,
							true  /*ensureContainerRealized*/,
							animateIfBringIntoView,
							ScrollIntoViewAlignment.Default,
							0.0 /*offset*/,
							unchecked((uint)GetFocusedGroupIndex()) /*currentGroupIndex*/);

						spHeaderDO = HeaderFromIndex((int)newGroupIndex);

						var isFocusable = IsFocusableHelper(spHeaderDO!);

						if (isFocusable)
						{
							// This one is focusable.
							break;
						}
						else
						{
							currentGroupIndex = newGroupIndex;
							isActionHandled = false;
							spHeaderDO = null;
						}
					}
					else
					{
						// Handled, same index -- maybe scrolling?  Leave.
						break;
					}
				}
				else
				{
					// Not handled -- no need to continue.
					break;
				}
			}

			if (isActionHandled &&
				GetFocusedGroupIndex() != newGroupIndex)
			{
				var spHeaderAsUIElement = spHeaderDO as UIElement;
				if (spHeaderAsUIElement is not null)
				{
					spHeaderAsUIElement.Focus(FocusState.Keyboard);

					// Set focused group indices.  This will be set again in GroupHeaderItemFocused.
					SetFocusedGroupIndex((int)newGroupIndex);
					SetLastFocusedGroupIndex((int)newGroupIndex);
				}
			}

			pHandled = isActionHandled;
		}
		else
		{
			// Keep asking the panel for groups to go to, until we find a group that has a
			// focusable header (or we run out of groups).
			do
			{
				var newGroupIndex = -1;
				var newElementType = Controls.ElementType.GroupHeader;
				var useFallback = false;
				var isHandled = false;

				// Get the index of the destination group.
				QueryPanelForItemNavigation(
						spOuterPanel,
						action,
						GetLastFocusedGroupIndex(), /*sourceFocusedIndex*/
						Controls.ElementType.GroupHeader,
						(groupIndex, /*type*/ _) => IsGroupItemFocusable(GetGroupHeaderMapping()!, groupIndex),
						!FocusHelper.IsGamepadNavigationDirection(originalKey), /*allowWrap*/
						unchecked((uint)-1), /*itemIndexHintForHeaderNavigation*/
						out newGroupIndex,
						ref newElementType,
						out isHandled,
						out useFallback);

				global::System.Diagnostics.Debug.Assert(newElementType == Controls.ElementType.GroupHeader);

				if (!isHandled && useFallback)
				{
					// We've tried to use the outer panel for nav, but that has failed and
					// thus we need to do inter-group navigation ourselves.
					QueryGroupFallbackForGroupNavigation(
						key,
						GetLastFocusedGroupIndex(),
						out newGroupIndex,
						out isHandled);
				}

				if (isHandled)
				{
#if DEBUG
					global::System.Diagnostics.Debug.Assert(newGroupIndex < groupCount);
#endif

					base.ScrollIntoView(
						(uint)newGroupIndex,
						true  /*isGroupItemIndex*/,
						false /*isHeader*/,
						false /*isFooter*/,
						false /*isFromPublicAPI*/,
						true  /*ensureContainerRealized*/,
						animateIfBringIntoView,
						ScrollIntoViewAlignment.Default,
						0.0 /*offset*/,
						unchecked((uint)GetFocusedGroupIndex()) /*currentGroupIndex*/);
					var spGroupItemAsIDO = HeaderFromIndex(newGroupIndex);
					var spGroupItem = spGroupItemAsIDO as GroupItem;

					if (spGroupItem is not null)
					{
						pHandled = spGroupItem.FocusHeader(FocusState.Keyboard);
					}

					if (spGroupItem is null || newGroupIndex == GetLastFocusedGroupIndex())
					{
						// Safety check, shouldn't happen. If this is hit, we should break out because either the panel or the ICG is acting wonky.
						// Note the key will still be handled if focus was taken above. Determining whether or not a key is handled
						// is really up to the panel itself (and whether or not our header is focusable).

						// The following 'impossible' conditions will trip us up:
						// * We still dont' have a container after scrolling into view.
						// * We get a non-IGroupItem as a group container.
						// * Our panel gives us our current group index, but claims that it's given us
						//   somewhere new to go (indicated by isHandled).

#if DEBUG
						global::System.Diagnostics.Debug.Assert(spGroupItemAsIDO is not null, "Expected a container to be available after ScrollIntoView.");
						global::System.Diagnostics.Debug.Assert(spGroupItem is not null, "Expected an IGroupItem from ContainerFromGroupIndex.");
						global::System.Diagnostics.Debug.Assert(newGroupIndex != GetLastFocusedGroupIndex(), "Panel handled key, but gave us an index that is the same as the current index.");
#endif
						break;
					}

					SetLastFocusedGroupIndex(newGroupIndex);
				}
				else
				{
					// Nobody handled the key. Nothing to do. Let other controls have the key.
					// We're querying outer panel, but it decided to do nothing.
					// As such, we need to call ElementScrollViewerScrollInDirection so we can scroll
					// any header into view..
					ElementScrollViewerScrollInDirection(key);
					break;
				}
			} while (!pHandled);
		}

		return pHandled;
	}

	// Handles keyboard navigation for the ListViewBase.  By default this will refer to
	// the ItemsHost Panel to provide layout specific knowledge of keyboard
	// navigation, but also includes generic StackPanel-like behavior if ListViewBase is
	// being used with an unsophisticated Panel implementation.
	private protected void OnKeyboardNavigation(
		VirtualKey key,
		VirtualKey originalKey,
		bool animateIfBringIntoView,
		out int pNewFocusedIndex,
		out Controls.ElementType pNewFocusedType)
	{
#if !HAS_UNO // LVB_DEBUG
		// TODO Uno: gps->DebugTrace has no equivalent.
		IGNOREHR(gps->DebugTrace(XCP_TRACE_OUTPUT_MSG /*traceType*/, L"LVB(%s)[0x%p]: OnKeyboardNavigation. key=%d, originalKey=%d, animateIfBringIntoView=%d",
			ctl::is<xaml_controls::IGridView>(this) ? L"GV" : L"LV", this, key, originalKey, animateIfBringIntoView));
#endif

		var isHandled = false;
		Panel? spTargetPanel = null;
		var sourceIndexWithinGroup = -1;
		var sourceGroupIndex = -1;
		var newFocusedIndex = -1;
		var newFocusedType = Controls.ElementType.ItemContainer;
		var considerNestedPanels = false;
		var isQueryingOuterPanel = false;
		var doForceUseOuterPanel = false;
		var allowPanelWrap = true;
		var outerLogicalOrientation = Orientation.Vertical;
		GroupItem? spSourceGroupItem = null;
		var isNavDirectionParallelToInnerOrientation = false;

		pNewFocusedIndex = -1;

		pNewFocusedType = Controls.ElementType.ItemContainer;

		var spItemsHost = ItemsPanelRoot;

		if (spItemsHost is null)
		{
			return;
		}

		var isGrouping = IsGrouping;
		if (isGrouping)
		{
			// Always use outer if panel is a IModernCollectionBasePanel. It handles everything. Otherwise,
			// deal with nested panels here.
			if (spItemsHost is not ModernCollectionBasePanel)
			{
				considerNestedPanels = true;
			}
		}

		TranslateKeyToKeyNavigationAction(key, out var action, out var isValidKey);
		global::System.Diagnostics.Debug.Assert(isValidKey);

		var spMapping = GetItemContainerMapping();
		var spGroupMapping = GetGroupHeaderMapping()!;

		doForceUseOuterPanel = ShouldNavDirectionIgnoreGroups(action);
		GetPanelOrientations(spItemsHost, out _ /*pPhysicalOrientation*/, out outerLogicalOrientation);

		if (!considerNestedPanels || doForceUseOuterPanel)
		{
			// If we are not grouping, nav within the main panel.
			isQueryingOuterPanel = true;
			spTargetPanel = spItemsHost;
		}
		else
		{
			// Otherwise, nav within the group's panel, but only if this isn't
			// a global navigation key (Home, End, etc).
			isQueryingOuterPanel = true; // Default to outer panel in case we don't find an inner panel.

			GetGroupFromItem((uint)GetLastFocusedIndex(), out _, out spSourceGroupItem, out sourceGroupIndex, out sourceIndexWithinGroup);

			if (spSourceGroupItem is not null)
			{
				var spGroupItemsControl = spSourceGroupItem.GetTemplatedItemsControl();
				if (spGroupItemsControl is not null)
				{
					spTargetPanel = spGroupItemsControl.ItemsPanelRoot;
					isQueryingOuterPanel = false;
				}
			}
		}

		if (!isQueryingOuterPanel)
		{
			// Only allow wrapping for VSPs and SPs as the outer panel, if they match in orientation with the inner panel.
			allowPanelWrap = false;
			if (spItemsHost is StackPanel || spItemsHost is VirtualizingStackPanel)
			{
				GetPanelOrientations(spTargetPanel, out _ /*pPhysicalOrientation*/, out var innerLogicalOrientation);
				isNavDirectionParallelToInnerOrientation = (innerLogicalOrientation == Orientation.Vertical) ?
					(action == KeyNavigationAction.Up) || (action == KeyNavigationAction.Down) :
					(action == KeyNavigationAction.Left) || (action == KeyNavigationAction.Right);
				allowPanelWrap = (innerLogicalOrientation != outerLogicalOrientation);
			}
		}

		if (spTargetPanel is not null)
		{
			var useFallback = false;
			if (isQueryingOuterPanel && considerNestedPanels)
			{
				// If we're grouping, but we're still querying the outer panel, this means that either we're processing a key where
				// ShouldNavDirectionIgnoreGroups returns true, or we couldn't find a group panel. In that case, we need to use
				// the fallback logic, since the outer panel does not have the item-specific context it needs (at most, it could
				// give us which group to go to, but not which specific item to go to). The fallback code is panel-agnostic
				// and will handle this case properly.
				useFallback = true;
				isHandled = false;
			}
			else
			{
				var isFocusOnHeaderOrFooter = false;
				var isGamepadPageNavigation = FocusHelper.IsGamepadPageNavigationDirection(originalKey);

				var itemsPresenter = ItemsPresenter;
				if (itemsPresenter is not null)
				{
					var container = itemsPresenter.HeaderContentControl;
					if (container is not null)
					{
						isFocusOnHeaderOrFooter = container.HasFocus();
					}

					if (!isFocusOnHeaderOrFooter)
					{
						container = itemsPresenter.FooterContentControl;
						if (container is not null)
						{
							isFocusOnHeaderOrFooter = container.HasFocus();
						}
					}
				}

				// when using gamepad or if focus is on header or footer, don't ask the panel,
				// use ScrollViewer's logic instead
				if (!isGamepadPageNavigation && !isFocusOnHeaderOrFooter)
				{
					var lastFocusedIndex = GetLastFocusedIndex();
					var panelStartIndexInCollection = (isQueryingOuterPanel) ? 0 : unchecked(lastFocusedIndex - (uint)sourceIndexWithinGroup);
					var startIndex = (isQueryingOuterPanel) ? lastFocusedIndex : (uint)sourceIndexWithinGroup;
					var startType = Controls.ElementType.ItemContainer;

					// Only allow focusing of the group header if the items panel is an IModernCollectionBasePanel, which
					// we can test for by validating that considerNestedPanels is false.
					if (!considerNestedPanels && HasFocusedGroup())
					{
						startIndex = (uint)GetFocusedGroupIndex();
						startType = Controls.ElementType.GroupHeader;
					}

					// First, ask the panel what to do with our key.
					QueryPanelForItemNavigation(
						spTargetPanel,
						action,
						(int)startIndex,
						startType,
						(index, type) =>
						{
							var pIsFocusable = false;
							if (type == Controls.ElementType.GroupHeader)
							{
								// Since group headers are treated the same as items, use the same selector logic for
								// determining whether a header is selectable/focusable.

								// Here the index is just the GroupIndex and we don't need to translate its values depending
								// on which panel we're querying, like we do for items.
								var groupItemDO = spGroupMapping.HeaderFromIndex((int)index);
								pIsFocusable = IsFocusableHelper(groupItemDO!);
							}
							else
							{
								pIsFocusable = IsItemFocusable(spMapping, panelStartIndexInCollection + index, true /*unrealizedItemsAssumedFocusable*/);
							}

							return pIsFocusable;
						},
						allowPanelWrap && !FocusHelper.IsGamepadNavigationDirection(originalKey),
						unchecked((uint)m_itemIndexHintForHeaderNavigation),
						out newFocusedIndex,
						ref newFocusedType,
						out isHandled,
						out useFallback);
				}
			}

			if (!isQueryingOuterPanel && isHandled)
			{
				// If the new focus element is an item rather than a group, then translate its focused index
				// (which is relative to group panel) to collection index.
				if (newFocusedType == Controls.ElementType.ItemContainer)
				{
					var indexDelta = newFocusedIndex - sourceIndexWithinGroup;
					newFocusedIndex = (int)GetLastFocusedIndex() + indexDelta;
				}
			}

			if (isQueryingOuterPanel && !isHandled && !useFallback)
			{
				// We're querying the outer panel, but it decided to do nothing.
				// if it was a gamepad key,
				//  - we delegate to the ScrollViewer to move focus within the header/footer
				// if it is not a gamepad key,
				//  - we need to call ElementScrollViewerScrollInDirection so we can scroll
				//    any header into view. QuerySelectorFallbackForItemNavigation does this for us,
				//    so we're set for that case.
				if (!FocusHelper.IsGamepadPageNavigationDirection(originalKey))
				{
					ElementScrollViewerScrollInDirection(key, animateIfBringIntoView);
				}
			}

			// Panel wants us to deal with the navigation key, as it doesn't implement its own key nav for this case.
			if (useFallback)
			{
				if (!considerNestedPanels || doForceUseOuterPanel)
				{
					// We're safe, just fall back.
					QuerySelectorFallbackForItemNavigation(key, (int)GetLastFocusedIndex(), out newFocusedIndex, out isHandled);
				}
				else
				{
					// Selector doesn't support group keyboard navigation. This must be handled in tricky ways.
					// Invoke our own group-aware fallback.
					global::System.Diagnostics.Debug.Assert(considerNestedPanels);

					GetGroupInformation(
						(uint)sourceGroupIndex,
						out var groupLeftIndex,
						out var groupSize,
						out var foundGroup);

					global::System.Diagnostics.Debug.Assert(foundGroup);

					if (foundGroup)
					{
						QueryGroupFallbackForItemNavigation(
							spTargetPanel,
							key,
							(int)GetLastFocusedIndex(),
							(int)groupLeftIndex,
							(int)(groupLeftIndex + groupSize),
							out newFocusedIndex,
							ref isHandled);
					}
				}
			}
		}

		if (isHandled)
		{
			// We're done.
			pNewFocusedIndex = newFocusedIndex;
			pNewFocusedType = newFocusedType;
		}
		else if (allowPanelWrap && !isQueryingOuterPanel && isNavDirectionParallelToInnerOrientation)
		{
			// Special case. We're grouping, outer is VSP or SP, inner and outer orientations don't match.
			// We're also at a group boundary, and the navigation action would result in wrapping
			// in the inner panel (if there were more items - remember, we're at an edge).
			var doIncreaseIndex = (action == KeyNavigationAction.Right) || (action == KeyNavigationAction.Down);
			var lastFocusedIndex = GetLastFocusedIndex();

			if (doIncreaseIndex)
			{
				// Coerce the new index under the upper bound of the ItemCollection
				var itemCount = Items.Size;
				if (lastFocusedIndex <= unchecked(itemCount - 2))
				{
					pNewFocusedIndex = (int)(lastFocusedIndex + 1);
				}
			}
			else if (lastFocusedIndex > 0)
			{
				pNewFocusedIndex = (int)(lastFocusedIndex - 1);
			}
		}
		else if (!isQueryingOuterPanel)
		{
			// Our group's panel didn't think it was time to handle a key press (reached an edge).
			// We need to do navigation between groups now.
			var foundSelectableItem = false;
			var groupCount = GetGroupCount();

			// Keep asking the relevant panels for groups to go to, until we find a group that has a
			// selectable item (or we run out of groups).
			do
			{
				var newGroupIndex = -1;
				var newElementType = Controls.ElementType.GroupHeader;
				var useFallback = false;

				// Get the index of the destination group.
				QueryPanelForItemNavigation(
					spItemsHost,
					action,
					sourceGroupIndex, /*sourceFocusedIndex*/
					Controls.ElementType.GroupHeader,
					(groupIndex, /*type*/ _) => IsGroupItemFocusable(GetGroupHeaderMapping()!, groupIndex),
					!FocusHelper.IsGamepadNavigationDirection(originalKey), /*allowWrap*/
					unchecked((uint)-1), /*itemIndexHintForHeaderNavigation*/
					out newGroupIndex,
					ref newElementType,
					out isHandled,
					out useFallback);

				// If the navigation was handled, then the item type we get back is expected to be a container.
				global::System.Diagnostics.Debug.Assert(!isHandled || newElementType == Controls.ElementType.ItemContainer);

				if (!isHandled)
				{
					if (useFallback)
					{
						// At this point, our group panel has not handled the navigation at all.
						// We've tried to use the outer panel for nav, but that has failed and
						// thus we need to do inter-group navigation ourselves.
						QueryGroupFallbackForGroupNavigation(
							key,
							sourceGroupIndex,
							out newGroupIndex,
							out isHandled);
					}
				}

				if (isHandled)
				{
					//Figure out which item to go to within new group.

					var targetItemIndexInCollection = -1;
					var wentForward = newGroupIndex > sourceGroupIndex;
					var useFallback2 = true;
					var fallbackSearchStartIndex = -1; /* default to beginning/end of group.*/

					// We only support smart navigation between groups if the outer panel NOT a VariableSizedWrapGrid. See design notes attached to Win8: 398951 for details.
					if (spItemsHost is not VariableSizedWrapGrid)
					{
						var foundItem = false;
						var closestIndex = -1;
						// Smart navigation.
						GetClosestIndexToItemInGroupPanel(
							GetLastFocusedIndex(),
							(uint)newGroupIndex,
							action,
							spSourceGroupItem!,
							out closestIndex,
							out foundItem,
							out useFallback2);

						foundSelectableItem = foundItem && !useFallback2;
						if (foundItem && useFallback2)
						{
							// Panel found the item, but it wasn't Focusable. We need to use the fallback, starting
							// from the (unfocusable) index found.
							fallbackSearchStartIndex = closestIndex;
						}
						else if (foundSelectableItem)
						{
							// No need to do anything special. We'll use this value if !useFallback2.
							targetItemIndexInCollection = closestIndex;
						}
					}

					if (useFallback2)
					{
						// Fallback navigation - go to first or last item, depending on action.
						// If GetClosestIndexToItemInGroupPanel found an unfocusable item, start the search
						// from that item.
						FindFirstSelectableItemInGroup(
							(uint)newGroupIndex,
							wentForward,
							fallbackSearchStartIndex,
							out targetItemIndexInCollection,
							out foundSelectableItem);
					}

					if (foundSelectableItem)
					{
						pNewFocusedIndex = targetItemIndexInCollection;
						SetFocusedGroupIndex(newGroupIndex);
						SetLastFocusedGroupIndex(newGroupIndex);
					}
					else
					{
						// Continue with the next group, if possible.
						sourceGroupIndex = newGroupIndex;
						if (
							(wentForward && sourceGroupIndex >= groupCount) ||
							(!wentForward && sourceGroupIndex <= 0))
						{
							break;
						}
					}
				}
				else
				{
					// Nobody handled the key. Nothing to do. Let other controls have the key.
					// We're querying outer panel, but it decided to do nothing.
					if (!FocusHelper.IsGamepadPageNavigationDirection(originalKey))
					{
						// we need to call ElementScrollViewerScrollInDirection so we can scroll
						// any header into view..
						ElementScrollViewerScrollInDirection(key);
					}
					break;
				}
			} while (!foundSelectableItem);
		}
	}

	// Given a source item and target group and a navigation action,
	// find which item in the target group is closest to the source item.
	// "Closest" means: closest to the point returned by
	// GetNavigationSourcePoint(source item).
	// Also indicates whether or not an item was found,
	// and whether or not to default to the fallback implementation
	// (FindFirstSelectableItemInGroup).
	private protected void GetClosestIndexToItemInGroupPanel(
		uint sourceIndexInCollection,
		uint targetGroupIndex,
		KeyNavigationAction action,
		GroupItem pSourceGroupItem,
		out int pClosestIndex,
		out bool pFoundItem,
		out bool pUseFallback)
	{
		pUseFallback = true;
		pFoundItem = false;
		pClosestIndex = -1;

		GetGroupInformation(
			targetGroupIndex,
			out var targetGroupStartIndex,
			out var targetGroupSize,
			out var isGroupValid);
		if (!isGroupValid)
		{
			throw new InvalidOperationException();
		}

		if (targetGroupSize > 0)
		{
			Panel? spTargetGroupPanel = null;

			GetGroupFromItem(targetGroupStartIndex, out _, out var spTargetGroupItem, out _, out _);

			// Try to get the Panel.
			if (spTargetGroupItem is not null)
			{
				var spTargetGroupItemsControl = spTargetGroupItem.GetTemplatedItemsControl();
				if (spTargetGroupItemsControl is not null)
				{
					spTargetGroupPanel = spTargetGroupItemsControl.ItemsPanelRoot;
				}
			}

			if (spTargetGroupPanel is not null)
			{
				// Get the source item.
				var spMapping = GetItemContainerMapping();
				var spSourceContainer = ContainerFromIndex((int)sourceIndexInCollection);
				var spSourceContainerAsFE = spSourceContainer as FrameworkElement;
				if (spSourceContainerAsFE is not null)
				{
					var sourcePointRelativeToSourceItem = new Point(0, 0);
					var sourcePointRelativeToTargetPanel = new Point(0, 0);
					var targetElementInfoInTargetPanel = new Primitives.ElementInfo { m_childIndex = -1, m_childIsHeader = false };

					// Get the source point relative to the source item.
					sourcePointRelativeToSourceItem = GetNavigationSourcePoint(spSourceContainerAsFE, action);

					// We could just transform directly to the target panel here. Unfortunately, consider the case of
					// the outer panel limiting the size of our source GroupItem - say, in the case of a WrapGrid.
					// Then, our source panel could extend underneath our target panel! This will usually cause our GetClosestElementInfo
					// call below to return odd (but technically correct) indexes, since we could query for a point
					// that's in the middle of our target panel. So, we must clip our source point to the bounds
					// of the source GroupItem.
					pSourceGroupItem.ClipPointToElementBounds(
						spSourceContainerAsFE,
						ref sourcePointRelativeToSourceItem);

					// Get the source point relative to the target panel.
					var spItemToTargetPanel = spSourceContainerAsFE.TransformToVisual(spTargetGroupPanel);
					sourcePointRelativeToTargetPanel = spItemToTargetPanel.TransformPoint(sourcePointRelativeToSourceItem);

					// Calculate the closest index in the target panel, using IItemLookupPanel or the fallback method.
					var spTargetGroupPanelAsILP = spTargetGroupPanel as IItemLookupPanel;
					if (spTargetGroupPanelAsILP is not null)
					{
						targetElementInfoInTargetPanel = spTargetGroupPanelAsILP.GetClosestElementInfo(sourcePointRelativeToTargetPanel);
					}
					else
					{
#if !HAS_UNO
						// TODO Uno: CoreImports.CPanel_PanelGetClosestIndexSlow does not exist.
						// fallback to a naive and slow (full iteration) implementation of all the
						// arranged children
						XPOINTF p = {sourcePointRelativeToTargetPanel.X, sourcePointRelativeToTargetPanel.Y};
						IFC(CoreImports::CPanel_PanelGetClosestIndexSlow(static_cast<CPanel*>(spTargetGroupPanel.Cast<Panel>()->GetHandle()), p, &targetElementInfoInTargetPanel.m_childIndex));
#endif
					}

					// We have a target index in the new group's panel. Convert it to an index in the full collection.
					var targetIndexInCollection = (int)(targetElementInfoInTargetPanel.m_childIndex + targetGroupStartIndex);

					// See if the target is selectable.
					var isTargetFocusable = IsItemFocusable(spMapping, (uint)targetIndexInCollection, true /*unrealizedItemsAssumedFocusable*/);

					if (isTargetFocusable)
					{
						pUseFallback = false;
					}
					else
					{
						// The closest item was unselectable. We need to use the fallback.
						// Ideally we'd have some sort of GetClosestSelectableIndex, but
						// we decided to punt that for now.
						pUseFallback = true;
					}

					// We found the item either way.
					pClosestIndex = targetIndexInCollection;
					pFoundItem = true;
				}
			}
		}
		else
		{
			// Group was empty.
			pUseFallback = false;
		}
	}

	// Given an element and a navigation action, return the point
	// that should be considered the "source" of the navigation.
	private protected Point GetNavigationSourcePoint(
		FrameworkElement pElementNavigatedFrom,
		KeyNavigationAction action)
	{
		Point pPoint = default;
		var sourceHeight = pElementNavigatedFrom.ActualHeight;
		var sourceWidth = pElementNavigatedFrom.ActualWidth;

		switch (action)
		{
			case KeyNavigationAction.Right:
			{
				pPoint.X = (float)sourceWidth;
				pPoint.Y = (float)(sourceHeight / 2.0f);
				break;
			}
			case KeyNavigationAction.Left:
			{
				pPoint.X = 0;
				pPoint.Y = (float)(sourceHeight / 2.0f);
				break;
			}
			case KeyNavigationAction.Down:
			{
				pPoint.X = (float)(sourceWidth / 2.0f);
				pPoint.Y = (float)sourceHeight;
				break;
			}
			case KeyNavigationAction.Up:
			{
				pPoint.X = (float)(sourceWidth / 2.0f);
				pPoint.Y = 0;
				break;
			}
		}

		return pPoint;
	}

	// Gets the first selectable item within the given group.
	// Can start from the end or the beginning of the group.
	private protected void FindFirstSelectableItemInGroup(
		uint groupIndex,
		bool fromBeginning,
		int startIndexInCollection,
		out int pFirstSelectableItemIndexInCollection,
		out bool pFoundSelectableItem)
	{
		IItemContainerMapping? spMapping = null;
		var isFocusable = false;
		var startIndex = -1;

		pFoundSelectableItem = false;
		pFirstSelectableItemIndexInCollection = -1;

		GetGroupInformation(
			groupIndex,
			out var groupStartIndex,
			out var groupSize,
			out var isGroupValid);
		if (!isGroupValid)
		{
			throw new InvalidOperationException();
		}

		var spItems = Items;
		var nCount = spItems.Size;

		if (startIndexInCollection >= 0)
		{
			startIndex = startIndexInCollection;
		}
		else
		{
			startIndex = (int)groupStartIndex;
			if (!fromBeginning)
			{
				startIndex += (int)groupSize - 1;
			}
		}

		for (var currentItemIndex = startIndex;
			currentItemIndex < (int)nCount && currentItemIndex >= (int)groupStartIndex && currentItemIndex < (int)(groupStartIndex + groupSize) && currentItemIndex >= 0;
			currentItemIndex += (fromBeginning ? 1 : -1))
		{
			var spItem = spItems[currentItemIndex];
			isFocusable = IsFocusableHelper(spItem);
			if (isFocusable)
			{
				spMapping ??= GetItemContainerMapping();

				isFocusable = IsItemFocusable(spMapping, (uint)currentItemIndex, true /*unrealizedItemsAssumedFocusable*/);
				if (isFocusable)
				{
					pFoundSelectableItem = true;
					pFirstSelectableItemIndexInCollection = currentItemIndex;
					break;
				}
			}
		}
	}

	// Fetches group details, given a group index.
	private protected void GetGroupInformation(
		uint groupIndex,
		out uint pGroupStartIndex,
		out uint pGroupSize,
		out bool pIsValid)
	{
		pGroupStartIndex = 0;
		pGroupSize = 0;
		pIsValid = false;

		var spCollectionView = ((IGeneratorHost)this).CollectionView;
		var spCollectionGroups = spCollectionView!.CollectionGroups;

		var spCollectionGroupsAsV = spCollectionGroups;
		var groupCount = (uint)spCollectionGroupsAsV.Count;

		if (groupCount > groupIndex)
		{
			uint i = 0;

			for (i = 0; i <= groupIndex && i < groupCount; i++)
			{
				var spCurrent = spCollectionGroupsAsV[(int)i];
				var spCurrentGroup = (ICollectionViewGroup)spCurrent;
				var spCurrentGroupItems = spCurrentGroup.GroupItems;
				var spCurrentGroupItemsAsV = spCurrentGroupItems;
				pGroupSize = (uint)spCurrentGroupItemsAsV.Count;

				if (i != groupIndex)
				{
					pGroupStartIndex += pGroupSize;
				}
			}
			pIsValid = true;
		}
	}

	// Given a key, returns whether or not we must handle the nav action at the outer panel.
	// This is true for keys such as home or end - they should ignore groups.
	private protected bool ShouldNavDirectionIgnoreGroups(KeyNavigationAction action)
	{
		var pIsGlobal =
			(action == KeyNavigationAction.Next) ||
			(action == KeyNavigationAction.Previous) ||
			(action == KeyNavigationAction.First) ||
			(action == KeyNavigationAction.Last);

		return pIsGlobal;
	}

	// Given a key, returns a focus navigation action.
	private protected void TranslateKeyToKeyNavigationAction(
		VirtualKey key,
		out KeyNavigationAction pNavAction,
		out bool pIsValidKey)
	{
		pNavAction = KeyNavigationAction.Up;
		pIsValidKey = false;
		var flowDirection = FlowDirection;

		KeyboardNavigation.TranslateKeyToKeyNavigationAction(flowDirection, key, out pNavAction, out pIsValidKey);
	}

	// Asks the given Panel what to do with a navigation action,
	// given a starting index. Returns the destination index,
	// whether or not the Panel handled the key, and whether
	// or not the panel needs us to fall back to the default
	// Selector behavior.
	private protected void QueryPanelForItemNavigation(
		Panel pTargetPanel,
		KeyNavigationAction action,
		int sourceFocusedIndex,
		Controls.ElementType sourceFocusedType,
		Func<uint, Controls.ElementType, bool> verificationFunction,
		bool allowWrap,
		uint itemIndexHintForHeaderNavigation,
		out int pNewFocusedIndex,
		ref Controls.ElementType pNewFocusedType,
		out bool pIsHandled,
		out bool pUseFallback)
	{
#if !HAS_UNO // LVB_DEBUG
		// TODO Uno: gps->DebugTrace has no equivalent.
		IGNOREHR(gps->DebugTrace(XCP_TRACE_OUTPUT_MSG /*traceType*/, L"LVB(%s)[0x%p]: QueryPanelForItemNavigation.", ctl::is<xaml_controls::IGridView>(this) ? L"GV" : L"LV", this));
#endif

		var isHandled = false;

		pNewFocusedIndex = -1;
		pIsHandled = false;
		pUseFallback = false;

		var spKeyNavPanel = pTargetPanel as IKeyboardNavigationPanel;
		if (spKeyNavPanel is not null)
		{
			uint newFocusedIndexUint = 0;
			var newFocusedType = Controls.ElementType.ItemContainer;
			var actionValidForSourceIndex = false;

			var supportsAction = spKeyNavPanel.SupportsKeyNavigationAction(action);

			if (supportsAction)
			{
				var queryPanel = true;
				do
				{
					if (queryPanel)
					{
						spKeyNavPanel.GetTargetIndexFromNavigationAction(
							(uint)sourceFocusedIndex,
							sourceFocusedType,
							action,
							allowWrap,
							itemIndexHintForHeaderNavigation,
							out newFocusedIndexUint,
							out newFocusedType,
							out actionValidForSourceIndex);
					}

					isHandled = actionValidForSourceIndex;
					if (actionValidForSourceIndex)
					{
						var isEligible = verificationFunction(newFocusedIndexUint, newFocusedType);
						if (isEligible)
						{
							pNewFocusedIndex = (int)newFocusedIndexUint;
							pNewFocusedType = newFocusedType;
							break;
						}
						else if ((uint)sourceFocusedIndex == newFocusedIndexUint && sourceFocusedType == newFocusedType)
						{
							// Safety.
							break;
						}
						else
						{
							sourceFocusedType = newFocusedType;

							if (action == KeyNavigationAction.First)
							{
								// We wanted the first item, but the one we got from the panel was not eligible.
								// stop querying the panel and walk through the indicies to get to the next eligible item
								var itemCount = GetItemsCount();
								if ((int)newFocusedIndexUint < itemCount - 1)
								{
									++newFocusedIndexUint;
									queryPanel = false;
								}
								else
								{
									// reached the end
									break;
								}
							}
							else if (action == KeyNavigationAction.Last)
							{
								// We wanted the last item, but the one we got from the panel was not eligible.
								// stop querying the panel and walk through the indicies to get to the previous eligible item
								if (newFocusedIndexUint > 0)
								{
									--newFocusedIndexUint;
									queryPanel = false;
								}
								else
								{
									// reached the beginning.
									break;
								}
							}
							else
							{
								// Proceed in the same direction.
								sourceFocusedIndex = (int)newFocusedIndexUint;
							}
						}
					}
					else
					{
						// We ran out of items.
						break;
					}
				} while (true);
			}
			else
			{
				// Panel doesn't support the key. Fall back.
				pUseFallback = true;
			}
		}
		else
		{
			pUseFallback = true;
		}

		pIsHandled = isHandled;
	}


	// Asks the default Selector key navigation logic what to do
	// with the given navigation key. Never call this if we're
	// grouping.
	// This should really be combined with QueryPanelForItemNavigation,
	// but Selector's logic is too convoluted to justify messing with it.
	// Better to encapsulate Selector's inability to handle group navigation
	// into this function and QueryGroupFallbackForItemNavigation.
	private protected void QuerySelectorFallbackForItemNavigation(
		VirtualKey key,
		int sourceFocusedIndex,
		out int pNewFocusedIndex,
		out bool pIsHandled)
	{
		// Store sourceFocusedIndex in newFocusedIndex. HandleNavigationKey uses this
		// as an _Inout_ parameter.
		var newFocusedIndex = sourceFocusedIndex;

		pIsHandled = false;

		// Forward along to Selector's generic keyboard navigation handling.
		HandleNavigationKey(key, /*scrollViewport*/ true, ref newFocusedIndex);
		pNewFocusedIndex = newFocusedIndex;

		pIsHandled = (newFocusedIndex != sourceFocusedIndex);
	}

	// Panels may decide to use the default Selector logic for keyboard
	// navigation (see QueryPanelForItemNavigation's pUseFallback). However,
	// the usual Selector logic cannot handle grouping. Instead, we re-implement
	// a grouping-aware version here. This particular method attempts to navigate
	// BETWEEN groups. QueryGroupFallbackForItemNavigation handles the in-group
	// navigation.
	private protected void QueryGroupFallbackForGroupNavigation(
		VirtualKey key,
		int sourceGroupIndex,
		out int pNewGroupIndex,
		out bool pIsHandled)
	{
		var newGroupIndex = sourceGroupIndex;
		var indexDeltaPerStep = 0;

		// At this point, we know we're grouping, but the inner panels have not handled the event (probably
		// because they reached the edge of the items).
		// So, we need to do a group-aware implementation of Selector's HandleKeyNavigation - but this tiem,
		// the implementation deals with groups instead of items, since we're crossing groups.

		pNewGroupIndex = -1;
		pIsHandled = false;

		var groupCount = GetGroupCount();

		var direction = FlowDirection;
		var bInvertForRTL = (direction == FlowDirection.RightToLeft);

		GetItemsHostOrientations(out var physicalOrientation, out _ /*pLogicalOrientation*/);
		var isVertical = (physicalOrientation == Orientation.Vertical);

		switch (key)
		{
			case VirtualKey.Left:

				if (!isVertical)
				{
					if (bInvertForRTL)
					{
						indexDeltaPerStep = 1;
					}
					else
					{
						indexDeltaPerStep = -1;
					}
				}
				break;
			case VirtualKey.Up:
				if (isVertical)
				{
					indexDeltaPerStep = -1;
				}
				break;
			case VirtualKey.Right:
				if (!isVertical)
				{
					if (bInvertForRTL)
					{
						indexDeltaPerStep = -1;
					}
					else
					{
						indexDeltaPerStep = 1;
					}
				}
				break;
			case VirtualKey.Down:
				if (isVertical)
				{
					indexDeltaPerStep = 1;
				}
				break;
		}

		if (indexDeltaPerStep != 0 &&
			((sourceGroupIndex + indexDeltaPerStep) >= 0) &&
			((sourceGroupIndex + indexDeltaPerStep) < groupCount)
			)
		{
			bool isTargetFocusable;

			do
			{
				newGroupIndex += indexDeltaPerStep;
				isTargetFocusable = IsGroupItemFocusable(GetGroupHeaderMapping()!, (uint)newGroupIndex);
			}
			while (!isTargetFocusable && newGroupIndex > 0 && newGroupIndex < groupCount - 1);

			if (isTargetFocusable)
			{
				pIsHandled = true;
				pNewGroupIndex = newGroupIndex;
			}
		}
	}

	// Gets the number of groups we have.
	private protected int GetGroupCount()
	{
		var spCollectionView = ((IGeneratorHost)this).CollectionView;
		var spCollectionGroups = spCollectionView!.CollectionGroups;

		var spCollectionGroupsAsV = spCollectionGroups;
		var groupCount = (uint)spCollectionGroupsAsV.Count;

		var pGroupCount = (int)groupCount;

		return pGroupCount;
	}

	// Panels may decide to use the default Selector logic for keyboard
	// navigation (see QueryPanelForItemNavigation's pUseFallback). However,
	// the usual Selector logic cannot handle grouping. Instead, we re-implement
	// a grouping-aware version here. This particular method attempts to navigate
	// WITHIN a group - it returns pIsHandled = FALSE if it hits a group boundary.
	// See QueryGroupFallbackForGroupNavigation.
	private protected void QueryGroupFallbackForItemNavigation(
		Panel pGroupPanel,
		VirtualKey key,
		int sourceIndexInCollection,
		int groupLeftEdgeIndex,
		int groupRightEdgeIndex,
		out int pNewFocusedIndexInCollection,
		ref bool pIsHandled)
	{
		var newFocusedIndexInCollection = sourceIndexInCollection;

		// At this point, we know we're grouping, and we've given the group's panel a chance to handle the key nav.
		// Turns out, the panel is either not a KeyNavPanel, or the panel explicitly decided to let us deal with it.
		// So, we need to do a group-aware implementation of Selector's HandleKeyNavigation.
		// We shouldn't handle the event if we can't navigate (say, we're trying to go outside the bounds of the group).
		// Other logic will handle that.

		pNewFocusedIndexInCollection = 0;

		var nCount = (uint)(groupRightEdgeIndex - groupLeftEdgeIndex);

		var direction = FlowDirection;
		var bInvertForRTL = (direction == FlowDirection.RightToLeft);

		GetPanelOrientations(pGroupPanel, out var physicalOrientation, out _ /*pLogicalOrientation*/);
		var isVertical = (physicalOrientation == Orientation.Vertical);

		switch (key)
		{
			case VirtualKey.Left:

				if (!isVertical)
				{
					if (bInvertForRTL)
					{
						SelectNext(ref newFocusedIndexInCollection);
					}
					else
					{
						SelectPrev(ref newFocusedIndexInCollection);
					}
				}
				break;
			case VirtualKey.Up:
				if (isVertical)
				{
					SelectPrev(ref newFocusedIndexInCollection);
				}
				break;
			case VirtualKey.Right:
				if (!isVertical)
				{
					if (bInvertForRTL)
					{
						SelectPrev(ref newFocusedIndexInCollection);
					}
					else
					{
						SelectNext(ref newFocusedIndexInCollection);
					}
				}
				break;
			case VirtualKey.Down:
				if (isVertical)
				{
					SelectNext(ref newFocusedIndexInCollection);
				}
				break;
			default:
				// Don't handle these keys. They should have already been handled
				// by prior code.
				global::System.Diagnostics.Debug.Assert(false);
				break;
		}

		// Don't nav to outside of group.
		if (newFocusedIndexInCollection >= groupLeftEdgeIndex && newFocusedIndexInCollection < groupRightEdgeIndex)
		{
			pIsHandled = (newFocusedIndexInCollection != sourceIndexInCollection);
			pNewFocusedIndexInCollection = newFocusedIndexInCollection;
		}
	}


	// Called when the ListView receives focus.
	protected override void OnGotFocus(RoutedEventArgs pArgs)
	{
		base.OnGotFocus(pArgs);

		if (!IsFocusEngagementEnabled || IsFocusEngaged)
		{
			var spOriginalSource = pArgs.OriginalSource;
			var isOriginalSource = ReferenceEquals(spOriginalSource, this);

			if (isOriginalSource)
			{
				var hasFocus = HasFocus();
				OnFocusChanged(hasFocus);
			}
		}
	}

	// Called when the ListView loses focus.
	protected override void OnLostFocus(RoutedEventArgs pArgs)
	{
		base.OnLostFocus(pArgs);

		var spOriginalSource = pArgs.OriginalSource;
		var isOriginalSource = ReferenceEquals(spOriginalSource, this);
		if (isOriginalSource)
		{
			var hasFocus = HasFocus();
			OnFocusChanged(hasFocus);
		}
	}

	// Updates IsSelectionActive when the ListView receives or loses focus and
	// optionally refocuses the last focused child ListViewBaseItem.
	private protected void OnFocusChanged(bool hasFocus)
	{
		IsSelectionActive = hasFocus;

		if (hasFocus)
		{
			var targetFocusedItem = -1;

			if (GetFocusedGroupIndex() >= 0)
			{
				// Tabbing from group header to items.
				GetGroupInformation((uint)GetFocusedGroupIndex(), out var groupStartIndex, out _, out var isValid);
				if (isValid)
				{
					targetFocusedItem = (int)groupStartIndex;
				}
			}
			else if (GetFocusedIndex() >= 0)
			{
				targetFocusedItem = GetFocusedIndex();
			}
			else
			{
				targetFocusedItem = (int)GetLastFocusedIndex();
			}

			if (targetFocusedItem >= 0)
			{
				SetFocusedItem(targetFocusedItem, /* shouldScrollIntoView */ false);
			}
		}
		else
		{
			SetFocusedGroupIndex(-1);
		}
	}

	// Handles changes to GroupItems focus (as a whole collection of groups). If a group
	// header has focus, forward focus to the last focused header.
	internal void OnGroupFocusChanged(
		bool hasFocus,
		bool headerHasFocus,
		FocusState howFocusChanged)
	{
		if (hasFocus)
		{
			SetFocusedGroupIndex(GetFocusedGroupIndex() >= 0 ? GetFocusedGroupIndex() : GetLastFocusedGroupIndex());
			SetLastFocusedGroupIndex(GetFocusedGroupIndex());

			// Redirect focus if the header has focus.
			if (headerHasFocus)
			{
				var spGroupItemAsIDO = HeaderFromIndex(GetFocusedGroupIndex());
				var spGroupItem = spGroupItemAsIDO as GroupItem;
				if (spGroupItem is not null)
				{
					spGroupItem.FocusHeader(howFocusChanged);
				}
			}
		}
	}

	// Focus a ListViewBaseItem.
	internal void FocusItem(
		FocusState focusState,
		ListViewBaseItem pItem)
	{
		// TODO: Cleanup the mechanics around SetFocusedItem because it's currently
		// kind of complicated.  We've also got several methods with similar names
		// that do kind of the same things in different ways.  I'm waiting until we
		// get to InteractionManager before deciding how to split things up.
		//
		// This FocusItem(item) does the following:
		//  1. This method
		//    a. Gets and optionally stores the to-be-focused item's index as last
		//    b. Calls Focus(...) on the item
		//  3. The item handles OnGotFocus and calls its FocusChanged
		//  4. The item's FocusChanged will invoke Selector.ItemFocused
		//  5. Selector.ItemFocused stores the currently focused index
		//  6. (in parallel with 3) Any unfocused item will call
		//     Selector.ItemUnfocused
		//  7. If the caller of this SetFocusedItem also changes selection, then
		//     Selector.OnSelectionChanged will call SetFocusedItem as well.  The
		//     danger of just skipping 1a is that we may be called via ItemClick
		//     scenarios where no additional selection will take place.4
		//
		// The other SetFocusedItem(index, scroll) does the following:
		//  1. Optionally stores the to-be-focused item's index as last
		//  2. Calls Selector.SetSelectedItem which will
		//    a. Ensure the bounds of the index
		//    b. Optionally scroll the item into view (if it's desired and it can)
		//    c. Optionally lookup the container from the index and focus it
		//  3. The item handles OnGotFocus and calls its FocusChanged
		//  4. The item's FocusChanged will invoke Selector.ItemFocused
		//  5. Selector.ItemFocused stores the currently focused index
		//  6. (in parallel with 3) Any unfocused item will call
		//     Selector.ItemUnfocused

		// Get the index of the item
		var index = IndexFromContainer(pItem);

#if !HAS_UNO // LVB_DEBUG
		// TODO Uno: gps->DebugTrace has no equivalent.
		IGNOREHR(gps->DebugTrace(XCP_TRACE_OUTPUT_MSG /*traceType*/, L"LVB(%s)[0x%p]: FocusItem. focusState=%d, index=%d",
			ctl::is<xaml_controls::IGridView>(this) ? L"GV" : L"LV", this, focusState, index));
#endif

		// Store the index of the last focused item so we can jump back to this item
		// if ListViewBase loses and regains focus
		if (index >= 0)
		{
			SetLastFocusedIndex((uint)index);
		}

		// Focus the item
		pItem.FocusSelfOrChild(
			focusState,
			false /*animateIfBringIntoView*/,
			out _,
			FocusNavigationDirection.None,
			InputActivationBehavior.RequestActivation);
	}

	// Focus a ListViewBaseItem at the given index and optionally scroll it into view.
	private protected override void SetFocusedItem(
		int index,
		bool shouldScrollIntoView,
		bool animateIfBringIntoView = false,
		FocusNavigationDirection focusNavigationDirection = FocusNavigationDirection.None,
		InputActivationBehavior inputActivationBehavior = InputActivationBehavior.RequestActivation)
	{
		// Store the index of the last focused item so we can jump back to this item
		// if ListViewBase loses and regains focus
		if (index >= 0)
		{
			SetLastFocusedIndex((uint)index);
		}

		var effectiveShouldScrollIntoView = shouldScrollIntoView && !m_isInOnSelectionModeChanged;

		// Focus the item by calling Selector.SetFocusedItem
		base.SetFocusedItem(index, effectiveShouldScrollIntoView, animateIfBringIntoView, focusNavigationDirection, inputActivationBehavior);
	}

	// Set the ListViewBaseItem  at index to be focused using an explicit FocusState
	// The focus is only set if forceFocus is TRUE or the Selector already has focus.
	// ScrollIntoView is always called if shouldScrollIntoView is set (regardless of focus).
	private protected override void SetFocusedItem(
		int index,
		bool shouldScrollIntoView,
		bool forceFocus,
		FocusState focusState,
		bool animateIfBringIntoView = false,
		FocusNavigationDirection focusNavigationDirection = FocusNavigationDirection.None,
		InputActivationBehavior inputActivationBehavior = InputActivationBehavior.RequestActivation)
	{
#if !HAS_UNO // LVB_DEBUG
		// TODO Uno: gps->DebugTrace has no equivalent.
		IGNOREHR(gps->DebugTrace(XCP_TRACE_OUTPUT_MSG /*traceType*/, L"LVB(%s)[0x%p]: SetFocusedItem. index=%d, shouldScrollIntoView=%d, forceFocus=%d, focusState=%d, animateIfBringIntoView=%d, focusNavigationDirection=%d",
			ctl::is<xaml_controls::IGridView>(this) ? L"GV" : L"LV", this, index, shouldScrollIntoView, forceFocus, focusState, animateIfBringIntoView, focusNavigationDirection));
#endif

		// Store the index of the last focused item so we can jump back to this item
		// if ListViewBase loses and regains focus
		if (index >= 0)
		{
			SetLastFocusedIndex((uint)index);
		}

		var effectiveShouldScrollIntoView = shouldScrollIntoView && !m_isInOnSelectionModeChanged;

		// If i need to scroll intoview, check if the host panel
		// is in a state where we can force scroll into view.
		if (effectiveShouldScrollIntoView)
		{
			var spItemsHostPanel = ItemsPanelRoot;
			var spItemsHostPanelModernCollection = spItemsHostPanel as ModernCollectionBasePanel;
			if (spItemsHostPanelModernCollection is not null)
			{
				effectiveShouldScrollIntoView = spItemsHostPanelModernCollection.CanScrollIntoView();
			}
		}

		// Focus the item by calling Selector.SetFocusedItem
		base.SetFocusedItem(index, effectiveShouldScrollIntoView, forceFocus, focusState, animateIfBringIntoView, focusNavigationDirection, inputActivationBehavior);
	}

	// Handles changes to the IsSelectionActive property by updating the visual
	// states of all the currently selected ListViewBaseItems.
	private void OnIsSelectionActiveChanged()
	{
		var selectedCount = m_selection.GetNumItemsSelected();
		if (selectedCount > 0)
		{
			for (uint i = 0; i < selectedCount; i++)
			{
				var itemIndex = m_selection.GetIndexAt(i);
				var spContainer = ContainerFromIndex((int)itemIndex);
				// If the container at given Index is not relaised, it will return NULL
				// If it is realised then only VisualStates needs to be updated
				if (spContainer is not null)
				{
					var spItem = spContainer as SelectorItem;
					if (spItem is not null && IsListViewBaseItem(spItem))
					{
						((ListViewBaseItem)spItem).ChangeVisualStateFromOwner(true);
					}
				}
			}
		}
	}

	internal void SetHoldingItem(ListViewBaseItem pItem)
	{
		if (m_tpHoldingItem != pItem)
		{
			ClearHoldingState();
			m_tpHoldingItem = pItem;
		}
	}

	internal void ClearHoldingItem(ListViewBaseItem pItem)
	{
		if (m_tpHoldingItem == pItem)
		{
			m_tpHoldingItem = null;
		}
	}

	internal void ClearHoldingState()
	{
		if (m_tpHoldingItem is not null)
		{
			var holdingItem = (ListViewBaseItem)m_tpHoldingItem;
			holdingItem.ClearHoldingState();
			ClearHoldingItem(holdingItem);
			SetIsHolding(false);
		}
	}

	// Returns TRUE if the ListViewBase is currently engaged in an exclusive interaction
	// and should thus ignore all new interactions.
	internal bool IsInExclusiveInteraction()
	{
		return IsInDragDrop();
	}

	// Checks whether the group item returned for the given index
	// by pGenerator is focusable.
	private static bool IsGroupItemFocusable(
		IGroupHeaderMapping pMapping,
		uint groupIndex)
	{
		var pIsFocusable = false;
		bool isFocusable;

		var spGroupItemAsIDO = pMapping.HeaderFromIndex((int)groupIndex);

		var spGroupItemAsControl = spGroupItemAsIDO as Control;

		if (spGroupItemAsControl is not null)
		{
			isFocusable = spGroupItemAsControl.IsEnabled;
		}
		else
		{
			// Non-Controls focusable by default (see ItemsControl::IsFocusableHelper).
			isFocusable = true;
		}

		if (isFocusable)
		{
			var spGroupItemAsUIE = spGroupItemAsIDO as UIElement;
			if (spGroupItemAsUIE is not null)
			{
				var visibility = spGroupItemAsUIE.Visibility;
				isFocusable = visibility != Visibility.Collapsed;
			}
		}

		pIsFocusable = isFocusable;

		return pIsFocusable;
	}

	// Checks whether the item container returned for the given index
	// by pGenerator is focusable.
	private static bool IsItemFocusable(
		IItemContainerMapping pMapping,
		uint itemIndex,
		bool unrealizedItemsAssumedFocusable)
	{
		var pIsFocusable = false;

		var spContainerAsIDO = pMapping.ContainerFromIndex((int)itemIndex);

		if (spContainerAsIDO is not null)
		{
			var spContainerAsI = (object?)spContainerAsIDO;
			var isFocusable = IsFocusableHelper(spContainerAsI);
			pIsFocusable = isFocusable;
		}
		else if (unrealizedItemsAssumedFocusable)
		{
			pIsFocusable = true;
		}

		return pIsFocusable;
	}

	private bool CanPerformKeyboardNavigation(VirtualKey key)
	{
		var canNavigate = true;

		var itemsHost = ItemsPanelRoot;

		if (itemsHost is null)
		{
			canNavigate = false;
		}
		else
		{
			GetItemsHostOrientations(out var physicalOrientation, out _ /* pLogicalOrientation */);

			// Triggers should navigate only for vertical lists, and Shoulders should navigate only for horizontal lists
			if ((key == VirtualKey.GamepadLeftTrigger || key == VirtualKey.GamepadRightTrigger) &&
				physicalOrientation != Orientation.Vertical)
			{
				canNavigate = false;
			}

			if ((key == VirtualKey.GamepadLeftShoulder || key == VirtualKey.GamepadRightShoulder) &&
				physicalOrientation != Orientation.Horizontal)
			{
				canNavigate = false;
			}
		}

		return canNavigate;
	}
}


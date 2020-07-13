// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Windows.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Controls
{

	public partial class TabView : Control
	{
		private const double c_tabMinimumWidth = 48.0;
		private const double c_tabMaximumWidth = 200.0;

		private const string c_tabViewItemMinWidthName = "TabViewItemMinWidth";
		private const string c_tabViewItemMaxWidthName = "TabViewItemMaxWidth";

		// TODO: what is the right number and should this be customizable?
		private const double c_scrollAmount = 50.0;

		public TabView()
		{
			var items = winrt.make < Vector < winrt.DependencyObject, MakeVectorParam< VectorFlag.Observable > () >> ();
			SetValue(s_TabItemsProperty, items);

			SetDefaultStyleKey(this);

			Loaded({ this, &TabView.OnLoaded });

			// KeyboardAccelerator is only available on RS3+
			if (SharedHelpers.IsRS3OrHigher())
			{
				winrt.KeyboardAccelerator ctrlf4Accel;
				ctrlf4Accel.Key(winrt.VirtualKey.F4);
				ctrlf4Accel.Modifiers(winrt.VirtualKeyModifiers.Control);
				ctrlf4Accel.Invoked({ this, &TabView.OnCtrlF4Invoked });
				ctrlf4Accel.ScopeOwner(*this);
				KeyboardAccelerators().Append(ctrlf4Accel);

				m_tabCloseButtonTooltipText = ResourceAccessor.GetLocalizedStringResource(SR_TabViewCloseButtonTooltipWithKA);
			}
			else
			{
				m_tabCloseButtonTooltipText = ResourceAccessor.GetLocalizedStringResource(SR_TabViewCloseButtonTooltip);
			}

			// Ctrl+Tab as a KeyboardAccelerator only works on 19H1+
			if (SharedHelpers.Is19H1OrHigher())
			{
				winrt.KeyboardAccelerator ctrlTabAccel;
				ctrlTabAccel.Key(winrt.VirtualKey.Tab);
				ctrlTabAccel.Modifiers(winrt.VirtualKeyModifiers.Control);
				ctrlTabAccel.Invoked({ this, &TabView.OnCtrlTabInvoked });
				ctrlTabAccel.ScopeOwner(*this);
				KeyboardAccelerators().Append(ctrlTabAccel);

				winrt.KeyboardAccelerator ctrlShiftTabAccel;
				ctrlShiftTabAccel.Key(winrt.VirtualKey.Tab);
				ctrlShiftTabAccel.Modifiers(winrt.VirtualKeyModifiers.Control | winrt.VirtualKeyModifiers.Shift);
				ctrlShiftTabAccel.Invoked({ this, &TabView.OnCtrlShiftTabInvoked });
				ctrlShiftTabAccel.ScopeOwner(*this);
				KeyboardAccelerators().Append(ctrlShiftTabAccel);
			}
		}
	}
}

void OnApplyTemplate()
{
	UnhookEventsAndClearFields();

	winrt.IControlProtected controlProtected{ *this };

	m_tabContentPresenter.set(GetTemplateChildT<winrt.ContentPresenter>("TabContentPresenter", controlProtected));
	m_rightContentPresenter.set(GetTemplateChildT<winrt.ContentPresenter>("RightContentPresenter", controlProtected));

	m_leftContentColumn.set(GetTemplateChildT<winrt.ColumnDefinition>("LeftContentColumn", controlProtected));
	m_tabColumn.set(GetTemplateChildT<winrt.ColumnDefinition>("TabColumn", controlProtected));
	m_addButtonColumn.set(GetTemplateChildT<winrt.ColumnDefinition>("AddButtonColumn", controlProtected));
	m_rightContentColumn.set(GetTemplateChildT<winrt.ColumnDefinition>("RightContentColumn", controlProtected));

	if (auto & containerGrid = GetTemplateChildT<winrt.Grid>("TabContainerGrid", controlProtected))
	{
		m_tabContainerGrid.set(containerGrid);
		m_tabStripPointerExitedRevoker = containerGrid.PointerExited(winrt.auto_revoke, { this,&TabView.OnTabStripPointerExited });
	}

	m_shadowReceiver.set(GetTemplateChildT<winrt.Grid>("ShadowReceiver", controlProtected));

	m_listView.set([this, controlProtected]() {
		var listView = GetTemplateChildT<winrt.ListView>("TabListView", controlProtected);
		if (listView)
		{
			m_listViewLoadedRevoker = listView.Loaded(winrt.auto_revoke, { this, &TabView.OnListViewLoaded });
			m_listViewSelectionChangedRevoker = listView.SelectionChanged(winrt.auto_revoke, { this, &TabView.OnListViewSelectionChanged });

			m_listViewDragItemsStartingRevoker = listView.DragItemsStarting(winrt.auto_revoke, { this, &TabView.OnListViewDragItemsStarting });
			m_listViewDragItemsCompletedRevoker = listView.DragItemsCompleted(winrt.auto_revoke, { this, &TabView.OnListViewDragItemsCompleted });
			m_listViewDragOverRevoker = listView.DragOver(winrt.auto_revoke, { this, &TabView.OnListViewDragOver });
			m_listViewDropRevoker = listView.Drop(winrt.auto_revoke, { this, &TabView.OnListViewDrop });

			m_listViewGettingFocusRevoker = listView.GettingFocus(winrt.auto_revoke, { this, &TabView.OnListViewGettingFocus });

			m_listViewCanReorderItemsPropertyChangedRevoker = RegisterPropertyChanged(listView, winrt.ListViewBase.CanReorderItemsProperty(), { this, &TabView.OnListViewDraggingPropertyChanged });
			m_listViewAllowDropPropertyChangedRevoker = RegisterPropertyChanged(listView, winrt.UIElement.AllowDropProperty(), { this, &TabView.OnListViewDraggingPropertyChanged });
		}
		return listView;
	} ());

	m_addButton.set([this, controlProtected]() {
		var addButton = GetTemplateChildT<winrt.Button>("AddButton", controlProtected);
		if (addButton)
		{
			// Do localization for the add button
			if (winrt.AutomationProperties.GetName(addButton).empty())
			{
				var addButtonName = ResourceAccessor.GetLocalizedStringResource(SR_TabViewAddButtonName);
				winrt.AutomationProperties.SetName(addButton, addButtonName);
			}

			var toolTip = winrt.ToolTipService.GetToolTip(addButton);
			if (!toolTip)
			{
				winrt.ToolTip tooltip = winrt.ToolTip();
				tooltip.Content(box_value(ResourceAccessor.GetLocalizedStringResource(SR_TabViewAddButtonTooltip)));
				winrt.ToolTipService.SetToolTip(addButton, tooltip);
			}

			m_addButtonClickRevoker = addButton.Click(winrt.auto_revoke, { this, &TabView.OnAddButtonClick });
		}
		return addButton;
	} ());

	if (SharedHelpers.IsThemeShadowAvailable())
	{
		if (var shadowCaster = GetTemplateChildT<winrt.Grid>("ShadowCaster", controlProtected))
        {
			winrt.ThemeShadow shadow;
			shadow.Receivers().Append(GetShadowReceiver());

			double shadowDepth = unbox_value<double>(SharedHelpers.FindInApplicationResources(c_tabViewShadowDepthName, box_value(c_tabShadowDepth)));

			var currentTranslation = shadowCaster.Translation();
			var translation = winrt.float3{ currentTranslation.x, currentTranslation.y, (float)shadowDepth };
			shadowCaster.Translation(translation);

			shadowCaster.Shadow(shadow);
		}
	}

	UpdateListViewItemContainerTransitions();
}

void OnListViewDraggingPropertyChanged(winrt.DependencyObject& sender, winrt.DependencyProperty& args)
{
	UpdateListViewItemContainerTransitions();
}

void OnListViewGettingFocus(winrt.DependencyObject& sender, winrt.GettingFocusEventArgs& args)
{
	// TabViewItems overlap each other by one pixel in order to get the desired visuals for the separator.
	// This causes problems with 2d focus navigation. Because the items overlap, pressing Down or Up from a
	// TabViewItem navigates to the overlapping item which is not desired.
	//
	// To resolve this issue, we detect the case where Up or Down focus navigation moves from one TabViewItem
	// to another.
	// How we handle it, depends on the input device.
	// For GamePad, we want to move focus to something in the direction of movement (other than the overlapping item)
	// For Keyboard, we cancel the focus movement.

	var direction = args.Direction();
	if (direction == winrt.FocusNavigationDirection.Up || direction == winrt.FocusNavigationDirection.Down)
	{
		var oldItem = args.OldFocusedElement().try_as<winrt.TabViewItem>();
		var newItem = args.NewFocusedElement().try_as<winrt.TabViewItem>();
		if (oldItem && newItem)
		{
			if (auto && listView = m_listView.get())
			{
				bool oldItemIsFromThisTabView = listView.IndexFromContainer(oldItem) != -1;
				bool newItemIsFromThisTabView = listView.IndexFromContainer(newItem) != -1;
				if (oldItemIsFromThisTabView && newItemIsFromThisTabView)
				{
					var inputDevice = args.InputDevice();
					if (inputDevice == winrt.FocusInputDeviceKind.GameController)
					{
						var listViewBoundsLocal = winrt.Rect{ 0, 0, (float)(listView.ActualWidth()), (float)(listView.ActualHeight()) };
						var listViewBounds = listView.TransformToVisual(null).TransformBounds(listViewBoundsLocal);
						winrt.FindNextElementOptions options;
						options.ExclusionRect(listViewBounds);
						var next = winrt.FocusManager.FindNextElement(direction, options);
						if (var args2 = args.try_as<winrt.IGettingFocusEventArgs2>())
                        {
							args2.TrySetNewFocusedElement(next);
						}

						else
						{
							// Without TrySetNewFocusedElement, we cannot set focus while it is changing.
							m_dispatcherHelper.RunAsync([next]()

							{
								SetFocus(next, winrt.FocusState.Programmatic);
							});
						}
						args.Handled(true);
					}
					else
					{
						args.Cancel(true);
						args.Handled(true);
					}
				}
			}
		}
	}
}

void OnSelectedIndexPropertyChanged(winrt.DependencyPropertyChangedEventArgs& args)
{
	UpdateSelectedIndex();
}

void OnSelectedItemPropertyChanged(winrt.DependencyPropertyChangedEventArgs& args)
{
	UpdateSelectedItem();
}

void OnTabItemsSourcePropertyChanged(winrt.DependencyPropertyChangedEventArgs&)
{
	UpdateListViewItemContainerTransitions();
}

void UpdateListViewItemContainerTransitions()
{
	if (TabItemsSource())
	{
		if (auto && listView = m_listView.get())
		{
			if (listView.CanReorderItems() && listView.AllowDrop())
			{
				// Remove all the AddDeleteThemeTransition/ContentThemeTransition instances in the inner ListView's ItemContainerTransitions
				// collection to avoid attempting to reparent a tab's content while it is still parented during a tab reordering user gesture.
				// This is only required when:
				//  - the TabViewItem' contents are databound to UIElements (this condition is not being checked below though).
				//  - System animations turned on (this condition is not being checked below though to maximize behavior consistency).
				//  - TabViewItem reordering is turned on.
				// With all those conditions met, the databound UIElements are still parented to the old item container as the tab is being dropped in
				// its new location. Without animations, the old item container is already put into the recycling pool and picked as the new container.
				// Its ContentControl.Content is kept unchanged and no reparenting is attempted.
				// Because the default ItemContainerTransitions collection is defined in the TabViewListView style, all ListView instances share the same
				// collection by default. Thus to avoid one TabView affecting all other ones, a new ItemContainerTransitions collection is created
				// when the original one contains an AddDeleteThemeTransition or ContentThemeTransition instance.
				bool transitionCollectionHasAddDeleteOrContentThemeTransition = [listView]()

				{
					if (var itemContainerTransitions = listView.ItemContainerTransitions())
                    {
						for (auto && transition : itemContainerTransitions)
						{
							if (transition &&
								(transition.try_as<winrt.AddDeleteThemeTransition>() || transition.try_as<winrt.ContentThemeTransition>()))
							{
								return true;
							}
						}
					}
					return false;
				} ();

				if (transitionCollectionHasAddDeleteOrContentThemeTransition)
				{
					var newItemContainerTransitions = winrt.TransitionCollection();
					var oldItemContainerTransitions = listView.ItemContainerTransitions();

					for (auto && transition : oldItemContainerTransitions)
					{
						if (transition)
						{
							if (transition.try_as<winrt.AddDeleteThemeTransition>() || transition.try_as<winrt.ContentThemeTransition>())
							{
								continue;
							}
							newItemContainerTransitions.Append(transition);
						}
					}

					listView.ItemContainerTransitions(newItemContainerTransitions);
				}
			}
		}
	}
}

void UnhookEventsAndClearFields()
{
	m_listViewLoadedRevoker.revoke();
	m_listViewSelectionChangedRevoker.revoke();
	m_listViewDragItemsStartingRevoker.revoke();
	m_listViewDragItemsCompletedRevoker.revoke();
	m_listViewDragOverRevoker.revoke();
	m_listViewDropRevoker.revoke();
	m_listViewGettingFocusRevoker.revoke();
	m_listViewCanReorderItemsPropertyChangedRevoker.revoke();
	m_listViewAllowDropPropertyChangedRevoker.revoke();
	m_addButtonClickRevoker.revoke();
	m_itemsPresenterSizeChangedRevoker.revoke();
	m_tabStripPointerExitedRevoker.revoke();
	m_scrollViewerLoadedRevoker.revoke();
	m_scrollViewerViewChangedRevoker.revoke();
	m_scrollDecreaseClickRevoker.revoke();
	m_scrollIncreaseClickRevoker.revoke();

	m_tabContentPresenter.set(null);
	m_rightContentPresenter.set(null);
	m_leftContentColumn.set(null);
	m_tabColumn.set(null);
	m_addButtonColumn.set(null);
	m_rightContentColumn.set(null);
	m_tabContainerGrid.set(null);
	m_shadowReceiver.set(null);
	m_listView.set(null);
	m_addButton.set(null);
	m_itemsPresenter.set(null);
	m_scrollViewer.set(null);
	m_scrollDecreaseButton.set(null);
	m_scrollIncreaseButton.set(null);
}

void OnTabWidthModePropertyChanged(winrt.DependencyPropertyChangedEventArgs&)
{
	UpdateTabWidths();

	// Switch the visual states of all tab items to the correct TabViewWidthMode
	for (auto && item : TabItems())
	{
		var tvi = [item, this]()

		{
			if (var tabViewItem = item.try_as<TabViewItem>())
            {
		return tabViewItem;
	}
	return ContainerFromItem(item).try_as<TabViewItem>();
}
();

        if (tvi)
        {
            tvi.OnTabViewWidthModeChanged(TabWidthMode());
        }
    }
}

void OnCloseButtonOverlayModePropertyChanged(winrt.DependencyPropertyChangedEventArgs&)
{
	// Switch the visual states of all tab items to to the correct closebutton overlay mode
	for (auto && item : TabItems())
	{
		var tvi = [item, this]()

		{
			if (var tabViewItem = item.try_as<TabViewItem>())
            {
		return tabViewItem;
	}
	return ContainerFromItem(item).try_as<TabViewItem>();
}
();

        if (tvi)
        {
            tvi.OnCloseButtonOverlayModeChanged(CloseButtonOverlayMode());
        }
    }
}

void OnAddButtonClick(winrt.DependencyObject&, winrt.RoutedEventArgs& args)
{
	m_addTabButtonClickEventSource(*this, args);
}

winrt.AutomationPeer OnCreateAutomationPeer()
{
	return winrt.make<TabViewAutomationPeer>(*this);
}

void OnLoaded(winrt.DependencyObject&, winrt.RoutedEventArgs&)
{
	UpdateTabContent();
}

void OnListViewLoaded(winrt.DependencyObject&, winrt.RoutedEventArgs& args)
{
	if (auto && listView = m_listView.get())
	{
		// Now that ListView exists, we can start using its Items collection.
		if (var  lvItems = listView.Items())
        {
			if (!listView.ItemsSource())
			{
				// copy the list, because clearing lvItems may also clear TabItems
				winrt.IVector<winrt.DependencyObject> itemList{ winrt.single_threaded_vector<winrt.DependencyObject>() };

				for (var item : TabItems())
				{
					itemList.Append(item);
				}

				lvItems.Clear();

				for (var item : itemList)
				{
					// App put items in our Items collection; copy them over to ListView.Items
					if (item)
					{
						lvItems.Append(item);
					}
				}
			}
			TabItems(lvItems);
		}

		if (ReadLocalValue(s_SelectedItemProperty) != winrt.DependencyProperty.UnsetValue())
		{
			UpdateSelectedItem();
		}
		else
		{
			// If SelectedItem wasn't set, default to selecting the first tab
			UpdateSelectedIndex();
		}

		SelectedIndex(listView.SelectedIndex());
		SelectedItem(listView.SelectedItem());

		// Find TabsItemsPresenter and listen for SizeChanged
		m_itemsPresenter.set([this, listView]() {
			var itemsPresenter = SharedHelpers.FindInVisualTreeByName(listView, "TabsItemsPresenter").as< winrt.ItemsPresenter > ();
			if (itemsPresenter)
			{
				m_itemsPresenterSizeChangedRevoker = itemsPresenter.SizeChanged(winrt.auto_revoke, { this, &TabView.OnItemsPresenterSizeChanged });
			}
			return itemsPresenter;
		} ());

		var scrollViewer = SharedHelpers.FindInVisualTreeByName(listView, "ScrollViewer").as< winrt.FxScrollViewer > ();
		m_scrollViewer.set(scrollViewer);
		if (scrollViewer)
		{
			if (SharedHelpers.IsIsLoadedAvailable() && scrollViewer.IsLoaded())
			{
				// This scenario occurs reliably for Terminal in XAML islands
				OnScrollViewerLoaded(null, null);
			}
			else
			{
				m_scrollViewerLoadedRevoker = scrollViewer.Loaded(winrt.auto_revoke, { this, &TabView.OnScrollViewerLoaded });
			}
		}
	}
}

void OnTabStripPointerExited(winrt.DependencyObject& sender, winrt.PointerRoutedEventArgs& args)
{
	if (m_updateTabWidthOnPointerLeave)
	{
		var scopeGuard = gsl.finally([this]()

		{
			m_updateTabWidthOnPointerLeave = false;
		});
		UpdateTabWidths();
		}
	}

	void OnScrollViewerLoaded(winrt.DependencyObject&,  winrt.RoutedEventArgs & args)
{
		if (auto && scrollViewer = m_scrollViewer.get())
		{
			m_scrollDecreaseButton.set([this, scrollViewer]() {
				var decreaseButton = SharedHelpers.FindInVisualTreeByName(scrollViewer, "ScrollDecreaseButton").as< winrt.RepeatButton > ();
				if (decreaseButton)
				{
					// Do localization for the scroll decrease button
					var toolTip = winrt.ToolTipService.GetToolTip(decreaseButton);
					if (!toolTip)
					{
						var tooltip = winrt.ToolTip();
						tooltip.Content(box_value(ResourceAccessor.GetLocalizedStringResource(SR_TabViewScrollDecreaseButtonTooltip)));
						winrt.ToolTipService.SetToolTip(decreaseButton, tooltip);
					}

					m_scrollDecreaseClickRevoker = decreaseButton.Click(winrt.auto_revoke, { this, &TabView.OnScrollDecreaseClick });
				}
				return decreaseButton;
			} ());

			m_scrollIncreaseButton.set([this, scrollViewer]() {
				var increaseButton = SharedHelpers.FindInVisualTreeByName(scrollViewer, "ScrollIncreaseButton").as< winrt.RepeatButton > ();
				if (increaseButton)
				{
					// Do localization for the scroll increase button
					var toolTip = winrt.ToolTipService.GetToolTip(increaseButton);
					if (!toolTip)
					{
						var tooltip = winrt.ToolTip();
						tooltip.Content(box_value(ResourceAccessor.GetLocalizedStringResource(SR_TabViewScrollIncreaseButtonTooltip)));
						winrt.ToolTipService.SetToolTip(increaseButton, tooltip);
					}

					m_scrollIncreaseClickRevoker = increaseButton.Click(winrt.auto_revoke, { this, &TabView.OnScrollIncreaseClick });
				}
				return increaseButton;
			} ());

			m_scrollViewerViewChangedRevoker = scrollViewer.ViewChanged(winrt.auto_revoke, { this, &TabView.OnScrollViewerViewChanged });
		}

		UpdateTabWidths();
	}

	void OnScrollViewerViewChanged(winrt.DependencyObject &sender, winrt.ScrollViewerViewChangedEventArgs & args)
{
		UpdateScrollViewerDecreaseAndIncreaseButtonsViewState();
	}

	void UpdateScrollViewerDecreaseAndIncreaseButtonsViewState()
	{
		if (auto && scrollViewer = m_scrollViewer.get())
		{
			auto && decreaseButton = m_scrollDecreaseButton.get();
			auto && increaseButton = m_scrollIncreaseButton.get();

			var minThreshold = 0.1;
			var horizontalOffset = scrollViewer.HorizontalOffset();
			var scrollableWidth = scrollViewer.ScrollableWidth();

			if (abs(horizontalOffset - scrollableWidth) < minThreshold)
			{
				if (decreaseButton)
				{
					decreaseButton.IsEnabled(true);
				}
				if (increaseButton)
				{
					increaseButton.IsEnabled(false);
				}
			}
			else if (abs(horizontalOffset) < minThreshold)
			{
				if (decreaseButton)
				{
					decreaseButton.IsEnabled(false);
				}
				if (increaseButton)
				{
					increaseButton.IsEnabled(true);
				}
			}
			else
			{
				if (decreaseButton)
				{
					decreaseButton.IsEnabled(true);
				}
				if (increaseButton)
				{
					increaseButton.IsEnabled(true);
				}
			}
		}
	}

	void OnItemsPresenterSizeChanged(winrt.DependencyObject&sender,  winrt.SizeChangedEventArgs & args)
{
		if (!m_updateTabWidthOnPointerLeave)
		{
			// Presenter size didn't change because of item being removed, so update manually
			UpdateScrollViewerDecreaseAndIncreaseButtonsViewState();
			UpdateTabWidths();
		}
	}

	void OnItemsChanged(winrt.DependencyObject &item)
{
		if (var args = item.as< winrt.IVectorChangedEventArgs > ())
    {
			m_tabItemsChangedEventSource(*this, args);

			int numItems = (int)(TabItems().Size());

			if (args.CollectionChange() == winrt.CollectionChange.ItemRemoved)
			{
				m_updateTabWidthOnPointerLeave = true;
				if (numItems > 0)
				{
					// SelectedIndex might also already be -1
					var selectedIndex = SelectedIndex();
					if (selectedIndex == -1 || selectedIndex == (int32_t)(args.Index()))
					{
						// Find the closest tab to select instead.
						int startIndex = (int)(args.Index());
						if (startIndex >= numItems)
						{
							startIndex = numItems - 1;
						}
						int index = startIndex;

						do
						{
							var nextItem = ContainerFromIndex(index).as< winrt.ListViewItem > ();

							if (nextItem && nextItem.IsEnabled() && nextItem.Visibility() == winrt.Visibility.Visible)
							{
								SelectedItem(TabItems().GetAt(index));
								break;
							}

							// try the next item
							index++;
							if (index >= numItems)
							{
								index = 0;
							}
						} while (index != startIndex);
					}

				}
				// Last item removed, update sizes
				// The index of the last element is "Size() - 1", but in TabItems, it is already removed.
				if (TabWidthMode() == winrt.TabViewWidthMode.Equal)
				{
					m_updateTabWidthOnPointerLeave = true;
					if (args.Index() == TabItems().Size())
					{
						UpdateTabWidths(true, false);
					}
				}
			}
			else
			{
				if (var newItem = TabItems().GetAt(args.Index()).try_as<TabViewItem>())
            {
					newItem.OnTabViewWidthModeChanged(TabWidthMode());
				}
				UpdateTabWidths();
			}
		}

	}

	void OnListViewSelectionChanged(winrt.DependencyObject&sender,  winrt.SelectionChangedEventArgs & args)
{
		if (auto && listView = m_listView.get())
		{
			SelectedIndex(listView.SelectedIndex());
			SelectedItem(listView.SelectedItem());
		}

		UpdateTabContent();

		m_selectionChangedEventSource(*this, args);
	}

	winrt.TabViewItem FindTabViewItemFromDragItem(winrt.DependencyObject&item)
{
		var tab = ContainerFromItem(item).try_as<winrt.TabViewItem>();

		if (!tab)
		{
			if (var fe = item.try_as<winrt.FrameworkElement>())
        {
				tab = winrt.VisualTreeHelper.GetParent(fe).try_as<winrt.TabViewItem>();
			}
		}

		if (!tab)
		{
			// This is a fallback scenario for tabs without a data context
			var numItems = (int)(TabItems().Size());
			for (int i = 0; i < numItems; i++)
			{
				var tabItem = ContainerFromIndex(i).try_as<winrt.TabViewItem>();
				if (tabItem.Content() == item)
				{
					tab = tabItem;
					break;
				}
			}
		}

		return tab;
	}

	void OnListViewDragItemsStarting(winrt.DependencyObject&sender,  winrt.DragItemsStartingEventArgs & args)
{
		var item = args.Items().GetAt(0);
		var tab = FindTabViewItemFromDragItem(item);
		var myArgs = winrt.make_self<TabViewTabDragStartingEventArgs>(args, item, tab);

		m_tabDragStartingEventSource(*this, *myArgs);
	}

	void OnListViewDragOver(winrt.DependencyObject&sender,  winrt.DragEventArgs & args)
{
		m_tabStripDragOverEventSource(*this, args);
	}

	void OnListViewDrop(winrt.DependencyObject&sender,  winrt.DragEventArgs & args)
{
		m_tabStripDropEventSource(*this, args);
	}

	void OnListViewDragItemsCompleted(winrt.DependencyObject&sender,  winrt.DragItemsCompletedEventArgs & args)
{
		var item = args.Items().GetAt(0);
		var tab = FindTabViewItemFromDragItem(item);
		var myArgs = winrt.make_self<TabViewTabDragCompletedEventArgs>(args, item, tab);

		m_tabDragCompletedEventSource(*this, *myArgs);

		// None means it's outside of the tab strip area
		if (args.DropResult() == winrt.DataPackageOperation.None)
		{
			var tabDroppedArgs = winrt.make_self<TabViewTabDroppedOutsideEventArgs>(item, tab);
			m_tabDroppedOutsideEventSource(*this, *tabDroppedArgs);
		}
	}

	void UpdateTabContent()
	{
		if (auto && tabContentPresenter = m_tabContentPresenter.get())
		{
			if (!SelectedItem())
			{
				tabContentPresenter.Content(null);
				tabContentPresenter.ContentTemplate(null);
				tabContentPresenter.ContentTemplateSelector(null);
			}
			else
			{
				var tvi = SelectedItem().try_as<winrt.TabViewItem>();
				if (!tvi)
				{
					tvi = ContainerFromItem(SelectedItem()).try_as<winrt.TabViewItem>();
				}

				if (tvi)
				{
					// If the focus was in the old tab content, we will lose focus when it is removed from the visual tree.
					// We should move the focus to the new tab content.
					// The new tab content is not available at the time of the LosingFocus event, so we need to
					// move focus later.
					bool shouldMoveFocusToNewTab = false;
					var revoker = tabContentPresenter.LosingFocus(winrt.auto_revoke, [&shouldMoveFocusToNewTab](winrt.DependencyObject &, winrt.LosingFocusEventArgs & args)
	
				{
						shouldMoveFocusToNewTab = true;
					});

	tabContentPresenter.Content(tvi.Content());
	tabContentPresenter.ContentTemplate(tvi.ContentTemplate());
	tabContentPresenter.ContentTemplateSelector(tvi.ContentTemplateSelector());

	// It is not ideal to call UpdateLayout here, but it is necessary to ensure that the ContentPresenter has expanded its content
	// into the live visual tree.
	tabContentPresenter.UpdateLayout();

	if (shouldMoveFocusToNewTab)
	{
		var focusable = winrt.FocusManager.FindFirstFocusableElement(tabContentPresenter);
		if (!focusable)
		{
			// If there is nothing focusable in the new tab, just move focus to the TabViewItem itself.
			focusable = tvi;
		}

		if (focusable)
		{
			SetFocus(focusable, winrt.FocusState.Programmatic);
		}
	}
}
        }
    }
}

void RequestCloseTab(winrt.TabViewItem & container)
{
	if (auto && listView = m_listView.get())
	{
		var args = winrt.make_self<TabViewTabCloseRequestedEventArgs>(listView.ItemFromContainer(container), container);

		m_tabCloseRequestedEventSource(*this, *args);

		if (var internalTabViewItem = winrt.get_self<TabViewItem>(container))
        {
			internalTabViewItem.RaiseRequestClose(*args);
		}
	}
	UpdateTabWidths(false);
}

void OnScrollDecreaseClick(winrt.DependencyObject&, winrt.RoutedEventArgs&)
{
	if (auto && scrollViewer = m_scrollViewer.get())
	{
		scrollViewer.ChangeView(std.max(0.0, scrollViewer.HorizontalOffset() - c_scrollAmount), null, null);
	}
}

void OnScrollIncreaseClick(winrt.DependencyObject&, winrt.RoutedEventArgs&)
{
	if (auto && scrollViewer = m_scrollViewer.get())
	{
		scrollViewer.ChangeView(std.min(scrollViewer.ScrollableWidth(), scrollViewer.HorizontalOffset() + c_scrollAmount), null, null);
	}
}

winrt.Size MeasureOverride(winrt.Size & availableSize)
{
	if (previousAvailableSize.Width != availableSize.Width)
	{
		previousAvailableSize = availableSize;
		UpdateTabWidths();
	}

	return __super.MeasureOverride(availableSize);
}

void UpdateTabWidths(bool shouldUpdateWidths, bool fillAllAvailableSpace)
{
	double tabWidth = std.numeric_limits<double>.quiet_NaN();

	if (auto && tabGrid = m_tabContainerGrid.get())
	{
		// Add up width taken by custom content and + button
		double widthTaken = 0.0;
		if (auto && leftContentColumn = m_leftContentColumn.get())
		{
			widthTaken += leftContentColumn.ActualWidth();
		}
		if (auto && addButtonColumn = m_addButtonColumn.get())
		{
			widthTaken += addButtonColumn.ActualWidth();
		}
		if (auto && rightContentColumn = m_rightContentColumn.get())
		{
			if (auto && rightContentPresenter = m_rightContentPresenter.get())
			{
				winrt.Size rightContentSize = rightContentPresenter.DesiredSize();
				rightContentColumn.MinWidth(rightContentSize.Width);
				widthTaken += rightContentSize.Width;
			}
		}

		if (auto && tabColumn = m_tabColumn.get())
		{
			// Note: can be infinite
			var availableWidth = previousAvailableSize.Width - widthTaken;

			// Size can be 0 when window is first created; in that case, skip calculations; we'll get a new size soon
			if (availableWidth > 0)
			{
				if (TabWidthMode() == winrt.TabViewWidthMode.Equal)
				{

					var minTabWidth = unbox_value<double>(SharedHelpers.FindInApplicationResources(c_tabViewItemMinWidthName, box_value(c_tabMinimumWidth)));
					var maxTabWidth = unbox_value<double>(SharedHelpers.FindInApplicationResources(c_tabViewItemMaxWidthName, box_value(c_tabMaximumWidth)));

					// If we should fill all of the available space, use scrollviewer dimensions
					var padding = Padding();
					if (fillAllAvailableSpace)
					{
						// Calculate the proportional width of each tab given the width of the ScrollViewer.
						var tabWidthForScroller = (availableWidth - (padding.Left + padding.Right)) / (double)(TabItems().Size());
						tabWidth = std.clamp(tabWidthForScroller, minTabWidth, maxTabWidth);
					}
					else
					{
						double availableTabViewSpace = (tabColumn.ActualWidth() - (padding.Left + padding.Right));
						if (var increaseButton = m_scrollIncreaseButton.get())
                        {
							if (increaseButton.Visibility() == winrt.Visibility.Visible)
							{
								availableTabViewSpace -= increaseButton.ActualWidth();
							}
						}

						if (var decreaseButton = m_scrollDecreaseButton.get())
                        {
							if (decreaseButton.Visibility() == winrt.Visibility.Visible)
							{
								availableTabViewSpace -= decreaseButton.ActualWidth();
							}
						}

						// Use current size to update items to fill the currently occupied space
						tabWidth = availableTabViewSpace / (double)(TabItems().Size());
					}


					// Size tab column to needed size
					tabColumn.MaxWidth(availableWidth);
					var requiredWidth = tabWidth * TabItems().Size();
					if (requiredWidth >= availableWidth)
					{
						tabColumn.Width(winrt.GridLengthHelper.FromPixels(availableWidth));
						if (auto && listview = m_listView.get())
						{
							winrt.FxScrollViewer.SetHorizontalScrollBarVisibility(listview, winrt.Windows.UI.Xaml.Controls.ScrollBarVisibility.Visible);
							UpdateScrollViewerDecreaseAndIncreaseButtonsViewState();
						}
					}
					else
					{
						tabColumn.Width(winrt.GridLengthHelper.FromValueAndType(1.0, winrt.GridUnitType.Auto));
						if (auto && listview = m_listView.get())
						{
							if (shouldUpdateWidths && fillAllAvailableSpace)
							{
								winrt.FxScrollViewer.SetHorizontalScrollBarVisibility(listview, winrt.Windows.UI.Xaml.Controls.ScrollBarVisibility.Hidden);
							}
							else
							{
								if (auto && decreaseButton = m_scrollDecreaseButton.get())
								{
									decreaseButton.IsEnabled(false);
								}
								if (auto && increaseButton = m_scrollIncreaseButton.get())
								{
									increaseButton.IsEnabled(false);
								}
							}
						}
					}
				}
				else
				{
					// Case: TabWidthMode "Compact" or "FitToContent"
					tabColumn.MaxWidth(availableWidth);
					tabColumn.Width(winrt.GridLengthHelper.FromValueAndType(1.0, winrt.GridUnitType.Auto));
					if (auto && listview = m_listView.get())
					{
						listview.MaxWidth(availableWidth);

						// Calculate if the scroll buttons should be visible.
						if (auto && itemsPresenter = m_itemsPresenter.get())
						{
							var visible = itemsPresenter.ActualWidth() > availableWidth;
							winrt.FxScrollViewer.SetHorizontalScrollBarVisibility(listview, visible
								? winrt.Windows.UI.Xaml.Controls.ScrollBarVisibility.Visible
								: winrt.Windows.UI.Xaml.Controls.ScrollBarVisibility.Hidden);
							if (visible)
							{
								UpdateScrollViewerDecreaseAndIncreaseButtonsViewState();
							}
						}
					}
				}
			}
		}
	}


	if (shouldUpdateWidths || TabWidthMode() != winrt.TabViewWidthMode.Equal)
	{
		for (var item : TabItems())
		{
			// Set the calculated width on each tab.
			var tvi = item.try_as<winrt.TabViewItem>();
			if (!tvi)
			{
				tvi = ContainerFromItem(item).as< winrt.TabViewItem > ();
			}

			if (tvi)
			{
				tvi.Width(tabWidth);
			}
		}
	}
}

void UpdateSelectedItem()
{
	if (auto && listView = m_listView.get())
	{
		var tvi = SelectedItem().try_as<winrt.TabViewItem>();
		if (!tvi)
		{
			tvi = ContainerFromItem(SelectedItem()).try_as<winrt.TabViewItem>();
		}

		if (tvi)
		{
			listView.SelectedItem(tvi);

			// Setting ListView.SelectedItem will not work here in all cases.
			// The reason why that doesn't work but this does is unknown.
			tvi.IsSelected(true);
		}
	}
}

void UpdateSelectedIndex()
{
	if (auto && listView = m_listView.get())
	{
		listView.SelectedIndex(SelectedIndex());
	}
}

winrt.DependencyObject ContainerFromItem(winrt.DependencyObject & item)
{
	if (auto && listView = m_listView.get())
	{
		return listView.ContainerFromItem(item);
	}
	return null;
}

winrt.DependencyObject ContainerFromIndex(int index)
{
	if (auto && listView = m_listView.get())
	{
		return listView.ContainerFromIndex(index);
	}
	return null;
}

winrt.DependencyObject ItemFromContainer(winrt.DependencyObject & container)
{
	if (auto && listView = m_listView.get())
	{
		return listView.ItemFromContainer(container);
	}
	return null;
}

int GetItemCount()
{
	if (var itemssource = TabItemsSource())
    {
		if (var iterable = itemssource.try_as<winrt.IIterable<winrt.DependencyObject>>())
        {
			int i = 1;
			var iter = iterable.First();
			while (iter.MoveNext())
			{
				i++;
			}
			return i;
		}
		return 0;
	}

	else
	{
		return (int)(TabItems().Size());
	}
}

bool SelectNextTab(int increment)
{
	bool handled = false;
	int itemsSize = GetItemCount();
	if (itemsSize > 1)
	{
		var index = SelectedIndex();
		index = (index + increment + itemsSize) % itemsSize;
		SelectedIndex(index);
		handled = true;
	}
	return handled;
}

bool RequestCloseCurrentTab()
{
	bool handled = false;
	if (var selectedTab = SelectedItem().try_as<winrt.TabViewItem>())
    {
		if (selectedTab.IsClosable())
		{
			// Close the tab on ctrl + F4
			RequestCloseTab(selectedTab);
			handled = true;
		}
	}

	return handled;
}

void OnKeyDown(winrt.KeyRoutedEventArgs & args)
{
	if (var coreWindow = winrt.CoreWindow.GetForCurrentThread())
    {
		if (args.Key() == winrt.VirtualKey.F4)
		{
			// Handle Ctrl+F4 on RS2 and lower
			// On RS3+, it is handled by a KeyboardAccelerator
			if (!SharedHelpers.IsRS3OrHigher())
			{
				var isCtrlDown = (coreWindow.GetKeyState(winrt.VirtualKey.Control) & winrt.CoreVirtualKeyStates.Down) == winrt.CoreVirtualKeyStates.Down;
				if (isCtrlDown)
				{
					args.Handled(RequestCloseCurrentTab());
				}
			}
		}
		else if (args.Key() == winrt.VirtualKey.Tab)
		{
			// Handle Ctrl+Tab/Ctrl+Shift+Tab on RS5 and lower
			// On 19H1+, it is handled by a KeyboardAccelerator
			if (!SharedHelpers.Is19H1OrHigher())
			{
				var isCtrlDown = (coreWindow.GetKeyState(winrt.VirtualKey.Control) & winrt.CoreVirtualKeyStates.Down) == winrt.CoreVirtualKeyStates.Down;
				var isShiftDown = (coreWindow.GetKeyState(winrt.VirtualKey.Shift) & winrt.CoreVirtualKeyStates.Down) == winrt.CoreVirtualKeyStates.Down;

				if (isCtrlDown && !isShiftDown)
				{
					args.Handled(SelectNextTab(1));
				}
				else if (isCtrlDown && isShiftDown)
				{
					args.Handled(SelectNextTab(-1));
				}
			}
		}
	}
}

void TabView.OnCtrlF4Invoked(winrt.KeyboardAccelerator& sender, winrt.KeyboardAcceleratorInvokedEventArgs& args)
{
	args.Handled(RequestCloseCurrentTab());
}

void OnCtrlTabInvoked(winrt.KeyboardAccelerator& sender, winrt.KeyboardAcceleratorInvokedEventArgs& args)
{
	args.Handled(SelectNextTab(1));
}

void OnCtrlShiftTabInvoked(winrt.KeyboardAccelerator& sender, winrt.KeyboardAcceleratorInvokedEventArgs& args)
{
	args.Handled(SelectNextTab(-1));
}
}

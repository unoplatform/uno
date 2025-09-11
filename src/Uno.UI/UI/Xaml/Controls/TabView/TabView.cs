// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference TabView.cpp, tag winui3/release/1.8.0, commit 61382c07e6cd8d

#pragma warning disable 105 // remove when moving to WinUI tree

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls.Primitives;
using Uno.Disposables;
using Uno.Extensions;
using Uno.Extensions.Specialized;
using Uno.UI.Helpers.WinUI;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.System;
using Windows.UI.Core;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml;
using System.Collections.ObjectModel;
using DirectUI;
using Microsoft.UI.Input;
using Microsoft.UI.Windowing;
using System.Reflection.Metadata.Ecma335;
using Microsoft.UI.Content;
using Windows.Graphics;
using MUXDragEventArgs = Microsoft.UI.Xaml.DragEventArgs; // Required for Android to avoid clashes.
using static Microsoft.UI.Xaml.Controls._Tracing;

namespace Microsoft.UI.Xaml.Controls;

[ContentProperty(Name = nameof(TabItems))]
public partial class TabView : Control
{
	private const double c_tabMinimumWidth = 48.0;
	private const double c_tabMaximumWidth = 200.0;

	private const string c_tabViewItemMinWidthName = "TabViewItemMinWidth";
	private const string c_tabViewItemMaxWidthName = "TabViewItemMaxWidth";

	// TODO (WinUI): what is the right number and should this be customizable?
	private const double c_scrollAmount = 50.0;

	public TabView()
	{
		var items = new ObservableVector<object>();
		SetValue(TabItemsProperty, items);

		this.SetDefaultStyleKey();

		Loaded += OnLoaded;
		Unloaded += OnUnloaded;

		KeyboardAccelerator ctrlf4Accel = new KeyboardAccelerator();
		ctrlf4Accel.Key = VirtualKey.F4;
		ctrlf4Accel.Modifiers = VirtualKeyModifiers.Control;
		ctrlf4Accel.Invoked += OnCtrlF4Invoked;
		ctrlf4Accel.ScopeOwner = this;
		KeyboardAccelerators.Add(ctrlf4Accel);

		m_tabCloseButtonTooltipText = ResourceAccessor.GetLocalizedStringResource(ResourceAccessor.SR_TabViewCloseButtonTooltipWithKA);

		KeyboardAccelerator ctrlTabAccel = new KeyboardAccelerator();
		ctrlTabAccel.Key = VirtualKey.Tab;
		ctrlTabAccel.Modifiers = VirtualKeyModifiers.Control;
		ctrlTabAccel.Invoked += OnCtrlTabInvoked;
		ctrlTabAccel.ScopeOwner = this;
		KeyboardAccelerators.Add(ctrlTabAccel);

		KeyboardAccelerator ctrlShiftTabAccel = new KeyboardAccelerator();
		ctrlShiftTabAccel.Key = VirtualKey.Tab;
		ctrlShiftTabAccel.Modifiers = VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift;
		ctrlShiftTabAccel.Invoked += OnCtrlShiftTabInvoked;
		ctrlShiftTabAccel.ScopeOwner = this;
		KeyboardAccelerators.Add(ctrlShiftTabAccel);
	}

	~TabView()
	{
		if (m_inputNonClientPointerSource is { })
		{
			m_inputNonClientPointerSource.EnteringMoveSize -= OnEnteringMoveSize;
			m_inputNonClientPointerSource.EnteredMoveSize -= OnEnteredMoveSize;
			m_inputNonClientPointerSource.WindowRectChanging -= OnWindowRectChanging;
			m_inputNonClientPointerSource.ExitedMoveSize -= OnExitedMoveSize;
		}
	}

	protected override void OnApplyTemplate()
	{
		base.OnApplyTemplate();

		UnhookEventsAndClearFields();

		m_isItemBeingDragged = false;
		m_isItemDraggedOver = false;
		m_expandedWidthForDragOver = default;

		//IControlProtected controlProtected{ *this };

		m_tabContentPresenter = GetTemplateChild<ContentPresenter>("TabContentPresenter");
		m_rightContentPresenter = GetTemplateChild<ContentPresenter>("RightContentPresenter");

		m_leftContentColumn = GetTemplateChild<ColumnDefinition>("LeftContentColumn");
		m_tabColumn = GetTemplateChild<ColumnDefinition>("TabColumn");
		m_addButtonColumn = GetTemplateChild<ColumnDefinition>("AddButtonColumn");
		m_rightContentColumn = GetTemplateChild<ColumnDefinition>("RightContentColumn");

		if (GetTemplateChild<Grid>("TabContainerGrid") is Grid containerGrid)
		{
			m_tabContainerGrid = containerGrid;
			containerGrid.PointerExited += OnTabStripPointerExited;
			m_tabStripPointerExitedRevoker.Disposable = Disposable.Create(() =>
			{
				containerGrid.PointerExited -= OnTabStripPointerExited;
			});
			containerGrid.PointerEntered += OnTabStripPointerEntered;
			m_tabStripPointerEnteredRevoker.Disposable = Disposable.Create(() =>
			{
				containerGrid.PointerEntered -= OnTabStripPointerEntered;
			});
		}

		ListView GetListView()
		{
			var listView = GetTemplateChild<ListView>("TabListView");
			if (listView != null)
			{
				listView.Loaded += OnListViewLoaded;
				m_listViewLoadedRevoker.Disposable = Disposable.Create(() =>
				{
					listView.Loaded -= OnListViewLoaded;
				});
				listView.SelectionChanged += OnListViewSelectionChanged;
				m_listViewSelectionChangedRevoker.Disposable = Disposable.Create(() =>
				{
					listView.SelectionChanged -= OnListViewSelectionChanged;
				});
				listView.SizeChanged += OnListViewSizeChanged;
				m_listViewSizeChangedRevoker.Disposable = Disposable.Create(() =>
				{
					listView.SizeChanged -= OnListViewSizeChanged;
				});

				listView.DragItemsStarting += OnListViewDragItemsStarting;
				m_listViewDragItemsStartingRevoker.Disposable = Disposable.Create(() =>
				{
					listView.DragItemsStarting -= OnListViewDragItemsStarting;
				});
				listView.DragItemsCompleted += OnListViewDragItemsCompleted;
				m_listViewDragItemsCompletedRevoker.Disposable = Disposable.Create(() =>
				{
					listView.DragItemsCompleted -= OnListViewDragItemsCompleted;
				});
				listView.Drop += OnListViewDrop;
				m_listViewDragOverRevoker.Disposable = Disposable.Create(() =>
				{
					listView.Drop -= OnListViewDrop;
				});

				listView.DragEnter += OnListViewDragEnter;
				m_listViewDragEnterRevoker.Disposable = Disposable.Create(() =>
				{
					listView.DragEnter -= OnListViewDragEnter;
				});
				listView.DragLeave += OnListViewDragLeave;
				m_listViewDragLeaveRevoker.Disposable = Disposable.Create(() =>
				{
					listView.DragLeave -= OnListViewDragLeave;
				});

				listView.Drop += OnListViewDrop;
				m_listViewDropRevoker.Disposable = Disposable.Create(() =>
				{
					listView.Drop -= OnListViewDrop;
				});

				listView.GettingFocus += OnListViewGettingFocus;
				m_listViewGettingFocusRevoker.Disposable = Disposable.Create(() =>
				{
					listView.GettingFocus -= OnListViewGettingFocus;
				});

				var canReorderItemsToken = listView.RegisterPropertyChangedCallback(ListView.CanReorderItemsProperty, OnListViewDraggingPropertyChanged);
				m_listViewCanReorderItemsPropertyChangedRevoker.Disposable = Disposable.Create(() =>
				{
					listView.UnregisterPropertyChangedCallback(ListView.CanReorderItemsProperty, canReorderItemsToken);
				});
				var allowDropToken = listView.RegisterPropertyChangedCallback(UIElement.AllowDropProperty, OnListViewDraggingPropertyChanged);
				m_listViewAllowDropPropertyChangedRevoker.Disposable = Disposable.Create(() =>
				{
					listView.UnregisterPropertyChangedCallback(UIElement.AllowDropProperty, allowDropToken);
				});
			}
			return listView;
		}
		m_listView = GetListView();

		Button GetAddButton()
		{
			var addButton = GetTemplateChild<Button>("AddButton");
			if (addButton != null)
			{
				// Do localization for the add button
				if (string.IsNullOrEmpty(AutomationProperties.GetName(addButton)))
				{
					var addButtonName = ResourceAccessor.GetLocalizedStringResource(ResourceAccessor.SR_TabViewAddButtonName);
					AutomationProperties.SetName(addButton, addButtonName);
				}

				var toolTip = ToolTipService.GetToolTip(addButton);
				if (toolTip == null)
				{
					ToolTip tooltip = new ToolTip();
					tooltip.Content = ResourceAccessor.GetLocalizedStringResource(ResourceAccessor.SR_TabViewAddButtonTooltip);
					ToolTipService.SetToolTip(addButton, tooltip);
				}

				addButton.Click += OnAddButtonClick;
				m_addButtonClickRevoker.Disposable = Disposable.Create(() => addButton.Click -= OnAddButtonClick);
				addButton.KeyDown += OnAddButtonKeyDown;
				m_addButtonKeyDownRevoker.Disposable = Disposable.Create(() => addButton.KeyDown -= OnAddButtonKeyDown);
			}
			return addButton;
		}
		m_addButton = GetAddButton();

		UpdateListViewItemContainerTransitions();
	}

	internal void SetTabSeparatorOpacity(int index, int opacityValue)
	{
		if (ContainerFromIndex(index) is TabViewItem tvi)
		{
			// The reason we set the opacity directly instead of using VisualState
			// is because we want to hide the separator on hover/pressed
			// but the tab adjacent on the left to the selected tab
			// must hide the tab separator at all times.
			// It causes two visual states to modify the same property
			// what leads to undesired behaviour.
			if (tvi.GetTemplateChild("TabSeparator") is FrameworkElement tabSeparator)
			{
				tabSeparator.Opacity = opacityValue;
			}
		}
	}

	internal void SetTabSeparatorOpacity(int index)
	{
		var selectedIndex = SelectedIndex;

		// If Tab is adjacent on the left to selected one or
		// it is selected tab - we hide the tabSeparator.
		if (index == selectedIndex || index + 1 == selectedIndex)
		{
			SetTabSeparatorOpacity(index, 0);
		}
		else
		{
			SetTabSeparatorOpacity(index, 1);
		}
	}

	private void OnListViewDraggingPropertyChanged(DependencyObject sender, DependencyProperty args)
	{
		UpdateListViewItemContainerTransitions();
	}

	private void OnListViewGettingFocus(object sender, GettingFocusEventArgs args)
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

		var direction = args.Direction;
		if (direction == FocusNavigationDirection.Up || direction == FocusNavigationDirection.Down)
		{
			var oldItem = args.OldFocusedElement as TabViewItem;
			var newItem = args.NewFocusedElement as TabViewItem;
			if (oldItem != null && newItem != null)
			{
				if (m_listView is { } listView)
				{
					bool oldItemIsFromThisTabView = listView.IndexFromContainer(oldItem) != -1;
					bool newItemIsFromThisTabView = listView.IndexFromContainer(newItem) != -1;
					if (oldItemIsFromThisTabView && newItemIsFromThisTabView)
					{
						var inputDevice = args.InputDevice;
						if (inputDevice == FocusInputDeviceKind.GameController)
						{
							var listViewBoundsLocal = new Rect(0, 0, (float)listView.ActualWidth, (float)listView.ActualHeight);
							var listViewBounds = listView.TransformToVisual(null).TransformBounds(listViewBoundsLocal);
							FindNextElementOptions options = new FindNextElementOptions();
							options.ExclusionRect = listViewBounds;
							options.SearchRoot = XamlRoot.Content;
							var next = FocusManager.FindNextElement(direction, options);

							args.TrySetNewFocusedElement(next);
							args.Handled = true;
						}
						else
						{
							args.Cancel = true;
							args.Handled = true;
						}
					}
				}
			}
		}
	}

	private void OnSelectedIndexPropertyChanged(DependencyPropertyChangedEventArgs args)
	{
		// We update previous selected and adjacent on the left tab
		// as well as current selected and adjacent on the left tab
		// to show/hide tabSeparator accordingly.
		UpdateSelectedIndex();
		SetTabSeparatorOpacity((int)args.OldValue);
		SetTabSeparatorOpacity(((int)args.OldValue) - 1);
		SetTabSeparatorOpacity(SelectedIndex - 1);
		SetTabSeparatorOpacity(SelectedIndex);

		UpdateBottomBorderLineVisualStates();
	}

	private void UpdateTabBottomBorderLineVisualStates()
	{
		int numItems = TabItems.Count;
		int selectedIndex = SelectedIndex;

		for (int i = 0; i < numItems; i++)
		{
			var state = "NormalBottomBorderLine";
			if (m_isItemBeingDragged)
			{
				state = "NoBottomBorderLine";
			}
			else if (selectedIndex != -1)
			{
				if (i == selectedIndex)
				{
					state = "NoBottomBorderLine";
				}
				else if (i == selectedIndex - 1)
				{
					state = "LeftOfSelectedTab";
				}
				else if (i == selectedIndex + 1)
				{
					state = "RightOfSelectedTab";
				}
			}

			if (ContainerFromIndex(i) is Control tvi)
			{
				VisualStateManager.GoToState(tvi, state, false /*useTransitions*/);
			}
		}
	}

	private void UpdateBottomBorderLineVisualStates()
	{
		// Update border line on all tabs
		UpdateTabBottomBorderLineVisualStates();

		// Update border lines on the TabView
		VisualStateManager.GoToState(this, m_isItemBeingDragged ? "SingleBottomBorderLine" : "NormalBottomBorderLine", false /*useTransitions*/);

		// Update border lines in the inner TabViewListView
		if (m_listView is { } lv)
		{
			VisualStateManager.GoToState(lv, m_isItemBeingDragged ? "NoBottomBorderLine" : "NormalBottomBorderLine", false /*useTransitions*/);
		}

		// Update border lines in the ScrollViewer
		if (m_scrollViewer is { } scroller)
		{
			VisualStateManager.GoToState(scroller, m_isItemBeingDragged ? "NoBottomBorderLine" : "NormalBottomBorderLine", false /*useTransitions*/);
		}
	}

	private void OnSelectedItemPropertyChanged(DependencyPropertyChangedEventArgs args)
	{
		UpdateSelectedItem();
	}

	private void OnTabItemsSourcePropertyChanged(DependencyPropertyChangedEventArgs args)
	{
		UpdateListViewItemContainerTransitions();
	}

	private void UpdateListViewItemContainerTransitions()
	{
		if (TabItemsSource != null)
		{
			if (m_listView is { } listView)
			{
				if (listView.CanReorderItems && listView.AllowDrop)
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
					bool GetTransitionCollectionHasAddDeleteOrContentThemeTransition(ListView listView)
					{
						if (listView.ItemContainerTransitions is { } itemContainerTransitions)
						{
							foreach (var transition in itemContainerTransitions)
							{
								if (transition != null &&
									(transition is AddDeleteThemeTransition || transition is ContentThemeTransition))
								{
									return true;
								}
							}
						}
						return false;
					}
					bool transitionCollectionHasAddDeleteOrContentThemeTransition =
						GetTransitionCollectionHasAddDeleteOrContentThemeTransition(listView);

					if (transitionCollectionHasAddDeleteOrContentThemeTransition)
					{
						var newItemContainerTransitions = new TransitionCollection();
						var oldItemContainerTransitions = listView.ItemContainerTransitions;

						foreach (var transition in oldItemContainerTransitions)
						{
							if (transition != null)
							{
								if (transition is AddDeleteThemeTransition || transition is ContentThemeTransition)
								{
									continue;
								}
								newItemContainerTransitions.Add(transition);
							}
						}

						listView.ItemContainerTransitions = newItemContainerTransitions;
					}
				}
			}
		}
	}

	private void UnhookEventsAndClearFields()
	{
		m_listViewLoadedRevoker.Disposable = null;
		m_listViewSelectionChangedRevoker.Disposable = null;
		m_listViewDragItemsStartingRevoker.Disposable = null;
		m_listViewDragItemsCompletedRevoker.Disposable = null;
		m_listViewDragOverRevoker.Disposable = null;
		m_listViewGettingFocusRevoker.Disposable = null;
		m_listViewCanReorderItemsPropertyChangedRevoker.Disposable = null;
		m_listViewAllowDropPropertyChangedRevoker.Disposable = null;
		m_addButtonClickRevoker.Disposable = null;
		m_itemsPresenterSizeChangedRevoker.Disposable = null;
		m_tabStripPointerExitedRevoker.Disposable = null;
		m_tabStripPointerEnteredRevoker.Disposable = null;
		m_scrollViewerLoadedRevoker.Disposable = null;
		m_scrollViewerViewChangedRevoker.Disposable = null;
		m_scrollDecreaseClickRevoker.Disposable = null;
		m_scrollIncreaseClickRevoker.Disposable = null;
		m_addButtonKeyDownRevoker.Disposable = null;

		m_tabContentPresenter = null;
		m_rightContentPresenter = null;
		m_leftContentColumn = null;
		m_tabColumn = null;
		m_addButtonColumn = null;
		m_rightContentColumn = null;
		m_tabContainerGrid = null;
		m_listView = null;
		m_addButton = null;
		m_itemsPresenter = null;
		m_scrollViewer = null;
		m_scrollDecreaseButton = null;
		m_scrollIncreaseButton = null;
	}

	private void OnTabWidthModePropertyChanged(DependencyPropertyChangedEventArgs args)
	{
		UpdateTabWidths();

		foreach (var item in TabItems)
		{
			// Switch the visual states of all tab items to the correct TabViewWidthMode
			TabViewItem GetTabViewItem(object item)
			{
				if (item is TabViewItem tabViewItem)
				{
					return tabViewItem;
				}
				return ContainerFromItem(item) as TabViewItem;
			}
			var tvi = GetTabViewItem(item);

			if (tvi != null)
			{
				tvi.OnTabViewWidthModeChanged(TabWidthMode);
			}
		}
	}

	private void OnCloseButtonOverlayModePropertyChanged(DependencyPropertyChangedEventArgs args)
	{
		// Switch the visual states of all tab items to to the correct closebutton overlay mode
		foreach (var item in TabItems)
		{
			TabViewItem GetTabViewItem(object item)
			{
				if (item is TabViewItem tabViewItem)
				{
					return tabViewItem;
				}
				return ContainerFromItem(item) as TabViewItem;
			}
			var tvi = GetTabViewItem(item);

			if (tvi != null)
			{
				tvi.OnCloseButtonOverlayModeChanged(CloseButtonOverlayMode);
			}
		}
	}

	private void OnAddButtonClick(object sender, RoutedEventArgs args)
	{
		AddTabButtonClick?.Invoke(this, args);

		if (FrameworkElementAutomationPeer.FromElement(this) is { } peer)
		{
			peer.RaiseNotificationEvent(
				AutomationNotificationKind.ItemAdded,
				AutomationNotificationProcessing.MostRecent,
				ResourceAccessor.GetLocalizedStringResource(ResourceAccessor.SR_TabViewNewTabAddedNotification),
				"TabViewNewTabAddedNotificationActivityId");
		}
	}
	protected override AutomationPeer OnCreateAutomationPeer()
	{
		return new TabViewAutomationPeer(this);
	}

	private void OnLoaded(object sender, RoutedEventArgs args)
	{
		UpdateTabContent();
		UpdateTabViewWithTearOutList();
		AttachMoveSizeLoopEvents();
		UpdateNonClientRegion();
	}

	private void OnUnloaded(object sender, RoutedEventArgs e)
	{
		UpdateTabViewWithTearOutList();
	}

	private void OnListViewLoaded(object sender, RoutedEventArgs args)
	{
		if (m_listView is { } listView)
		{
			// Now that ListView exists, we can start using its Items collection.
			if (listView.Items is { } lvItems)
			{
				if (listView.ItemsSource == null)
				{
					// copy the list, because clearing lvItems may also clear TabItems
					var itemList = new ObservableVector<object>();

					foreach (var item in TabItems)
					{
						itemList.Add(item);
					}

					lvItems.Clear();

					foreach (var item in itemList)
					{
						// App put items in our Items collection; copy them over to ListView.Items
						if (item != null)
						{
							lvItems.Add(item);
						}
					}
				}
				TabItems = lvItems;
			}

			if (ReadLocalValue(SelectedItemProperty) != DependencyProperty.UnsetValue)
			{
				UpdateSelectedItem();
			}
			else
			{
				// If SelectedItem wasn't set, default to selecting the first tab
				UpdateSelectedIndex();
			}

			SelectedIndex = listView.SelectedIndex;
			SelectedItem = listView.SelectedItem;

			// Find TabsItemsPresenter and listen for SizeChanged
			ItemsPresenter GetItemsPresenter(ListView listView)
			{
				var itemsPresenter = SharedHelpers.FindInVisualTreeByName(listView, "TabsItemsPresenter") as ItemsPresenter;
				if (itemsPresenter != null)
				{
					itemsPresenter.SizeChanged += OnItemsPresenterSizeChanged;
					m_itemsPresenterSizeChangedRevoker.Disposable = Disposable.Create(() =>
					{
						itemsPresenter.SizeChanged -= OnItemsPresenterSizeChanged;
					});
				}
				return itemsPresenter;
			}
			m_itemsPresenter = GetItemsPresenter(listView);

			var scrollViewer = SharedHelpers.FindInVisualTreeByName(listView, "ScrollViewer") as ScrollViewer;
			m_scrollViewer = scrollViewer;
			if (scrollViewer != null)
			{
				if (scrollViewer.IsLoaded)
				{
					// This scenario occurs reliably for Terminal in XAML islands
					OnScrollViewerLoaded(null, null);
				}
				else
				{
					scrollViewer.Loaded += OnScrollViewerLoaded;
					m_scrollViewerLoadedRevoker.Disposable = Disposable.Create(() =>
					{
						scrollViewer.Loaded -= OnScrollViewerLoaded;
					});
				}
				// Uno workaround: Since Loaded is called before measure, the increase/decrease button visibility is not initially set
				// properly unless we subscribe to ScrollableWidth changing.
				var scrollViewerScrollableWidthToken = scrollViewer.RegisterPropertyChangedCallback(
					ScrollViewer.ScrollableWidthProperty,
					(_, __) => UpdateScrollViewerDecreaseAndIncreaseButtonsViewState()
				);
				m_ScrollViewerScrollableWidthPropertyChangedRevoker.Disposable = Disposable.Create(() =>
				{
					scrollViewer.UnregisterPropertyChangedCallback(ScrollViewer.ScrollableWidthProperty, scrollViewerScrollableWidthToken);
				});
			}
		}

		UpdateTabBottomBorderLineVisualStates();
	}

	private void OnTabStripPointerExited(object sender, PointerRoutedEventArgs args)
	{
		m_pointerInTabstrip = false;
		if (m_updateTabWidthOnPointerLeave)
		{
			try
			{
				UpdateTabWidths();
			}
			finally
			{
				m_updateTabWidthOnPointerLeave = false;
			}
		}
	}

	private void OnTabStripPointerEntered(object sender, PointerRoutedEventArgs args)
	{
		m_pointerInTabstrip = true;
	}

	private void OnScrollViewerLoaded(object sender, RoutedEventArgs args)
	{
		if (m_scrollViewer is { } scrollViewer)
		{
			RepeatButton GetDecreaseButton(ScrollViewer scrollViewer)
			{
				var decreaseButton = SharedHelpers.FindInVisualTreeByName(scrollViewer, "ScrollDecreaseButton") as RepeatButton;
				if (decreaseButton != null)
				{
					// Do localization for the scroll decrease button
					var toolTip = ToolTipService.GetToolTip(decreaseButton);
					if (toolTip == null)
					{
						var tooltip = new ToolTip();
						tooltip.Content = ResourceAccessor.GetLocalizedStringResource(ResourceAccessor.SR_TabViewScrollDecreaseButtonTooltip);
						ToolTipService.SetToolTip(decreaseButton, tooltip);
					}

					decreaseButton.Click += OnScrollDecreaseClick;
					m_scrollDecreaseClickRevoker.Disposable = Disposable.Create(() =>
					{
						decreaseButton.Click -= OnScrollDecreaseClick;
					});
				}
				return decreaseButton;
			}
			m_scrollDecreaseButton = GetDecreaseButton(scrollViewer);

			RepeatButton GetIncreaseButton(ScrollViewer scrollViewer)
			{
				var increaseButton = SharedHelpers.FindInVisualTreeByName(scrollViewer, "ScrollIncreaseButton") as RepeatButton;
				if (increaseButton != null)
				{
					// Do localization for the scroll increase button
					var toolTip = ToolTipService.GetToolTip(increaseButton);
					if (toolTip == null)
					{
						var tooltip = new ToolTip();
						tooltip.Content = ResourceAccessor.GetLocalizedStringResource(ResourceAccessor.SR_TabViewScrollIncreaseButtonTooltip);
						ToolTipService.SetToolTip(increaseButton, tooltip);
					}

					increaseButton.Click += OnScrollIncreaseClick;
					m_scrollIncreaseClickRevoker.Disposable = Disposable.Create(() =>
					{
						increaseButton.Click -= OnScrollIncreaseClick;
					});
				}
				return increaseButton;
			}
			m_scrollIncreaseButton = GetIncreaseButton(scrollViewer);

			scrollViewer.ViewChanged += OnScrollViewerViewChanged;
			m_scrollViewerViewChangedRevoker.Disposable = Disposable.Create(() =>
			{
				scrollViewer.ViewChanged -= OnScrollViewerViewChanged;
			});
		}

		UpdateTabWidths();
	}

	private void OnScrollViewerViewChanged(object sender, ScrollViewerViewChangedEventArgs args)
	{
		UpdateScrollViewerDecreaseAndIncreaseButtonsViewState();
	}

	private void UpdateScrollViewerDecreaseAndIncreaseButtonsViewState()
	{
		var scrollViewer = m_scrollViewer;
		if (scrollViewer != null)
		{
			var decreaseButton = m_scrollDecreaseButton;
			var increaseButton = m_scrollIncreaseButton;

			var minThreshold = 0.1;
			var horizontalOffset = scrollViewer.HorizontalOffset;
			var scrollableWidth = scrollViewer.ScrollableWidth;

			if (Math.Abs(horizontalOffset - scrollableWidth) < minThreshold)
			{
				if (decreaseButton != null)
				{
					decreaseButton.IsEnabled = true;
				}
				if (increaseButton != null)
				{
					increaseButton.IsEnabled = false;
				}
			}
			else if (Math.Abs(horizontalOffset) < minThreshold)
			{
				if (decreaseButton != null)
				{
					decreaseButton.IsEnabled = false;
				}
				if (increaseButton != null)
				{
					increaseButton.IsEnabled = true;
				}
			}
			else
			{
				if (decreaseButton != null)
				{
					decreaseButton.IsEnabled = true;
				}
				if (increaseButton != null)
				{
					increaseButton.IsEnabled = true;
				}
			}
		}
	}

	private void OnItemsPresenterSizeChanged(object sender, SizeChangedEventArgs args)
	{
		if (!m_updateTabWidthOnPointerLeave)
		{
			// Presenter size didn't change because of item being removed, so update manually
			UpdateScrollViewerDecreaseAndIncreaseButtonsViewState();
			UpdateTabWidths();
			// Make sure that the selected tab is fully in view and not cut off
			BringSelectedTabIntoView();
		}
	}

	private void BringSelectedTabIntoView()
	{
		if (SelectedItem is not null)
		{
			var tvi = SelectedItem as TabViewItem;
			if (tvi is null)
			{
				tvi = ContainerFromItem(SelectedItem) as TabViewItem;
			}

			tvi?.StartBringTabIntoView();
		}
	}

	//TODO Uno workaround: The second parameter is needed, as OnItemsChanged may get called before OnApplyTemplate due to control lifecycle differences
	internal void OnItemsChanged(object item, TabViewListView tabListView)
	{
		var args = item as IVectorChangedEventArgs;
		if (args != null)
		{
			TabItemsChanged?.Invoke(this, args);

			int numItems = TabItems.Count;

			var listViewInnerSelectedIndex = (m_listView ?? tabListView).SelectedIndex;
			var selectedIndex = SelectedIndex;

			if (selectedIndex != listViewInnerSelectedIndex && listViewInnerSelectedIndex != -1)
			{
				SelectedIndex = listViewInnerSelectedIndex;
				selectedIndex = listViewInnerSelectedIndex;
			}

			if (args.CollectionChange == CollectionChange.ItemRemoved)
			{
				m_updateTabWidthOnPointerLeave = true;
				if (numItems > 0)
				{
					// SelectedIndex might also already be -1
					if (selectedIndex == -1 || selectedIndex == args.Index)
					{
						// Find the closest tab to select instead.
						int startIndex = (int)args.Index;
						if (startIndex >= numItems)
						{
							startIndex = numItems - 1;
						}
						int index = startIndex;

						do
						{
							var nextItem = ContainerFromIndex(index) as ListViewItem;

							if (nextItem != null && nextItem.IsEnabled && nextItem.Visibility == Visibility.Visible)
							{
								SelectedItem = TabItems[index];
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
				if (TabWidthMode == TabViewWidthMode.Equal)
				{
					if (!m_pointerInTabstrip || args.Index == TabItems.Count)
					{
						UpdateTabWidths(true, false);
					}
				}
			}
			else
			{
				UpdateTabWidths();
				SetTabSeparatorOpacity(numItems - 1);
			}
		}

		UpdateBottomBorderLineVisualStates();
	}

	private void OnListViewSelectionChanged(object sender, SelectionChangedEventArgs args)
	{
		var listView = m_listView;
		if (listView != null)
		{
			SelectedIndex = listView.SelectedIndex;
			SelectedItem = listView.SelectedItem;
		}

		UpdateTabContent();

		SelectionChanged?.Invoke(this, args);
	}

	private void OnListViewSizeChanged(object sender, SizeChangedEventArgs args)
	{
		UpdateNonClientRegion();
	}

	TabViewItem FindTabViewItemFromDragItem(object item)
	{
		var tab = ContainerFromItem(item) as TabViewItem;

		if (tab == null)
		{
			if (item is FrameworkElement fe)
			{
				tab = VisualTreeHelper.GetParent(fe) as TabViewItem;
			}
		}

		if (tab == null)
		{
			// This is a fallback scenario for tabs without a data context
			var numItems = TabItems.Count;
			for (int i = 0; i < numItems; i++)
			{
				if (ContainerFromIndex(i) is TabViewItem tabItem)
				{
					if (tabItem.Content == item)
					{
						tab = tabItem;
						break;
					}
				}
			}
		}

		return tab;
	}

	private void OnListViewDragItemsStarting(object sender, DragItemsStartingEventArgs args)
	{
		m_isItemBeingDragged = true;

		var item = args.Items[0];
		var tab = FindTabViewItemFromDragItem(item);
		var myArgs = new TabViewTabDragStartingEventArgs(args, item, tab);

		TabDragStarting?.Invoke(this, myArgs);

		UpdateBottomBorderLineVisualStates();
	}

	private void OnListViewDragOver(object sender, MUXDragEventArgs args)
	{
		TabStripDragOver?.Invoke(this, args);
	}

	void OnListViewDrop(object sender, MUXDragEventArgs args)
	{
		if (!args.Handled)
		{
			TabStripDrop?.Invoke(this, args);
		}

		UpdateIsItemDraggedOver(false);
	}

	private void OnListViewDragEnter(object sender, MUXDragEventArgs args)
	{
		// DragEnter can occur when we're dragging an item from within this TabView,
		// which will be handled internally.  In that case, we don't want to do anything here.
		foreach (var tabItem in TabItems)
		{
			if (ContainerFromItem(tabItem) is TabViewItem tabViewItem)
			{
				if (tabViewItem.IsBeingDragged)
				{
					return;
				}
			}
		}

		UpdateIsItemDraggedOver(true);
	}

	private void OnListViewDragLeave(object sender, MUXDragEventArgs args)
	{
		UpdateIsItemDraggedOver(false);
	}

	private void OnListViewDragItemsCompleted(object sender, DragItemsCompletedEventArgs args)
	{
		m_isItemBeingDragged = false;

		// Selection may have changed during drag if dragged outside, so we update SelectedIndex again.
		if (m_listView is { } listView)
		{
			SelectedIndex = listView.SelectedIndex;
			SelectedItem = listView.SelectedItem;

			BringSelectedTabIntoView();
		}

		var item = args.Items[0];
		var tab = FindTabViewItemFromDragItem(item);
		var myArgs = new TabViewTabDragCompletedEventArgs(args, item, tab);

		TabDragCompleted?.Invoke(this, myArgs);

		// None means it's outside of the tab strip area
		if (args.DropResult == DataPackageOperation.None)
		{
			var tabDroppedArgs = new TabViewTabDroppedOutsideEventArgs(item, tab);
			TabDroppedOutside?.Invoke(this, tabDroppedArgs);
		}

		UpdateBottomBorderLineVisualStates();
	}

	internal void UpdateTabContent()
	{
		var tabContentPresenter = m_tabContentPresenter;
		if (tabContentPresenter != null)
		{
			if (SelectedItem == null)
			{
				tabContentPresenter.Content = null;
				tabContentPresenter.ContentTemplate = null;
				tabContentPresenter.ContentTemplateSelector = null;
			}
			else
			{
				var tvi = SelectedItem as TabViewItem;
				if (tvi == null)
				{
					tvi = ContainerFromItem(SelectedItem) as TabViewItem;
				}

				if (tvi != null)
				{
					// If the focus was in the old tab content, we will lose focus when it is removed from the visual tree.
					// We should move the focus to the new tab content.
					// The new tab content is not available at the time of the LosingFocus event, so we need to
					// move focus later.
					bool shouldMoveFocusToNewTab = false;

					void OnTabContentPresenterLosingFocus(object sender, LosingFocusEventArgs args)
					{
						shouldMoveFocusToNewTab = true;
						tabContentPresenter.LosingFocus -= OnTabContentPresenterLosingFocus;
					}

					tabContentPresenter.LosingFocus += OnTabContentPresenterLosingFocus;

					tabContentPresenter.Content = tvi.Content;
					tabContentPresenter.ContentTemplate = tvi.ContentTemplate;
					tabContentPresenter.ContentTemplateSelector = tvi.ContentTemplateSelector;

#if IS_UNO
					// TODO: Uno specific - issue #4894 - in UWP the ContentPresenter does not become
					// the parent of the Content. In Uno it does, so we need to make sure
					// the inherited DataContext will match the TabViewItem.
					tabContentPresenter.DataContext = tvi.DataContext;
#endif

					if (shouldMoveFocusToNewTab)
					{
						var focusable = FocusManager.FindFirstFocusableElement(tabContentPresenter);
						if (focusable == null)
						{
							// If there is nothing focusable in the new tab, just move focus to the TabViewItem itself.
							focusable = tvi;
						}

						if (focusable != null)
						{
							CppWinRTHelpers.SetFocus(focusable, FocusState.Programmatic);
						}
					}
				}
			}
		}
	}

	internal void RequestCloseTab(TabViewItem container, bool updateTabWidths)
	{
		// If the tab being closed is the currently focused tab, we'll move focus to the next tab
		// when the tab closes.
		bool tabIsFocused = false;
		var focusedElement = XamlRoot is not null ? FocusManager.GetFocusedElement(XamlRoot) as DependencyObject : null;

		while (focusedElement is not null)
		{
			if (focusedElement == container)
			{
				tabIsFocused = true;
				break;
			}

			focusedElement = VisualTreeHelper.GetParent(focusedElement);
		}

		if (tabIsFocused)
		{
			// If the tab specified both is focused and loses focus, then we'll move focus to an adjacent focusable tab, if one exists.
			// TODO:MZ: losingFocusRevoker = WHEN TO REVOKE THIS????

			void OnLosingFocus(UIElement sender, LosingFocusEventArgs args)
			{
				if (!args.Cancel && !args.Handled)
				{
					int focusedIndex = IndexFromContainer(container);
					DependencyObject newFocusedElement = null;

					for (int i = focusedIndex + 1; i < GetItemCount(); i++)
					{
						var candidateElement = ContainerFromIndex(i);

						if (IsFocusable(candidateElement))
						{
							newFocusedElement = candidateElement;
							break;
						}
					}

					if (newFocusedElement is null)
					{
						for (int i = focusedIndex - 1; i >= 0; i--)
						{
							var candidateElement = ContainerFromIndex(i);

							if (IsFocusable(candidateElement))
							{
								newFocusedElement = candidateElement;
								break;
							}
						}
					}

					if (newFocusedElement == args.NewFocusedElement)
					{
						// No-op If the new focused element is the same as the one we're already trying to focus.
						return;
					}

					if (newFocusedElement is null)
					{
						newFocusedElement = m_addButton;
					}

					args.Handled = args.TrySetNewFocusedElement(newFocusedElement);
				}
			}
			container.LosingFocus += OnLosingFocus;
		}

		var listView = m_listView;
		if (listView != null)
		{
			var args = new TabViewTabCloseRequestedEventArgs(listView.ItemFromContainer(container), container);

			TabCloseRequested?.Invoke(this, args);

			var internalTabViewItem = container;
			if (container != null)
			{
				internalTabViewItem.RaiseRequestClose(args);
			}
		}
		UpdateTabWidths(updateTabWidths);
	}

	private void OnScrollDecreaseClick(object sender, RoutedEventArgs args)
	{
		var scrollViewer = m_scrollViewer;
		if (scrollViewer != null)
		{
			scrollViewer.ChangeView(Math.Max(0.0, scrollViewer.HorizontalOffset - c_scrollAmount), null, null);
		}
	}

	private void OnScrollIncreaseClick(object sender, RoutedEventArgs args)
	{
		var scrollViewer = m_scrollViewer;
		if (scrollViewer != null)
		{
			scrollViewer.ChangeView(Math.Min(scrollViewer.ScrollableWidth, scrollViewer.HorizontalOffset + c_scrollAmount), null, null);
		}
	}

	protected override Size MeasureOverride(Size availableSize)
	{
		if (m_previousAvailableSize.Width != availableSize.Width)
		{
			m_previousAvailableSize = availableSize;
			UpdateTabWidths();
		}

		return base.MeasureOverride(availableSize);
	}

	internal void UpdateTabWidths(bool shouldUpdateWidths = true, bool fillAllAvailableSpace = true)
	{
		// Don't update any tab widths when we're in the middle of a tab tear-out loop -
		// we'll update tab widths when it's done.
		if (m_isInTabTearOutLoop)
		{
			return;
		}

		var maxTabWidth = (double)SharedHelpers.FindInApplicationResources(c_tabViewItemMaxWidthName, c_tabMaximumWidth);
		double tabWidth = double.NaN;
		int tabCount = TabItems.Count;

		// If an item is being dragged over this TabView, then we'll want to act like there's an extra item
		// when updating tab widths, which will create a hole into which the item can be dragged.
		if (m_isItemDraggedOver)
		{
			tabCount++;
		}

		var tabGrid = m_tabContainerGrid;
		if (tabGrid != null)
		{
			// Add up width taken by custom content and + button
			double widthTaken = 0.0;
			var leftContentColumn = m_leftContentColumn;
			if (leftContentColumn != null)
			{
				widthTaken += leftContentColumn.ActualWidth;
			}
			var addButtonColumn = m_addButtonColumn;
			if (addButtonColumn != null)
			{
				var addButtonColumnWidth = addButtonColumn.ActualWidth;
				if (addButtonColumn.ActualWidth == 0 && m_addButton?.Visibility == Visibility.Visible && m_previousAvailableSize.Width > 0)
				{
					// Uno workaround: We may arrive here before the AddButton has been measured, and if there are enough tabs to take
					// all the space, the Grid will not assign any to the button. As a workaround we measure the button directly and use
					// its desired size.
					m_addButton.Measure(m_previousAvailableSize);
					addButtonColumnWidth = m_addButton.DesiredSize.Width;
				}
				widthTaken += addButtonColumnWidth;
			}
			var rightContentColumn = m_rightContentColumn;
			if (rightContentColumn != null)
			{
				var rightContentPresenter = m_rightContentPresenter;
				if (rightContentPresenter != null)
				{
					var rightContentSize = rightContentPresenter.DesiredSize;
					rightContentColumn.MinWidth = rightContentSize.Width;
					widthTaken += rightContentSize.Width;
				}
			}

			var tabColumn = m_tabColumn;
			if (tabColumn != null)
			{
				// Note: can be infinite
				var availableWidth = m_previousAvailableSize.Width - widthTaken;

				// Size can be 0 when window is first created; in that case, skip calculations; we'll get a new size soon
				if (availableWidth > 0)
				{
					if (TabWidthMode == TabViewWidthMode.Equal)
					{

						var minTabWidth = (double)SharedHelpers.FindInApplicationResources(c_tabViewItemMinWidthName, c_tabMinimumWidth);

						// If we should fill all of the available space, use scrollviewer dimensions
						var padding = Padding;

						double headerWidth = 0.0;
						double footerWidth = 0.0;
						if (m_itemsPresenter is { } itemsPresenter)
						{
							if (itemsPresenter.Header is FrameworkElement header)
							{
								headerWidth = header.ActualWidth;
							}
							if (itemsPresenter.Footer is FrameworkElement footer)
							{
								footerWidth = footer.ActualWidth;
							}
						}

						if (fillAllAvailableSpace)
						{
							// Calculate the proportional width of each tab given the width of the ScrollViewer.
							var tabWidthForScroller = (availableWidth - (padding.Left + padding.Right + headerWidth + footerWidth)) / tabCount;
							tabWidth = Math.Clamp(tabWidthForScroller, minTabWidth, maxTabWidth);
						}
						else
						{
							double availableTabViewSpace = (tabColumn.ActualWidth - (padding.Left + padding.Right + headerWidth + footerWidth));
							var increaseButton = m_scrollIncreaseButton;
							if (increaseButton != null)
							{
								if (increaseButton.Visibility == Visibility.Visible)
								{
									availableTabViewSpace -= increaseButton.ActualWidth;
								}
							}

							var decreaseButton = m_scrollDecreaseButton;
							if (decreaseButton != null)
							{
								if (decreaseButton.Visibility == Visibility.Visible)
								{
									availableTabViewSpace -= decreaseButton.ActualWidth;
								}
							}

							// Use current size to update items to fill the currently occupied space
							var tabWidthUnclamped = availableTabViewSpace / tabCount;
							tabWidth = Math.Clamp(tabWidthUnclamped, minTabWidth, maxTabWidth);
						}


						// Size tab column to needed size
						tabColumn.MaxWidth = availableWidth + headerWidth + footerWidth;
						var requiredWidth = tabWidth * tabCount + headerWidth + footerWidth + padding.Left + padding.Right;
						if (requiredWidth > availableWidth)
						{
							tabColumn.Width = GridLengthHelper.FromPixels(availableWidth);
							var listview = m_listView;
							if (listview != null)
							{
								ScrollViewer.SetHorizontalScrollBarVisibility(listview, ScrollBarVisibility.Visible);
								UpdateScrollViewerDecreaseAndIncreaseButtonsViewState();
							}
						}
						else
						{
							// If we're dragging over the TabView, we need to set the width to a specific value,
							// since we want it to be larger than the items actually in it in order to accommodate
							// the item being dragged into the TabView.  Otherwise, we can just set its width to Auto.
							tabColumn.Width =
								m_isItemDraggedOver ?
								GridLengthHelper.FromPixels(requiredWidth) :
								GridLengthHelper.FromValueAndType(1.0, GridUnitType.Auto);

							var listview = m_listView;
							var tabListView = m_listView as TabViewListView;
							if (listview != null)
							{
								if (shouldUpdateWidths && fillAllAvailableSpace)
								{
									ScrollViewer.SetHorizontalScrollBarVisibility(listview, ScrollBarVisibility.Hidden);
								}
								else
								{
									var decreaseButton = m_scrollDecreaseButton;
									if (decreaseButton != null)
									{
										decreaseButton.IsEnabled = false;
									}
									var increaseButton = m_scrollIncreaseButton;
									if (increaseButton != null)
									{
										increaseButton.IsEnabled = false;
									}
								}
							}
						}
					}
					else
					{
						// Case: TabWidthMode "Compact" or "SizeToContent"
						tabColumn.MaxWidth = availableWidth;
						var listview = m_listView;
						var tabListView = m_listView as TabViewListView;
						if (listview != null)
						{
							// When an item is being dragged over, we need to reserve extra space for the potential new tab,
							// so we can't rely on auto sizing in that case.  However, the ListView expands to the size of the column,
							// so we need to store the value lest we keep expanding the width of the column every time we call this method.
							if (m_isItemDraggedOver)
							{
								if (!m_expandedWidthForDragOver.HasValue)
								{
									m_expandedWidthForDragOver = listview.ActualWidth + maxTabWidth;
								}

								tabColumn.Width = GridLengthHelper.FromPixels((double)m_expandedWidthForDragOver);
							}
							else
							{
								if (m_expandedWidthForDragOver.HasValue)
								{
									m_expandedWidthForDragOver = null;
								}

								tabColumn.Width = GridLengthHelper.FromValueAndType(1.0, GridUnitType.Auto);
							}


							listview.MaxWidth = availableWidth;

							// Calculate if the scroll buttons should be visible.
							var itemsPresenter = m_itemsPresenter;
							if (itemsPresenter != null)
							{
								var visible = itemsPresenter.ActualWidth > availableWidth;
								ScrollViewer.SetHorizontalScrollBarVisibility(listview, visible
									? ScrollBarVisibility.Visible
									: ScrollBarVisibility.Hidden);
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


		if (shouldUpdateWidths || TabWidthMode != TabViewWidthMode.Equal)
		{
			foreach (var item in TabItems)
			{
				// Set the calculated width on each tab.
				var tvi = item as TabViewItem;
				if (tvi == null)
				{
					tvi = ContainerFromItem(item) as TabViewItem;
				}

				if (tvi != null)
				{
					tvi.Width = tabWidth;
				}
			}
		}
	}

	void UpdateSelectedItem()
	{
		var listView = m_listView;
		if (listView != null)
		{
			listView.SelectedItem = SelectedItem;
		}
	}

	private void UpdateSelectedIndex()
	{
		var listView = m_listView;
		if (listView != null)
		{
			var selectedIndex = SelectedIndex;
			// Ensure that the selected index is within range of the items
			if (selectedIndex < listView.Items.Count)
			{
				listView.SelectedIndex = selectedIndex;
			}
		}
	}

	public DependencyObject ContainerFromItem(object item)
	{
		var listView = m_listView;
		if (listView != null)
		{
			return listView.ContainerFromItem(item);
		}
		return null;
	}

	public DependencyObject ContainerFromIndex(int index)
	{
		var listView = m_listView;
		if (listView != null)
		{
			return listView.ContainerFromIndex(index);
		}
		return null;
	}

	internal int IndexFromContainer(DependencyObject container)
	{
		if (m_listView is ListView listView)
		{
			return listView.IndexFromContainer(container);
		}
		return -1;
	}

	public object ItemFromContainer(DependencyObject container)
	{
		var listView = m_listView;
		if (listView != null)
		{
			return listView.ItemFromContainer(container);
		}
		return null;
	}

	private int GetItemCount()
	{
		var itemsSource = TabItemsSource;
		if (itemsSource != null)
		{
			if (itemsSource is IVector<object> vector)
			{
				return vector.Count;
			}
			else
			if (itemsSource is IEnumerable iterable)
			{
				var i = 0;
				foreach (var o in iterable)
				{
					i++;
				}

				return i;
			}
			return 0;
		}
		else
		{
			return (int)TabItems.Count;
		}
	}

	internal bool MoveFocus(bool moveForward)
	{
		var focusedControl = XamlRoot is not null ? FocusManager.GetFocusedElement(XamlRoot) as Control : null;

		// If there's no focused control, then we have nothing to do.
		if (focusedControl is null)
		{
			return false;
		}

		// Focus goes in this order:
		//
		//    Tab 1 -> Tab 1 close button -> Tab 2 -> Tab 2 close button -> ... -> Tab N -> Tab N close button -> Add tab button -> Tab 1
		//
		// Any element that's not focusable is skipped.
		//
		List<Control> focusOrderList = new();

		for (int i = 0; i < GetItemCount(); i++)
		{
			if (ContainerFromIndex(i) is TabViewItem tab)
			{
				if (IsFocusable(tab, false /* checkTabStop */))
				{
					focusOrderList.Add(tab);

					if (tab.GetCloseButton() is { } closeButton)
					{
						if (IsFocusable(closeButton, false /* checkTabStop */))
						{
							focusOrderList.Add(closeButton);
						}
					}
				}
			}
		}

		if (m_addButton is { } addButton)
		{
			if (IsFocusable(addButton, false /* checkTabStop */))
			{
				focusOrderList.Add(addButton);
			}
		}

		var position = focusOrderList.IndexOf(focusedControl);

		// The focused control is not in the focus order list - nothing for us to do here either.
		if (position < 0)
		{
			return false;
		}

		// At this point, we know that the focused control is indeed in the focus list, so we'll move focus to the next or previous control in the list.

		int sourceIndex = position;
		int listSize = focusOrderList.Count;
		int increment = moveForward ? 1 : -1;
		int nextIndex = sourceIndex + increment;

		if (nextIndex < 0)
		{
			nextIndex = listSize - 1;
		}
		else if (nextIndex >= listSize)
		{
			nextIndex = 0;
		}

		// We have to do a bit of a dance for the close buttons - we don't want users to be able to give them focus when tabbing through an app,
		// since we only want to tab into the TabView once and then tab out on the next tab press.  However, IsTabStop also controls keyboard
		// focusability in general - we can't give keyboard focus to a control with IsTabStop = false.  To work around this, we'll temporarily set
		// IsTabStop = true before calling Focus(), and then set it back to false if it was previously false.

		var control = focusOrderList[nextIndex];
		bool originalIsTabStop = control.IsTabStop;

		using var scopeGuard = Disposable.Create(() =>
		{
			control.IsTabStop = originalIsTabStop;
		});

		control.IsTabStop = true;

		// We checked focusability above, so we should never be in a situation where Focus() returns false.
		MUX_ASSERT(control.Focus(FocusState.Keyboard));
		return true;
	}

	private bool MoveSelection(bool moveForward)
	{
		int originalIndex = SelectedIndex;
		int increment = moveForward ? 1 : -1;
		int currentIndex = originalIndex + increment;
		int itemCount = GetItemCount();

		while (currentIndex != originalIndex)
		{
			if (currentIndex < 0)
			{
				currentIndex = itemCount - 1;
			}
			else if (currentIndex >= itemCount)
			{
				currentIndex = 0;
			}

			if (IsFocusable(ContainerFromIndex(currentIndex)))
			{
				SelectedIndex = currentIndex;
				return true;
			}

			currentIndex += increment;
		}

		return false;
	}

	private bool RequestCloseCurrentTab()
	{
		bool handled = false;
		var selectedTab = SelectedItem as TabViewItem;
		if (selectedTab != null)
		{
			if (selectedTab.IsClosable)
			{
				// Close the tab on ctrl + F4
				RequestCloseTab(selectedTab, true);
				handled = true;
			}
		}

		return handled;
	}

	private void OnCtrlF4Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
	{
		args.Handled = RequestCloseCurrentTab();
	}

	private void OnCtrlTabInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
	{
		args.Handled = MoveSelection(true /* moveForward */);
	}

	private void OnCtrlShiftTabInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
	{
		args.Handled = MoveSelection(false /* moveForward */);
	}

	private void OnAddButtonKeyDown(object sender, KeyRoutedEventArgs args)
	{
		if (m_addButton is { } addButton)
		{
			if (args.Key == VirtualKey.Right)
			{
				args.Handled = MoveFocus(addButton.FlowDirection == FlowDirection.LeftToRight);
			}
			else if (args.Key == VirtualKey.Left)
			{
				args.Handled = MoveFocus(addButton.FlowDirection != FlowDirection.LeftToRight);
			}
		}
	}

	// Note that the parameter is a DependencyObject for convenience to allow us to call this on the return value of ContainerFromIndex.
	// There are some non-control elements that can take focus - e.g. a hyperlink in a RichTextBlock - but those aren't relevant for our purposes here.
	private new bool IsFocusable(DependencyObject dependencyObject, bool checkTabStop = false)
	{
		if (dependencyObject is null)
		{
			return false;
		}

		if (dependencyObject is Control control)
		{
			return control is not null &&
				control.Visibility == Visibility.Visible &&
				(control.IsEnabled || control.AllowFocusWhenDisabled) &&
				(control.IsTabStop || !checkTabStop);
		}

		else
		{
			return false;
		}
	}

	private void UpdateIsItemDraggedOver(bool isItemDraggedOver)
	{
		if (m_isItemDraggedOver != isItemDraggedOver)
		{
			m_isItemDraggedOver = isItemDraggedOver;
			UpdateTabWidths();
		}
	}

	private void UpdateTabViewWithTearOutList()
	{
		var tabViewWithTearOutList = GetTabViewWithTearOutList();

		var thisAsWeak = new WeakReference<TabView>(this);
		var existingIterator = tabViewWithTearOutList.FirstOrDefault(item => item == thisAsWeak);

		if (CanTearOutTabs && IsLoaded && existingIterator == null)
		{
			tabViewWithTearOutList.Add(thisAsWeak);
		}
		else if ((!CanTearOutTabs || !IsLoaded) && existingIterator != null)
		{
			tabViewWithTearOutList.Remove(thisAsWeak);
		}
	}

	private void AttachMoveSizeLoopEvents()
	{
#pragma warning disable CS0162
		//UNO TODO: Implement Microsoft.UI.GetWindowFromWindowId, GetWindowPlacement and SetWindowPos, GetWindowRect 
		//if (CanTearOutTabs)
		if (false)
		{
			if (IsLoaded && m_enteringMoveSizeToken.Disposable == null)
			{
				var nonClientPointerSource = GetInputNonClientPointerSource();

				nonClientPointerSource.EnteringMoveSize += OnEnteringMoveSize;
				m_enteringMoveSizeToken.Disposable = Disposable.Create(() =>
				{
					nonClientPointerSource.EnteringMoveSize -= OnEnteringMoveSize;
				});
				nonClientPointerSource.EnteredMoveSize += OnEnteredMoveSize;
				m_enteredMoveSizeToken.Disposable = Disposable.Create(() =>
				{
					nonClientPointerSource.EnteredMoveSize -= OnEnteredMoveSize;
				});
				nonClientPointerSource.WindowRectChanging += OnWindowRectChanging;
				m_windowRectChangingToken.Disposable = Disposable.Create(() =>
				{
					nonClientPointerSource.WindowRectChanging -= OnWindowRectChanging;
				});
				nonClientPointerSource.ExitedMoveSize += OnExitedMoveSize;
				m_exitedMoveSizeToken.Disposable = Disposable.Create(() =>
				{
					nonClientPointerSource.ExitedMoveSize -= OnExitedMoveSize;
				});
			}
		}
		else
		if (m_inputNonClientPointerSource != null)
		{
			m_enteringMoveSizeToken.Disposable = null;
			m_enteredMoveSizeToken.Disposable = null;
			m_windowRectChangingToken.Disposable = null;
			m_exitedMoveSizeToken.Disposable = null;
		}
#pragma warning restore CS0162
	}

	//
	// We initialize the tab tear-out state machine when we enter the move-size loop. The state machine has two states it can be in:
	// either we're dragging a tab within a tab view, or we're dragging a tab that has been torn out of a tab view.
	//
	// If we start dragging a tab in a tab view with multiple tabs, then we'll start in the former state.  We'll raise the TabTearOutWindowRequested event,
	// which prompts the app to create a new window to host the tab's data object.
	// 
	// If we start dragging a tab in a tab view where that is its only tab, then we'll start in the latter state.  We will *not* raise the TabTearOutWindowRequested event,
	// because in this case, the window being dragged is the one that owns the tab view with a single tab.
	//
	// We update the state machine in the WindowRectChanging event.  See that method for a description of the state machine's functionality.
	//

	private void OnEnteringMoveSize(InputNonClientPointerSource sender, EnteringMoveSizeEventArgs args)
	{
		// We only perform tab tear-out when a move is being performed.
		if (args.MoveSizeOperation != MoveSizeOperation.Move)
		{
			return;
		}

		var pointInIslandCords = XamlRoot.CoordinateConverter.ConvertScreenToLocal(args.PointerScreenPoint);
		var tab = GetTabAtPoint(pointInIslandCords);

		if (tab is not { })
		{
			return;
		}

		var dataItem = ItemFromContainer(tab);

		m_isInTabTearOutLoop = true;
		m_tabBeingDragged = tab;
		m_dataItemBeingDragged = dataItem;
		m_tabViewContainingTabBeingDragged = this;
		m_originalTabBeingDraggedPoint = m_tabBeingDragged.TransformToVisual(null).TransformPoint(new Point(0, 0));

		SelectedItem = m_dataItemBeingDragged;

		// We don't want to create a new window for tearing out if every tab is being torn out -
		// in that case, we just want to drag the window.
		if (GetItemCount() > 1)
		{
			var windowRequestedArgs = new TabViewTabTearOutWindowRequestedEventArgs(dataItem, tab);
			TabTearOutWindowRequested?.Invoke(this, windowRequestedArgs);

			//UNO TODO: Implement Microsoft.UI.GetWindowFromWindowId, GetWindowPlacement, ContentIslandEnvironment and SetWindowPos, GetWindowRect
			/*			
			args.MoveSizeWindowId = windowRequestedArgs.NewWindowId;

			var newWindow = GetWindowFromWindowId;
			m_tabTearOutNewAppWindow = AppWindow.GetFromWindowId(windowRequestedArgs.NewWindowId);
			var currentWindow = Microsoft.UI.GetWindowFromWindowId(XamlRoot.ContentIslandEnvironment.AppWindowId);

			WINDOWPLACEMENT wp{ };
			wp.length = sizeof(wp);
			GetWindowPlacement(currentWindow, &wp);

			// We'll position the new window to be hidden at the same position as the current window and with the restored size of the current window.
			Rect windowRect;

			GetWindowRect(currentWindow, &windowRect);
			SetWindowPos(
				newWindow,
				currentWindow,
				windowRect.left,
				windowRect.top,
				wp.rcNormalPosition.right - wp.rcNormalPosition.left,
				wp.rcNormalPosition.bottom - wp.rcNormalPosition.top,
				SWP_SHOWWINDOW);
			*/
		}
		else
		{
			//m_tabTearOutNewAppWindow = AppWindow.GetFromWindowId(XamlRoot.ContentIslandEnvironment.AppWindowId);
		}
	}

	private void OnEnteredMoveSize(InputNonClientPointerSource sender, EnteredMoveSizeEventArgs args)
	{
		if (!m_isInTabTearOutLoop)
		{
			return;
		}

		MUX_ASSERT(CanTearOutTabs && m_tabTearOutDraggingState == TabTearOutDraggingState.Idle);
		m_tabTearOutDraggingState = GetItemCount() > 1 ? TabTearOutDraggingState.DraggingTabWithinTabView : TabTearOutDraggingState.DraggingTornOutTab;
		m_tabTearOutInitialPosition = args.PointerScreenPoint;
		m_dragPositionOffset = default;

		// If we're starting in the state of dragging a torn out tab, let's populate the list of tab views and their bounds now.
		if (m_tabTearOutDraggingState == TabTearOutDraggingState.DraggingTornOutTab)
		{
			PopulateTabViewList();
		}
	}

	//
	// The tab tear-out state machine proceeds as follows.
	// 
	// When dragging a tab within a tab view:
	//   - If the tab is still within the bounds of the tab view, then we'll update its position in the item list based on where the user has dragged it -
	//     e.g., if the user has dragged it more than 1/2 of the way across the width of the tab to the right, then we'll swap the positions of those two tabs
	//     to keep the dragged tab underneath the user's pointer.
	//   - If the tab is no longer within the bounds of the tab view, then we'll transition to the torn-out tab state.  We'll raise the TabTearOutRequested event,
	//     which prompts the app to remove the tab's data object from the item list of the tab view it's being torn out from.  We'll then show the window created
	//     in response to TabTearOutWindowRequested, which will now display the data object that has been torn out.
	//
	// When dragging a torn-out tab:
	//   - If the tab is not over a tab view with CanTearOutTabs set to true, then we won't do anything, which will allow the window to be dragged as normal.
	//   - If the tab is over a tab view with CanTearOutTabs set to true, then we'll raise the ExternalTornOutTabsDropping event, which allows the app
	//     to decide whether it wants to allow the tab to be dropped into the tab view.  If it does, then we'll raise the ExternalTornOutTabsDropped event,
	//     which prompts the app to move the tab's data object to the item list of the tab view in question, then hide the window being dragged,
	//     and finally transition to the dragging within tab view state.
	//
	// The tab tear-out state concludes when the user releases the pointer.
	//

	private void OnWindowRectChanging(InputNonClientPointerSource sender, WindowRectChangingEventArgs args)
	{
		if (!m_isInTabTearOutLoop)
		{
			return;
		}

		switch (m_tabTearOutDraggingState)
		{
			case TabTearOutDraggingState.DraggingTabWithinTabView:
				DragTabWithinTabView(args);
				break;
			case TabTearOutDraggingState.DraggingTornOutTab:
				DragTornOutTab(args);
				break;
		}

		var newWindowRect = args.NewWindowRect;
		var rasterizationScale = XamlRoot.RasterizationScale;
		newWindowRect.X -= (int)(m_dragPositionOffset.X * rasterizationScale);
		newWindowRect.Y -= (int)(m_dragPositionOffset.Y * rasterizationScale);
		args.NewWindowRect = newWindowRect;
	}

	private void DragTabWithinTabView(WindowRectChangingEventArgs args)
	{
		var pointInIslandCoords = m_tabViewContainingTabBeingDragged.XamlRoot.CoordinateConverter.ConvertScreenToLocal(args.PointerScreenPoint);
		var tabBeingDragged = m_tabViewContainingTabBeingDragged.ContainerFromItem(m_dataItemBeingDragged) as TabViewItem;

		if (tabBeingDragged is not { })
		{
			return;
		}

		if (m_tabViewContainingTabBeingDragged.m_listView is { } listView)
		{
			// We'll retrieve the bounds of the tab view's list view in which we're dragging the tab, in order to be able to tell whether the tab has been dragged out of it.
			var bounds = listView.TransformToVisual(null).TransformBounds(new Rect(0, 0, listView.ActualWidth, listView.ActualHeight));

			// We'll add a one-pixel margin to the bounds, since otherwise we could run into the situation where we immediately reattach after dragging out of a tab view,
			// depending on how sub-pixel rounding works out.
			bounds.X -= 1;
			bounds.Y -= 1;
			bounds.Width += 2;
			bounds.Height += 2;

			if (SharedHelpers.DoesRectContainPoint(bounds, pointInIslandCoords))
			{
				// If the tab view bounds contain the pointer point, then we'll update the index of the tab being dragged within its tab view.
				UpdateTabIndex(tabBeingDragged, pointInIslandCoords);
			}
			else
			{
				// Otherwise, we'll tear out the tab and show the window created to host the torn-out tab.
				TearOutTab(tabBeingDragged, pointInIslandCoords);
			}
		}
	}

	private void UpdateTabIndex(TabViewItem tabBeingDragged, Point pointerPosition)
	{
		var tabViewImpl = m_tabViewContainingTabBeingDragged;

		// We'll first figure out what tab is located at the position in question.  This may return null if, for example,
		// the user has dragged over the add-tab button, in which case we'll just early-out.
		if (tabViewImpl.GetTabAtPoint(pointerPosition) is { } tabAtPoint)
		{
			// Now we'll retrieve the data item associated with that tab.  If it's the data item of the tab we're dragging,
			// then we know that the tab doesn't need to move - the pointer is still over the tab in question.
			// If it's *not* the data item of the tab we're dragging, then we'll swap the tab the pointer is over
			// with the tab we're dragging.
			var dataItemAtPoint = tabViewImpl.ItemFromContainer(tabAtPoint);

			if (dataItemAtPoint != m_dataItemBeingDragged)
			{
				var newIndex = tabViewImpl.IndexFromContainer(tabAtPoint);

				// If this tab view has an items source set, we'll swap the items in the items source.
				// Otherwise, we'll swap the tab items themselves.
				if (tabViewImpl.TabItemsSource is { } tabItemsSource)
				{
					if (tabItemsSource is IVector<object> tabItemsSourceVector)
					{
						tabItemsSourceVector.RemoveAt(tabViewImpl.IndexFromContainer(tabBeingDragged));
						tabItemsSourceVector.Insert(newIndex, m_dataItemBeingDragged);
					}
				}
				else
				{
					m_tabViewContainingTabBeingDragged.TabItems.RemoveAt(tabViewImpl.IndexFromContainer(tabBeingDragged));
					m_tabViewContainingTabBeingDragged.TabItems.Insert(newIndex, tabBeingDragged);
				}

				// Finally, we'll re-select the tab being dragged, since it has changed positions.
				m_tabViewContainingTabBeingDragged.SelectedIndex = newIndex;
			}
		}
	}

	private void TearOutTab(TabViewItem tabBeingDragged, Point pointerPosition)
	{
		// We'll first raise the TabTearOutRequested event, which prompts the app to move the torn-out tab data item from its current tab view to the one in the new window.
		m_tabViewContainingTabBeingDragged.TabTearOutRequested.Invoke(this, new TabViewTabTearOutRequestedEventArgs(m_dataItemBeingDragged, tabBeingDragged));

		// We're now dragging a torn out tab, so let's populate the list of tab views.
		m_tabTearOutDraggingState = TabTearOutDraggingState.DraggingTornOutTab;
		PopulateTabViewList();

		// Now we'll show the window.
		m_tabTearOutNewAppWindow.Show();

		UpdateLayout();

		if (m_tabViewContainingTabBeingDragged is not { })
		{
			m_tabViewContainingTabBeingDragged.UpdateLayout();

			// We want to keep the tab under the user's pointer, so we'll subtract off the difference from the XAML position of the tab in the original window,
			// in order to ensure we position the window such that the tab in the new window is in the same position as the tab in the old window.
			var containingTabPosition = (m_tabViewContainingTabBeingDragged.ContainerFromIndex(m_tabViewContainingTabBeingDragged.SelectedIndex) as TabViewItem).TransformToVisual(null).TransformPoint(new Point(0, 0));
			m_dragPositionOffset = new Point(containingTabPosition.X - m_originalTabBeingDraggedPoint.X, containingTabPosition.Y - m_originalTabBeingDraggedPoint.Y);
		}
		else
		{
			m_dragPositionOffset = default;
		}
	}

	private void DragTornOutTab(WindowRectChangingEventArgs args)
	{
		// When we're dragging a torn-out tab, we want to check, as the window moves, whether the user has dragged the tab over a tab view that will allow it to be dropped into it.
		// We'll iterate through the list of tab views and their bounds and check each of their screen positions against the screen position of the pointer.
		foreach (var (otherTabViewScreenBounds, otherTabView) in m_tabViewBoundsTuples)
		{
			var otherTabViewScreenBoundsRect = new Rect(otherTabViewScreenBounds.X, otherTabViewScreenBounds.Y, otherTabViewScreenBounds.Width, otherTabViewScreenBounds.Height);
			var pointerScreenPoint = new Point(args.NewWindowRect.X, args.NewWindowRect.Y);

			if (SharedHelpers.DoesRectContainPoint(otherTabViewScreenBoundsRect, pointerScreenPoint))
			{
				// We'll check which index we need to insert the tab at.
				int insertionIndex = GetTabInsertionIndex(otherTabView, args.PointerScreenPoint);

				// If we got a valid index, we'll begin attempting to merge the tab into this tab view.
				if (insertionIndex >= 0)
				{
					// First, we'll raise the ExternalTornOutTabsDropping event, which asks the app whether this tab view should accept the tab being dropped into it.
					var tabsDroppingArgs = new TabViewExternalTornOutTabsDroppingEventArgs(m_dataItemBeingDragged, m_tabBeingDragged, insertionIndex);

					otherTabView.ExternalTornOutTabsDropping.Invoke(otherTabView, tabsDroppingArgs);

					// If the response was yes, then we'll raise the ExternalTornOutTabsDropped event, which prompts the app to actually move the tab's data item
					// to the new tab view; we'll flag the new tab view as the one containing the tab we're dragging; we'll move to the dragging tab within a tab view state;
					// and finally we'll then hide the torn-out tab window.
					if (tabsDroppingArgs.AllowDrop)
					{
						// We're about to merge the tab into the other tab view, so we'll retrieve and save off the tab view that currently holds the tab being dragged.
						// We'll need to remove it from the list of tab views with CanTearOutTabs set to true if its window is destroyed.
						m_tabViewInNewAppWindow = m_tabViewContainingTabBeingDragged;

						// Raise the ExternalTornOutTabsDropped event
						otherTabView.ExternalTornOutTabsDropped.Invoke(otherTabView,
							new TabViewExternalTornOutTabsDroppedEventArgs(m_dataItemBeingDragged, m_tabBeingDragged, insertionIndex));

						otherTabView.SelectedItem = m_dataItemBeingDragged;

						// If the other tab view's app window is a different app window than the one being dragged, bring it to the front beneath the one being dragged.
						var otherTabViewAppWindow = AppWindow.GetFromWindowId(otherTabView.XamlRoot.ContentIslandEnvironment.AppWindowId);

						//UNO TODO: Implement MoveInZOrderAtTop on AppWindow
						//otherTabViewAppWindow?.MoveInZOrderAtTop();

						m_tabViewContainingTabBeingDragged = otherTabView;
						m_dragPositionOffset = default;
						m_tabTearOutDraggingState = TabTearOutDraggingState.DraggingTabWithinTabView;

						//UNO TODO: Implement Hide on AppWindow
						//m_tabTearOutNewAppWindow.Hide();

						break;
					}
				}
			}
		}
	}

	private int GetTabInsertionIndex(TabView otherTabView, PointInt32 screenPosition)
	{
		var index = -1;

		// To get the insertion index, we'll first check what tab (if any) is beneath the screen position.
		var tab = otherTabView.GetTabAtPoint(otherTabView.XamlRoot.CoordinateConverter.ConvertScreenToLocal(screenPosition));

		if (tab is not { })
		{
			// If there was a tab underneath the position, then we'll check whether the screen position is on its left side or its right side.
			// If it's on the left side, we'll set the insertion position to be before this tab. Otherwise, we'll set it to be after this tab.
			var tabIndex = otherTabView.IndexFromContainer(tab);
			var tabRect = otherTabView.XamlRoot.CoordinateConverter.ConvertLocalToScreen(tab.TransformToVisual(null).TransformBounds(new Rect(0, 0, tab.ActualWidth, tab.ActualHeight)));

			if (screenPosition.X < tabRect.X + tabRect.Width / 2)
			{
				index = tabIndex;
			}
			else
			{
				index = tabIndex + 1;
			}
		}
		else
		if (otherTabView.GetItemCount() > 0)
		{
			// If there was no tab, under the cursor, then that suggests we want to insert the tab either at the very beginning or at the very end.
			// We'll first check whether the screen position is to the left of the bounds of the first tab.  If so, we'll set the insertion position
			// to be the start of the item list.
			var firstTab = otherTabView.ContainerFromIndex(0) as TabViewItem;

			if (firstTab is { })
			{
				var firstTabRect = otherTabView.XamlRoot.CoordinateConverter.ConvertLocalToScreen(firstTab.TransformToVisual(null).TransformBounds(new Rect(0, 0, firstTab.ActualWidth, firstTab.ActualHeight)));

				if (screenPosition.X < firstTabRect.X)
				{
					index = 0;
				}
			}

			// If that wasn't the case, then next we'll check whether the screen position is to the right of the bounds of the last tab.
			// If so, we'll set the insertion position to be the end of the item list.
			if (index < 0)
			{
				var lastTabIndex = otherTabView.GetItemCount() - 1;
				var lastTab = otherTabView.ContainerFromIndex(lastTabIndex) as TabViewItem;

				if (lastTab is { })
				{
					var lastTabRect = otherTabView.XamlRoot.CoordinateConverter.ConvertLocalToScreen(lastTab.TransformToVisual(null).TransformBounds(new Rect(0, 0, lastTab.ActualWidth, lastTab.ActualHeight)));
					if (screenPosition.X > lastTabRect.X + lastTabRect.Width)
					{
						index = otherTabView.GetItemCount();
					}
				}
			}
		}

		return index;
	}

	// When we exit the move-size loop, we'll reset the tab tear-out state machine to an idle state, and check the status of the window that was created.
	// If the window is currently hidden, then the user has merged the torn out tab with another tab view, and the window is no longer needed.
	// In that case, we'll queue the window for destruction.

	private void OnExitedMoveSize(InputNonClientPointerSource sender, ExitedMoveSizeEventArgs args)
	{
		if (!m_isInTabTearOutLoop)
		{
			return;
		}

		m_tabTearOutDraggingState = TabTearOutDraggingState.Idle;

		if (!m_tabTearOutNewAppWindow.IsVisible)
		{
			// We're about to close the window containing the tab view that had been holding the tab view,
			// so we'll remove it from the list of tab views with CanTearOutTabs set to true
			// This will ensure that it's immediately removed from the list rather than waiting for the
			// WM_CLOSE message to be handled.
			if (m_tabViewInNewAppWindow is { })
			{
				var tabViewWithTearOutList = GetTabViewWithTearOutList();
				var windowTabViewAsWeak = new WeakReference<TabView>(m_tabViewInNewAppWindow);
				var existingIterator = tabViewWithTearOutList.Find(tv => tv == windowTabViewAsWeak);

				if (existingIterator != null)
				{
					tabViewWithTearOutList.Remove(existingIterator);
				}
			}

			//PostMessageW(winrt::Microsoft::UI::GetWindowFromWindowId(m_tabTearOutNewAppWindow.Id()), WM_CLOSE, 0, 0);
		}
		else if (m_tabViewContainingTabBeingDragged != null)
		{
			// Otherwise, if the window is still open, let's update its tab view's non-client region.
			m_tabViewContainingTabBeingDragged.UpdateNonClientRegion();
		}

		// We'll also update this tab view's non-client region, now that it's stabilized.
		UpdateNonClientRegion();

		m_isInTabTearOutLoop = false;

		// We'll also update tab widths, since we ceased doing so while the move-size loop was underway.
		UpdateTabWidths();
	}

	private TabViewItem GetTabAtPoint(Point point)
	{
		// Convert the point to a point in the TabView's coordinate space
		// and then detect which TabViewItem is at that point.
		var tabViewRect = TransformToVisual(null).TransformBounds(new Rect(0, 0, ActualWidth, ActualHeight));

		if (SharedHelpers.DoesRectContainPoint(tabViewRect, point))
		{
			var tabCount = GetItemCount();
			for (int i = 0; i < tabCount; i++)
			{
				if (ContainerFromIndex(i) is TabViewItem tab)
				{
					var tabRect = tab.TransformToVisual(null).TransformBounds(new Rect(0, 0, tab.ActualWidth, tab.ActualHeight));
					if (SharedHelpers.DoesRectContainPoint(tabRect, point))
					{
						return tab;
					}
				}
			}
		}

		return null;
	}

	private void PopulateTabViewList()
	{
		// When we're dragging a torn-out tab, we want to check, as the window moves, whether the user has dragged the tab over a tab view that will allow it to be dropped into it.
		// We'll pre-fill a list of tab views and their screen bounds when we start dragging a torn-out tab, on the basis that they are unlikely to move while we're dragging the tab.
		m_tabViewBoundsTuples.Clear();

		// We'll also track which tab view holds the torn-out tab.
		m_tabViewContainingTabBeingDragged = null;

		var tabViewWithTearOutList = GetTabViewWithTearOutList();

		for (int iterator = 0; iterator < tabViewWithTearOutList.Count;)
		{
			if (!tabViewWithTearOutList[iterator].TryGetTarget(out var otherTabView))
			{
				tabViewWithTearOutList.RemoveAt(iterator);
				continue;
			}

			// We only want to populate the tuple list with tab views that don't currently contain the item being dragged,
			// since this tuple list is used to detect tab views that the item being dragged can be dragged onto.
			bool otherTabViewContainsTab = false;

			if (otherTabView.TabItemsSource is IList<object> tabItemsSourceVector)
			{
				otherTabViewContainsTab = tabItemsSourceVector.Contains(m_dataItemBeingDragged);
			}
			else
			{
				otherTabViewContainsTab = otherTabView.TabItems.IndexOf(m_tabBeingDragged) != -1;
			}

			if (otherTabViewContainsTab)
			{
				m_tabViewContainingTabBeingDragged = otherTabView;
			}
			else
			{
				if (otherTabView.m_listView is { } otherTabViewListView)
				{
					var dragBounds = otherTabViewListView.TransformToVisual(null).TransformBounds(new Rect(0, 0, otherTabViewListView.ActualWidth, otherTabViewListView.ActualHeight));

					// We'll add the add button's bounds to the drag bounds, since it's also a valid element to drag a tab over, which will add the tab to the end of the tab view.
					if (otherTabView.IsAddTabButtonVisible)
					{
						if (otherTabView.m_addButton is { } otherTabViewAddButton)
						{
							var addButtonBounds = otherTabViewAddButton.TransformToVisual(null).TransformBounds(new Rect(0, 0, otherTabViewAddButton.ActualWidth, otherTabViewAddButton.ActualHeight));

							var dragBoundsRight = dragBounds.X + dragBounds.Width;
							var dragBoundsBottom = dragBounds.Y + dragBounds.Height;
							var addButtonBoundsRight = addButtonBounds.X + addButtonBounds.Width;
							var addButtonBoundsBottom = addButtonBounds.Y + addButtonBounds.Height;

							var minX = Math.Min(dragBounds.X, addButtonBounds.X);
							var minY = Math.Min(dragBounds.Y, addButtonBounds.Y);

							dragBounds = new Rect(
								minX,
								minY,
								Math.Max(dragBoundsRight, addButtonBoundsRight) - minX,
								Math.Max(dragBoundsBottom, addButtonBoundsBottom) - minY);
						}
					}

					var dragScreenBounds = otherTabView.XamlRoot.CoordinateConverter.ConvertLocalToScreen(dragBounds);

					m_tabViewBoundsTuples.Add(new(dragScreenBounds, otherTabView));
				}
			}

			iterator++;
		}
	}

	// At the moment, all TabViews and windows are on the same thread.
	// However, that won't always be the case, so to handle things
	// when it isn't, we'll lock the accessing of the list of TabViews
	// behind a mutex and require its acquisition to interact with the list.
	public static List<WeakReference<TabView>> GetTabViewWithTearOutList()
	{
		lock (_tabViewListLock)
		{
			return new List<WeakReference<TabView>>(s_tabViewWithTearOutList);
		}
	}

	private InputNonClientPointerSource GetInputNonClientPointerSource()
	{
		var windowId = GetAppWindowId();

		if (m_inputNonClientPointerSource is not { } && windowId.Value != 0)
		{
			// UNO TODO: Port this properly from WinUI, returns new InputNonClientPointerSource
			m_inputNonClientPointerSource = InputNonClientPointerSource.GetForWindowId(windowId);
		}

		return m_inputNonClientPointerSource;
	}

	private ContentCoordinateConverter GetAppWindowCoordinateConverter()
	{
		var windowId = GetAppWindowId();

		if (m_appWindowCoordinateConverter is not { } && windowId.Value != 0)
		{
			// UNO TODO: Port this properly from WinUI, returns new ContentCoordinateConverter
			m_appWindowCoordinateConverter = ContentCoordinateConverter.CreateForWindowId(windowId);
		}

		return m_appWindowCoordinateConverter;
	}

	private void UpdateNonClientRegion()
	{
		if (GetInputNonClientPointerSource() is not { } nonClientPointerSource)
		{
			return;
		}

		var captionRects = nonClientPointerSource.GetRegionRects(NonClientRegionKind.Caption);

		// We need to preserve non-client caption regions set by components other than us,
		// so we'll keep around all caption regions except the one that we set.
		var captionRegions = captionRects
			.Where(rect => !m_nonClientRegionSet || rect != m_nonClientRegion)
			.ToList();

		if (CanTearOutTabs && IsLoaded)
		{
			if (m_listView is { } listView)
			{
				if (listView.IsLoaded)
				{
					if (GetAppWindowCoordinateConverter() is { } appWindowCoordinateConverter)
					{
						var listViewBounds = listView.TransformToVisual(null).TransformBounds(new Rect(0, 0, listView.ActualWidth, listView.ActualHeight));

						if (listViewBounds.X < 0 || listViewBounds.Y < 0)
						{
							return;
						}

						// Non-client region rects need to be in the coordinate system of the owning app window, so we'll take our XAML island coordinates,
						// convert them to screen coordinates, and then convert from there to app window coordinates.
						var appWindowListViewBounds = appWindowCoordinateConverter.ConvertScreenToLocal(XamlRoot.CoordinateConverter.ConvertLocalToScreen(listViewBounds));

						m_nonClientRegion = new RectInt32(
							(int)appWindowListViewBounds.X,
							(int)appWindowListViewBounds.Y,
							(int)appWindowListViewBounds.Width,
							(int)appWindowListViewBounds.Height);

						m_nonClientRegionSet = true;

						captionRegions.Add(m_nonClientRegion);
					}
				}
			}
		}

		//UNO TODO: Implement SetRegionRects on InputNonClientPointerSource
		//nonClientPointerSource.SetRegionRects(NonClientRegionKind.Caption, captionRegions.ToArray());
	}

	private Microsoft.UI.WindowId GetAppWindowId()
	{
		Microsoft.UI.WindowId appWindowId = default;

		if (XamlRoot is { } xamlRoot)
		{
			//UNO TODO: Implement ContentIslandEnvironment https://github.com/unoplatform/uno/issues/19392

			//if (xamlRoot.ContentIslandEnvironment is { } contentIslandEnvironment)
			//{
			//	appWindowId = contentIslandEnvironment.AppWindowId;
			//}
		}

		if (appWindowId.Value != m_lastAppWindowId.Value)
		{
			m_lastAppWindowId = appWindowId;

			m_inputNonClientPointerSource = null;
			m_appWindowCoordinateConverter = null;
		}

		return appWindowId;
	}
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference TabView.cpp, commit ed31e13

#pragma warning disable 105 // remove when moving to WinUI tree

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Microsoft/* UWP don't rename */.UI.Xaml.Automation.Peers;
using Microsoft/* UWP don't rename */.UI.Xaml.Controls.Primitives;
using Uno.Disposables;
using Uno.Extensions;
using Uno.Extensions.Specialized;
using Uno.UI.Helpers.WinUI;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.System;
using Windows.UI.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;

namespace Microsoft/* UWP don't rename */.UI.Xaml.Controls
{
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
			// Uno specific: Needs to be initialized here, as field can't use "this"
			m_dispatcherHelper = new DispatcherHelper(this);

			var items = new ObservableVector<object>();
			SetValue(TabItemsProperty, items);

			SetDefaultStyleKey(this);

			Loaded += OnLoaded;

			// KeyboardAccelerator is only available on RS3+
			if (SharedHelpers.IsRS3OrHigher())
			{
				KeyboardAccelerator ctrlf4Accel = new KeyboardAccelerator();
				ctrlf4Accel.Key = VirtualKey.F4;
				ctrlf4Accel.Modifiers = VirtualKeyModifiers.Control;
				ctrlf4Accel.Invoked += OnCtrlF4Invoked;
				ctrlf4Accel.ScopeOwner = this;
				KeyboardAccelerators.Add(ctrlf4Accel);

				m_tabCloseButtonTooltipText = ResourceAccessor.GetLocalizedStringResource(ResourceAccessor.SR_TabViewCloseButtonTooltipWithKA);
			}
			else
			{
				m_tabCloseButtonTooltipText = ResourceAccessor.GetLocalizedStringResource(ResourceAccessor.SR_TabViewCloseButtonTooltip);
			}

			// Ctrl+Tab as a KeyboardAccelerator only works on 19H1+
			if (SharedHelpers.Is19H1OrHigher())
			{
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
		}

		protected override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			UnhookEventsAndClearFields();

			//IControlProtected controlProtected{ *this };

			m_tabContentPresenter = (ContentPresenter)GetTemplateChild("TabContentPresenter");
			m_rightContentPresenter = (ContentPresenter)GetTemplateChild("RightContentPresenter");

			m_leftContentColumn = (ColumnDefinition)GetTemplateChild("LeftContentColumn");
			m_tabColumn = (ColumnDefinition)GetTemplateChild("TabColumn");
			m_addButtonColumn = (ColumnDefinition)GetTemplateChild("AddButtonColumn");
			m_rightContentColumn = (ColumnDefinition)GetTemplateChild("RightContentColumn");

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

			if (!SharedHelpers.Is21H1OrHigher())
			{
				m_shadowReceiver = (Grid)GetTemplateChild("ShadowReceiver");
			}


			ListView GetListView()
			{
				var listView = GetTemplateChild("TabListView") as ListView;
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
					listView.DragOver += OnListViewDragOver;
					m_listViewDragOverRevoker.Disposable = Disposable.Create(() =>
					{
						listView.DragOver -= OnListViewDragOver;
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
				var addButton = GetTemplateChild("AddButton") as Button;
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
					m_addButtonClickRevoker.Disposable = Disposable.Create(() =>
					{
						addButton.Click -= OnAddButtonClick;
					});
				}
				return addButton;
			}
			m_addButton = GetAddButton();

			if (SharedHelpers.IsThemeShadowAvailable())
			{
				if (!SharedHelpers.Is21H1OrHigher())
				{
					var shadowCaster = GetTemplateChild("ShadowCaster") as Grid;
					if (shadowCaster != null)
					{
						var shadow = new ThemeShadow();
						shadow.Receivers.Add(GetShadowReceiver());

						double shadowDepth = (double)SharedHelpers.FindInApplicationResources(c_tabViewShadowDepthName, c_tabShadowDepth);

						var currentTranslation = shadowCaster.Translation;
						var translation = new Vector3(currentTranslation.X, currentTranslation.Y, (float)shadowDepth);
						shadowCaster.Translation = translation;

						shadowCaster.Shadow = shadow;
					}
				}
			}

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
								var next = FocusManager.FindNextElement(direction, options);
								var args2 = args;
								if (args != null)
								{
									args2.TrySetNewFocusedElement(next);
								}

								else
								{
									// Without TrySetNewFocusedElement, we cannot set focus while it is changing.
									m_dispatcherHelper.RunAsync(() =>
									{
										CppWinRTHelpers.SetFocus(next, FocusState.Programmatic);
									});
								}
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
			m_listViewDropRevoker.Disposable = null;
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

			m_tabContentPresenter = null;
			m_rightContentPresenter = null;
			m_leftContentColumn = null;
			m_tabColumn = null;
			m_addButtonColumn = null;
			m_rightContentColumn = null;
			m_tabContainerGrid = null;
			m_shadowReceiver = null;
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
		}

		protected override AutomationPeer OnCreateAutomationPeer()
		{
			return new TabViewAutomationPeer(this);
		}

		private void OnLoaded(object sender, RoutedEventArgs args)
		{
			UpdateTabContent();
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
						var itemList = new List<object>();

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
					if (SharedHelpers.IsIsLoadedAvailable() && scrollViewer.IsLoaded)
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
			}
		}

		//TODO Uno: The second parameter is needed, as OnItemsChanged may get called before OnApplyTemplate due to control lifecycle differences
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

		TabViewItem FindTabViewItemFromDragItem(object item)
		{
			var tab = ContainerFromItem(item) as TabViewItem;

			if (tab == null)
			{
				var fe = item as FrameworkElement;
				if (fe != null)
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
					var tabItem = ContainerFromIndex(i) as TabViewItem;
					if (tabItem.Content == item)
					{
						tab = tabItem;
						break;
					}
				}
			}

			return tab;
		}

		private void OnListViewDragItemsStarting(object sender, DragItemsStartingEventArgs args)
		{
			var item = args.Items[0];
			var tab = FindTabViewItemFromDragItem(item);
			var myArgs = new TabViewTabDragStartingEventArgs(args, item, tab);

			TabDragStarting?.Invoke(this, myArgs);
		}

		private void OnListViewDragOver(object sender, Microsoft.UI.Xaml.DragEventArgs args)
		{
			TabStripDragOver?.Invoke(this, args);
		}

		void OnListViewDrop(object sender, Microsoft.UI.Xaml.DragEventArgs args)
		{
			TabStripDrop?.Invoke(this, args);
		}

		private void OnListViewDragItemsCompleted(object sender, DragItemsCompletedEventArgs args)
		{
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

						// It is not ideal to call UpdateLayout here, but it is necessary to ensure that the ContentPresenter has expanded its content
						// into the live visual tree.
						tabContentPresenter.UpdateLayout();

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
			if (previousAvailableSize.Width != availableSize.Width)
			{
				previousAvailableSize = availableSize;
				UpdateTabWidths();
			}

			return base.MeasureOverride(availableSize);
		}

		private void UpdateTabWidths(bool shouldUpdateWidths = true, bool fillAllAvailableSpace = true)
		{
			double tabWidth = double.NaN;

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
					if (addButtonColumn.ActualWidth == 0 && m_addButton?.Visibility == Visibility.Visible && previousAvailableSize.Width > 0)
					{
						// Uno workaround: We may arrive here before the AddButton has been measured, and if there are enough tabs to take
						// all the space, the Grid will not assign any to the button. As a workaround we measure the button directly and use
						// its desired size.
						m_addButton.Measure(previousAvailableSize);
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
					var availableWidth = previousAvailableSize.Width - widthTaken;

					// Size can be 0 when window is first created; in that case, skip calculations; we'll get a new size soon
					if (availableWidth > 0)
					{
						if (TabWidthMode == TabViewWidthMode.Equal)
						{

							var minTabWidth = (double)SharedHelpers.FindInApplicationResources(c_tabViewItemMinWidthName, c_tabMinimumWidth);
							var maxTabWidth = (double)SharedHelpers.FindInApplicationResources(c_tabViewItemMaxWidthName, c_tabMaximumWidth);

							// If we should fill all of the available space, use scrollviewer dimensions
							var padding = Padding;
							if (fillAllAvailableSpace)
							{
								// Calculate the proportional width of each tab given the width of the ScrollViewer.
								var tabWidthForScroller = (availableWidth - (padding.Left + padding.Right)) / (double)(TabItems.Count);
								tabWidth = MathEx.Clamp(tabWidthForScroller, minTabWidth, maxTabWidth);
							}
							else
							{
								double availableTabViewSpace = (tabColumn.ActualWidth - (padding.Left + padding.Right));
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
								var tabWidthUnclamped = availableTabViewSpace / (double)(TabItems.Count);
								tabWidth = MathEx.Clamp(tabWidthUnclamped, minTabWidth, maxTabWidth);
							}


							// Size tab column to needed size
							tabColumn.MaxWidth = availableWidth;
							var requiredWidth = tabWidth * TabItems.Count;
							if (requiredWidth > (availableWidth - (padding.Left + padding.Right)))
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
								// TODO: Uno specific workaround - ListView stretches to full available width, even when it does not require it
								// tabColumn.Width = GridLengthHelper.FromValueAndType(1.0, GridUnitType.Auto);
								tabColumn.Width = GridLengthHelper.FromPixels(requiredWidth);
								var listview = m_listView;
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
							// Case: TabWidthMode "Compact" or "FitToContent"
							tabColumn.MaxWidth = availableWidth;
							tabColumn.Width = GridLengthHelper.FromValueAndType(1.0, GridUnitType.Auto);
							var listview = m_listView;
							if (listview != null)
							{
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
				var iterable = itemsSource as IEnumerable;
				if (iterable != null)
				{
					//int i = 1;
					//var iter = iterable.First();
					//while (iter.MoveNext())
					//{
					//	i++;
					//}
					//return i;
					return iterable.Count();
				}
				return 0;
			}

			else
			{
				return (int)TabItems.Count;
			}
		}

		private bool SelectNextTab(int increment)
		{
			bool handled = false;
			int itemsSize = GetItemCount();
			if (itemsSize > 1)
			{
				var index = SelectedIndex;
				index = (index + increment + itemsSize) % itemsSize;
				SelectedIndex = index;
				handled = true;
			}
			return handled;
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

		protected override void OnKeyDown(KeyRoutedEventArgs args)
		{
			var coreWindow = CoreWindow.IShouldntUseGetForCurrentThread();
			if (coreWindow != null)
			{
				if (args.Key == VirtualKey.F4)
				{
					// Handle Ctrl+F4 on RS2 and lower
					// On RS3+, it is handled by a KeyboardAccelerator
					if (!SharedHelpers.IsRS3OrHigher())
					{
						var isCtrlDown = (coreWindow.GetKeyState(VirtualKey.Control) & CoreVirtualKeyStates.Down) == CoreVirtualKeyStates.Down;
						if (isCtrlDown)
						{
							args.Handled = RequestCloseCurrentTab();
						}
					}
				}
				else if (args.Key == VirtualKey.Tab)
				{
					// Handle Ctrl+Tab/Ctrl+Shift+Tab on RS5 and lower
					// On 19H1+, it is handled by a KeyboardAccelerator
					if (!SharedHelpers.Is19H1OrHigher())
					{
						var isCtrlDown = (coreWindow.GetKeyState(VirtualKey.Control) & CoreVirtualKeyStates.Down) == CoreVirtualKeyStates.Down;
						var isShiftDown = (coreWindow.GetKeyState(VirtualKey.Shift) & CoreVirtualKeyStates.Down) == CoreVirtualKeyStates.Down;

						if (isCtrlDown && !isShiftDown)
						{
							args.Handled = SelectNextTab(1);
						}
						else if (isCtrlDown && isShiftDown)
						{
							args.Handled = SelectNextTab(-1);
						}
					}
				}
			}
		}

		private void OnCtrlF4Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
		{
			args.Handled = RequestCloseCurrentTab();
		}

		private void OnCtrlTabInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
		{
			args.Handled = SelectNextTab(1);
		}

		private void OnCtrlShiftTabInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
		{
			args.Handled = SelectNextTab(-1);
		}

		/* Header file */

		internal UIElement GetShadowReceiver() => m_shadowReceiver;

		internal string GetTabCloseButtonTooltipText() => m_tabCloseButtonTooltipText;
	}
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference BreadcrumbBar.cpp, tag winui3/release/1.4.2

#nullable enable

using System.Collections.ObjectModel;
using Microsoft.UI.Input;
using Uno.Disposables;
using Uno.UI.Core;
using Uno.UI.Helpers.WinUI;
using Windows.System;
using Windows.UI.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

namespace Microsoft/* UWP don't rename */.UI.Xaml.Controls;

/// <summary>
/// The BreadcrumbBar control provides the direct path of pages or folders to the current location.
/// </summary>
public partial class BreadcrumbBar : Control
{
	/// <summary>
	/// Initializes a new instance of the BreadcrumbBar class.
	/// </summary>
	public BreadcrumbBar()
	{
		//__RP_Marker_ClassById(RuntimeProfiler.ProfId_BreadcrumbBar);

		DefaultStyleKey = typeof(BreadcrumbBar);

		m_itemsRepeaterElementFactory = new BreadcrumbElementFactory();
		m_itemsRepeaterLayout = new BreadcrumbLayout(this);
		m_itemsIterable = new BreadcrumbIterable();

#if HAS_UNO
		this.Loaded += BreadcrumbBar_Loaded;
		this.Unloaded += BreadcrumbBar_Unloaded;
#endif
	}

	private void RevokeListeners()
	{
		m_itemsRepeaterLoadedRevoker.Disposable = null;
		m_itemsRepeaterElementPreparedRevoker.Disposable = null;
		m_itemsRepeaterElementIndexChangedRevoker.Disposable = null;
		m_itemsRepeaterElementClearingRevoker.Disposable = null;
		m_itemsSourceChanged.Disposable = null;
		m_itemsSourceAsObservableVectorChanged.Disposable = null;

#if HAS_UNO
		m_breadcrumbKeyDownHandlerRevoker.Disposable = null;
		AccessKeyInvoked -= OnAccessKeyInvoked;
		GettingFocus -= OnGettingFocus;
		PreviewKeyDown -= OnChildPreviewKeyDown;
		UnregisterPropertyChangedCallback(FlowDirectionProperty, _flowPropertyToken);
#endif
	}

	protected override void OnApplyTemplate()
	{
		base.OnApplyTemplate();

		RevokeListeners();

		//IControlProtected controlProtected{ this };

		m_itemsRepeater = (ItemsRepeater)GetTemplateChild(s_itemsRepeaterPartName);

		PreviewKeyDown += OnChildPreviewKeyDown;
		AccessKeyInvoked += OnAccessKeyInvoked;
		GettingFocus += OnGettingFocus;

		_flowPropertyToken = RegisterPropertyChangedCallback(FrameworkElement.FlowDirectionProperty, OnFlowDirectionChanged);

		if (m_itemsRepeater is { } itemsRepeater)
		{
			itemsRepeater.Layout = m_itemsRepeaterLayout;
			itemsRepeater.ItemsSource = new ObservableCollection<object>();
			itemsRepeater.ItemTemplate = m_itemsRepeaterElementFactory;

			m_itemsRepeaterElementPreparedRevoker.Disposable = Disposable.Create(() =>
			{
				itemsRepeater.ElementPrepared -= OnElementPreparedEvent;
			});
			itemsRepeater.ElementPrepared += OnElementPreparedEvent;

			m_itemsRepeaterElementIndexChangedRevoker.Disposable = Disposable.Create(() =>
			{
				itemsRepeater.ElementIndexChanged -= OnElementIndexChangedEvent;
			});
			itemsRepeater.ElementIndexChanged += OnElementIndexChangedEvent;

			m_itemsRepeaterElementClearingRevoker.Disposable = Disposable.Create(() =>
			{
				itemsRepeater.ElementClearing -= OnElementClearingEvent;
			});
			itemsRepeater.ElementClearing += OnElementClearingEvent;

			m_itemsRepeaterLoadedRevoker.Disposable = Disposable.Create(() =>
			{
				itemsRepeater.Loaded -= OnBreadcrumbBarItemsRepeaterLoaded;
			});
			itemsRepeater.Loaded += OnBreadcrumbBarItemsRepeaterLoaded;
		}

		UpdateItemsRepeaterItemsSource();
	}

	private void OnPropertyChanged(DependencyPropertyChangedEventArgs args)
	{
		var property = args.Property;

		if (property == ItemsSourceProperty)
		{
			UpdateItemsRepeaterItemsSource();
		}
		else if (property == ItemTemplateProperty)
		{
			UpdateItemTemplate();
			UpdateEllipsisBreadcrumbBarItemDropDownItemTemplate();
		}
	}

	private void OnFlowDirectionChanged(DependencyObject sender, DependencyProperty property)
	{
		UpdateBreadcrumbBarItemsFlowDirection();
	}

	private void OnBreadcrumbBarItemsRepeaterLoaded(object sender, RoutedEventArgs property)
	{
		if (m_itemsRepeater is ItemsRepeater breadcrumbItemsRepeater)
		{
			OnBreadcrumbBarItemsSourceCollectionChanged(null, null);
		}
	}

	private void UpdateItemTemplate()
	{
		var newItemTemplate = ItemTemplate;
		m_itemsRepeaterElementFactory?.UserElementFactory(newItemTemplate);
	}

	private void UpdateEllipsisBreadcrumbBarItemDropDownItemTemplate()
	{
		var newItemTemplate = ItemTemplate;

		// Copy the item template to the ellipsis item too
		if (m_ellipsisBreadcrumbBarItem is { } ellipsisBreadcrumbBarItem)
		{
			if (ellipsisBreadcrumbBarItem is { } itemImpl)
			{
				itemImpl.SetEllipsisDropDownItemDataTemplate(newItemTemplate);
			}
		}
	}

	private void UpdateBreadcrumbBarItemsFlowDirection()
	{
		// Only if some ItemsSource has been defined then we change the BreadcrumbBarItems flow direction
		if (ItemsSource != null)
		{
			if (m_itemsRepeater is { } itemsRepeater)
			{
				// Add 1 to account for the leading null
				int elementCount = (m_breadcrumbItemsSourceView?.Count ?? 0) + 1;
				for (int i = 0; i < elementCount; ++i)
				{
					var element = itemsRepeater.TryGetElement(i) as BreadcrumbBarItem;
					if (element != null)
					{
						element.FlowDirection = FlowDirection;
					}
				}
			}
		}
	}

	private void UpdateItemsRepeaterItemsSource()
	{
		m_itemsSourceChanged.Disposable = null;
		m_itemsSourceAsObservableVectorChanged.Disposable = null;

		m_breadcrumbItemsSourceView = null;
		if (ItemsSource != null)
		{
			m_breadcrumbItemsSourceView = new InspectingDataSource(ItemsSource);

			if (m_itemsRepeater is { } itemsRepeater)
			{
				m_itemsIterable = new BreadcrumbIterable(ItemsSource);
				itemsRepeater.ItemsSource = m_itemsIterable;
			}

			if (m_breadcrumbItemsSourceView != null)
			{
				m_itemsSourceChanged.Disposable = Disposable.Create(() =>
				{
					m_breadcrumbItemsSourceView.CollectionChanged -= OnBreadcrumbBarItemsSourceCollectionChanged;
				});
				m_breadcrumbItemsSourceView.CollectionChanged += OnBreadcrumbBarItemsSourceCollectionChanged;
			}
		}
	}

	private void OnBreadcrumbBarItemsSourceCollectionChanged(object? sender, object? args)
	{
		if (m_itemsRepeater is { } itemsRepeater)
		{
			// A new BreadcrumbIterable must be created as ItemsRepeater compares if the previous
			// itemsSource is equals to the new one
			m_itemsIterable = new BreadcrumbIterable(ItemsSource);
			itemsRepeater.ItemsSource = m_itemsIterable;

			// For some reason, when interacting with keyboard, the last element doesn't raise the OnPrepared event
			ForceUpdateLastElement();
		}
	}

	private void ResetLastBreadcrumbBarItem()
	{
		if (m_lastBreadcrumbBarItem is { } lastItem)
		{
			var lastItemImpl = lastItem;
			lastItemImpl.ResetVisualProperties();
		}
	}

	private void ForceUpdateLastElement()
	{
		if (m_breadcrumbItemsSourceView != null)
		{
			var itemCount = m_breadcrumbItemsSourceView.Count;

			if (m_itemsRepeater is { } itemsRepeater)
			{
				var newLastItem = itemsRepeater.TryGetElement(itemCount) as BreadcrumbBarItem;
				UpdateLastElement(newLastItem);
			}

			// If the given collection is empty, then reset the last element visual properties
			if (itemCount == 0)
			{
				ResetLastBreadcrumbBarItem();
			}
		}
		else
		{
			// Or if the ItemsSource was null, also reset the last breadcrumb Item
			ResetLastBreadcrumbBarItem();
		}
	}

	private void UpdateLastElement(BreadcrumbBarItem? newLastBreadcrumbBarItem)
	{
		// If the element is the last element in the array,
		// then we reset the visual properties for the previous
		// last element
		ResetLastBreadcrumbBarItem();

		if (newLastBreadcrumbBarItem is { } newLastItemImpl)
		{
			newLastItemImpl.SetPropertiesForLastItem();
			m_lastBreadcrumbBarItem = newLastBreadcrumbBarItem;
		}
	}

	private void OnElementPreparedEvent(ItemsRepeater itemsRepeater, ItemsRepeaterElementPreparedEventArgs args)
	{
		if (args.Element is BreadcrumbBarItem item)
		{
			if (item is { } itemImpl)
			{
				itemImpl.SetIsEllipsisDropDownItem(false /*isEllipsisDropDownItem*/);

				// Set the parent breadcrumb reference for raising click events
				itemImpl.SetParentBreadcrumb(this);

				// Set the item index to fill the Index parameter in the ClickedEventArgs
				int itemIndex = args.Index;
				itemImpl.SetIndex(itemIndex);

				// The first element is always the ellipsis item
				if (itemIndex == 0)
				{
					itemImpl.SetPropertiesForEllipsisItem();
					m_ellipsisBreadcrumbBarItem = item;
					UpdateEllipsisBreadcrumbBarItemDropDownItemTemplate();

					AutomationProperties.SetName(item, ResourceAccessor.GetLocalizedStringResource(ResourceAccessor.SR_AutomationNameEllipsisBreadcrumbBarItem));
				}
				else
				{
					if (m_breadcrumbItemsSourceView != null)
					{
						int itemCount = m_breadcrumbItemsSourceView.Count;

						if (itemIndex == itemCount)
						{
							UpdateLastElement(item);
						}
						else
						{
							// Any other element just resets the visual properties
							itemImpl.ResetVisualProperties();
						}
					}
				}
			}

#if IS_UNO
			// TODO: Uno specific - remove when #4689 is fixed
			// This ensures the item is properly initialized and the selected item is displayed
			item.Reinitialize();
#endif
		}
	}

	private void OnElementIndexChangedEvent(ItemsRepeater sender, ItemsRepeaterElementIndexChangedEventArgs args)
	{
		if (m_focusedIndex == args.OldIndex)
		{
			var newIndex = args.NewIndex;

			if (args.Element is BreadcrumbBarItem item)
			{
				if (item is { } itemImpl)
				{
					itemImpl.SetIndex(newIndex);
				}
			}

			FocusElementAt(newIndex);
		}
	}

	private void OnElementClearingEvent(ItemsRepeater sender, ItemsRepeaterElementClearingEventArgs args)
	{
		if (args.Element is BreadcrumbBarItem item)
		{
			var itemImpl = item;
			itemImpl.ResetVisualProperties();
		}
	}

	internal void RaiseItemClickedEvent(object content, int index)
	{
		var eventArgs = new BreadcrumbBarItemClickedEventArgs(content, index);

		ItemClicked?.Invoke(this, eventArgs);
	}

	private ObservableCollection<object> GetHiddenElementsList(uint firstShownElement)
	{
		var hiddenElements = new ObservableCollection<object>();

		if (m_breadcrumbItemsSourceView != null)
		{
			for (int i = 0; i < firstShownElement - 1; ++i)
			{
				hiddenElements.Add(m_breadcrumbItemsSourceView.GetAt(i));
			}
		}

		return hiddenElements;
	}

	internal ObservableCollection<object> HiddenElements()
	{
		// The hidden element list is generated in the BreadcrumbLayout during
		// the arrange method, so we retrieve the list from it
		if (m_itemsRepeater is { } itemsRepeater)
		{
			if (m_itemsRepeaterLayout != null)
			{
				if (m_itemsRepeaterLayout.EllipsisIsRendered())
				{
					return GetHiddenElementsList(m_itemsRepeaterLayout.FirstRenderedItemIndexAfterEllipsis());
				}
			}
		}

		// By default just return an empty list
		return new ObservableCollection<object>();
	}

	internal void ReIndexVisibleElementsForAccessibility()
	{
		// Once the arrangement of BreadcrumbBar Items has happened then index all visible items
		if (m_itemsRepeater is { } itemsRepeater && m_itemsRepeaterLayout is not null)
		{
			uint visibleItemsCount = m_itemsRepeaterLayout.GetVisibleItemsCount();
			var isEllipsisRendered = m_itemsRepeaterLayout.EllipsisIsRendered();
			int firstItemToIndex = 1;

			if (isEllipsisRendered)
			{
				firstItemToIndex = (int)m_itemsRepeaterLayout.FirstRenderedItemIndexAfterEllipsis();
			}

			// In order to make the ellipsis inaccessible to accessbility tools when it's hidden,
			// we set the accessibilityView to raw and restore it to content when it becomes visible.
			if (m_ellipsisBreadcrumbBarItem is { } ellipsisItem)
			{
				var accessibilityView = isEllipsisRendered ? AccessibilityView.Content : AccessibilityView.Raw;
				ellipsisItem.SetValue(AutomationProperties.AccessibilityViewProperty, accessibilityView);
			}

			var itemsSourceView = itemsRepeater.ItemsSourceView;

			// For every BreadcrumbBar item we set the index (starting from 1 for the root/highest-level item)
			// accessibilityIndex is the index to be assigned to each item
			// itemToIndex is the real index and it may differ from accessibilityIndex as we must only index the visible items
			for (int accessibilityIndex = 1, itemToIndex = firstItemToIndex; accessibilityIndex <= visibleItemsCount; ++accessibilityIndex, ++itemToIndex)
			{
				if (itemsRepeater.TryGetElement(itemToIndex) is { } element)
				{
					element.SetValue(AutomationProperties.PositionInSetProperty, accessibilityIndex);
					element.SetValue(AutomationProperties.SizeOfSetProperty, visibleItemsCount);
				}
			}
		}
	}

	// When focus comes from outside the BreadcrumbBar control we will put focus on the selected item.
	private void OnGettingFocus(object sender, GettingFocusEventArgs args)
	{
		if (m_itemsRepeater is { } itemsRepeater)
		{
			var inputDevice = args.InputDevice;
			if (inputDevice == FocusInputDeviceKind.Keyboard)
			{
				// If focus is coming from outside the repeater, put focus on the selected item.
				var oldFocusedElement = args.OldFocusedElement;
				if (oldFocusedElement == null || itemsRepeater != VisualTreeHelper.GetParent(oldFocusedElement))
				{
					// Reset the focused index
					if (m_itemsRepeaterLayout != null)
					{
						if (m_itemsRepeaterLayout.EllipsisIsRendered())
						{
							m_focusedIndex = 0;
						}
						else
						{
							m_focusedIndex = 1;
						}

						FocusElementAt(m_focusedIndex);
					}

					if (itemsRepeater.TryGetElement(m_focusedIndex) is { } selectedItem)
					{
						if (args.TrySetNewFocusedElement(selectedItem))
						{
							args.Handled = true;
						}
					}
				}

				// Focus was already in the repeater: in RS3+ Selection follows focus unless control is held down.
				else if ((InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control) &
						CoreVirtualKeyStates.Down) != CoreVirtualKeyStates.Down)
				{
					if (args.NewFocusedElement is UIElement newFocusedElementAsUIE)
					{
						FocusElementAt(itemsRepeater.GetElementIndex(newFocusedElementAsUIE));
						args.Handled = true;
					}
				}
			}
		}
	}

	private void FocusElementAt(int index)
	{
		if (index >= 0)
		{
			m_focusedIndex = index;
		}
	}

	private bool MoveFocus(int indexIncrement)
	{
		if (m_itemsRepeater is { } itemsRepeater)
		{
			var focusedElem = XamlRoot is null ?
				FocusManager.GetFocusedElement() :
				FocusManager.GetFocusedElement(XamlRoot);

			if (focusedElem is UIElement focusedElement)
			{
				var focusedIndex = itemsRepeater.GetElementIndex(focusedElement);

				if (focusedIndex >= 0)
				{
					focusedIndex += indexIncrement;
					var itemCount = itemsRepeater.ItemsSourceView.Count;
					while (focusedIndex >= 0 && focusedIndex < itemCount)
					{
						if (itemsRepeater.TryGetElement(focusedIndex) is { } item)
						{
							if (item is UIElement itemAsUIE)
							{
								if (itemAsUIE.Focus(FocusState.Programmatic))
								{
									FocusElementAt(focusedIndex);
									return true;
								}
							}
						}
						focusedIndex += indexIncrement;
					}
				}
			}
		}
		return false;
	}

	private bool MoveFocusPrevious()
	{
		int movementPrevious = -1;

		// If the focus is in the first visible item, then move to the ellipsis
		if (m_itemsRepeater is { } itemsRepeater)
		{
			var repeaterLayout = itemsRepeater.Layout;
			if (m_itemsRepeaterLayout != null)
			{
				if (m_focusedIndex == 1)
				{
					movementPrevious = 0;
				}
				else if (m_itemsRepeaterLayout.EllipsisIsRendered() &&
					m_focusedIndex == (int)m_itemsRepeaterLayout.FirstRenderedItemIndexAfterEllipsis())
				{
					movementPrevious = -m_focusedIndex;
				}
			}
		}

		return MoveFocus(movementPrevious);
	}

	private bool MoveFocusNext()
	{
		int movementNext = 1;

		// If the focus is in the ellipsis, then move to the first visible item 
		if (m_focusedIndex == 0)
		{
			if (m_itemsRepeater is { } itemsRepeater)
			{
				var repeaterLayout = itemsRepeater.Layout; //TODO MZ: Huh?
				if (m_itemsRepeaterLayout != null)
				{
					movementNext = (int)m_itemsRepeaterLayout.FirstRenderedItemIndexAfterEllipsis();
				}
			}
		}

		return MoveFocus(movementNext);
	}

	// If we haven't handled the key yet and the original source was the first(for up and left)
	// or last(for down and right) element in the repeater we need to handle the key so
	// BreadcrumbBarItem doesn't, which would result in the behavior.
	private bool HandleEdgeCaseFocus(bool first, object source)
	{
		if (m_itemsRepeater is { } itemsRepeater)
		{
			if (source is UIElement sourceAsUIElement)
			{
				int GetIndex()
				{
					if (first)
					{
						return 0;
					}
					if (itemsRepeater.ItemsSourceView is { } itemsSourceView)
					{
						return itemsSourceView.Count - 1;
					}
					return -1;
				}
				var index = GetIndex();

				if (itemsRepeater.GetElementIndex(sourceAsUIElement) == index)
				{
					return true;
				}
			}
		}
		return false;
	}

	private FindNextElementOptions GetFindNextElementOptions()
	{
		var findNextElementOptions = new FindNextElementOptions();
		findNextElementOptions.SearchRoot = this;
		return findNextElementOptions;
	}

	private void OnChildPreviewKeyDown(object sender, KeyRoutedEventArgs args)
	{
		bool flowDirectionIsLTR = (FlowDirection == FlowDirection.LeftToRight);
		bool keyIsLeft = (args.Key == VirtualKey.Left);
		bool keyIsRight = (args.Key == VirtualKey.Right);

		// Moving to the next element
		if ((flowDirectionIsLTR && keyIsRight) || (!flowDirectionIsLTR && keyIsLeft))
		{
			if (MoveFocusNext())
			{
				args.Handled = true;
				return;
			}
			else if ((flowDirectionIsLTR && (args.OriginalKey == VirtualKey.GamepadDPadRight)) ||
						(!flowDirectionIsLTR && (args.OriginalKey == VirtualKey.GamepadDPadLeft)))
			{
				var options = GetFindNextElementOptions();
				if (FocusManager.TryMoveFocus(FocusNavigationDirection.Next, options))
				{
					args.Handled = true;
					return;
				}
			}
			args.Handled = HandleEdgeCaseFocus(false, args.OriginalSource);
		}
		// Moving to previous element
		else if ((flowDirectionIsLTR && keyIsLeft) || (!flowDirectionIsLTR && keyIsRight))
		{
			if (MoveFocusPrevious())
			{
				args.Handled = true;
				return;
			}
			else if ((flowDirectionIsLTR && (args.OriginalKey == VirtualKey.GamepadDPadLeft)) ||
						(!flowDirectionIsLTR && (args.OriginalKey == VirtualKey.GamepadDPadRight)))
			{
				var options = GetFindNextElementOptions();
				if (FocusManager.TryMoveFocus(FocusNavigationDirection.Previous, options))
				{
					args.Handled = true;
					return;
				}
			}
			args.Handled = HandleEdgeCaseFocus(true, args.OriginalSource);
		}
		else if (args.Key == VirtualKey.Down)
		{
			if (args.OriginalKey != VirtualKey.GamepadDPadDown)
			{
				if (FocusManager.TryMoveFocus(FocusNavigationDirection.Right, GetFindNextElementOptions()))
				{
					args.Handled = true;
					return;
				}
			}
			else
			{
				if (FocusManager.TryMoveFocus(FocusNavigationDirection.Right))
				{
					args.Handled = true;
					return;
				}
			}
			args.Handled = HandleEdgeCaseFocus(false, args.OriginalSource);
		}
		else if (args.Key == VirtualKey.Up)
		{
			if (args.OriginalKey != VirtualKey.GamepadDPadUp)
			{
				if (FocusManager.TryMoveFocus(FocusNavigationDirection.Left, GetFindNextElementOptions()))
				{
					args.Handled = true;
					return;
				}
			}
			else
			{
				if (FocusManager.TryMoveFocus(FocusNavigationDirection.Left))
				{
					args.Handled = true;
					return;
				}
			}
			args.Handled = HandleEdgeCaseFocus(true, args.OriginalSource);
		}
	}

	private void OnAccessKeyInvoked(UIElement sender, AccessKeyInvokedEventArgs args)
	{
		// If BreadcrumbBar is an AccessKeyScope then we do not want to handle the access
		// key invoked event because the user has (probably) set up access keys for the
		// BreadcrumbBarItem elements.
		if (!IsAccessKeyScope)
		{
			if (m_focusedIndex > 0)
			{
				if (m_itemsRepeater is { } itemsRepeater)
				{
					if (itemsRepeater.TryGetElement(m_focusedIndex) is { } selectedItem)
					{
						if (selectedItem is Control selectedItemAsControl)
						{
							args.Handled = selectedItemAsControl.Focus(FocusState.Programmatic);
							return;
						}
					}
				}
			}

			// If we don't have a selected index, focus the RadioButton's which under normal
			// circumstances will put focus on the first radio button.
			args.Handled = this.Focus(FocusState.Programmatic);
		}
	}

#if HAS_UNO
	private bool _isUnloaded = false;
	private long _flowPropertyToken = 0;

	private void BreadcrumbBar_Loaded(object sender, RoutedEventArgs e)
	{
		if (_isUnloaded)
		{
			_isUnloaded = false;
			OnApplyTemplate();
		}
	}

	private void BreadcrumbBar_Unloaded(object sender, RoutedEventArgs e)
	{
		RevokeListeners();
		_isUnloaded = true;
	}
#endif
}

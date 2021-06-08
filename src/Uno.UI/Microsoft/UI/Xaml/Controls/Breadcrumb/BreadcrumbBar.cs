// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#nullable enable

using Uno.UI.Helpers.WinUI;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Automation;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class BreadcrumbBar : Control
	{
		public BreadcrumbBar()
		{
			//__RP_Marker_ClassById(RuntimeProfiler.ProfId_BreadcrumbBar);

			DefaultStyleKey = typeof(BreadcrumbBar);

			m_itemsRepeaterElementFactory = new BreadcrumbElementFactory();
			m_itemsRepeaterLayout = new BreadcrumbLayout(this);
			m_itemsIterable = new BreadcrumbIterable();
		}

		private void RevokeListeners()
		{
			m_itemsRepeaterLoadedRevoker.Disposable = null;
			m_itemsRepeaterElementPreparedRevoker.Disposable = null;
			m_itemsRepeaterElementIndexChangedRevoker.Disposable = null;
			m_itemsRepeaterElementClearingRevoker.Disposable = null;
			m_itemsSourceChanged.Disposable = null;
			m_itemsSourceAsObservableVectorChanged.Disposable = null;
		}

		protected override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			RevokeListeners();

			//IControlProtected controlProtected{ this };

			m_itemsRepeater = (ItemsRepeater)GetTemplateChild(s_itemsRepeaterPartName);

			if (var thisAsUIElement7 = this as UIElement7())
    {
				thisAsUIElement7.PreviewKeyDown({ this, &BreadcrumbBar.OnChildPreviewKeyDown });
			}

	else if (var thisAsUIElement = this as UIElement())
    {
				m_breadcrumbKeyDownHandlerRevoker = AddRoutedEventHandler<RoutedEventType.KeyDown>(thisAsUIElement,

			{ this, &BreadcrumbBar.OnChildPreviewKeyDown },
            true /*handledEventsToo*/);
			}

			AccessKeyInvoked({ this, &BreadcrumbBar.OnAccessKeyInvoked });
			GettingFocus({ this, &BreadcrumbBar.OnGettingFocus });

			RegisterPropertyChangedCallback(FrameworkElement.FlowDirectionProperty(), { this, &BreadcrumbBar.OnFlowDirectionChanged });

			if (var itemsRepeater = m_itemsRepeater)
    {
				itemsRepeater.Layout(*m_itemsRepeaterLayout);
				itemsRepeater.ItemsSource(new Vector<object>());
				itemsRepeater.ItemTemplate(*m_itemsRepeaterElementFactory);

				m_itemsRepeaterElementPreparedRevoker = itemsRepeater.ElementPrepared(auto_revoke, { this, &BreadcrumbBar.OnElementPreparedEvent });
				m_itemsRepeaterElementIndexChangedRevoker = itemsRepeater.ElementIndexChanged(auto_revoke, { this, &BreadcrumbBar.OnElementIndexChangedEvent });
				m_itemsRepeaterElementClearingRevoker = itemsRepeater.ElementClearing(auto_revoke, { this, &BreadcrumbBar.OnElementClearingEvent });

				m_itemsRepeaterLoadedRevoker = itemsRepeater.Loaded(auto_revoke, { this, &BreadcrumbBar.OnBreadcrumbBarItemsRepeaterLoaded });
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

		void UpdateItemTemplate()
		{
			object&newItemTemplate = ItemTemplate();
			m_itemsRepeaterElementFactory.UserElementFactory(newItemTemplate);
		}

		void UpdateEllipsisBreadcrumbBarItemDropDownItemTemplate()
		{
			object&newItemTemplate = ItemTemplate();

			// Copy the item template to the ellipsis item too
			if (var ellipsisBreadcrumbBarItem = m_ellipsisBreadcrumbBarItem)
    {
				if (var itemImpl = get_self<BreadcrumbBarItem>(ellipsisBreadcrumbBarItem))
        {
					itemImpl.SetEllipsisDropDownItemDataTemplate(newItemTemplate);
				}
			}
		}

		void UpdateBreadcrumbBarItemsFlowDirection()
		{
			// Only if some ItemsSource has been defined then we change the BreadcrumbBarItems flow direction
			if (ItemsSource())
			{
				if (var itemsRepeater = m_itemsRepeater)
        {
					// Add 1 to account for the leading null
					int elementCount = m_breadcrumbItemsSourceView.Count() + 1;
					for (int i{ }; i < elementCount; ++i)
            {
						var element = itemsRepeater.TryGetElement(i) as BreadcrumbBarItem();
						element.FlowDirection(FlowDirection());
					}
				}
			}
		}

		void UpdateItemsRepeaterItemsSource()
		{
			m_itemsSourceChanged.revoke();
			m_itemsSourceAsObservableVectorChanged.revoke();

			m_breadcrumbItemsSourceView = null;
			if (ItemsSource())
			{
				m_breadcrumbItemsSourceView = ItemsSourceView(ItemsSource());

				if (m_breadcrumbItemsSourceView)
				{
					m_itemsSourceChanged = m_breadcrumbItemsSourceView.CollectionChanged(auto_revoke, { this, &BreadcrumbBar.OnBreadcrumbBarItemsSourceCollectionChanged });
				}
			}
		}

		private void OnBreadcrumbBarItemsSourceCollectionChanged(object sender, object args)
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

		private void UpdateLastElement(BreadcrumbBarItem newLastBreadcrumbBarItem)
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
					uint itemIndex = args.Index;
					itemImpl.SetIndex(itemIndex);

					// The first element is always the ellipsis item
					if (itemIndex == 0)
					{
						itemImpl.SetPropertiesForEllipsisItem();
						m_ellipsisBreadcrumbBarItem = item;
						UpdateEllipsisBreadcrumbBarItemDropDownItemTemplate();

						AutomationProperties.SetName(item, ResourceAccessor.GetLocalizedStringResource(SR_AutomationNameEllipsisBreadcrumbBarItem));
					}
					else
					{
						if (m_breadcrumbItemsSourceView)
						{
							uint itemCount = m_breadcrumbItemsSourceView.Count();

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

		private IVector<object> GetHiddenElementsList(uint firstShownElement)
		{
			var hiddenElements = new Vector<object>();

			if (m_breadcrumbItemsSourceView)
			{
				for (uint i = 0; i < firstShownElement - 1; ++i)
				{
					hiddenElements.Append(m_breadcrumbItemsSourceView.GetAt(i));
				}
			}

			return hiddenElements;
		}

		private IVector<object> HiddenElements()
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
			return new Vector<object>();
		}

		internal void ReIndexVisibleElementsForAccessibility()
		{
			// Once the arrangement of BreadcrumbBar Items has happened then index all visible items
			if (m_itemsRepeater is { } itemsRepeater)
			{
				uint visibleItemsCount = m_itemsRepeaterLayout.GetVisibleItemsCount();
				uint firstItemToIndex = 1;

				if (m_itemsRepeaterLayout.EllipsisIsRendered())
				{
					firstItemToIndex = m_itemsRepeaterLayout.FirstRenderedItemIndexAfterEllipsis();
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
					if (!oldFocusedElement || itemsRepeater != VisualTreeHelper.GetParent(oldFocusedElement))
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
							if (args is GettingFocusEventArgs argsAsIGettingFocusEventArgs2)
							{
								if (args.TrySetNewFocusedElement(selectedItem))
								{
									args.Handled = true;
								}
							}
						}
					}

					// Focus was already in the repeater: in RS3+ Selection follows focus unless control is held down.
					else if (SharedHelpers.IsRS3OrHigher() &&
						(Window.Current.CoreWindow.GetKeyState(VirtualKey.Control) &
							CoreVirtualKeyStates.Down) != CoreVirtualKeyStates.Down)
					{
						if (args.NewFocusedElement is { } newFocusedElementAsUIE)
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
				var focusedElem = FocusManager.GetFocusedElement();

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
								if (item is Control itemAsControl)
								{
									if (itemAsControl.Focus(FocusState.Programmatic))
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
						m_focusedIndex == (int)(m_itemsRepeaterLayout.FirstRenderedItemIndexAfterEllipsis()))
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
						movementNext = m_itemsRepeaterLayout.FirstRenderedItemIndexAfterEllipsis();
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
					var index = [first, itemsRepeater]()

			{
						if (first)
						{
							return 0;
						}
						if (var itemsSourceView = itemsRepeater.ItemsSourceView())
                {
							return itemsSourceView.Count() - 1;
						}
						return -1;
					} ();

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
					if (FocusManager.TryMoveFocus(FocusNavigationDirection.Next))
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
					if (FocusManager.TryMoveFocus(FocusNavigationDirection.Previous))
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
				if (m_focusedIndex)
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

	}
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#nullable enable

using System.Diagnostics;
using Microsoft.UI.Xaml.Automation.Peers;
using Uno.UI.Helpers.WinUI;
using Windows.Devices.Input;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Automation;
using Windows.UI.Xaml.Automation.Peers;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using static Microsoft.UI.Xaml.Controls._Tracing;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class BreadcrumbBarItem : ContentControl
	{
		BreadcrumbBarItem()
		{
			//__RP_Marker_ClassById(RuntimeProfiler.ProfId_BreadcrumbBarItem);

			DefaultStyleKey = typeof(BreadcrumbBarItem);

#if HAS_UNO
			// Uno specific: We use Unloaded instead of destructor
			this.Unloaded += BreadcrumbBarItem_Unloaded;
#endif
		}

		private void BreadcrumbBarItem_Unloaded(object sender, Windows.UI.Xaml.RoutedEventArgs e)
		{
			RevokeListeners();
		}

		void HookListeners(bool forEllipsisDropDownItem)
		{
			if (this is UIElement thisAsUIElement7)
			{
				if (!m_childPreviewKeyDownToken.value)
				{
					m_childPreviewKeyDownToken = thisAsUIElement7.PreviewKeyDown({ this, &BreadcrumbBarItem.OnChildPreviewKeyDown });
				}
			}
			else if (this is UIElement thisAsUIElement)
			{
				if (!m_keyDownRevoker)
				{
					m_keyDownRevoker = AddRoutedEventHandler<RoutedEventType.KeyDown>(thisAsUIElement,


				{ this, &BreadcrumbBarItem.OnChildPreviewKeyDown },
                true /*handledEventsToo*/);
				}
			}

			if (forEllipsisDropDownItem)
			{
				if (m_isEnabledChangedRevoker == null)
				{
					m_isEnabledChangedRevoker = IsEnabledChanged(auto_revoke, { this,  &BreadcrumbBarItem.OnIsEnabledChanged });
				}
			}
			else if (!m_flowDirectionChangedToken.value)
			{
				m_flowDirectionChangedToken.value = RegisterPropertyChangedCallback(FrameworkElement.FlowDirectionProperty(), { this, &BreadcrumbBarItem.OnFlowDirectionChanged });
			}
		}

		void RevokeListeners()
		{
			if (m_flowDirectionChangedToken.value)
			{
				UnregisterPropertyChangedCallback(FrameworkElement.FlowDirectionProperty(), m_flowDirectionChangedToken.value);
				m_flowDirectionChangedToken.value = 0;
			}

			if (m_childPreviewKeyDownToken.value)
			{
				if (var thisAsUIElement7 = this as UIElement7())
        {
					thisAsUIElement7.PreviewKeyDown(m_childPreviewKeyDownToken);
					m_childPreviewKeyDownToken.value = 0;
				}
			}

			m_keyDownRevoker.revoke();
		}

		private void RevokePartsListeners()
		{
			m_buttonLoadedRevoker.Disposable = null;
			m_buttonClickRevoker.Disposable = null;
			m_ellipsisRepeaterElementPreparedRevoker.Disposable = null;
			m_ellipsisRepeaterElementIndexChangedRevoker.Disposable = null;
			m_isPressedButtonRevoker.Disposable = null;
			m_isPointerOverButtonRevoker.Disposable = null;
			m_isEnabledButtonRevoker.Disposable = null;
		}

		void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			if (m_isEllipsisDropDownItem)
			{
				UpdateEllipsisDropDownItemCommonVisualState(false /*useTransitions*/);
			}
			else
			{
				RevokePartsListeners();
				//IControlProtected controlProtected{ this };

				if (m_isEllipsisItem)
				{
					m_ellipsisFlyout = (Flyout)GetTemplateChild(s_itemEllipsisFlyoutPartName);
				}

				m_button = (Button)GetTemplateChild(s_itemButtonPartName);

				if (m_button is { } button)
				{
					m_buttonLoadedRevoker = button.Loaded(auto_revoke, { this, &BreadcrumbBarItem.OnLoadedEvent });

					m_isPressedButtonRevoker = RegisterPropertyChanged(button, ButtonBase.IsPressedProperty, { this, &BreadcrumbBarItem.OnVisualPropertyChanged });
					m_isPointerOverButtonRevoker = RegisterPropertyChanged(button, ButtonBase.IsPointerOverProperty, { this, &BreadcrumbBarItem.OnVisualPropertyChanged });
					m_isEnabledButtonRevoker = RegisterPropertyChanged(button, Control.IsEnabledProperty, { this, &BreadcrumbBarItem.OnVisualPropertyChanged });
				}

				UpdateButtonCommonVisualState(false /*useTransitions*/);
				UpdateInlineItemTypeVisualState(false /*useTransitions*/);
			}

			UpdateItemTypeVisualState();
		}

		void OnLoadedEvent(object&, RoutedEventArgs&)
		{
			MUX_ASSERT(!m_isEllipsisDropDownItem);

			m_buttonLoadedRevoker.revoke();

			if (var button = m_button)
    {
				m_buttonClickRevoker.revoke();
				if (m_isEllipsisItem)
				{
					m_buttonClickRevoker = button.Click(auto_revoke, { this, &BreadcrumbBarItem.OnEllipsisItemClick });
				}
				else
				{
					m_buttonClickRevoker = button.Click(auto_revoke, { this, &BreadcrumbBarItem.OnBreadcrumbBarItemClick });
				}
			}

			if (m_isEllipsisItem)
			{
				SetPropertiesForEllipsisItem();
			}
			else if (m_isLastItem)
			{
				SetPropertiesForLastItem();
			}
			else
			{
				ResetVisualProperties();
			}
		}

		void SetParentBreadcrumb(BreadcrumbBar& parent)
		{
			MUX_ASSERT(!m_isEllipsisDropDownItem);

			m_parentBreadcrumb = parent;
		}

		void SetEllipsisDropDownItemDataTemplate(object newDataTemplate)
		{
			if (newDataTemplate is DataTemplate dataTemplate)
			{
				m_ellipsisDropDownItemDataTemplate = dataTemplate;
			}
			else if (newDataTemplate == null)
			{
				m_ellipsisDropDownItemDataTemplate = null;
			}
		}

		internal void SetIndex(uint index)
		{
			m_index = index;
		}

		void SetIsEllipsisDropDownItem(bool isEllipsisDropDownItem)
		{
			m_isEllipsisDropDownItem = isEllipsisDropDownItem;

			HookListeners(m_isEllipsisDropDownItem);

			UpdateItemTypeVisualState();
		}

		private void RaiseItemClickedEvent(object content, uint index)
		{
			if (m_parentBreadcrumb is { } breadcrumb)
			{
				var breadcrumbImpl = breadcrumb;
				breadcrumbImpl.RaiseItemClickedEvent(content, index);
			}
		}

		private void OnBreadcrumbBarItemClick(object? sender, RoutedEventArgs? args)
		{
			RaiseItemClickedEvent(Content, m_index - 1);
		}

		void OnFlyoutElementPreparedEvent(ItemsRepeater sender, ItemsRepeaterElementPreparedEventArgs args)
		{
			MUX_ASSERT(!m_isEllipsisDropDownItem);

			if (args.Element is BreadcrumbBarItem ellipsisDropDownItem)
			{
				if (ellipsisDropDownItem is { } ellipsisDropDownItemImpl)
				{
					ellipsisDropDownItemImpl.SetIsEllipsisDropDownItem(true /*isEllipsisDropDownItem*/);
				}
			}

			UpdateFlyoutIndex(args.Element, args.Index);
		}

		void OnFlyoutElementIndexChangedEvent(ItemsRepeater itemsRepeater, ItemsRepeaterElementIndexChangedEventArgs args)
		{
			MUX_ASSERT(!m_isEllipsisDropDownItem);

			UpdateFlyoutIndex(args.Element, args.NewIndex);
		}

		void OnFlowDirectionChanged(DependencyObject sender, DependencyProperty property)
		{
			MUX_ASSERT(!m_isEllipsisDropDownItem);

			UpdateInlineItemTypeVisualState(true /*useTransitions*/);
		}

		private void OnChildPreviewKeyDown(object sender, KeyRoutedEventArgs args)
		{
			if (m_isEllipsisDropDownItem)
			{
				if (args.Key == VirtualKey.Enter || args.Key == VirtualKey.Space)
				{
					this.OnClickEvent(sender, null);
					args.Handled = true;
				}
				else if (SharedHelpers.IsRS2OrHigher() && !SharedHelpers.IsRS3OrHigher())
				{
					if (args.Key == VirtualKey.Down)
					{
						FocusManager.TryMoveFocus(FocusNavigationDirection.Next);
						args.Handled = true;
					}
					else if (args.Key == VirtualKey.Up)
					{
						FocusManager.TryMoveFocus(FocusNavigationDirection.Previous);
						args.Handled = true;
					}
				}
			}
			else if (args.Key == VirtualKey.Enter || args.Key == VirtualKey.Space)
			{
				if (m_isEllipsisItem)
				{
					OnEllipsisItemClick(null, null);
				}
				else
				{
					OnBreadcrumbBarItemClick(null, null);
				}
				args.Handled = true;
			}
		}

		void OnIsEnabledChanged(
			 object&,
			 DependencyPropertyChangedEventArgs&)
		{
			MUX_ASSERT(m_isEllipsisDropDownItem);

			UpdateEllipsisDropDownItemCommonVisualState(true /*useTransitions*/);
		}

		void UpdateFlyoutIndex(UIElement element, uint index)
		{
			MUX_ASSERT(!m_isEllipsisDropDownItem);

			if (m_ellipsisItemsRepeater is { } ellipsisItemsRepeater)
			{
				if (ellipsisItemsRepeater.ItemsSourceView is { } itemSourceView)
				{
					uint itemCount = itemSourceView.Count;

					if (element is BreadcrumbBarItem ellipsisDropDownItemImpl)
					{
						ellipsisDropDownItemImpl.SetEllipsisItem(this);
						ellipsisDropDownItemImpl.SetIndex(itemCount - index);
					}

					element.SetValue(AutomationProperties.PositionInSetProperty, index + 1);
					element.SetValue(AutomationProperties.SizeOfSetProperty, itemCount);
				}
			}
		}

		object CloneEllipsisItemSource(Collections.IVector<object> ellipsisItemsSource)
		{
			MUX_ASSERT(!m_isEllipsisDropDownItem);

			// A copy of the hidden elements array in BreadcrumbLayout is created
			// to avoid getting a Layout cycle exception
			var newItemsSource = new Vector<object>();

			// The new list contains all the elements in reverse order
			int itemsSourceSize = ellipsisItemsSource.Size();

			// The itemsSourceSize should always be at least 1 as it must always contain the ellipsis item
			MUX_ASSERT(itemsSourceSize > 0);

			for (int i = itemsSourceSize - 1; i >= 0; --i)
			{
				var item = ellipsisItemsSource.GetAt(i);
				newItemsSource.Append(item);
			}

			return newItemsSource;
		}

		void OpenFlyout()
		{
			MUX_ASSERT(!m_isEllipsisDropDownItem);

			if (m_ellipsisFlyout is { } flyout)
			{
				if (SharedHelpers.IsFlyoutShowOptionsAvailable())
				{
					FlyoutShowOptions options = new FlyoutShowOptions();
					flyout.ShowAt(this, options);
				}
				else
				{
					flyout.ShowAt(this);
				}
			}
		}

		void CloseFlyout()
		{
			MUX_ASSERT(!m_isEllipsisDropDownItem);

			if (m_ellipsisFlyout is { } flyout)
			{
				flyout.Hide();
			}
		}

		void OnVisualPropertyChanged(DependencyObject sender, DependencyProperty property)
		{
			MUX_ASSERT(!m_isEllipsisDropDownItem);

			UpdateButtonCommonVisualState(true /*useTransitions*/);
		}

		void UpdateItemTypeVisualState()
		{
			VisualStateManager.GoToState(this, m_isEllipsisDropDownItem ? s_ellipsisDropDownStateName : s_inlineStateName, false /*useTransitions*/);
		}

		void UpdateEllipsisDropDownItemCommonVisualState(bool useTransitions)
		{
			MUX_ASSERT(m_isEllipsisDropDownItem);

			string commonVisualStateName;

			if (!IsEnabled)
			{
				commonVisualStateName = s_disabledStateName;
			}
			else if (m_isPressed)
			{
				commonVisualStateName = s_pressedStateName;
			}
			else if (m_isPointerOver)
			{
				commonVisualStateName = s_pointerOverStateName;
			}
			else
			{
				commonVisualStateName = s_normalStateName;
			}

			VisualStateManager.GoToState(this, commonVisualStateName, useTransitions);
		}

		void UpdateInlineItemTypeVisualState(bool useTransitions)
		{
			MUX_ASSERT(!m_isEllipsisDropDownItem);

			bool isLeftToRight = (FlowDirection == FlowDirection.LeftToRight);
			string visualStateName;

			if (m_isEllipsisItem)
			{
				if (isLeftToRight)
				{
					visualStateName = s_ellipsisStateName;
				}
				else
				{
					visualStateName = s_ellipsisRTLStateName;
				}
			}
			else if (m_isLastItem)
			{
				visualStateName = s_lastItemStateName;
			}
			else if (isLeftToRight)
			{
				visualStateName = s_defaultStateName;
			}
			else
			{
				visualStateName = s_defaultRTLStateName;
			}

			VisualStateManager.GoToState(this, visualStateName, useTransitions);
		}

		void UpdateButtonCommonVisualState(bool useTransitions)
		{
			MUX_ASSERT(!m_isEllipsisDropDownItem);

			if (m_button is { } button)
			{
				string commonVisualStateName = "";

				// If is last item: place Current as prefix for visual state
				if (m_isLastItem)
				{
					commonVisualStateName = s_currentStateName;
				}

				if (!button.IsEnabled)
				{
					commonVisualStateName = commonVisualStateName + s_disabledStateName;
				}
				else if (button.IsPressed)
				{
					commonVisualStateName = commonVisualStateName + s_pressedStateName;
				}
				else if (button.IsPointerOver)
				{
					commonVisualStateName = commonVisualStateName + s_pointerOverStateName;
				}
				else
				{
					commonVisualStateName = commonVisualStateName + s_normalStateName;
				}

				VisualStateManager.GoToState(button, commonVisualStateName, useTransitions);
			}
		}

		private void OnEllipsisItemClick(object? sender, RoutedEventArgs? args)
		{
			MUX_ASSERT(!m_isEllipsisDropDownItem);

			if (m_parentBreadcrumb is { } breadcrumb)
			{
				if (breadcrumb is BreadcrumbBar breadcrumbImpl)
				{
					var hiddenElements = CloneEllipsisItemSource(breadcrumbImpl.HiddenElements());

					if (m_ellipsisDropDownItemDataTemplate is { } dataTemplate)
					{
						m_ellipsisElementFactory.UserElementFactory(dataTemplate);
					}

					if (m_ellipsisItemsRepeater is { } flyoutRepeater)
					{
						flyoutRepeater.ItemsSource = hiddenElements;
					}

					OpenFlyout();
				}
			}
		}

		void SetPropertiesForLastItem()
		{
			MUX_ASSERT(!m_isEllipsisDropDownItem);

			m_isEllipsisItem = false;
			m_isLastItem = true;

			UpdateButtonCommonVisualState(false /*useTransitions*/);
			UpdateInlineItemTypeVisualState(false /*useTransitions*/);
		}

		internal void ResetVisualProperties()
		{
			if (m_isEllipsisDropDownItem)
			{
				UpdateEllipsisDropDownItemCommonVisualState(false /*useTransitions*/);
			}
			else
			{
				m_isEllipsisItem = false;
				m_isLastItem = false;

				if (m_button is { } button)
				{
					button.Flyout = null;
				}
				m_ellipsisFlyout = null;
				m_ellipsisItemsRepeater = null;
				m_ellipsisElementFactory = null;

				UpdateButtonCommonVisualState(false /*useTransitions*/);
				UpdateInlineItemTypeVisualState(false /*useTransitions*/);
			}
		}

		private void InstantiateFlyout()
		{
			MUX_ASSERT(!m_isEllipsisDropDownItem);

			// Only if the element has been created visually, instantiate the flyout
			if (m_button is { } button)
			{
				if (m_ellipsisFlyout is { } ellipsisFlyout)
				{
					// Create ItemsRepeater and set the DataTemplate 
					var ellipsisItemsRepeater = new ItemsRepeater();
					ellipsisItemsRepeater.Name(s_ellipsisItemsRepeaterPartName);
					AutomationProperties.SetName(ellipsisItemsRepeater, s_ellipsisItemsRepeaterAutomationName);
					ellipsisItemsRepeater.HorizontalAlignment = HorizontalAlignment.Stretch;

					m_ellipsisElementFactory = new BreadcrumbElementFactory();
					ellipsisItemsRepeater.ItemTemplate = m_ellipsisElementFactory;

					if (m_ellipsisDropDownItemDataTemplate is { } dataTemplate)
					{
						m_ellipsisElementFactory.UserElementFactory(dataTemplate);
					}

					m_ellipsisRepeaterElementPreparedRevoker = ellipsisItemsRepeater.ElementPrepared(auto_revoke, { this, &BreadcrumbBarItem.OnFlyoutElementPreparedEvent });
					m_ellipsisRepeaterElementIndexChangedRevoker = ellipsisItemsRepeater.ElementIndexChanged(auto_revoke, { this, &BreadcrumbBarItem.OnFlyoutElementIndexChangedEvent });

					m_ellipsisItemsRepeater = ellipsisItemsRepeater;

					// Set the repeater as the content.
					AutomationProperties.SetName(ellipsisFlyout, s_ellipsisFlyoutAutomationName);
					ellipsisFlyout.Content = ellipsisItemsRepeater);
					ellipsisFlyout.Placement = FlyoutPlacementMode.Bottom);

					m_ellipsisFlyout = ellipsisFlyout;
				}
			}
		}

		void SetPropertiesForEllipsisItem()
		{
			MUX_ASSERT(!m_isEllipsisDropDownItem);

			m_isEllipsisItem = true;
			m_isLastItem = false;

			InstantiateFlyout();

			UpdateButtonCommonVisualState(false /*useTransitions*/);
			UpdateInlineItemTypeVisualState(false /*useTransitions*/);
		}

		private void SetEllipsisItem(BreadcrumbBarItem ellipsisItem)
		{
			MUX_ASSERT(m_isEllipsisDropDownItem);

			m_ellipsisItem = ellipsisItem;
		}

		protected override AutomationPeer OnCreateAutomationPeer() =>
			new BreadcrumbBarItemAutomationPeer(this);

		protected override void OnPointerEntered(PointerRoutedEventArgs args)
		{
			base.OnPointerEntered(args);

			if (m_isEllipsisDropDownItem)
			{
				ProcessPointerOver(args);
			}
		}

		protected override void OnPointerMoved(PointerRoutedEventArgs args)
		{
			base.OnPointerMoved(args);

			if (m_isEllipsisDropDownItem)
			{
				ProcessPointerOver(args);
			}
		}

		protected override void OnPointerExited(PointerRoutedEventArgs args)
		{
			base.OnPointerExited(args);

			if (m_isEllipsisDropDownItem)
			{
				ProcessPointerCanceled(args);
			}
		}

		protected override void OnPointerPressed(PointerRoutedEventArgs args)
		{
			base.OnPointerPressed(args);

			if (m_isEllipsisDropDownItem)
			{
				if (IgnorePointerId(args))
				{
					return;
				}

				MUX_ASSERT(!m_isPressed);

				if (args.Pointer.PointerDeviceType == PointerDeviceType.Mouse)
				{
					var pointerProperties = args.GetCurrentPoint(this).Properties;
					m_isPressed = pointerProperties.IsLeftButtonPressed;
				}
				else
				{
					m_isPressed = true;
				}

				if (m_isPressed)
				{
					UpdateEllipsisDropDownItemCommonVisualState(true /*useTransitions*/);
				}
			}
		}

		protected override void OnPointerReleased(PointerRoutedEventArgs args)
		{
			base.OnPointerReleased(args);

			if (m_isEllipsisDropDownItem)
			{
				if (IgnorePointerId(args))
				{
					return;
				}

				if (m_isPressed)
				{
					m_isPressed = false;
					UpdateEllipsisDropDownItemCommonVisualState(true /*useTransitions*/);
					OnClickEvent(null, null);
				}
			}
		}

		protected override void OnPointerCanceled(PointerRoutedEventArgs args)
		{
			base.OnPointerCanceled(args);

			if (m_isEllipsisDropDownItem)
			{
				ProcessPointerCanceled(args);
			}
		}

		protected override void OnPointerCaptureLost(PointerRoutedEventArgs args)
		{
			base.OnPointerCaptureLost(args);

			if (m_isEllipsisDropDownItem)
			{
				ProcessPointerCanceled(args);
			}
		}

		private void ProcessPointerOver(PointerRoutedEventArgs args)
		{
			MUX_ASSERT(m_isEllipsisDropDownItem);

			if (IgnorePointerId(args))
			{
				return;
			}

			if (!m_isPointerOver)
			{
				m_isPointerOver = true;
				UpdateEllipsisDropDownItemCommonVisualState(true /*useTransitions*/);
			}
		}

		private void ProcessPointerCanceled(PointerRoutedEventArgs args)
		{
			MUX_ASSERT(m_isEllipsisDropDownItem);

			if (IgnorePointerId(args))
			{
				return;
			}

			m_isPressed = false;
			m_isPointerOver = false;
			ResetTrackedPointerId();
			UpdateEllipsisDropDownItemCommonVisualState(true /*useTransitions*/);
		}

		void ResetTrackedPointerId()
		{
			MUX_ASSERT(m_isEllipsisDropDownItem);

			m_trackedPointerId = 0;
		}

		// Returns False when the provided pointer Id matches the currently tracked Id.
		// When there is no currently tracked Id, sets the tracked Id to the provided Id and returns False.
		// Returns True when the provided pointer Id does not match the currently tracked Id.
		private bool IgnorePointerId(PointerRoutedEventArgs args)
		{
			MUX_ASSERT(m_isEllipsisDropDownItem);

			uint pointerId = args.Pointer.PointerId;

			if (m_trackedPointerId == 0)
			{
				m_trackedPointerId = pointerId;
			}
			else if (m_trackedPointerId != pointerId)
			{
				return true;
			}
			return false;
		}

		internal void OnClickEvent(object? sender, RoutedEventArgs? args)
		{
			if (m_isEllipsisDropDownItem)
			{
				if (m_ellipsisItem is { } ellipsisItem)
				{
					// Once an element has been clicked, close the flyout
					if (ellipsisItem is { } ellipsisItemImpl)
					{
						ellipsisItemImpl.CloseFlyout();
						ellipsisItemImpl.RaiseItemClickedEvent(Content, m_index - 1);
					}
				}
			}
			else if (m_isEllipsisItem)
			{
				OnEllipsisItemClick(null, null);
			}
			else
			{
				OnBreadcrumbBarItemClick(null, null);
			}
		}
	}
}

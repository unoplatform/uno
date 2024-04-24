// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// MUX Reference ItemContainer.cpp, tag winui3/release/1.5.0

using System;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Markup;
using Uno;
using Uno.UI.Helpers.WinUI;
using Windows.Foundation;
using Windows.System;

namespace Microsoft.UI.Xaml.Controls;

[ContentProperty(Name = nameof(Child))]
partial class ItemContainer : Control
{
	// Change to 'true' to turn on debugging outputs in Output window
	//bool ItemContainerTrace::s_IsDebugOutputEnabled{ false };
	//bool ItemContainerTrace::s_IsVerboseDebugOutputEnabled{ false };

	// Keeps track of the one ItemContainer with the mouse pointer over, if any.
	// The OnPointerExited method does not get invoked when the ItemContainer is scrolled away from the mouse pointer.
	// This static instance allows to clear the mouse over state when any other ItemContainer gets the mouse over state.
	[ThreadStatic]
	static WeakReference<ItemContainer> s_mousePointerOverInstance;

	public ItemContainer()
	{
		//ITEMCONTAINER_TRACE_INFO(null, TRACE_MSG_METH, METH_NAME, this);

		//__RP_Marker_ClassById(RuntimeProfiler::ProfId_ItemContainer);

		//EnsureProperties();
		SetDefaultStyleKey(this);
	}

	~ItemContainer()
	{
		//ITEMCONTAINER_TRACE_INFO(null, TRACE_MSG_METH, METH_NAME, this);

		if (s_mousePointerOverInstance?.TryGetTarget(out var mousePointerOverInstance) == true)
		{
			if (mousePointerOverInstance == this)
			{
				s_mousePointerOverInstance = null;
			}
		}
	}

	protected override void OnApplyTemplate()
	{
		//ITEMCONTAINER_TRACE_VERBOSE(*this, TRACE_MSG_METH, METH_NAME, this);

		// Unload events from previous template when ItemContainer gets a new template.
		if (m_selectionCheckbox is { } selectionCheckbox)
		{
			m_checked_revoker.Disposable = null;
			m_unchecked_revoker.Disposable = null;
		}

		m_rootPanel = GetTemplateChild<Panel>(s_containerRootName);

		// If the Child property is already set, add it to the tree. After this point, the
		// property changed event for Child property will add it to the tree.
		if (Child is { } child)
		{
			if (m_rootPanel is { } rootPanel)
			{
				rootPanel.Children.Insert(0, child);
			}
		}

		m_pointerInfo = new PointerInfo<ItemContainer>();
		// Uno docs: We override OnIsEnabledChanged instead of subscribing to IsEnabledChanged event.
		//m_isEnabledChangedRevoker.revoke();
		//m_isEnabledChangedRevoker = IsEnabledChanged(auto_revoke, { this,  &OnIsEnabledChanged });

		base.OnApplyTemplate();

		UpdateVisualState(true);
		UpdateMultiSelectState(true);
	}

	protected override AutomationPeer OnCreateAutomationPeer()
	{
		return new ItemContainerAutomationPeer(this);
	}

	// Uno docs: We use OnIsEnabledChanged override instead of subscribing to IsEnabledChanged event.
	private protected override void OnIsEnabledChanged(IsEnabledChangedEventArgs e)
	{
		//ITEMCONTAINER_TRACE_INFO(*this, TRACE_MSG_METH_INT, METH_NAME, this, IsEnabled());

		if (!IsEnabled)
		{
			ProcessPointerCanceled(null);
		}
		else
		{
			UpdateVisualState(true);
		}
	}

	void OnPropertyChanged(DependencyPropertyChangedEventArgs args)
	{
		var dependencyProperty = args.Property;

#if DEBUG
		if (dependencyProperty == IsSelectedProperty)
		{
			//bool oldValue = (bool)args.OldValue;
			//bool newValue = (bool)args.NewValue;

			//ITEMCONTAINER_TRACE_VERBOSE(null, TRACE_MSG_METH_STR_INT_INT, METH_NAME, this, L"IsSelected", oldValue, newValue);
		}
		else if (dependencyProperty == MultiSelectModeProperty)
		{
			//ItemContainerMultiSelectMode oldValue = (ItemContainerMultiSelectMode)args.OldValue;
			//ItemContainerMultiSelectMode newValue = (ItemContainerMultiSelectMode)args.NewValue;

			//ITEMCONTAINER_TRACE_VERBOSE(null, TRACE_MSG_METH_STR_STR, METH_NAME, this, L"MultiSelectMode", TypeLogging::ItemContainerMultiSelectModeToString(newValue).c_str());
		}
		else
		{
			//ITEMCONTAINER_TRACE_VERBOSE(null, L"%s[%p](property: %s)\n", METH_NAME, this, DependencyPropertyToString(dependencyProperty).c_str());
		}
#endif

		if (dependencyProperty == IsSelectedProperty)
		{
#if MUX_PRERELEASE
			bool isMultipleOrExtended = (MultiSelectMode() & (ItemContainerMultiSelectMode.Multiple | ItemContainerMultiSelectMode.Extended)) != 0;
#else
			bool isMultipleOrExtended = (MultiSelectModeInternal() & (ItemContainerMultiSelectMode.Multiple | ItemContainerMultiSelectMode.Extended)) != 0;
#endif

			AutomationEvents eventToRaise =
				IsSelected ?
				(isMultipleOrExtended ? AutomationEvents.SelectionItemPatternOnElementAddedToSelection : AutomationEvents.SelectionItemPatternOnElementSelected) :
				(isMultipleOrExtended ? AutomationEvents.SelectionItemPatternOnElementRemovedFromSelection : AutomationEvents.PropertyChanged);

			if (eventToRaise != AutomationEvents.PropertyChanged && AutomationPeer.ListenerExists(eventToRaise))
			{
				if (FrameworkElementAutomationPeer.CreatePeerForElement(this) is { } peer)
				{
					peer.RaiseAutomationEvent(eventToRaise);
				}
			}
		}
		else if (dependencyProperty == ChildProperty)
		{
			if (m_rootPanel is { } rootPanel)
			{
				if (args.OldValue != null && rootPanel.Children.IndexOf(args.OldValue as UIElement) is int oldChildIndex)
				{
					rootPanel.Children.RemoveAt(oldChildIndex);
				}

				rootPanel.Children.Insert(0, args.NewValue as UIElement);
			}
		}

		if (m_pointerInfo != null)
		{
			if (dependencyProperty == IsSelectedProperty ||
				dependencyProperty == MultiSelectModeProperty)
			{
				UpdateVisualState(true);
				UpdateMultiSelectState(true);
			}
		}
	}

	void GoToState(string stateName, bool useTransitions)
	{
		//ITEMCONTAINER_TRACE_VERBOSE(*this, TRACE_MSG_METH_STR_INT, METH_NAME, this, stateName.data(), useTransitions);

		VisualStateManager.GoToState(this, stateName, useTransitions);
	}

	internal override void UpdateVisualState(bool useTransitions)
	{
		//ITEMCONTAINER_TRACE_VERBOSE(*this, TRACE_MSG_METH_INT, METH_NAME, this, useTransitions);

		// DisabledStates
		if (!IsEnabled)
		{
			GoToState(s_disabledStateName, useTransitions);
			GoToState(IsSelected ? s_selectedNormalStateName : s_unselectedNormalStateName, useTransitions);
		}
		else
		{
			GoToState(s_enabledStateName, useTransitions);

			// CombinedStates
			if (m_pointerInfo == null)
			{
				return;
			}

			if (m_pointerInfo.IsPressed() && IsSelected)
			{
				GoToState(s_selectedPressedStateName, useTransitions);
			}
			else if (m_pointerInfo.IsPressed())
			{
				GoToState(s_unselectedPressedStateName, useTransitions);
			}
			else if (m_pointerInfo.IsPointerOver() && IsSelected)
			{
				GoToState(s_selectedPointerOverStateName, useTransitions);
			}
			else if (m_pointerInfo.IsPointerOver())
			{
				GoToState(s_unselectedPointerOverStateName, useTransitions);
			}
			else if (IsSelected)
			{
				GoToState(s_selectedNormalStateName, useTransitions);
			}
			else
			{
				GoToState(s_unselectedNormalStateName, useTransitions);
			}
		}
	}

	void UpdateMultiSelectState(bool useTransitions)
	{
		//ITEMCONTAINER_TRACE_VERBOSE(*this, TRACE_MSG_METH_INT, METH_NAME, this, useTransitions);

#if MUX_PRERELEASE
		ItemContainerMultiSelectMode multiSelectMode = MultiSelectMode();
#else
		ItemContainerMultiSelectMode multiSelectMode = MultiSelectModeInternal();
#endif

		// MultiSelectStates
		if ((multiSelectMode & ItemContainerMultiSelectMode.Multiple) != 0)
		{
			GoToState(s_multipleStateName, useTransitions);

			if (m_selectionCheckbox is null)
			{
				LoadSelectionCheckbox();
			}

			UpdateCheckboxState();
		}
		else
		{
			GoToState(s_singleStateName, useTransitions);
		}
	}

	void ProcessPointerOver(PointerRoutedEventArgs args, bool isPointerOver)
	{
		if (m_pointerInfo is not null)
		{
			bool oldIsPointerOver = m_pointerInfo.IsPointerOver();
			var pointerDeviceType = args.Pointer.PointerDeviceType;

			if (pointerDeviceType == PointerDeviceType.Touch)
			{
				if (isPointerOver)
				{
					m_pointerInfo.SetIsTouchPointerOver();
				}
				else
				{
					// Once a touch pointer leaves the ItemContainer it is no longer tracked because this
					// ItemContainer may never receive a PointerReleased, PointerCanceled or PointerCaptureLost
					// for that PointerId.
					m_pointerInfo.ResetAll();
				}
			}
			else if (pointerDeviceType == PointerDeviceType.Pen)
			{
				if (isPointerOver)
				{
					m_pointerInfo.SetIsPenPointerOver();
				}
				else
				{
					// Once a pen pointer leaves the ItemContainer it is no longer tracked because this
					// ItemContainer may never receive a PointerReleased, PointerCanceled or PointerCaptureLost
					// for that PointerId.
					m_pointerInfo.ResetAll();
				}
			}
			else
			{
				if (isPointerOver)
				{
					m_pointerInfo.SetIsMousePointerOver();
				}
				else
				{
					m_pointerInfo.ResetIsMousePointerOver();
				}
				UpdateMousePointerOverInstance(isPointerOver);
			}

			if (m_pointerInfo.IsPointerOver() != oldIsPointerOver)
			{
				UpdateVisualState(true);
			}
		}
	}

	void ProcessPointerCanceled(PointerRoutedEventArgs args)
	{
		//ITEMCONTAINER_TRACE_INFO(*this, TRACE_MSG_METH_INT, METH_NAME, this, args == null ? -1 : args.Pointer().PointerId());

		if (m_pointerInfo is not null)
		{
			if (args == null)
			{
				m_pointerInfo.ResetAll();
			}
			else
			{
				var pointer = args.Pointer;

				if (m_pointerInfo.IsPointerIdTracked(pointer.PointerId))
				{
					m_pointerInfo.ResetTrackedPointerId();
				}

				var pointerDeviceType = pointer.PointerDeviceType;

				switch (pointerDeviceType)
				{
					case PointerDeviceType.Touch:
						m_pointerInfo.ResetPointerPressed();
						m_pointerInfo.ResetIsTouchPointerOver();
						break;
					case PointerDeviceType.Pen:
						m_pointerInfo.ResetPointerPressed();
						m_pointerInfo.ResetIsPenPointerOver();
						break;
					case PointerDeviceType.Mouse:
						m_pointerInfo.ResetIsMouseButtonPressed(true /*isForLeftMouseButton*/);
						m_pointerInfo.ResetIsMouseButtonPressed(false /*isForLeftMouseButton*/);
						m_pointerInfo.ResetIsMousePointerOver();
						UpdateMousePointerOverInstance(false /*isPointerOver*/);
						break;
				}
			}
		}

		UpdateVisualState(true);
	}

	protected override void OnPointerEntered(PointerRoutedEventArgs args)
	{
		//ITEMCONTAINER_TRACE_VERBOSE(*this, TRACE_MSG_METH_INT, METH_NAME, this, args.Pointer().PointerId());

		base.OnPointerEntered(args);

		ProcessPointerOver(args, true /*isPointerOver*/);
	}

	protected override void OnPointerMoved(PointerRoutedEventArgs args)
	{
		base.OnPointerMoved(args);

		ProcessPointerOver(args, true /*isPointerOver*/);
	}

	protected override void OnPointerExited(PointerRoutedEventArgs args)
	{
		//ITEMCONTAINER_TRACE_VERBOSE(*this, TRACE_MSG_METH_INT, METH_NAME, this, args.Pointer().PointerId());

		base.OnPointerExited(args);

		ProcessPointerOver(args, false /*isPointerOver*/);
	}

	protected override void OnPointerPressed(PointerRoutedEventArgs args)
	{
		//ITEMCONTAINER_TRACE_VERBOSE(*this, TRACE_MSG_METH_INT, METH_NAME, this, args.Pointer().PointerId());

		base.OnPointerPressed(args);

		if (m_pointerInfo == null || args.Handled)
		{
			return;
		}

		var pointer = args.Pointer;
		var pointerDeviceType = pointer.PointerDeviceType;
		PointerUpdateKind pointerUpdateKind = PointerUpdateKind.Other;

		if (pointerDeviceType == PointerDeviceType.Mouse)
		{
			var pointerProperties = args.GetCurrentPoint(this).Properties;

			pointerUpdateKind = pointerProperties.PointerUpdateKind;

			if (pointerUpdateKind != PointerUpdateKind.LeftButtonPressed &&
				pointerUpdateKind != PointerUpdateKind.RightButtonPressed)
			{
				return;
			}
		}

		//ITEMCONTAINER_TRACE_VERBOSE(*this, TRACE_MSG_METH_STR_STR, METH_NAME, this, TypeLogging::PointerDeviceTypeToString(pointerDeviceType).c_str(), TypeLogging::PointerUpdateKindToString(pointerUpdateKind).c_str());

		var pointerId = pointer.PointerId;

		if (!m_pointerInfo.IsTrackingPointer())
		{
			m_pointerInfo.TrackPointerId(pointerId);
		}
		else if (!m_pointerInfo.IsPointerIdTracked(pointerId))
		{
			return;
		}

		bool canRaiseItemInvoked = CanRaiseItemInvoked();

		switch (pointerDeviceType)
		{
			case PointerDeviceType.Touch:
				m_pointerInfo.SetPointerPressed();
				m_pointerInfo.SetIsTouchPointerOver();
				break;
			case PointerDeviceType.Pen:
				m_pointerInfo.SetPointerPressed();
				m_pointerInfo.SetIsPenPointerOver();
				break;
			case PointerDeviceType.Mouse:
				m_pointerInfo.SetIsMouseButtonPressed(pointerUpdateKind == PointerUpdateKind.LeftButtonPressed /*isForLeftMouseButton*/);
				canRaiseItemInvoked &= pointerUpdateKind == PointerUpdateKind.LeftButtonPressed;
				m_pointerInfo.SetIsMousePointerOver();
				UpdateMousePointerOverInstance(true /*isPointerOver*/);
				break;
		}

		if (canRaiseItemInvoked)
		{
			args.Handled = RaiseItemInvoked(ItemContainerInteractionTrigger.PointerPressed, args.OriginalSource);
		}

		UpdateVisualState(true);
	}

	protected override void OnPointerReleased(PointerRoutedEventArgs args)
	{
		//ITEMCONTAINER_TRACE_VERBOSE(*this, TRACE_MSG_METH_INT, METH_NAME, this, args.Pointer().PointerId());

		base.OnPointerReleased(args);

		if (m_pointerInfo == null ||
			!m_pointerInfo.IsPointerIdTracked(args.Pointer.PointerId) ||
			!m_pointerInfo.IsPressed())
		{
			return;
		}

		m_pointerInfo.ResetTrackedPointerId();

		bool canRaiseItemInvoked = CanRaiseItemInvoked();
		var pointerDeviceType = args.Pointer.PointerDeviceType;

		if (pointerDeviceType == PointerDeviceType.Mouse)
		{
			var pointerProperties = args.GetCurrentPoint(this).Properties;
			var pointerUpdateKind = pointerProperties.PointerUpdateKind;

			//ITEMCONTAINER_TRACE_VERBOSE(*this, TRACE_MSG_METH_STR_STR, METH_NAME, this, TypeLogging::PointerDeviceTypeToString(pointerDeviceType).c_str(), TypeLogging::PointerUpdateKindToString(pointerUpdateKind).c_str());

			if (pointerUpdateKind == PointerUpdateKind.LeftButtonReleased ||
				pointerUpdateKind == PointerUpdateKind.RightButtonReleased)
			{
				m_pointerInfo.ResetIsMouseButtonPressed(pointerUpdateKind == PointerUpdateKind.LeftButtonReleased /*isForLeftMouseButton*/);
			}
			canRaiseItemInvoked &= pointerUpdateKind == PointerUpdateKind.LeftButtonReleased;
		}
		else
		{
			//ITEMCONTAINER_TRACE_VERBOSE(*this, TRACE_MSG_METH_STR, METH_NAME, this, TypeLogging::PointerDeviceTypeToString(pointerDeviceType).c_str());

			m_pointerInfo.ResetPointerPressed();
		}

		if (canRaiseItemInvoked &&
			!args.Handled &&
			!m_pointerInfo.IsPressed())
		{
			args.Handled = RaiseItemInvoked(ItemContainerInteractionTrigger.PointerReleased, args.OriginalSource);
		}

		UpdateVisualState(true);
	}

	protected override void OnPointerCanceled(PointerRoutedEventArgs args)
	{
		//ITEMCONTAINER_TRACE_VERBOSE(*this, TRACE_MSG_METH_INT, METH_NAME, this, args.Pointer().PointerId());

		base.OnPointerCanceled(args);

		ProcessPointerCanceled(args);
	}

	protected override void OnPointerCaptureLost(PointerRoutedEventArgs args)
	{
		//ITEMCONTAINER_TRACE_VERBOSE(*this, TRACE_MSG_METH_INT, METH_NAME, this, args.Pointer().PointerId());

		base.OnPointerCaptureLost(args);

		ProcessPointerCanceled(args);
	}

	protected override void OnTapped(TappedRoutedEventArgs args)
	{
		//ITEMCONTAINER_TRACE_VERBOSE(*this, TRACE_MSG_METH, METH_NAME, this);

		base.OnTapped(args);

		if (CanRaiseItemInvoked() && !args.Handled)
		{
			args.Handled = RaiseItemInvoked(ItemContainerInteractionTrigger.Tap, args.OriginalSource);
		}
	}

	protected override void OnDoubleTapped(DoubleTappedRoutedEventArgs args)
	{
		//ITEMCONTAINER_TRACE_VERBOSE(*this, TRACE_MSG_METH, METH_NAME, this);

		base.OnDoubleTapped(args);

		if (CanRaiseItemInvoked() && !args.Handled)
		{
			args.Handled = RaiseItemInvoked(ItemContainerInteractionTrigger.DoubleTap, args.OriginalSource);
		}
	}

	protected override void OnKeyDown(KeyRoutedEventArgs args)
	{
		//ITEMCONTAINER_TRACE_VERBOSE(*this, TRACE_MSG_METH_STR, METH_NAME, this, TypeLogging::KeyRoutedEventArgsToString(args).c_str());

		base.OnKeyDown(args);

		if (!args.Handled && CanRaiseItemInvoked())
		{
			if (args.Key == VirtualKey.Enter)
			{
				args.Handled = RaiseItemInvoked(ItemContainerInteractionTrigger.EnterKey, args.OriginalSource);
			}
			else if (args.Key == VirtualKey.Space)
			{
				args.Handled = RaiseItemInvoked(ItemContainerInteractionTrigger.SpaceKey, args.OriginalSource);
			}
		}
	}

	bool CanRaiseItemInvoked()
	{
#if MUX_PRERELEASE
		return static_cast<int>(CanUserInvoke() & ItemContainerUserInvokeMode::UserCanInvoke) ||
			static_cast<int>(CanUserSelect() & (ItemContainerUserSelectMode::Auto | ItemContainerUserSelectMode::UserCanSelect));
#else
		return (CanUserInvokeInternal() & ItemContainerUserInvokeMode.UserCanInvoke) != 0 ||
			(CanUserSelectInternal() & (ItemContainerUserSelectMode.Auto | ItemContainerUserSelectMode.UserCanSelect)) != 0;
#endif
	}

	internal bool RaiseItemInvoked(ItemContainerInteractionTrigger interactionTrigger, object originalSource)
	{
		//ITEMCONTAINER_TRACE_VERBOSE(*this, TRACE_MSG_METH_STR, METH_NAME, this, TypeLogging::ItemContainerInteractionTriggerToString(interactionTrigger).c_str());

		if (ItemInvoked is not null)
		{
			var itemInvokedEventArgs = new ItemContainerInvokedEventArgs(interactionTrigger, originalSource);
			ItemInvoked.Invoke(this, itemInvokedEventArgs);

			return itemInvokedEventArgs.Handled;
		}

		return false;
	}

	void LoadSelectionCheckbox()
	{
		m_selectionCheckbox = GetTemplateChild<CheckBox>(s_selectionCheckboxName);

		if (m_selectionCheckbox is { } selectionCheckbox)
		{
			selectionCheckbox.Checked += OnCheckToggle;
			m_checked_revoker.Disposable = new DisposableAction(() => selectionCheckbox.Checked -= OnCheckToggle);

			selectionCheckbox.Unchecked += OnCheckToggle;
			m_unchecked_revoker.Disposable = new DisposableAction(() => selectionCheckbox.Unchecked -= OnCheckToggle);
		}
	}

	void OnCheckToggle(object sender, RoutedEventArgs args)
	{
		UpdateCheckboxState();
	}

	void UpdateCheckboxState()
	{
		if (m_selectionCheckbox is { } selectionCheckbox)
		{
			bool isChecked = SharedHelpers.IsTrue(selectionCheckbox.IsChecked);
			bool isSelected = IsSelected;

			if (isChecked != isSelected)
			{
				selectionCheckbox.IsChecked = isSelected;
			}
		}
	}

	void UpdateMousePointerOverInstance(bool isPointerOver)
	{
		ItemContainer mousePointerOverInstance = null;
		s_mousePointerOverInstance?.TryGetTarget(out mousePointerOverInstance);

		if (isPointerOver)
		{
			if (mousePointerOverInstance == null || mousePointerOverInstance != this)
			{
				if (mousePointerOverInstance != null && mousePointerOverInstance.m_pointerInfo != null)
				{
					mousePointerOverInstance.m_pointerInfo.ResetIsMousePointerOver();
				}

				s_mousePointerOverInstance = new WeakReference<ItemContainer>(this);
			}
		}
		else
		{
			if (mousePointerOverInstance != null && mousePointerOverInstance == this)
			{
				s_mousePointerOverInstance = null;
			}
		}
	}

#if false

	string DependencyPropertyToString(
		DependencyProperty dependencyProperty)
	{
		if (dependencyProperty == IsSelectedProperty)
		{
			return "IsSelected";
		}
		else if (dependencyProperty == CanUserSelectProperty)
		{
			return "CanUserSelect";
		}
		else if (dependencyProperty == CanUserInvokeProperty)
		{
			return "CanUserInvoke";
		}
		else if (dependencyProperty == MultiSelectModeProperty)
		{
			return "MultiSelectMode";
		}
		else
		{
			return "UNKNOWN";
		}
	}

	protected override Size MeasureOverride(Size availableSize)
	{
		//ITEMCONTAINER_TRACE_VERBOSE(*this, TRACE_MSG_METH_STR_FLT_FLT, METH_NAME, this, L"availableSize", availableSize.Width, availableSize.Height);

		Size returnedSize = base.MeasureOverride(availableSize);

		//ITEMCONTAINER_TRACE_VERBOSE(*this, TRACE_MSG_METH_STR_FLT_FLT, METH_NAME, this, L"returnedSize", returnedSize.Width, returnedSize.Height);

		return returnedSize;
	}

	protected override Size ArrangeOverride(Size finalSize)
	{
		//ITEMCONTAINER_TRACE_VERBOSE(*this, TRACE_MSG_METH_STR_FLT_FLT, METH_NAME, this, L"finalSize", finalSize.Width, finalSize.Height);

		Size returnedSize = base.ArrangeOverride(finalSize);

		//ITEMCONTAINER_TRACE_VERBOSE(*this, TRACE_MSG_METH_STR_FLT_FLT, METH_NAME, this, L"returnedSize", returnedSize.Width, returnedSize.Height);

		return returnedSize;
	}

#endif
}

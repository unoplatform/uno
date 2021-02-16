// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Windows.Devices.Input;
using Windows.Foundation;
using Windows.UI.Input;
using Windows.UI.Xaml.Automation;
using Windows.UI.Xaml.Automation.Peers;
using Uno.UI;
using Windows.UI.nput;
using Windows.UI.Xaml.Input;

namespace Windows.UI.Xaml.Controls.Primitives
{
	public partial class LoopingSelector
	{
		private const string c_scrollViewerTemplatePart = "ScrollViewer";
		private const string c_upButtonTemplatePart = "UpButton";
		private const string c_downButtonTemplatePart = "DownButton";
		private const double c_targetScreenWidth = 400.0;


		public LoopingSelector()
		{
			_hasFocus = false;
			_isSized = false;
			_isSetupPending = true;
			_isScrollViewerInitialized = false;
			_skipNextBalance = false;
			_skipSelectionChangeUntilFinalViewChanged = false;
			_skipNextArrange = false;
			_itemState = ItemState.Expanded;
			_unpaddedExtentTop = 0.0;
			_unpaddedExtentBottom = 0.0;
			_realizedTop = 0.0;
			_realizedBottom = 0.0;
			_realizedTopIdx = -1;
			_realizedBottomIdx = -1;
			_realizedMidpointIdx = -1;
			_itemCount = 0;
			_scaledItemHeight = 0.0;
			_itemHeight = 0.0;
			_itemWidth = 0.0;
			_panelSize = 0.0;
			_panelMidpointScrollPosition = 0.0;
			_isWithinScrollChange = false;
			_isWithinArrangeOverride = false;
			_disablePropertyChange = false;
			_lastArrangeSizeHeight = 0.0f;
			_delayScrollPositionY = -1.0;
			_itemWidthFallback = 0.0;

			//_focusGot = 0;
			//_focusLost = 0;
			//_pressedToken = 0;
			//_viewChangingToken = 0;
			//_viewChangedToken = 0;
			//_upButtonClickedToken = 0;
			//_downButtonClickedToken = 0;
			//_pointerEnteredToken = 0;
			//_pointerExitedToken = 0;
		}


		void InitializeImpl()
		{

			//IControlFactory spInnerFactory;
			//IControl spInnerInstance;
			DependencyObject spInnerInspectable;
			UIElement spUIElement;

			//LoopingSelectorGenerated.InitializeImpl();
			//(wf.GetActivationFactory(
			//    wrl_wrappers.Hstring(RuntimeClass_Microsoft_UI_Xaml_Controls_Control),
			//    &spInnerFactory));

			//(spInnerFactory.CreateInstance(
			//    (DependencyObject)((ILoopingSelector)(this)),
			//    &spInnerInspectable,
			//    &spInnerInstance));

			//(SetComposableBasePointers(
			//        spInnerInspectable,
			//        spInnerFactory));

			//(Private.SetDefaultStyleKey(
			//        spInnerInspectable,
			//        "Microsoft.UI.Xaml.Controls.Primitives.LoopingSelector"));

			//QueryInterface(__uuidof(UIElement), &spUIElement);
			// These events are automatically removed when the
			// UIElement's destructor is called.

			//(spUIElement.add_PointerPressed(
			//    wrl.Callback<nput.IPointerEventHandler>
			//        (this, &LoopingSelector.OnPressed),
			//        &_pressedToken));
			PointerPressed += OnPressed;
			//(spUIElement.add_LostFocus(
			//    wrl.Callback<RoutedEventHandler>
			//    (this, &LoopingSelector.OnLostFocus),
			//    &_focusLost));
			LostFocus += OnLostFocus;
			//(spUIElement.add_GotFocus(
			//    wrl.Callback<RoutedEventHandler>
			//    (this, &LoopingSelector.OnGotFocus),
			//    &_focusGot));
			GotFocus += OnGotFocus;
			//(spUIElement.add_PointerEntered(
			//    wrl.Callback<nput.IPointerEventHandler>
			//    (this, &LoopingSelector.OnPointerEntered),
			//    &_pointerEnteredToken));
			PointerEntered += OnPointerEntered;
			//(spUIElement.add_PointerExited(
			//    wrl.Callback<nput.IPointerEventHandler>
			//    (this, &LoopingSelector.OnPointerExited),
			//    &_pointerExitedToken));
			PointerExited += OnPointerExited;

			//{
			//    DependencyObject spPreviousScrollPosition;
			//    Private.ValueBoxer.Createdouble(0.0, &spPreviousScrollPosition);
			//    _tpPreviousScrollPosition = spPreviousScrollPosition;
			//}

			// Cache the CanvasStatics because of frequency of use.
			//(wf.GetActivationFactory(
			//    wrl_wrappers.Hstring(RuntimeClass_Microsoft_UI_Xaml_Controls_Canvas),
			//    &_spCanvasStatics));

			//// Cleanup
			//    if (/FAILED/(hr) && spUIElement)
			//    {
			//        if (_pressedToken.value != 0)
			//        {
			//            IGNOREHR(spUIElement.remove_PointerPressed(_pressedToken));
			//        }

			//        if (_focusLost.value != 0)
			//        {
			//            IGNOREHR(spUIElement.remove_LostFocus(_focusLost));
			//        }

			//        if (_focusGot.value != 0)
			//        {
			//            IGNOREHR(spUIElement.remove_GotFocus(_focusGot));
			//        }

			//        if (_pointerEnteredToken.value != 0)
			//        {
			//            IGNOREHR(spUIElement.remove_PointerEntered(_pointerEnteredToken));
			//        }

			//        if (_pointerExitedToken.value != 0)
			//        {
			//            IGNOREHR(spUIElement.remove_PointerExited(_pointerExitedToken));
			//        }
			//    }
		}

		private void RaiseOnSelectionChanged(
			DependencyObject pOldItem,
			DependencyObject pNewItem)
		{

			//SelectionChangedEventArgsFactory spSelectionChangedEventArgsFactory;
			SelectionChangedEventArgs spSelectionChangedEventArgs;
			IList<DependencyObject> spRemovedItems;
			IList<DependencyObject> spAddedItems;
			DependencyObject spInner;
			DependencyObject spThisAsInspectable;

			//wfci_.Vector<DependencyObject.Make(spRemovedItems);
			spRemovedItems = new List<DependencyObject>(1);
			//wfci_.Vector<DependencyObject.Make(spAddedItems);
			spAddedItems = new List<DependencyObject>(1);
			spAddedItems.Append(pNewItem);
			spRemovedItems.Append(pOldItem);
			// Raise the SelectionChanged event
			//(wf.GetActivationFactory(
			//	wrl_wrappers.Hstring(RuntimeClass_Microsoft_UI_Xaml_Controls_SelectionChangedEventArgs),
			//	&spSelectionChangedEventArgsFactory));

			//(spSelectionChangedEventArgsFactory.CreateInstanceWithRemovedItemsAndAddedItems(
			//	spRemovedItems,
			//	spAddedItems,
			//	null,
			//	&spInner,
			//	&spSelectionChangedEventArgs));

			spSelectionChangedEventArgs = new SelectionChangedEventArgs(spRemovedItems, spAddedItems);


			//QueryInterface(__uuidof(DependencyObject), &spThisAsInspectable);
			//(m_SelectionChangedEventSource.InvokeAll(
			//	spThisAsInspectable,
			//	spSelectionChangedEventArgs));

			SelectionChanged?.Invoke(this, spSelectionChangedEventArgs);
		}

		#region FrameworkElementOverrides

		protected override Size MeasureOverride(Size availableSize)
		{
			int itemWidth;
			Size returnValue = default;

			////LSTRACE("[%d] Measure called.", (((int)this) >> 8) & 0xFF);

			//LoopingSelectorGenerated.MeasureOverrideImpl(availableSize, returnValue);

			itemWidth = ItemWidth;

			if (itemWidth == 0 && availableSize.Width.IsFinite())
			{
				// If there is no itemWidth set, we use all the available space.
				returnValue.Width = availableSize.Width;
			}
			else
			{
				// If we have an itemWidth, use that for the desired size.
				returnValue.Width = itemWidth;
			}

			return returnValue;
		}

		protected override Size ArrangeOverride(Size finalSize)

		{
			int itemWidth = 0;
			double verticalOffsetBeforeArrangeImpl = 0.0;
			bool expectedOffsetChange = false;
			double widthToReturn = 0.0;

			////LSTRACE("[%d] Arrange called.", (((int)this) >> 8) & 0xFF);

			_isWithinArrangeOverride = true;

			//var guard = wil.scope_exit([this, &finalSize]()
			//{
			//	_lastArrangeSizeHeight = finalSize.Height;
			//	_isWithinArrangeOverride = false;
			//});

			itemWidth = ItemWidth;

			if (itemWidth != 0)
			{
				// Override the width with that of the first item's
				// width. A Canvas doesn't wrap
				// content so we do this so the control sizes correctly.
				widthToReturn = (float)(itemWidth);
			}
			else
			{
				// If no itemWidth has been set, we use all the available space.
				widthToReturn = finalSize.Width;

				// We compute a new value for _itemWidthFallback
				double newItemWidthFallback = finalSize.Width;
				if (_itemWidthFallback != newItemWidthFallback)
				{
					_itemWidthFallback = newItemWidthFallback;
					_isSized = false;
				}
			}

			if (_delayScrollPositionY != -1.0)
			{
				SetScrollPosition(_delayScrollPositionY, false /* useAnimation */);
				// SetScrollPosition sets _skipNextBalance to true. This is to prevent the call to Balance from OnViewChanged from running.
				// But OnViewChanged guards against calling Balance with _isWithinArrangeOverride. So the _skipNextBalance flag will not get cleared.
				// We need to explictly clear the flag here, so that the call to Balance towards the end of this function has an effect.
				// See bug MSFT: 4711432 for more details.
				_skipNextBalance = false;

				_delayScrollPositionY = -1.0;
				expectedOffsetChange = true;
			}

			if (_isScrollViewerInitialized && !_isSetupPending)
			{
				Debug.Assert(_tpScrollViewer != null);
				verticalOffsetBeforeArrangeImpl = _tpScrollViewer.VerticalOffset;
			}

			Size returnValue = LoopingSelectorGenerated.ArrangeOverrideImpl(finalSize);

			if (finalSize.Height != _lastArrangeSizeHeight && _isScrollViewerInitialized && !_isSetupPending)
			{
				// Orientation must have changed or we got resized, what used to be the middle point has changed.
				// So we need to shift the items to restore the old middle point item.

				double oldPanelSize = _panelSize;
				double verticalOffsetAfterArrangeImpl = 0.0;

				verticalOffsetAfterArrangeImpl = _tpScrollViewer.VerticalOffset;

				SizePanel();

				double delta = (_panelSize - oldPanelSize) / 2;
				_realizedTop += delta;
				_realizedBottom += delta;

				ShiftChildren(delta);

				if (verticalOffsetAfterArrangeImpl != verticalOffsetBeforeArrangeImpl && !expectedOffsetChange)
				{
					// When moving from a small viewport to a large viewport during an orientation change,
					// the viewport vertical offset might get coerced and change. If that's the case,
					// we defer the scroll operation to the next layout pass because it's too late by then.
					_delayScrollPositionY = verticalOffsetBeforeArrangeImpl;
					_skipNextArrange = true;
				}
			}

			// Adding the first item on the first Arrange pass after the item has
			// been added to the visual tree (and scrollViewer is initialized) causes
			// a second Arrange pass to occur. We skip it as an optimization.
			if (_skipNextArrange && _isScrollViewerInitialized)
			{
				////LSTRACE("[%d] Skipping balance during this arrange.", (((int)this) >> 8) & 0xFF);
				_skipNextArrange = false;
			}
			else
			{
				// The ScrollViewer's extents aren't valid until the first
				// arrange pass has occured.
				if (!_isScrollViewerInitialized)
				{
					_isScrollViewerInitialized = true;
				}

				Balance(false /* isOnSnapPoint */);
			}


			if (returnValue != null)
			{
				returnValue.Width = widthToReturn;
			}

			return returnValue;
		}

		protected override void OnApplyTemplate()

		{


			//ControlProtected> spControlProtected;
			ContentControl spScrollViewerAsCC;
			DependencyObject spScrollViewerAsDO;
			DependencyObject spUpButtonAsDO;
			ButtonBase spUpButtonAsButtonBase;
			DependencyObject spDownButtonAsDO;
			ButtonBase spDownButtonAsButtonBase;

			////LSTRACE("[%d] OnApplyTemplate called.", (((int)this) >> 8) & 0xFF);

			if (_tpScrollViewer is { })
			{
				//_tpScrollViewer.remove_ViewChanged(_viewChangedToken);
				_tpScrollViewer.ViewChanged -= OnViewChanged;
				//_tpScrollViewer.remove_ViewChanging(_viewChangingToken);
				_tpScrollViewer.ViewChanging -= OnViewChanging;
			}

			if (_tpUpButton is { })
			{
				(_tpUpButton.remove_Click(_upButtonClickedToken));
			}

			if (_tpDownButton)
			{
				(_tpDownButton.remove_Click(_downButtonClickedToken));
			}

			if (_tpScrollViewerPrivate)
			{
				_tpScrollViewerPrivate.EnableOverpan();
			}

			_tpScrollViewer.Clear();
			_tpScrollViewerPrivate.Clear();
			_tpPanel.Clear();
			_tpUpButton.Clear();
			_tpDownButton.Clear();

			LoopingSelectorGenerated.OnApplyTemplateImpl();
			QueryInterface(__uuidof(ControlProtected), &spControlProtected);
			(spControlProtected.GetTemplateChild(
				wrl_wrappers.Hstring(c_upButtonTemplatePart),
				&spUpButtonAsDO));

			if (spUpButtonAsDO)
			{
				IGNOREHR(spUpButtonAsDO.As(spUpButtonAsButtonBase));
				_tpUpButton = spUpButtonAsButtonBase;
			}

			(spControlProtected.GetTemplateChild(
				wrl_wrappers.Hstring(c_downButtonTemplatePart),
				&spDownButtonAsDO));

			if (spDownButtonAsDO)
			{
				IGNOREHR(spDownButtonAsDO.As(spDownButtonAsButtonBase));
				_tpDownButton = spDownButtonAsButtonBase;
			}

			(spControlProtected.GetTemplateChild(
				wrl_wrappers.Hstring(c_scrollViewerTemplatePart),
				&spScrollViewerAsDO));

			if (spScrollViewerAsDO)
			{
				ScrollViewer> spScrollViewer;
				ScrollViewerPrivate> spScrollViewerPrivate;

				// Try to cast to IScrollViewer. If failed
				// just allow to remain null.
				IGNOREHR(spScrollViewerAsDO.As(spScrollViewer));
				IGNOREHR(spScrollViewerAsDO.As(spScrollViewerPrivate));
				IGNOREHR(spScrollViewerAsDO.As(spScrollViewerAsCC));

				_tpScrollViewer = spScrollViewer;
				_tpScrollViewerPrivate = spScrollViewerPrivate;
			}

			if (spScrollViewerAsCC)
			{
				xaml_primitives.ILoopingSelectorPanel> spPanel;
				DependencyObject spLoopingSelectorPanelAsInspectable;
				wrl.MakeAndInitialize<LoopingSelectorPanel>(spPanel);
				spPanel.As(spLoopingSelectorPanelAsInspectable);
				spScrollViewerAsCC.Content = spLoopingSelectorPanelAsInspectable;
				_tpPanel = spPanel;
			}

			if (_tpPanel)
			{
				xaml.FrameworkElement> spPanelAsFE;
				_tpPanel.As(spPanelAsFE);
				spPanelAsFE.Height = 1000000;
			}

			if (_tpUpButton)
			{
				(_tpUpButton.add_Click(
					wrl.Callback<RoutedEventHandler>(this, &LoopingSelector.OnUpButtonClicked),
					&_upButtonClickedToken));
			}

			if (_tpDownButton)
			{
				(_tpDownButton.add_Click(
					wrl.Callback<RoutedEventHandler>(this, &LoopingSelector.OnDownButtonClicked),
					&_downButtonClickedToken));
			}

			if (_tpScrollViewer)
			{
				(_tpScrollViewer.add_ViewChanged(
					wrl.Callback<wf.IEventHandler<xaml_controls.ScrollViewerViewChangedEventArgs>>
						(this, &LoopingSelector.OnViewChanged),
					&_viewChangedToken));

				(_tpScrollViewer.add_ViewChanging(
					wrl.Callback<wf.IEventHandler<xaml_controls.ScrollViewerViewChangingEventArgs>>
						(this, &LoopingSelector.OnViewChanging),
					&_viewChangingToken));
			}

			if (_tpScrollViewerPrivate)
			{
				_tpScrollViewerPrivate.DisableOverpan();
			}
		}

		#endregion

		#region UIElementOverrides

		protected override AutomationPeer OnCreateAutomationPeer()
		{

			LoopingSelector> spThis(this);
			ILoopingSelector> spThisAsILoopingSelector;
			xaml_automation_peers.LoopingSelectorAutomationPeer> spLoopingSelectorAutomationPeer;

			spThis.As(spThisAsILoopingSelector);
			(wrl.MakeAndInitialize<xaml_automation_peers.LoopingSelectorAutomationPeer>
				(spLoopingSelectorAutomationPeer, spThisAsILoopingSelector));

			spLoopingSelectorAutomationPeer.CopyTo(returnValue);
			spLoopingSelectorAutomationPeer.AsWeak(_wrAP);
			return returnValue;
		}

		#endregion

		void OnUpButtonClicked(
			DependencyObject pSender,
			RoutedEventArgs pEventArgs)
		{
			UNREFERENCED_PARAMETER(pSender);
			UNREFERENCED_PARAMETER(pEventArgs);

			SelectPreviousItem();
		}

		void OnDownButtonClicked(
			 DependencyObject pSender,
			 RoutedEventArgs pEventArgs)
		{
			UNREFERENCED_PARAMETER(pSender);
			UNREFERENCED_PARAMETER(pEventArgs);

			SelectNextItem();
		}

		void OnKeyDownImpl(
			KeyRoutedEventArgs pEventArgs)
		{

			bool bHandled = false;
			wsy.VirtualKey key = wsy.VirtualKey_None;
			wsy.VirtualKeyModifiers nModifierKeys;

			if (pEventArgs == null) throw new ArgumentNullException();

			bHandled = pEventArgs.Handled;
			if (bHandled)
			{
				goto Cleanup;
			}

			key = pEventArgs.Key;
			PlatformHelpers.GetKeyboardModifiers(nModifierKeys);
			if (!(nModifierKeys & wsy.VirtualKeyModifiers_Menu))
			{
				bHandled = true;

				switch (key)
				{
					case wsy.VirtualKey_Up:
						SelectPreviousItem();
						break;
					case wsy.VirtualKey_Down:
						SelectNextItem();
						break;
					case wsy.VirtualKey_GamepadLeftTrigger:
					case wsy.VirtualKey_PageUp:
						HandlePageUpKeyPress();
						break;
					case wsy.VirtualKey_GamepadRightTrigger:
					case wsy.VirtualKey_PageDown:
						HandlePageDownKeyPress();
						break;
					case wsy.VirtualKey_Home:
						HandleHomeKeyPress();
						break;
					case wsy.VirtualKey_End:
						HandleEndKeyPress();
						break;
					default:
						bHandled = false;
				}

				pEventArgs.Handled = bHandled;
			}
		}

		void SelectNextItem()
		{
			int index;
			bool shouldLoop = false;

			shouldLoop = ShouldLoop;
			index = SelectedIndex;

			//Don't scroll past the end of the list if we are not looping:
			if (index < (int)_itemCount - 1 || shouldLoop)
			{
				double pixelsToMove = _scaledItemHeight;
				SetScrollPosition(_unpaddedExtentTop + pixelsToMove, true /* use animation */);
				RequestInteractionSound(xaml.ElementSoundKind_Focus);
			}

			return;
		}

		void SelectPreviousItem()
		{
			int index;
			bool shouldLoop = false;

			shouldLoop = ShouldLoop;
			index = SelectedIndex;

			//Don't scroll past the start of the list if we are not looping
			if (index > 0 || shouldLoop)
			{
				double pixelsToMove = -1 * _scaledItemHeight;
				SetScrollPosition(_unpaddedExtentTop + pixelsToMove, true /* use animation */);
				RequestInteractionSound(xaml.ElementSoundKind_Focus);
			}

			return;
		}

		void HandlePageDownKeyPress()
		{
			double viewportHeight = _unpaddedExtentBottom - _unpaddedExtentTop;
			double pixelsToMove = viewportHeight / 2;
			SetScrollPosition(_unpaddedExtentTop + pixelsToMove, true /* use animation */);
			RequestInteractionSound(xaml.ElementSoundKind_Focus);

			return;
		}

		void HandlePageUpKeyPress()
		{
			double viewportHeight = _unpaddedExtentBottom - _unpaddedExtentTop;
			double pixelsToMove = -1 * viewportHeight / 2;
			SetScrollPosition(_unpaddedExtentTop + pixelsToMove, true /* use animation */);
			RequestInteractionSound(xaml.ElementSoundKind_Focus);

			return;
		}

		void HandleHomeKeyPress()
		{
			double pixelsToMove = -1 * _realizedMidpointIdx * _scaledItemHeight;
			SetScrollPosition(_unpaddedExtentTop + pixelsToMove, false /* use animation */);
			Balance(true /* isOnSnapPoint */);
			RequestInteractionSound(xaml.ElementSoundKind_Focus);

			return;
		}

		void HandleEndKeyPress()
		{
			int idxMovement = (_itemCount - 1) - _realizedMidpointIdx;
			double pixelsToMove = idxMovement * _scaledItemHeight;
			SetScrollPosition(_unpaddedExtentTop + pixelsToMove, false /* use animation */);
			Balance(true /* isOnSnapPoint */);
			RequestInteractionSound(xaml.ElementSoundKind_Focus);

			return;
		}

		void OnViewChanged(
			object pSender,
			ScrollViewerViewChangedEventArgs pEventArgs)
		{
			UNREFERENCED_PARAMETER(pSender);


			// In the new ScrollViwer2 interface OnViewChanged will actually
			// fire for all the intermediate ViewChanging events as well.
			// We only capture the final view.
			bool isIntermediate = false;
			isIntermediate = pEventArgs.IsIntermediate;
			if (!isIntermediate)
			{
				//LSTRACE("[%d] OnViewChanged called.", (((int)this) >> 8) & 0xFF);

				if (!_isWithinScrollChange && !_isWithinArrangeOverride)
				{
					Balance(true /* isOnSnapPoint */ );
					if (_itemState == ItemState.ManipulationInProgress)
					{
						TransitionItemsState(ItemState.Expanded);
						_itemState = ItemState.Expanded;
					}
					else if (_itemState == ItemState.LostFocus)
					{
						ExpandIfNecessary();
					}

					_skipSelectionChangeUntilFinalViewChanged = false;
				}
			}
			else
			{
				////LSTRACE("[%d] Skipping ViewChanged event, within scroll", (((int)this) >> 8) & 0xFF);
			}

			// Cleanup
			// return hr;
		}

		void OnViewChanging(
			object pSender,
			ScrollViewerViewChangingEventArgs pEventArgs)
		{
			UNREFERENCED_PARAMETER(pSender);
			UNREFERENCED_PARAMETER(pEventArgs);


			////LSTRACE("[%d] OnViewChanging called.", (((int)this) >> 8) & 0xFF);

			if (!_isWithinScrollChange && !_isWithinArrangeOverride)
			{
				Balance(false /* isOnSnapPoint */ );
				// The Focus transition doesn't occur sometimes when flicking
				// on one ScrollViewer, than another before the first flick completes.
				// This is a possible ScrollViewer bug. Any time a manipulation
				// occurs we force focus.
				if (_itemState != ItemState.LostFocus && !_hasFocus)
				{
					Control> spThisAsControl;
					UIElement spThisAsElement;
					bool didSucceed = false;
					xaml.FocusState focusState = xaml.FocusState.FocusState_Unfocused;
					QueryInterface(__uuidof(Control), &spThisAsControl);
					QueryInterface(__uuidof(UIElement), &spThisAsElement);
					focusState = spThisAsElement.FocusState;
					if (focusState == xaml.FocusState_Unfocused)
					{
						spThisAsControl.Focus(xaml.FocusState.FocusState_Programmatic, &didSucceed);
					}
				}

				// During a manipulation no item should be highlighted.
				if (_itemState == ItemState.Expanded)
				{
					TransitionItemsState(ItemState.ManipulationInProgress);
					_itemState = ItemState.ManipulationInProgress;
					AutomationRaiseExpandCollapse();
				}
			}
			else
			{
				////LSTRACE("[%d] Skipping ViewChanging event, within scroll", (((int)this) >> 8) & 0xFF);
			}
		}

		void OnPointerEntered(
			object pSender,
			PointerRoutedEventArgs pEventArgs)
		{

			//UNREFERENCED_PARAMETER(pSender);
			//UNREFERENCED_PARAMETER(pEventArgs);
			PointerDeviceType nPointerDeviceType = PointerDeviceType.Touch;
			PointerPoint spPointerPoint;
			PointerDevice spPointerDevice;

			spPointerPoint = pEventArgs.GetCurrentPoint(null);
			if (spPointerPoint == null) throw new ArgumentNullException();
			spPointerDevice = spPointerPoint.PointerDevice;
			if (spPointerDevice == null) throw new ArgumentNullException();
			nPointerDeviceType = spPointerDevice.PointerDeviceType;
			if (nPointerDeviceType != PointerDeviceType.Touch)
			{
				GoToState("PointerOver", false);
			}
		}

		void OnPointerExited(
			object pSender,
			PointerRoutedEventArgs pEventArgs)
		{

			//UNREFERENCED_PARAMETER(pSender);
			//UNREFERENCED_PARAMETER(pEventArgs);

			GoToState("Normal", false);
			TransitionItemsState(_itemState);
			// Cleanup
			// return hr;
		}

		void GoToState(string strState, bool useTransitions)
		{
			//VisualStateManagerStatics spVSMStatics;
			//Control spThisAsControl;

			//(wf.GetActivationFactory(
			//	wrl_wrappers.Hstring(RuntimeClass_Microsoft_UI_Xaml_VisualStateManager),
			//	&spVSMStatics));

			//QueryInterface(__uuidof(Control), &spThisAsControl);

			//bool returnValue = false;
			//spVSMStatics.GoToState(spThisAsControl, strState, useTransitions, &returnValue);
			VisualStateManager.GoToState(this, strState, useTransitions);

			return;
		}

		void OnPressed(
			object pSender,
			PointerRoutedEventArgs pEventArgs)
		{
			//UNREFERENCED_PARAMETER(pSender);
			//UNREFERENCED_PARAMETER(pEventArgs);

			////LSTRACE("[%d] OnPressed called.", (((int)this) >> 8) & 0xFF);

			if (_itemState == ItemState.LostFocus)
			{
				// LostFocus is only entered during a manipulation. Tapping
				// the control after LostFocus will reactivate it.
				_itemState = ItemState.ManipulationInProgress;
			}

			pEventArgs.Handled = true;
		}

		void OnLostFocus(
			object pSender,
			RoutedEventArgs pEventArgs)
		{
			//UNREFERENCED_PARAMETER(pSender);
			//UNREFERENCED_PARAMETER(pEventArgs);

			bool hasFocus = false;
			HasFocus(out hasFocus);
			// We check to ensure that we previously did have focus
			// (stored in _hasFocus) but now don't (the result of hasFocus)
			// to ensure this is actually a focus transition.
			if (!hasFocus && _hasFocus)
			{
				_hasFocus = false;
				if (_itemState == ItemState.ManipulationInProgress)
				{
					// If we're in the middle of a manipulation we
					// set itemState to LostFocus so completion of the
					// manipulation will collapse.
					_itemState = ItemState.LostFocus;
				}
				else
				{
					ExpandIfNecessary();
				}
			}
		}

		void OnGotFocus(
			object pSender,
			RoutedEventArgs pEventArgs)
		{
			bool hasFocus = false;
			HasFocus(out hasFocus);

			if (hasFocus)
			{
				_hasFocus = true;
			}

			return;
		}

		void OnItemTapped(
			object pSender,
			TappedRoutedEventArgs pArgs)
		{
			if (_itemState == ItemState.Expanded)
			{
				LoopingSelectorItem spLsiInterface;
				//pSender.QueryInterface<ILoopingSelectorItem>(spLsiInterface);
				spLsiInterface = pSender as LoopingSelectorItem;

				LoopingSelectorItem lsiPtr = (LoopingSelectorItem)(spLsiInterface);

				int itemVisualIndex = 0;
				uint itemIndex = 0;
				itemVisualIndex = lsiPtr.GetVisualIndex();
				VisualIndexToItemIndex(itemVisualIndex, out itemIndex);

				// We need to make sure that we check against the selected index,
				// not the midpoint index.  Normally, the item in the center of the view
				// is also the selected item, but in the case of UI automation, it
				// is not always guaranteed to be.
				int selectedIndex = 0;
				selectedIndex = SelectedIndex;

				if (itemIndex == (uint)(selectedIndex))
				{
					ExpandIfNecessary();
				}
				else
				{
					// Tapping any other time scrolls to that item.
					double pixelsToMove = (double)(itemVisualIndex - _realizedMidpointIdx) * (_scaledItemHeight);
					SetScrollPosition(_unpaddedExtentTop + pixelsToMove, true /* use animation */);
				}
			}

			return;
		}

		void OnPropertyChanged(
			DependencyPropertyChangedEventArgs pArgs)
		{

			DependencyProperty spPropertyInfo;

			spPropertyInfo = pArgs.Property;
			if (spPropertyInfo == ItemsProperty)
			{
				////LSTRACE("[%d] Items property refreshed.", (((int)this) >> 8) & 0xFF);

				// Optimization: When items change the size of the scrollviewer does not.
				// We clear all items and allow for the balancing function to realize
				// and replace the items as instead of refreshing the entire thing.
				ClearAllItems();
				Balance(false /* isOnSnapPoint */ );
				AutomationClearPeerMap();
				AutomationRaiseSelectionChanged();
			}
			else if (spPropertyInfo == ShouldLoopProperty)
			{
				////LSTRACE("[%d] ShouldLoop property refreshed.", (((int)this) >> 8) & 0xFF);

				// ShouldLoop will require resizing the bounds of the scrollviewer, as a result
				// we start from scratch and resetup everything.
				ClearAllItems();
				_isSized = false;
				_isSetupPending = true;
				Balance(false /* isOnSnapPoint */ );
			}
			else if (spPropertyInfo == SelectedIndexProperty && !_disablePropertyChange)
			{
				////LSTRACE("[%d] Selected Index property refreshed.", (((int)this) >> 8) & 0xFF);
				DependencyObject spValue;
				int spReferenceInt;
				int newIdx = 0;
				int oldIdx = 0;

				spValue = pArgs.NewValue;
				spValue.As(spReferenceInt);
				newIdx = spReferenceInt.Value;
				spValue = pArgs.OldValue;
				spValue.As(spReferenceInt);
				oldIdx = spReferenceInt.Value;
				// NOTE: This call only will be applied when the
				// scrollviewer is not being manipulated. Once a
				// manipulation has begun the user's eventual
				// selection takes dominance.
				SetSelectedIndex(oldIdx, newIdx);
				Balance(false /* isOnSnapPoint */ );
			}
		}

		void IsTemplateAndItemsAttached(out bool result)
		{

			IList<object> spItems;

			result = false;

			if (_tpScrollViewer is { } &&
			    _tpPanel is { })
			{
				spItems = Items;
				if (spItems is { })
				{
					uint itemCount = 0;
					itemCount = (uint)spItems.Count;
					_itemCount = itemCount;
					if (itemCount > 0)
					{
						result = true;
					}
				}
			}
		}

		void IsSetupForAutomation(out bool isSetup)
		{
			bool isTemplateAndItemsAttached = false;
			IsTemplateAndItemsAttached(out isTemplateAndItemsAttached);
			isSetup = isTemplateAndItemsAttached && _isSized && !_isSetupPending;
		}

		void Balance(bool isOnSnapPoint)
		{


			bool isTemplateAndItemsAttached = false;
			bool abortBalance = false;

			IsTemplateAndItemsAttached(out isTemplateAndItemsAttached);
			abortBalance = !isTemplateAndItemsAttached;

			// Normalize will call SetScrollPosition, which
			// triggers another ViewChanged event to occur.
			// We skip the balance called from ViewChanged.
			if (!abortBalance && _skipNextBalance)
			{
				_skipNextBalance = false;
				////LSTRACE("[%d] Skipping balance.", (((int)this) >> 8) & 0xFF);
				abortBalance = true;
			}

			if (!abortBalance)
			{
				MeasureExtent(out _unpaddedExtentTop, out _unpaddedExtentBottom);

				// 100000 is a arbitrary number that is larger than any possible screen size.
				// If the visible area of the scrollviewer is larger than this value then
				// it's likely the control is placed in a ScrollViewer or other layout element
				// that feeds infinite size to its children. In these situations we avoid
				// balancing.
				if (_unpaddedExtentBottom - _unpaddedExtentTop > 100000)
				{
					_skipNextArrange = false;
					////LSTRACE("[%d] Skipping balance. Extents too large, LoopingSelector is not designed to operate with infinite or very large extents.", (((int)this) >> 8) & 0xFF);
					abortBalance = true;
				}
			}

			if (!abortBalance)
			{
				EnsureSetup();
				abortBalance = !_isSized || _isSetupPending;
			}

			if (!abortBalance)
			{
				uint headAdd = 0;
				uint headTrim = 0;
				uint tailAdd = 0;
				uint tailTrim = 0;

				int maxTopIdx = 0;
				int maxBottomIdx = 0;
				double paddedExtentTop = 0.0;
				double paddedExtentBottom = 0.0;
				double viewportHeight = 0.0;

				GetMaximumAddIndexPosition(out maxTopIdx, out maxBottomIdx);
				// We only normalize on snap points because otherwise a manipulation
				// is in progress and SetScrollPosition will
				// coherce the scroll offsetonto a snappoint, even if we're currently
				// between two.
				if (isOnSnapPoint)
				{
					Normalize();
				}

				viewportHeight = _unpaddedExtentBottom - _unpaddedExtentTop;
				// Virtualization buffer is a half viewport in either direction.
				paddedExtentTop = _unpaddedExtentTop - viewportHeight / 2;
				paddedExtentBottom = _unpaddedExtentBottom + viewportHeight / 2;

				// By adding a pixel we assure we never trim and re-add an element.
				while (_realizedTop < (paddedExtentTop + 1.0 - _scaledItemHeight) && _realizedItems.size() > 0)
				{
					Trim(ListEnd.Head);
					headTrim++;
				}

				while (_realizedBottom > (paddedExtentBottom - 1.0 + _scaledItemHeight) && _realizedItems.size() > 0)
				{
					Trim(ListEnd.Tail);
					tailTrim++;
				}

				while (_realizedTop > paddedExtentTop && _realizedTopIdx > maxTopIdx)
				{
					Add(ListEnd.Head);
					headAdd++;
				}

				while (_realizedBottom < paddedExtentBottom && _realizedBottomIdx < maxBottomIdx)
				{
					Add(ListEnd.Tail);
					tailAdd++;
				}

				if (headAdd > 0 || tailAdd > 0 || tailTrim > 0 || tailAdd > 0)
				{
					AutomationRaiseStructureChanged();
				}

				////LSTRACE("[%d] HeadAdd: %d HeadTrim: %d TailAdd: %d TailTrim: %d Realized: %d Recycled: %d", (((int)this) >> 8) & 0xFF, headAdd, headTrim, tailAdd, tailTrim, _realizedItems.size(), _recycledItems.size());

				// During manipulation we don't update the selected index. Only when
				// we reach a final value.
				if (isOnSnapPoint || _itemState == ItemState.Expanded)
				{
					UpdateSelectedItem();
				}
			}
		}

		void Normalize()
		{

			bool shouldLoop = false;

			shouldLoop = ShouldLoop;
			// Only normalize if strayed 50px+ from center and if
			// in looping mode. Nonlooping selectors don't normalize.
			if (shouldLoop && Math.Abs(_unpaddedExtentTop - _panelMidpointScrollPosition) > 50.0)
			{
				double delta = _panelMidpointScrollPosition - _unpaddedExtentTop;

				// WORKAROUND: It's likely there's a bug in dmanip that causes it to
				// end a manipulation not on a snap point when two fingers are on the
				// scrollviewer. We explicitly make sure we are on a snappoint here.
				// Delaying bug filing until our input system is more final.
				// DManip work tracked by WPBLUE: 11547.
				bool isActuallyOnSnapPoint = (Math.Abs(delta / _scaledItemHeight - Math.Floor(delta / _scaledItemHeight)) < 0.001);

				if (isActuallyOnSnapPoint)
				{
					_realizedTop += delta;
					_realizedBottom += delta;

					// These are adjusted for the duration of the balance. ScrollViewer
					// requires an invalidate pass which occurs before the next ViewChanged
					// event for its extents to become correct , we manually account
					// for the offset here since the next Balance happens before that.
					_unpaddedExtentTop += delta;
					_unpaddedExtentBottom += delta;

					ShiftChildren(delta);
					////LSTRACE("[%d] Shifted %f (%f items) %d", (((int)this) >> 8) & 0xFF, delta, delta / _scaledItemHeight, isActuallyOnSnapPoint);

					SetScrollPosition(_panelMidpointScrollPosition, false);
					// Skip the balance occuring after ViewChanged.
					_skipNextBalance = true;
				}
			}
		}

		void EnsureSetup()
		{
			int selectedIdx = 0;

			selectedIdx = SelectedIndex;
			if (!_isSized)
			{
				//LSTRACE("[%d] Sizing panel.", (((int)this) >> 8) & 0xFF);
				int itemDimAsInt = 0;
				// Optimization: caching itemHeight and itemWidth.
				itemDimAsInt = ItemHeight;
				_itemHeight = (double)(itemDimAsInt);
				_scaledItemHeight = (double)(itemDimAsInt);

				itemDimAsInt = ItemWidth;
				if (itemDimAsInt == 0)
				{
					// If we don't have an explictly set ItemWidth, we fallback to this value which is computed during Arrange.
					_itemWidth = _itemWidthFallback;
				}
				else
				{
					_itemWidth = (double)(itemDimAsInt);
				}

				ClearAllItems();
				SizePanel();
				_isSized = true;
			}

			// ScrollViewer isn't fully initialized until the
			// first Arrange pass and will return invalid extents
			// and ignore SetScrollPosition requests.
			if (_isScrollViewerInitialized && _isSetupPending)
			{
				//LSTRACE("[%d] Setting up bounds and scroll viewer.", (((int)this) >> 8) & 0xFF);
				double viewportHeight = 0.0;
				double verticalOffset = 0.0;
				double newScrollPosition = 0.0;
				double startPoint = 0.0;
				bool shouldLoop = false;

				shouldLoop = ShouldLoop;
				viewportHeight = _tpScrollViewer.ViewportHeight;
				verticalOffset = _tpScrollViewer.VerticalOffset;
				SetupSnapPoints(0.0, _scaledItemHeight);
				// If in looping mode the selector is setup
				// in the middle of the scrollable area, if in nonlooping
				// mode it is setup to be at the currently selected item's offset.
				// (Nonlooping mode sizes the scrollable area to be percisely large
				//  enough for the item count)
				if (shouldLoop)
				{
					startPoint = _panelSize / 2;
					_panelMidpointScrollPosition = startPoint - viewportHeight / 2 + _scaledItemHeight / 2;

					_realizedTop = startPoint;
					_realizedBottom = startPoint;
					newScrollPosition = _panelMidpointScrollPosition;
				}
				else
				{
					startPoint = (_panelSize - (_itemCount) * _scaledItemHeight) / 2;
					_panelMidpointScrollPosition = startPoint - viewportHeight / 2 + _scaledItemHeight / 2;
					_realizedTop = startPoint + selectedIdx * _scaledItemHeight;
					_realizedBottom = startPoint + selectedIdx * _scaledItemHeight;
					newScrollPosition = _panelMidpointScrollPosition + selectedIdx * _scaledItemHeight;
				}

				_realizedTopIdx = selectedIdx;
				_realizedBottomIdx = selectedIdx - 1;

				// Optimization: skip scrolling if scrollviewer is
				// in correct position (often true after normalization)
				if (Math.Abs(verticalOffset - newScrollPosition) > 1.0)
				{
					SetScrollPosition(newScrollPosition, false);
					_unpaddedExtentTop += newScrollPosition - verticalOffset;
					_unpaddedExtentBottom += newScrollPosition - verticalOffset;

					// Skip the balance caused by the ViewChange event fired
					// from SetScrollPosition.
					_skipNextBalance = true;
				}

				_isSetupPending = false;
			}
		}

		void SetSelectedIndex(int oldIdx, int newIdx)
		{
			double pixelsToMove = 0.0;
			bool isTemplateAndItemsAttached = false;

			IsTemplateAndItemsAttached(out isTemplateAndItemsAttached);
			// Only set the new index position if we're in the idle position
			// and the control is properly initialized and the oldIndex is meanful.
			if (oldIdx != -1 && isTemplateAndItemsAttached && _itemState == ItemState.Expanded)
			{
				//LSTRACE("[%d] SetSelectedIndex From %d To %d called.", (((int)this) >> 8) & 0xFF, oldIdx, newIdx);
				pixelsToMove = (double)(newIdx - oldIdx) * (_scaledItemHeight);
				SetScrollPosition(_unpaddedExtentTop + pixelsToMove, false);
			}
		}

		void Trim(ListEnd end)
		{
			LoopingSelectorItem spChildAsLSI;

			if (_realizedItems.empty())
			{
				//LSTRACE("[%d] Trim called with empty list.", (((int)this) >> 8) & 0xFF);
				//goto Cleanup;
				return;
			}

			if (end == ListEnd.Head)
			{
				//COMPtr assignment causes AddRef.
				spChildAsLSI = _realizedItems.back();
				_realizedItems.pop_back();
			}
			else
			{
				spChildAsLSI = _realizedItems.front();
				_realizedItems.erase(_realizedItems.begin());
			}

			if (end == ListEnd.Head)
			{
				_realizedTop += _scaledItemHeight;
				_realizedTopIdx++;
			}
			else
			{
				_realizedBottom -= _scaledItemHeight;
				_realizedBottomIdx--;
			}

			RecycleItem(spChildAsLSI);
		}

		void Add(ListEnd end)
		{
			LoopingSelectorItem spChild;
			UIElement spChildAsUI;

			if (end == ListEnd.Head)
			{
				RealizeItem((uint)(_realizedTopIdx - 1), out spChild);
				// Panel's Children keeps the reference to this item.
				_realizedItems.push_back(spChild);
				spChildAsUI = spChild;
				SetPosition(spChildAsUI, _realizedTop - _scaledItemHeight);
				_realizedTop -= _scaledItemHeight;
				_realizedTopIdx--;
			}
			else
			{
				RealizeItem((uint)(_realizedBottomIdx + 1), out spChild);
				// Panel's Children keeps the reference to this item.
				_realizedItems.insert(_realizedItems.begin(), spChild);
				spChildAsUI = spChild;
				SetPosition(spChildAsUI, _realizedBottom);
				_realizedBottom += _scaledItemHeight;
				_realizedBottomIdx++;
			}
		}

		void GetMaximumAddIndexPosition(out int headIdx, out int tailIdx)
		{
			bool shouldLoop;
			shouldLoop = ShouldLoop;
			if (shouldLoop)
			{
				headIdx = int.MinValue;
				tailIdx = int.MaxValue;
			}
			else
			{
				headIdx = 0;
				tailIdx = (int)(_itemCount - 1);
			}
		}

		// In cases where we're directly setting the selected item (e.g., via UIA),
		// we don't care whether we're in the middle of scrolling.
		// We'll use ignoreScrollingState to flag such scenarios.
		void UpdateSelectedItem(bool ignoreScrollingState)
		{
			IList<DependencyObject> spItemsCollection;
			DependencyObject spSelectedItem;
			DependencyObject spPreviouslySelectedItem;

			uint itemCount = 0;
			double midpoint = 0.0;
			uint newIdx = 0;
			int oldIdx = 0;

			// This will be in the middle of the currently selected item.
			midpoint = (_unpaddedExtentTop + _unpaddedExtentBottom) / 2 - _realizedTop;
			newIdx = _realizedTopIdx +
				(uint)(midpoint) / (uint)(_scaledItemHeight);

			spItemsCollection = Items;
			itemCount = spItemsCollection.Count;
			// Normally, when an item is scrolled into the center of our view,
			// we want to automatically select that item.  However, in the case of
			// UI automation (e.g., Narrator), users will be iterating through the
			// looping selector items one at a time to hear them read out,
			// in order to find the one that they want to select.  In this case,
			// we don't want to automatically select the item that is scrolled into view,
			// so in that circumstance we skip selecting the new item.
			// However, we do still want to put the item in the visual state
			// of being selected, since it will be appearing in the middle,
			// meaning that we want the font color of the item in that position
			// to properly match the background.
			UpdateVisualSelectedItem(_realizedMidpointIdx, newIdx);
			_realizedMidpointIdx = newIdx;

			if (ignoreScrollingState || !_skipSelectionChangeUntilFinalViewChanged)
			{
				newIdx = PositiveMod((int)newIdx, (int)itemCount);

				spSelectedItem = spItemsCollection[(int)newIdx];
				_disablePropertyChange = true;
				oldIdx = SelectedIndex;
				SelectedIndex = (int)(newIdx);
				spPreviouslySelectedItem = SelectedItem as DependencyObject;
				SelectedItem = spSelectedItem;
				if ((uint)oldIdx != newIdx)
				{
					RaiseOnSelectionChanged(spPreviouslySelectedItem, spSelectedItem);
					AutomationRaiseSelectionChanged();
				}

				_disablePropertyChange = false;
			}
		}


		// NOTE: Only called when the ScrollViewer is done Running (e.g. no scrolling is happening).
		void UpdateVisualSelectedItem(uint oldIdx, uint newIdx)
		{
			LoopingSelectorItem spEltAsLSI;
			LoopingSelectorItem lsi = null;

			if (_realizedItems.size() > 0)
			{
				if (_realizedItems.size() > oldIdx - _realizedTopIdx)
				{
					spEltAsLSI = _realizedItems[_realizedItems.size() - (oldIdx - _realizedTopIdx) - 1];
					lsi = (LoopingSelectorItem)(spEltAsLSI);
					if (_itemState == ItemState.Expanded)
					{
						lsi.SetState(LoopingSelectorItem.State.Expanded, true);
					}
					else
					{
						lsi.SetState(LoopingSelectorItem.State.Normal, true);
					}
				}

				if (_realizedItems.size() > newIdx - _realizedTopIdx)
				{
					spEltAsLSI = _realizedItems[_realizedItems.size() - (newIdx - _realizedTopIdx) - 1];
					lsi = (LoopingSelectorItem)(spEltAsLSI);
					lsi.SetState(LoopingSelectorItem.State.Selected, true);
				}
			}
		}

		void VisualIndexToItemIndex(uint visualIndex, out uint itemIndex)
		{
			//if (itemIndex == null) { return; }

			IList<object> itemsCollection;
			itemsCollection = Items;

			int itemCount = 0;
			itemCount = itemsCollection.Count;

			itemIndex = PositiveMod((int)visualIndex, itemCount);

			return;
		}

		void RealizeItem(uint itemIdxToRealize, out LoopingSelectorItem ppItem)
		{
			LoopingSelectorItem spLoopingSelectorItem;
			ContentControl spLoopingSelectorItemAsCC;
			DependencyObject spLoopingSelectorItemAsDO;
			LoopingSelectorItem lsi = null;

			//if (ppItem == null) { return; }

			uint moddedItemIdx = 0;
			VisualIndexToItemIndex(itemIdxToRealize, out moddedItemIdx);

			bool wasItemRecycled = false;

			RetreiveItemFromAPRealizedItems(moddedItemIdx, out spLoopingSelectorItem);
			if (!(spLoopingSelectorItem is { }) && _recycledItems.size() != 0)
			{
				spLoopingSelectorItem = _recycledItems.back();
				wasItemRecycled = true;
			}

			if (!(spLoopingSelectorItem is { }))
			{
				UIElement spLSIAsUIElt;
				FrameworkElement spLSIAsFE;
				IList<UIElement> spPanelChildren;
				DataTemplate spDataTemplate;
				Control spLSIAsControl;
				Control spThisAsControl;
				//EventRegistrationToken tappedToken = default;
				bool visualTreeRebuilt = false;

				//QueryInterface(__uuidof(Control), &spThisAsControl);
				spThisAsControl = this;

				uint childCount;
				GetPanelChildren(out spPanelChildren, out childCount);

				//wrl.MakeAndInitialize<LoopingSelectorItem>(spLoopingSelectorItem);
				spLoopingSelectorItem = new LoopingSelectorItem();
				//spLoopingSelectorItem.As(spLSIAsUIElt);
				spLSIAsUIElt = spLoopingSelectorItem;
				//spLoopingSelectorItem.As(spLSIAsControl);
				spLSIAsControl = spLoopingSelectorItem;
				//spLoopingSelectorItem.As(spLoopingSelectorItemAsCC);
				spLoopingSelectorItemAsCC = spLoopingSelectorItem;
				//spLoopingSelectorItem.As(spLoopingSelectorItemAsDO);
				spLoopingSelectorItemAsDO = spLoopingSelectorItem;
				//spLoopingSelectorItem.As(spLSIAsFE);
				spLSIAsFE = spLoopingSelectorItem;
				lsi = (LoopingSelectorItem)(spLoopingSelectorItem);

				lsi.SetParent(this);
				spDataTemplate = ItemTemplate;
				spLoopingSelectorItemAsCC.ContentTemplate = spDataTemplate;
				spLSIAsFE.Width = _itemWidth;
				spLSIAsFE.Height = _itemHeight;
				spPanelChildren.Append(spLSIAsUIElt);

				HorizontalAlignment horizontalAlignment;
				Thickness padding;

				horizontalAlignment = spThisAsControl.HorizontalContentAlignment;
				spLSIAsControl.HorizontalContentAlignment = horizontalAlignment;

				padding = spThisAsControl.Padding;
				spLSIAsControl.Padding = padding;

				// The event will be disconnected when the item is destroyed. No
				// need to keep track of the token.
				//(spLSIAsUIElt.add_Tapped(wrl.Callback<nput.ITappedEventHandler>
				//		(this, &LoopingSelector.OnItemTapped),
				//	&tappedToken));
				spLSIAsUIElt.Tapped += OnItemTapped;

				spLSIAsControl.ApplyTemplate(visualTreeRebuilt);
			}
			else
			{
				FrameworkElement spLSIAsFE;

				lsi = (LoopingSelectorItem)(spLoopingSelectorItem);

				if (wasItemRecycled)
				{
					_recycledItems.pop_back();
				}

				//spLoopingSelectorItem.As(spLoopingSelectorItemAsCC);
				spLoopingSelectorItemAsCC = spLoopingSelectorItem;
				//spLoopingSelectorItem.As(spLoopingSelectorItemAsDO);
				spLoopingSelectorItemAsDO = spLoopingSelectorItem;
				//spLoopingSelectorItem.As(spLSIAsFE);
				spLSIAsFE = spLoopingSelectorItem;
				spLSIAsFE.Width = _itemWidth;
			}

			// Retrieve the data item and set it as content.
			IList<object> spItemsCollection;
			spItemsCollection = Items;
			DependencyObject spItem;
			//spItemsCollection.GetAt(moddedItemIdx, &spItem);
			spItem = spItemsCollection[(int)moddedItemIdx];
			spLoopingSelectorItemAsCC.Content = spItem;

			//xaml_automation.IAutomationPropertiesStatics> spAutomationPropertiesStatics;
			//(wf.GetActivationFactory(
			//	wrl_wrappers.Hstring(RuntimeClass_Microsoft_UI_Xaml_Automation_AutomationProperties),
			//	&spAutomationPropertiesStatics));

			// To get the position in set, we add 1 to the item index - this is so Narrator announces
			// (e.g.) "1 of 30" for the item at index 0, since "0 of 30" through "29 of 30" would be
			// very unexpected to users.
			int itemCount = 0;
			itemCount = spItemsCollection.Count;
			//spAutomationPropertiesStatics.SetPositionInSet(spLoopingSelectorItemAsDO, moddedItemIdx + 1);
			AutomationProperties.SetPositionInSet(spLoopingSelectorItemAsDO, (int)moddedItemIdx + 1);
			//spAutomationPropertiesStatics.SetSizeOfSet(spLoopingSelectorItemAsDO, itemCount);
			AutomationProperties.SetSizeOfSet(spLoopingSelectorItemAsDO, (int)itemCount);

			lsi.SetVisualIndex(itemIdxToRealize);

			if (_itemState == ItemState.Expanded || _itemState == ItemState.ManipulationInProgress ||
				_itemState == ItemState.LostFocus)
			{
				lsi.SetState(LoopingSelectorItem.State.Expanded, false);
			}
			else
			{
				lsi.SetState(LoopingSelectorItem.State.Normal, false);
			}

			lsi.AutomationUpdatePeerIfExists(moddedItemIdx);
			spLoopingSelectorItem.CopyTo(ppItem);
		}

		void RecycleItem(LoopingSelectorItem pItem)
		{

			LoopingSelectorItem spItemAsLSI;
			UIElement spItemAsUI;

			spItemAsLSI = pItem;
			//spItemAsLSI.As(spItemAsUI);
			spItemAsUI = spItemAsLSI;

			_recycledItems.push_back(pItem);

			// Removing from the visual tree is expensive. Place offscreen instead.
			//NT_global.System.Diagnostics.Debug.Assert(_spCanvasStatics);
			//_spCanvasStatics.SetLeft(spItemAsUI, -10000);
			Canvas.SetLeft(spItemAsUI, -10000);
		}

		#region Helpers

		void HasFocus(out bool pHasFocus)
		{
			DependencyObject spFocusedElt;
			DependencyObject spFocusedEltAsDO;

			pHasFocus = false;

			UIElement thisAsUIE;
			//this.QueryInterface(IID_PPV_ARGS(thisAsUIE));
			thisAsUIE = this;

			XamlRoot xamlRoot;
			xamlRoot = thisAsUIE.XamlRoot;

			if (xamlRoot is { })
			{
				//xaml_input.IFocusManagerStatics> spFocusManager;
				//(wf.GetActivationFactory(
				//	wrl_wrappers.Hstring(RuntimeClass_Microsoft_UI_Xaml_Input_FocusManager),
				//	&spFocusManager));

				//spFocusManager.GetFocusedElementWithRoot(xamlRoot, &spFocusedElt);
				FocusManager.GetFocusedElementWithRoot(xamlRoot, out spFocusedElt);

				if (spFocusedElt is { })
				{
					//spFocusedElt.As(spFocusedEltAsDO);
					spFocusedEltAsDO = spFocusedElt;
					IsAscendantOfTarget(spFocusedEltAsDO, out pHasFocus);
				}
			}

			return;
		}

		void IsAscendantOfTarget(DependencyObject pChild, out bool pIsChildOfTarget)
		{
			DependencyObject spCurrentDO = pChild;
			DependencyObject spParentDO;
			DependencyObject spThisAsDO;
			//xaml_media.IVisualTreeHelperStatics> spVTHStatics;

			bool isFound = false;

			//(wf.GetActivationFactory(
			//	wrl_wrappers.Hstring(RuntimeClass_Microsoft_UI_Xaml_Media_VisualTreeHelper),
			//	&spVTHStatics));
			//QueryInterface(__uuidof(DependencyObject), &spThisAsDO);
			spThisAsDO = this;
			while (spCurrentDO is { } && !isFound)
			{
				if (spCurrentDO == spThisAsDO)
				{
					isFound = true;
				}
				else
				{
					//spVTHStatics.GetParent(spCurrentDO, spParentDO);
					spParentDO = VisualTreeHelper.GetParent(spCurrentDO);
					spCurrentDO.Attach(spParentDO.Detach());
				}
			}

			pIsChildOfTarget = isFound;
		}

		void ShiftChildren(double delta)
		{
			LoopingSelectorItem.iterator iter;

			for (iter = _realizedItems.begin(); iter != _realizedItems.end(); iter++)
			{
				LoopingSelectorItem spChild;
				UIElement spChildAsUI;
				double currentPosition = 0.0;
				// This keeps the count unchanged. Attach doesn't
				// AddRef, and Detech doesn't Release.
				spChild.Attach(iter);
				//spChild.As(spChildAsUI);
				spChildAsUI = spChild;
				//NT_global.System.Diagnostics.Debug.Assert(_spCanvasStatics);
				//_spCanvasStatics.GetTop(spChildAsUI, &currentPosition);
				currentPosition = Canvas.GetTop(spChildAsUI);
				//_spCanvasStatics.SetTop(spChildAsUI, currentPosition + delta);
				Canvas.SetTop(spChildAsUI, currentPosition + delta);
				spChild.Detach();
			}
		}

		void MeasureExtent(out double extentTop, out double extentBottom)
		{

			double viewportHeight = 0.0;
			double verticalOffset = 0.0;

			viewportHeight = _tpScrollViewer.ViewportHeight;
			verticalOffset = _tpScrollViewer.VerticalOffset;
			extentTop = verticalOffset;
			extentBottom = verticalOffset + viewportHeight;
		}

		void ClearAllItems()
		{
			IList<object> spItems;
			LoopingSelectorItem.iterator iter;

			for (iter = _realizedItems.begin(); iter != _realizedItems.end(); iter++)
			{
				RecycleItem(iter);
				_realizedBottom -= _scaledItemHeight;
				_realizedBottomIdx--;
			}
			_realizedItems.clear();
			_realizedItemsForAP.clear();

			spItems = Items;
			if (spItems is { })
			{
				// We reset the logical indices to not contain any extra multiples of
				// the item count. This makes scenarios where the items collection is
				// changed while the user is manipulating the control do the 'expected'
				// thing and not jump around.
				int itemCount = 0;
				int indexDelta = 0;
				itemCount = spItems.Count;
				_itemCount = (uint)itemCount;
				indexDelta = _realizedMidpointIdx - (int)PositiveMod(_realizedMidpointIdx, (int)_itemCount);

				_realizedMidpointIdx -= indexDelta;
				_realizedTopIdx -= indexDelta;
				_realizedBottomIdx -= indexDelta;
			}
		}

		void GetPanelChildren(out IList<UIElement> ppChildren, out uint count)
		{
			Panel spPanel;
			IList<UIElement> spChildren;

			//_tpPanel.As(spPanel);
			spPanel = _tpPanel;
			spChildren = spPanel.Children;
			//spChildren.get_Size(count);
			count = (uint)spChildren.Count;
			ppChildren = spChildren.Detach();
		}

		void SizePanel()
		{
			FrameworkElement spPanelAsFE;
			UIElement spThisAsUI;

			bool shouldLoop = false;
			shouldLoop = ShouldLoop;
			//QueryInterface(__uuidof(UIElement), &spThisAsUI);
			spThisAsUI = this;
			//_tpPanel.As(spPanelAsFE);
			spPanelAsFE = _tpPanel;
			if (shouldLoop)
			{
				double scrollViewerHeight = 0.0;
				scrollViewerHeight = _tpScrollViewer.ViewportHeight;
				// This is a large number. It is large enough to ensure for any
				// item size the panel size exceeds that which is reasonable
				// to expect the user to flick to the end of continuously while
				// not allowing a manipulation to complete.
				//
				// It is odd so the panel sizes correctly. The
				// midpoint aligns with the snap points and visual item realization
				// position.
				_panelSize = scrollViewerHeight + (1001) * _scaledItemHeight;
			}
			else
			{
				double scrollViewerHeight = 0.0;

				scrollViewerHeight = _tpScrollViewer.ViewportHeight;
				_panelSize = scrollViewerHeight + (_itemCount - 1) * _scaledItemHeight;

				// WPB# 264945
				// on high resolution device, actual height will be rounded based
				// on the plateau scale factor, sometime the panel's actual height
				// is less than given height. In this case the items can't fit in
				// Panel and cause items shifting up a bit. This will trigger a
				// view changing event and the items are expanded when
				// LoopingSelector is loaded.
				// So here we should give panel 1 more pixel to make sure items
				// can fill in the panel.
				_panelSize += 1.0;
			}

			spPanelAsFE.Height = (double)(_panelSize);
		}

		void SetScrollPosition(double offset, bool useAnimation)
		{
			DependencyObject spVerticalOffsetAsInspectable;
			double spVerticalOffset;

			Private.ValueBoxer.Createdouble(offset, &spVerticalOffsetAsInspectable);
			spVerticalOffsetAsInspectable.As(spVerticalOffset);
			//LSTRACE("[%d] Setting scroll position %f", (((int)this) >> 8) & 0xFF, offset);


			_skipNextBalance = true;

			if (!useAnimation)
			{
				bool didSucceed = false;
				// We use this booleaen as a performance optimization. When
				// this function is called with useAnimation set to false it
				// is an instantaneous jump, and balance will happen afterwards.
				_isWithinScrollChange = true;
				(_tpScrollViewer.ChangeViewWithOptionalAnimation(
					null, spVerticalOffset, null, true /* disableAnimation */, &didSucceed));

				// If ChangeView doesn't succeed it implies the ScrollViewer is no longer in the visual tree.
				// We delay the setting of the scroll position until after it's placed in the visual tree again.
				if (!didSucceed)
				{
					_delayScrollPositionY = offset;
				}
				_isWithinScrollChange = false;
			}
			else
			{
				// We call animate from the OnTapped event of children elements. This event
				// is generated when the gesture recognizer processes the PointerReleased event.
				// Unfortunately at this point in time the InputManager hasn't informed itself
				// the viewport is no longer active. As a result ScrollViewer will skip
				// the animation. To avoid this we schedule the ChangeView on the core dispatcher
				// for execution immediately after this tick completes.
				wsy.IDispatcherQueueStatics> spDispatcherQueueStatics;
				wsy.IDispatcherQueue> spDispatcherQueue;
				bool enqueued;
				LoopingSelector> spThis(this);
				wrl.WeakRef wrThis;

				spThis.AsWeak(wrThis);
				(wf.GetActivationFactory(
					wrl_wrappers.Hstring(RuntimeClass_Windows_System_DispatcherQueue),
					&spDispatcherQueueStatics));
				spDispatcherQueueStatics.GetForCurrentThread(spDispatcherQueue);
				(spDispatcherQueue.TryEnqueue(
					WRLHelper.MakeAgileCallback<wsy.IDispatcherQueueHandler>([wrThis, spVerticalOffset]() mutable {

					bool returnValue = false;
					ILoopingSelector> spThis;
					wrThis.As(spThis);
					if (spThis)
					{
						(((LoopingSelector)(spThis))._tpScrollViewer.ChangeViewWithOptionalAnimation(
							null, spVerticalOffset, null, false /* disableAnimation */, &returnValue));
					}
				}),
			&enqueued));
				IFCEXPECT(enqueued);
			}
		}

		void SetupSnapPoints(double offset, double size)
		{


			LoopingSelectorPanel lsp = null;
			lsp = (LoopingSelectorPanel)(_tpPanel);

			lsp.SetOffsetInPixels((float)(offset));
			lsp.SetSizeInPixels((float)(size));
		}

		void SetPosition(UIElement pitem, double offset)
		{
			//NT_global.System.Diagnostics.Debug.Assert(_spCanvasStatics);
			//_spCanvasStatics.SetTop(pitem, offset);
			Canvas.SetTop(pitem, offset);
			// Items are set offset with a large left offset
			// when recycled.
			//_spCanvasStatics.SetLeft(pitem, 0.0);
			Canvas.SetLeft(pitem, 0.0d);
		}

		void ExpandIfNecessary()
		{
			if (_itemState != ItemState.Expanded)
			{
				TransitionItemsState(ItemState.Expanded);
				_itemState = ItemState.Expanded;
			}
		}

		void TransitionItemsState(ItemState state)
		{
			uint childIdx = 0;
			LoopingSelectorItem>.iterator iter;

			for (iter = _realizedItems.begin(); iter != _realizedItems.end(); iter++)
			{
				LoopingSelectorItem lsi = (LoopingSelectorItem)(iter);

				if (state == ItemState.ManipulationInProgress)
				{
					lsi.SetState(LoopingSelectorItem.State.Expanded, true);
				}
				else if (state == ItemState.Expanded)
				{
					if (_realizedTopIdx + (_realizedItems.size() - 1 - childIdx) == (uint)(_realizedMidpointIdx))
					{
						lsi.SetState(LoopingSelectorItem.State.Selected, true);
					}
					else
					{
						lsi.SetState(LoopingSelectorItem.State.Expanded, true);
					}
				}
				else // collapsed
				{
					if (_realizedTopIdx + (_realizedItems.size() - 1 - childIdx) == (uint)(_realizedMidpointIdx))
					{
						lsi.SetState(LoopingSelectorItem.State.Selected, true);
					}
					else
					{
						lsi.SetState(LoopingSelectorItem.State.Normal, true);
					}
				}

				childIdx++;
			}
		}

		void AutomationGetSelectedItem(out LoopingSelectorItem ppItemNoRef)
		{
			ppItemNoRef = null;

			for (var iter = _realizedItems.begin(); iter != _realizedItems.end(); iter++)
			{
				LoopingSelectorItem pLSINoRef = (LoopingSelectorItem)(iter);

				int itemVisualIndex = 0;
				pLSINoRef.GetVisualIndex(itemVisualIndex);

				uint itemIndex = 0;
				VisualIndexToItemIndex(itemVisualIndex, &itemIndex);

				// We need to make sure that we check against the selected index,
				// not the midpoint index.  Normally, the item in the center of the view
				// is also the selected item, but in the case of UI automation, it
				// is not always guaranteed to be.
				int selectedIndex = 0;
				selectedIndex = SelectedIndex;

				if (itemIndex == (uint)(selectedIndex))
				{
					ppItemNoRef = pLSINoRef;
					break;
				}
			}
		}

		void RetreiveItemFromAPRealizedItems(uint moddeItemdIdx, out LoopingSelectorItem ppItem)
		{
			ppItem = null;
			std.map<int, ILoopingSelectorItem>>.iterator iter;

			iter = _realizedItemsForAP.find(moddeItemdIdx);
			if (iter != _realizedItemsForAP.end())
			{
				ppItem = iter.second.Detach();
				_realizedItemsForAP.erase(iter);
			}
		}

		#region Sound Helpers

		void RequestInteractionSound(ElementSoundKind soundKind)
		{
			DependencyObject thisAsDO;

			QueryInterface(__uuidof(DependencyObject), &thisAsDO);
			PlatformHelpers.RequestInteractionSoundForElement(soundKind, thisAsDO);
		}

		#endregion Sound Helpers

		#region AutomationInternalInterface

		void AutomationScrollToVisualIdx(int visualIdx, bool ignoreScrollingState)
		{
			bool isFullySetup = false;
			IsSetupForAutomation(isFullySetup);
			if (isFullySetup && _itemState == ItemState.Expanded)
			{
				int idxMovement = visualIdx - _realizedMidpointIdx;
				double pixelsToMove = idxMovement * _scaledItemHeight;

				SetScrollPosition(_unpaddedExtentTop + pixelsToMove, false /* use animation */ );
				Balance(true /* isOnSnapPoint */ );
				// If we aren't going to scroll at all, then we need to update the selected index,
				// since we won't get a ViewChanged event during which to do that.
				if (pixelsToMove == 0)
				{
					UpdateSelectedItem(ignoreScrollingState);
				}
			}
		}

		void AutomationGetIsScrollable(out bool pIsScrollable)
		{
			// LoopingSelector doesn't currently have a disabled
			// state so as long as the itemCount is greater than
			// zero it is scrollable.
			pIsScrollable = _itemCount > 0;
			//return S_OK;
		}

		void AutomationGetScrollPercent(out double pScrollPercent)
		{


			int selectedIndex;
			// We assume the scroll percent can be derived from the currently
			// selected item, since it's always in the middle.
			selectedIndex = SelectedIndex;
			if (selectedIndex < 0)
			{
				selectedIndex = 0;
			}

			if (_itemCount > 0)
			{
				pScrollPercent = (double)(selectedIndex) / (double)(_itemCount) * 100.0;
			}
			else
			{
				pScrollPercent = 0.0;
			}
		}

		void AutomationGetScrollViewSize(out double pScrollPercent)
		{
			bool isSetup = false;
			pScrollPercent = 100.0;
			IsSetupForAutomation(isSetup);
			if (isSetup)
			{
				double viewportHeight = _unpaddedExtentBottom - _unpaddedExtentTop;
				if (viewportHeight > 0)
				{
					pScrollPercent = viewportHeight / (_itemCount * _scaledItemHeight) * 100;
				}
			}
		}

		void AutomationSetScrollPercent(double scrollPercent)
		{
			bool isSetup = false;

			if (scrollPercent < 0.0 || scrollPercent > 100.0)
			{
				IFC_NOTRACE(UIA_E_INVALIDOPERATION);
			}

			IsSetupForAutomation(isSetup);
			if (isSetup && _itemState == ItemState.Expanded)
			{
				int itemIdxOffset = (INT)((_itemCount - 1) * scrollPercent / 100.0);
				int currentItemIdx = PositiveMod(_realizedMidpointIdx, _itemCount);
				int idxMovement = itemIdxOffset - currentItemIdx;
				double pixelsToMove = idxMovement * _scaledItemHeight;
				SetScrollPosition(_unpaddedExtentTop + pixelsToMove, false /* use animation */ );
				Balance(true /* isOnSnapPoint */ );
			}
		}

		void AutomationTryGetSelectionUIAPeer(out xaml.Automation.Peers.IAutomationPeer ppPeer)
		{
			ppPeer = null;

			LoopingSelectorItem pLSINoRef = null;
			AutomationGetSelectedItem(pLSINoRef);
			if (pLSINoRef)
			{
				xaml.Automation.Peers.FrameworkElementAutomationPeerStatics> spAutomationPeerStatics;
				xaml.Automation.Peers.IAutomationPeer> spAutomationPeer;
				UIElement spChildAsUI;

				(pLSINoRef.QueryInterface(
					__uuidof(UIElement),
					&spChildAsUI));
				(wf.GetActivationFactory(
					  wrl_wrappers.Hstring(RuntimeClass_Microsoft_UI_Xaml_Automation_Peers_FrameworkElementAutomationPeer),
					  &spAutomationPeerStatics));
				spAutomationPeerStatics.CreatePeerForElement(spChildAsUI, &spAutomationPeer);
				spAutomationPeer.CopyTo(ppPeer);
			}
		}

		void AutomationScroll(xaml.Automation.ScrollAmount scrollAmount)
		{
			bool isSetup = false;
			IsSetupForAutomation(isSetup);
			// We don't allow automation interaction when the ScrollViewer is undergoing
			// a manipulation.
			if (isSetup && _itemState == ItemState.Expanded)
			{
				double pixelsToMove = 0.0;
				int itemsToMove = 0;

				switch (scrollAmount)
				{
					case xaml.Automation.ScrollAmount_LargeDecrement:
						itemsToMove = -c_automationLargeIncrement;
						break;
					case xaml.Automation.ScrollAmount_LargeIncrement:
						itemsToMove = c_automationLargeIncrement;
						break;
					case xaml.Automation.ScrollAmount_SmallDecrement:
						itemsToMove = -c_automationSmallIncrement;
						break;
					case xaml.Automation.ScrollAmount_SmallIncrement:
						itemsToMove = c_automationSmallIncrement;
						break;
					default:
						break;
				}

				int currentIndex = PositiveMod(_realizedMidpointIdx, _itemCount);

				if (currentIndex + itemsToMove > (INT)(_itemCount - 1))
				{
					itemsToMove = _itemCount - currentIndex - 1;
				}
				else if (currentIndex + itemsToMove < 0)
				{
					itemsToMove = -currentIndex;
				}

				pixelsToMove = itemsToMove * _scaledItemHeight;

				SetScrollPosition(_unpaddedExtentTop + pixelsToMove, false /* use animation */ );
				Balance(true /* isOnSnapPoint */ );
			}
		}

		void AutomationFillCollectionWithRealizedItems(IList<DependencyObject pVector)
		{
			// When the number of items is smaller than the number of items LoopingSelector calculates it
			// needs to realize to ensure gapless scrolling the realizedItem list will contain duplicate items.
			// This will cause infinite loops when UIA clients attempt to find an element because of the logic UIAutomationCore
			// uses to traverse the UIA tree. Because the realizedItem list is in order we simply add items until we reach
			// a point where we've added either all available items or added the number of items in the data list.
			uint counter = 0;
			for (var iter = _realizedItems.begin(); iter != _realizedItems.end() && counter < _itemCount; iter++)
			{
				ContentControl> spChildAsCC;
				DependencyObject spChildContent;

				counter++;

				((iter).QueryInterface<ContentControl>(
					&spChildAsCC));
				spChildContent = spChildAsCC.Content;
				pVector.Append(spChildContent);
			}
		}

		void AutomationTryScrollItemIntoView(DependencyObject pItem)
		{
			uint index = 0;
			bool found = false;
			IList<DependencyObject> spVector;
			spVector = Items;
			spVector.IndexOf(pItem, &index, &found);
			if (found)
			{
				_skipSelectionChangeUntilFinalViewChanged = true;

				// The _realizedMidpointIdx points to the currently selected item's visual index. Because the visual index
				// always starts at the first item we subtract it from itself modded with the item count to obtain the
				// nearest first item visual index.
				int desiredVisualIdx = _realizedMidpointIdx - PositiveMod(_realizedMidpointIdx, _itemCount) + index;
				AutomationScrollToVisualIdx(desiredVisualIdx);
			}

			// Cleanup
			// return hr;
		}

		void AutomationRaiseSelectionChanged()
		{


			UIElement spThisAsUI;
			ScrollPatternIdentifiersStatics spScrollPatternStatics;
			AutomationProperty spAutomationScrollProperty;

			UIElement spSelectedItemAsUI;
			LoopingSelectorItem pLSINoRef = null;

			DependencyObject spNewScrollValue;

			double scrollPercent = 0.0;

			(QueryInterface(
				__uuidof(UIElement),
				&spThisAsUI));

			(wf.GetActivationFactory(
				wrl_wrappers.Hstring(RuntimeClass_Microsoft_UI_Xaml_Automation_ScrollPatternIdentifiers),
				&spScrollPatternStatics));
			spAutomationScrollProperty = spScrollPatternStatics.VerticalScrollPercentProperty;
			AutomationGetScrollPercent(scrollPercent);
			Private.ValueBoxer.Createdouble(scrollPercent, &spNewScrollValue);
			AutomationGetSelectedItem(pLSINoRef);
			if (pLSINoRef)
			{
				(pLSINoRef.QueryInterface(
					__uuidof(UIElement),
					&spSelectedItemAsUI));

				(Private.AutomationHelper.RaiseEventIfListener(
					spSelectedItemAsUI,
					xaml.Automation.Peers.AutomationEvents_SelectionItemPatternOnElementSelected));
				(Private.AutomationHelper.SetAutomationFocusIfListener(
					spSelectedItemAsUI));
			}

		   (Private.AutomationHelper.RaisePropertyChangedIfListener(
			   spThisAsUI,
			   spAutomationScrollProperty,
			   _tpPreviousScrollPosition,
			   spNewScrollValue));

			_tpPreviousScrollPosition = spNewScrollValue;
		}

		void AutomationRaiseExpandCollapse()
		{
			UIElement spThisAsUI;
			ExpandCollapsePatternIdentifiersStatics> spExpandCollapsePatternStatics;
			AutomationProperty> spAutomationProperty;

			DependencyObject spOldValue;
			DependencyObject spNewValue;
			wf.IPropertyValue> spOldValueAsPV;
			wf.IPropertyValue> spNewValueAsPV;

			(QueryInterface(
				__uuidof(UIElement),
				&spThisAsUI));

			(wf.GetActivationFactory(
				wrl_wrappers.Hstring(RuntimeClass_Microsoft_UI_Xaml_Automation_ExpandCollapsePatternIdentifiers),
				&spExpandCollapsePatternStatics));

			spAutomationProperty = spExpandCollapsePatternStatics.ExpandCollapseStateProperty;
			(Private.ValueBoxer.CreateReference<xaml.Automation.ExpandCollapseState>
				(xaml.Automation.ExpandCollapseState_Collapsed, &spOldValueAsPV));
			(Private.ValueBoxer.CreateReference<xaml.Automation.ExpandCollapseState>
				(xaml.Automation.ExpandCollapseState_Expanded, &spNewValueAsPV));

			spOldValueAsPV.As(spOldValue);
			spNewValueAsPV.As(spNewValue);
			(Private.AutomationHelper.RaisePropertyChangedIfListener(
				spThisAsUI,
				spAutomationProperty,
				spOldValue,
				spNewValue));
		}

		void AutomationRaiseStructureChanged()
		{
			UIElement spThisAsUI;
			(QueryInterface(
				__uuidof(UIElement),
				&spThisAsUI));

			// The visible children has changed. Notify UIA of
			// a new structure.
			(Private.AutomationHelper.RaiseEventIfListener(
				spThisAsUI,
				xaml.Automation.Peers.AutomationEvents_StructureChanged));
		}

		void AutomationGetContainerUIAPeerForItem(
			DependencyObject pItem,
		   out LoopingSelectorItemAutomationPeer ppPeer)
		{
			ppPeer = null;
			ILoopingSelectorItem> spChild;

			for (var iter = _realizedItems.begin(); iter != _realizedItems.end(); iter++)
			{
				ILoopingSelectorItem> spTentativeChild(iter);
				ContentControl> spTentativeChildAsCC;
				DependencyObject spItem;
				spTentativeChild.As(spTentativeChildAsCC);

				spItem = spTentativeChildAsCC.Content;

				if (spItem == pItem)
				{
					spChild = spTentativeChild;
					break;
				}
			}

			if (!spChild)
			{
				for (var iter = _realizedItemsForAP.begin(); iter != _realizedItemsForAP.end(); iter++)
				{
					ILoopingSelectorItem> spTentativeChild(iter.second);
					ContentControl> spTentativeChildAsCC;
					DependencyObject spItem;
					spTentativeChild.As(spTentativeChildAsCC);

					spItem = spTentativeChildAsCC.Content;

					if (spItem == pItem)
					{
						spChild = spTentativeChild;
						break;
					}
				}
			}

			if (spChild)
			{
				UIElement spChildAsUIElt;
				AutomationPeer> spChildAP;
				LoopingSelectorItemAutomationPeer> spChildAPAsLSIAP;
				spChild.As(spChildAsUIElt);
				(Private.AutomationHelper.CreatePeerForElement(spChildAsUIElt,
					&spChildAP));
				spChildAP.As(spChildAPAsLSIAP);

				ppPeer = spChildAPAsLSIAP.Detach();
			}

			//return S_OK;
		}

		void AutomationClearPeerMap()
		{
			LoopingSelectorAutomationPeer spLSAP;
			_wrAP.CopyTo<LoopingSelectorAutomationPeer>(spLSAP);
			if (spLSAP)
			{
				(LoopingSelectorAutomationPeer)(spLSAP).ClearPeerMap();
			}
		}

		void AutomationRealizeItemForAP(uint itemIdxToRealize)
		{
			ILoopingSelectorItem> spItem;
			RealizeItem(itemIdxToRealize, &spItem);
			_realizedItemsForAP[itemIdxToRealize] = spItem;
			//return S_OK;
		}

		#endregion

	}
}

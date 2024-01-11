using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Windows.Foundation;
using Windows.System;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Uno.Extensions;
using Uno.UI;

#if HAS_UNO_WINUI
using Microsoft.UI.Input;
#else
using Windows.Devices.Input;
using Windows.UI.Input;
#endif

namespace Microsoft.UI.Xaml.Controls.Primitives
{
	partial class LoopingSelector
	{
		private const string c_scrollViewerTemplatePart = "ScrollViewer";
		private const string c_upButtonTemplatePart = "UpButton";
		private const string c_downButtonTemplatePart = "DownButton";
		//private const double c_targetScreenWidth = 400.0;

		internal LoopingSelector()
		{
			this.RegisterDisposablePropertyChangedCallback((i, s, e) => OnPropertyChanged(e));

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

			InitializeImpl();
		}


		void InitializeImpl()
		{
			//IControlFactory spInnerFactory;
			//IControl spInnerInstance;
			//DependencyObject spInnerInspectable;
			//UIElement spUIElement;

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
			//        "Microsoft/* UWP don't rename */.UI.Xaml.Controls.Primitives.LoopingSelector"));
			DefaultStyleKey = typeof(LoopingSelector);

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

			//
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
			IList<object> spRemovedItems;
			IList<object> spAddedItems;
			//DependencyObject spInner;
			//DependencyObject spThisAsInspectable;

			//wfci_.Vector<DependencyObject.Make(spRemovedItems);
			spRemovedItems = new List<object>(1);
			//wfci_.Vector<DependencyObject.Make(spAddedItems);
			spAddedItems = new List<object>(1);
			spAddedItems.Add(pNewItem);
			spRemovedItems.Add(pOldItem);
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

		#region UIElementOverrides

		protected override AutomationPeer OnCreateAutomationPeer()
		{
			var spThis = this;
			LoopingSelector spThisAsILoopingSelector;
			LoopingSelectorAutomationPeer spLoopingSelectorAutomationPeer;

			//spThis.As(spThisAsILoopingSelector);
			spThisAsILoopingSelector = spThis;
			//(wrl.MakeAndInitialize<xaml_automation_peers.LoopingSelectorAutomationPeer>
			//	(spLoopingSelectorAutomationPeer, spThisAsILoopingSelector));
			spLoopingSelectorAutomationPeer = new LoopingSelectorAutomationPeer(spThisAsILoopingSelector);

			//spLoopingSelectorAutomationPeer.CopyTo(returnValue);
			var returnValue = spLoopingSelectorAutomationPeer;
			//spLoopingSelectorAutomationPeer.AsWeak(_wrAP);
			_wrAP = new WeakReference<LoopingSelectorAutomationPeer>(spLoopingSelectorAutomationPeer);
			return returnValue;
		}

		#endregion

		void OnUpButtonClicked(
			object pSender,
			RoutedEventArgs pEventArgs)
		{
			//UNREFERENCED_PARAMETER(pSender);
			//UNREFERENCED_PARAMETER(pEventArgs);

			SelectPreviousItem();
		}

		void OnDownButtonClicked(
			object pSender,
			RoutedEventArgs pEventArgs)
		{
			//UNREFERENCED_PARAMETER(pSender);
			//UNREFERENCED_PARAMETER(pEventArgs);

			SelectNextItem();
		}


		//void OnKeyDownImpl(
		//	KeyRoutedEventArgs pEventArgs)
		protected override void OnKeyDown(KeyRoutedEventArgs pEventArgs)
		{
			var bHandled = false;
			var key = VirtualKey.None;
			VirtualKeyModifiers nModifierKeys;

			if (pEventArgs == null)
			{
				throw new ArgumentNullException();
			}

			bHandled = pEventArgs.Handled;
			if (bHandled)
			{
				//goto Cleanup;
				return;
			}

			key = pEventArgs.Key;
			nModifierKeys = PlatformHelpers.GetKeyboardModifiers();
			if ((nModifierKeys & VirtualKeyModifiers.Menu) == 0)
			{
				bHandled = true;

				switch (key)
				{
					case VirtualKey.Up:
						SelectPreviousItem();
						break;
					case VirtualKey.Down:
						SelectNextItem();
						break;
					case VirtualKey.GamepadLeftTrigger:
					case VirtualKey.PageUp:
						HandlePageUpKeyPress();
						break;
					case VirtualKey.GamepadRightTrigger:
					case VirtualKey.PageDown:
						HandlePageDownKeyPress();
						break;
					case VirtualKey.Home:
						HandleHomeKeyPress();
						break;
					case VirtualKey.End:
						HandleEndKeyPress();
						break;
					default:
						bHandled = false;
						break;
				}

				pEventArgs.Handled = bHandled;
			}
		}

		void SelectNextItem()
		{
			int index;
			var shouldLoop = false;

			shouldLoop = ShouldLoop;
			index = SelectedIndex;

			//Don't scroll past the end of the list if we are not looping:
			if (index < (int)_itemCount - 1 || shouldLoop)
			{
				var pixelsToMove = _scaledItemHeight;
				SetScrollPosition(_unpaddedExtentTop + pixelsToMove, true /* use animation */);
				RequestInteractionSound(ElementSoundKind.Focus);
			}
		}

		void SelectPreviousItem()
		{
			int index;
			var shouldLoop = false;

			shouldLoop = ShouldLoop;
			index = SelectedIndex;

			//Don't scroll past the start of the list if we are not looping
			if (index > 0 || shouldLoop)
			{
				var pixelsToMove = -1 * _scaledItemHeight;
				SetScrollPosition(_unpaddedExtentTop + pixelsToMove, true /* use animation */);
				RequestInteractionSound(ElementSoundKind.Focus);
			}
		}

		void HandlePageDownKeyPress()
		{
			var viewportHeight = _unpaddedExtentBottom - _unpaddedExtentTop;
			var pixelsToMove = viewportHeight / 2;
			SetScrollPosition(_unpaddedExtentTop + pixelsToMove, true /* use animation */);
			RequestInteractionSound(ElementSoundKind.Focus);
		}

		void HandlePageUpKeyPress()
		{
			var viewportHeight = _unpaddedExtentBottom - _unpaddedExtentTop;
			var pixelsToMove = -1 * viewportHeight / 2;
			SetScrollPosition(_unpaddedExtentTop + pixelsToMove, true /* use animation */);
			RequestInteractionSound(ElementSoundKind.Focus);
		}

		void HandleHomeKeyPress()
		{
			var pixelsToMove = -1 * _realizedMidpointIdx * _scaledItemHeight;
			SetScrollPosition(_unpaddedExtentTop + pixelsToMove, false /* use animation */);
			Balance(true /* isOnSnapPoint */);
			RequestInteractionSound(ElementSoundKind.Focus);
		}

		void HandleEndKeyPress()
		{
			var idxMovement = (int)_itemCount - 1 - _realizedMidpointIdx;
			var pixelsToMove = idxMovement * _scaledItemHeight;
			SetScrollPosition(_unpaddedExtentTop + pixelsToMove, false /* use animation */);
			Balance(true /* isOnSnapPoint */);
			RequestInteractionSound(ElementSoundKind.Focus);
		}

		void OnViewChanged(
			object pSender,
			ScrollViewerViewChangedEventArgs pEventArgs)
		{
			void ProcessEvent()
			{
				//UNREFERENCED_PARAMETER(pSender);

				// In the new ScrollViwer2 interface OnViewChanged will actually
				// fire for all the intermediate ViewChanging events as well.
				// We only capture the final view.
				var isIntermediate = false;
				isIntermediate = pEventArgs.IsIntermediate;
				if (!isIntermediate)
				{
					//LSTRACE("[%d] OnViewChanged called.", (((int)this) >> 8) & 0xFF);

					if (!_isWithinScrollChange && !_isWithinArrangeOverride)
					{
						// --------------------
						// 2021-03-17 / Uno 3.6
						// --------------------
						// Uno: There's a difference between Windows and Uno here.
						// On Uno, the ViewChanging event is not raised from the ScrollViewer
						// and the ViewChanged is called for every change in the scroll position.
						// On Windows, the ViewChanged is called only with values on "snap point".
						// So letting the isOnSnapPoint to true here is causing strange problems in Uno.

						//Balance(true /* isOnSnapPoint */);
						Balance(false);
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
			}

			// This event can be raised synchronously
			// with the ScrollViewer's ChangeView().
			// It must be delayed to prevent incorrect re-selection of another value.
			Windows.System.DispatcherQueue.GetForCurrentThread().TryEnqueue(ProcessEvent);
		}

		void OnViewChanging(
			object pSender,
			ScrollViewerViewChangingEventArgs pEventArgs)
		{
			//UNREFERENCED_PARAMETER(pSender);
			//UNREFERENCED_PARAMETER(pEventArgs);


			////LSTRACE("[%d] OnViewChanging called.", (((int)this) >> 8) & 0xFF);

			if (!_isWithinScrollChange && !_isWithinArrangeOverride)
			{
				Balance(false /* isOnSnapPoint */);
				// The Focus transition doesn't occur sometimes when flicking
				// on one ScrollViewer, than another before the first flick completes.
				// This is a possible ScrollViewer bug. Any time a manipulation
				// occurs we force focus.
				if (_itemState != ItemState.LostFocus && !_hasFocus)
				{
					Control spThisAsControl;
					//UIElement spThisAsElement;
					var didSucceed = false;
					var focusState = FocusState.Unfocused;
					//QueryInterface(__uuidof(Control), &spThisAsControl);
					spThisAsControl = this;
					//QueryInterface(__uuidof(UIElement), &spThisAsElement);
					//spThisAsElement = this;
					//focusState = spThisAsElement.FocusState;
					focusState = FocusState;
					if (focusState == FocusState.Unfocused)
					{
						didSucceed = spThisAsControl.Focus(FocusState.Programmatic);
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
		}

		void OnPointerEntered(
			object pSender,
			PointerRoutedEventArgs pEventArgs)
		{
			//UNREFERENCED_PARAMETER(pSender);
			//UNREFERENCED_PARAMETER(pEventArgs);
			var nPointerDeviceType = PointerDeviceType.Touch;
			PointerPoint spPointerPoint;
			Windows.Devices.Input.PointerDevice spPointerDevice;

			spPointerPoint = pEventArgs.GetCurrentPoint(null);
			if (spPointerPoint == null)
			{
				throw new ArgumentNullException();
			}

			spPointerDevice = spPointerPoint.PointerDevice;
			if (spPointerDevice == null)
			{
				throw new ArgumentNullException();
			}

			nPointerDeviceType = (PointerDeviceType)spPointerDevice.PointerDeviceType;
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

			var hasFocus = false;
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
			var hasFocus = false;
			HasFocus(out hasFocus);

			if (hasFocus)
			{
				_hasFocus = true;
			}
		}

		void OnItemTapped(
			object pSender,
			TappedRoutedEventArgs pArgs)
		{
			if (_itemState == ItemState.Expanded)
			{
				LoopingSelectorItem spLsiInterface;
				//pSender.QueryInterface<LoopingSelectorItem>(spLsiInterface);
				spLsiInterface = pSender as LoopingSelectorItem;

				var lsiPtr = spLsiInterface;

				var itemVisualIndex = 0;
				uint itemIndex = 0;
				lsiPtr.GetVisualIndex(out itemVisualIndex);
				VisualIndexToItemIndex((uint)itemVisualIndex, out itemIndex);

				// We need to make sure that we check against the selected index,
				// not the midpoint index.  Normally, the item in the center of the view
				// is also the selected item, but in the case of UI automation, it
				// is not always guaranteed to be.
				var selectedIndex = 0;
				selectedIndex = SelectedIndex;

				if (itemIndex == (uint)selectedIndex)
				{
					ExpandIfNecessary();
				}
				else
				{
					// Tapping any other time scrolls to that item.
					var pixelsToMove = (itemVisualIndex - _realizedMidpointIdx) * _scaledItemHeight;
					SetScrollPosition(_unpaddedExtentTop + pixelsToMove, true /* use animation */);
				}
			}
		}

		void OnPropertyChanged(DependencyPropertyChangedEventArgs pArgs)
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
				Balance(false /* isOnSnapPoint */);
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
				Balance(false /* isOnSnapPoint */);
			}
			else if (spPropertyInfo == SelectedIndexProperty && !_disablePropertyChange)
			{
				////LSTRACE("[%d] Selected Index property refreshed.", (((int)this) >> 8) & 0xFF);
				int spValue;
				int spReferenceInt;
				var newIdx = 0;
				var oldIdx = 0;

				spValue = (int)pArgs.NewValue;
				//spValue.As(spReferenceInt);
				spReferenceInt = spValue;
				newIdx = spReferenceInt; //.Value;
				spValue = (int)pArgs.OldValue;
				//spValue.As(spReferenceInt);
				spReferenceInt = spValue;
				oldIdx = spReferenceInt; //.Value;
										 // NOTE: This call only will be applied when the
										 // scrollviewer is not being manipulated. Once a
										 // manipulation has begun the user's eventual
										 // selection takes dominance.
				SetSelectedIndex(oldIdx, newIdx);
				Balance(false /* isOnSnapPoint */);
			}
		}

		void IsTemplateAndItemsAttached(out bool result)
		{
			IList<object> spItems;

			result = false;

			if (ItemHeight == 0)
			{
				return; // not ready yet...
			}

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
			var isTemplateAndItemsAttached = false;
			IsTemplateAndItemsAttached(out isTemplateAndItemsAttached);
			isSetup = isTemplateAndItemsAttached && _isSized && !_isSetupPending;
		}

		void Balance(bool isOnSnapPoint)
		{
			var isTemplateAndItemsAttached = false;
			var abortBalance = false;

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

				var maxTopIdx = 0;
				var maxBottomIdx = 0;
				var paddedExtentTop = 0.0;
				var paddedExtentBottom = 0.0;
				var viewportHeight = 0.0;

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
				while (_realizedTop < paddedExtentTop + 1.0 - _scaledItemHeight && _realizedItems.Count > 0)
				{
					Trim(ListEnd.Head);
					headTrim++;
				}

				while (_realizedBottom > paddedExtentBottom - 1.0 + _scaledItemHeight && _realizedItems.Count > 0)
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
			var shouldLoop = false;

			shouldLoop = ShouldLoop;
			// Only normalize if strayed 50px+ from center and if
			// in looping mode. Nonlooping selectors don't normalize.
			if (shouldLoop && Math.Abs(_unpaddedExtentTop - _panelMidpointScrollPosition) > 50.0)
			{
				var delta = _panelMidpointScrollPosition - _unpaddedExtentTop;

				// WORKAROUND: It's likely there's a bug in dmanip that causes it to
				// end a manipulation not on a snap point when two fingers are on the
				// scrollviewer. We explicitly make sure we are on a snappoint here.
				// Delaying bug filing until our input system is more final.
				// DManip work tracked by WPBLUE: 11547.
				var isActuallyOnSnapPoint =
					Math.Abs(delta / _scaledItemHeight - Math.Floor(delta / _scaledItemHeight)) < 0.001;

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
			var selectedIdx = 0;

			selectedIdx = SelectedIndex;
			if (!_isSized)
			{
				//LSTRACE("[%d] Sizing panel.", (((int)this) >> 8) & 0xFF);
				var itemDimAsInt = 0;
				// Optimization: caching itemHeight and itemWidth.
				itemDimAsInt = ItemHeight;
				_itemHeight = itemDimAsInt;
				_scaledItemHeight = itemDimAsInt;

				itemDimAsInt = ItemWidth;
				if (itemDimAsInt == 0)
				{
					// If we don't have an explictly set ItemWidth, we fallback to this value which is computed during Arrange.
					_itemWidth = _itemWidthFallback;
				}
				else
				{
					_itemWidth = itemDimAsInt;
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
				var viewportHeight = 0.0;
				var verticalOffset = 0.0;
				var newScrollPosition = 0.0;
				var startPoint = 0.0;
				var shouldLoop = false;

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
					startPoint = (_panelSize - _itemCount * _scaledItemHeight) / 2;
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
			var pixelsToMove = 0.0;
			var isTemplateAndItemsAttached = false;

			IsTemplateAndItemsAttached(out isTemplateAndItemsAttached);
			// Only set the new index position if we're in the idle position
			// and the control is properly initialized and the oldIndex is meanful.
			if (oldIdx != -1 && isTemplateAndItemsAttached && _itemState == ItemState.Expanded)
			{
				//LSTRACE("[%d] SetSelectedIndex From %d To %d called.", (((int)this) >> 8) & 0xFF, oldIdx, newIdx);
				pixelsToMove = (newIdx - oldIdx) * _scaledItemHeight;
				SetScrollPosition(_unpaddedExtentTop + pixelsToMove, false);
			}
		}

		void Trim(ListEnd end)
		{
			LoopingSelectorItem spChildAsLSI;

			if (_realizedItems.Count == 0)
			{
				//LSTRACE("[%d] Trim called with empty list.", (((int)this) >> 8) & 0xFF);
				//goto Cleanup;
				return;
			}

			if (end == ListEnd.Head)
			{
				//COMPtr assignment causes AddRef.
				spChildAsLSI = _realizedItems.Last.Value;
				//_realizedItems.pop_back();
				_realizedItems.RemoveLast();
			}
			else
			{
				//spChildAsLSI = _realizedItems.Peek();
				//_realizedItems.erase(_realizedItems.begin());
				spChildAsLSI = _realizedItems.First.Value;
				_realizedItems.RemoveFirst();
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
				_realizedItems.AddLast(spChild);
				spChildAsUI = spChild;
				SetPosition(spChildAsUI, _realizedTop - _scaledItemHeight);
				_realizedTop -= _scaledItemHeight;
				_realizedTopIdx--;
			}
			else
			{
				RealizeItem((uint)(_realizedBottomIdx + 1), out spChild);
				// Panel's Children keeps the reference to this item.
				_realizedItems.AddFirst(spChild);
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
		void UpdateSelectedItem(bool ignoreScrollingState = false)
		{
			IList<object> spItemsCollection;
			DependencyObject spSelectedItem;
			DependencyObject spPreviouslySelectedItem;

			uint itemCount = 0;
			var midpoint = 0.0;
			uint newIdx = 0;
			var oldIdx = 0;

			// This will be in the middle of the currently selected item.
			midpoint = (_unpaddedExtentTop + _unpaddedExtentBottom) / 2 - _realizedTop;
			newIdx = (uint)_realizedTopIdx +
					 (uint)midpoint / (uint)_scaledItemHeight;

			spItemsCollection = Items;
			itemCount = (uint)spItemsCollection.Count;
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
			UpdateVisualSelectedItem((uint)_realizedMidpointIdx, newIdx);
			_realizedMidpointIdx = (int)newIdx;

			if (ignoreScrollingState || !_skipSelectionChangeUntilFinalViewChanged)
			{
				newIdx = PositiveMod((int)newIdx, (int)itemCount);

				spSelectedItem = spItemsCollection[(int)newIdx] as DependencyObject;
				_disablePropertyChange = true;
				oldIdx = SelectedIndex;
				SelectedIndex = (int)newIdx;
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
			if (oldIdx == newIdx)
			{
				return;
			}

			LoopingSelectorItem spEltAsLSI;
			LoopingSelectorItem lsi = null;

			var realizedItemsCount = _realizedItems.Count;
			if (realizedItemsCount > 0)
			{
				if (realizedItemsCount > oldIdx - _realizedTopIdx)
				{
					var selectionIndex = realizedItemsCount - ((int)oldIdx - _realizedTopIdx) - 1;
					spEltAsLSI = _realizedItems.ElementAt(selectionIndex % realizedItemsCount);
					lsi = spEltAsLSI;
					if (_itemState == ItemState.Expanded)
					{
						lsi.SetState(LoopingSelectorItem.State.Expanded, true);
					}
					else
					{
						lsi.SetState(LoopingSelectorItem.State.Normal, true);
					}
				}

				if (realizedItemsCount > newIdx - _realizedTopIdx)
				{
					spEltAsLSI = _realizedItems.ElementAt(realizedItemsCount - ((int)newIdx - _realizedTopIdx) - 1);
					lsi = spEltAsLSI;
					lsi.SetState(LoopingSelectorItem.State.Selected, true);
				}
			}
		}

		internal void VisualIndexToItemIndex(uint visualIndex, out uint itemIndex)
		{
			//if (itemIndex == null) { return; }

			IList<object> itemsCollection;
			itemsCollection = Items;

			var itemCount = 0;
			itemCount = itemsCollection.Count;

			itemIndex = PositiveMod((int)visualIndex, itemCount);
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

			//var wasItemRecycled = false;

			RetreiveItemFromAPRealizedItems(moddedItemIdx, out spLoopingSelectorItem);
			if (!(spLoopingSelectorItem is { }) && _recycledItems.Count != 0)
			{
				spLoopingSelectorItem = _recycledItems.Pop();
				//wasItemRecycled = true;
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
				//var visualTreeRebuilt = false;

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
				lsi = spLoopingSelectorItem;

				lsi.SetParent(this);
				spDataTemplate = ItemTemplate;
				spLoopingSelectorItemAsCC.ContentTemplate = spDataTemplate;
				spLSIAsFE.Width = _itemWidth;
				spLSIAsFE.Height = _itemHeight;
				spPanelChildren.Add(spLSIAsUIElt);

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

				//spLSIAsControl.ApplyTemplate(visualTreeRebuilt);
				spLSIAsControl.ApplyTemplate();
			}
			else
			{
				FrameworkElement spLSIAsFE;

				lsi = spLoopingSelectorItem;

				//if (wasItemRecycled)
				//{
				//	_recycledItems.pop_back();
				//}

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
			spItem = spItemsCollection[(int)moddedItemIdx] as DependencyObject;
			spLoopingSelectorItemAsCC.Content = spItem;

			//xaml_automation.IAutomationPropertiesStatics> spAutomationPropertiesStatics;
			//(wf.GetActivationFactory(
			//	wrl_wrappers.Hstring(RuntimeClass_Microsoft_UI_Xaml_Automation_AutomationProperties),
			//	&spAutomationPropertiesStatics));

			// To get the position in set, we add 1 to the item index - this is so Narrator announces
			// (e.g.) "1 of 30" for the item at index 0, since "0 of 30" through "29 of 30" would be
			// very unexpected to users.
			var itemCount = 0;
			itemCount = spItemsCollection.Count;
			//spAutomationPropertiesStatics.SetPositionInSet(spLoopingSelectorItemAsDO, moddedItemIdx + 1);
			AutomationProperties.SetPositionInSet(spLoopingSelectorItemAsDO, (int)moddedItemIdx + 1);
			//spAutomationPropertiesStatics.SetSizeOfSet(spLoopingSelectorItemAsDO, itemCount);
			AutomationProperties.SetSizeOfSet(spLoopingSelectorItemAsDO, itemCount);

			lsi.SetVisualIndex((int)itemIdxToRealize);

			if (_itemState == ItemState.Expanded || _itemState == ItemState.ManipulationInProgress ||
				_itemState == ItemState.LostFocus)
			{
				lsi.SetState(LoopingSelectorItem.State.Expanded, false);
			}
			else
			{
				lsi.SetState(LoopingSelectorItem.State.Normal, false);
			}

			lsi.AutomationUpdatePeerIfExists((int)moddedItemIdx);
			//spLoopingSelectorItem.CopyTo(ppItem);
			ppItem = spLoopingSelectorItem;
		}

		void RecycleItem(LoopingSelectorItem pItem)
		{
			LoopingSelectorItem spItemAsLSI;
			UIElement spItemAsUI;

			spItemAsLSI = pItem;
			//spItemAsLSI.As(spItemAsUI);
			spItemAsUI = spItemAsLSI;

			_recycledItems.Push(pItem);

			// Removing from the visual tree is expensive. Place offscreen instead.
			//NT_global.System.Diagnostics.Debug.Assert(_spCanvasStatics);
			//_spCanvasStatics.SetLeft(spItemAsUI, -10000);
			Canvas.SetLeft(spItemAsUI, -10000);
		}

		#region Sound Helpers

		void RequestInteractionSound(ElementSoundKind soundKind)
		{
			DependencyObject thisAsDO;

			//QueryInterface(__uuidof(DependencyObject), &thisAsDO);
			thisAsDO = this;
			PlatformHelpers.RequestInteractionSoundForElement(soundKind, thisAsDO);
		}

		#endregion Sound Helpers

		#region FrameworkElementOverrides

		protected override Size MeasureOverride(Size availableSize)
		{
			int itemWidth;
			Size returnValue = default;

			////LSTRACE("[%d] Measure called.", (((int)this) >> 8) & 0xFF);

			//LoopingSelectorGenerated.MeasureOverrideImpl(availableSize, returnValue);
			returnValue = base.MeasureOverride(availableSize);

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
			var itemWidth = 0;
			var verticalOffsetBeforeArrangeImpl = 0.0;
			var expectedOffsetChange = false;
			var widthToReturn = 0.0;

			////LSTRACE("[%d] Arrange called.", (((int)this) >> 8) & 0xFF);

			_isWithinArrangeOverride = true;

			//var guard = wil.scope_exit([this, &finalSize]()
			//{
			//	_lastArrangeSizeHeight = finalSize.Height;
			//	_isWithinArrangeOverride = false;
			//});

			try
			{

				itemWidth = ItemWidth;

				if (itemWidth != 0)
				{
					// Override the width with that of the first item's
					// width. A Canvas doesn't wrap
					// content so we do this so the control sizes correctly.
					widthToReturn = itemWidth;
				}
				else
				{
					// If no itemWidth has been set, we use all the available space.
					widthToReturn = finalSize.Width;

					// We compute a new value for _itemWidthFallback
					var newItemWidthFallback = finalSize.Width;
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
					global::System.Diagnostics.Debug.Assert(_tpScrollViewer != null);
					verticalOffsetBeforeArrangeImpl = _tpScrollViewer.VerticalOffset;
				}

				//Size returnValue = LoopingSelectorGenerated.ArrangeOverrideImpl(finalSize);
				Size returnValue = base.ArrangeOverride(finalSize);

				if (finalSize.Height != _lastArrangeSizeHeight && _isScrollViewerInitialized && !_isSetupPending)
				{
					// Orientation must have changed or we got resized, what used to be the middle point has changed.
					// So we need to shift the items to restore the old middle point item.

					var oldPanelSize = _panelSize;
					var verticalOffsetAfterArrangeImpl = 0.0;

					verticalOffsetAfterArrangeImpl = _tpScrollViewer.VerticalOffset;

					SizePanel();

					var delta = (_panelSize - oldPanelSize) / 2;
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

				returnValue.Width = widthToReturn;

				return returnValue;
			}
			finally
			{
				_lastArrangeSizeHeight = finalSize.Height;
				_isWithinArrangeOverride = false;
			}
		}

		protected override void OnApplyTemplate()
		{
			//ControlProtected> spControlProtected;
			ContentControl spScrollViewerAsCC = default;
			DependencyObject spScrollViewerAsDO;
			DependencyObject spUpButtonAsDO;
			//ButtonBase spUpButtonAsButtonBase;
			DependencyObject spDownButtonAsDO;
			//ButtonBase spDownButtonAsButtonBase;

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
				//(_tpUpButton.remove_Click(_upButtonClickedToken));
				_tpUpButton.Click -= OnUpButtonClicked;
			}

			if (_tpDownButton is { })
			{
				//(_tpDownButton.remove_Click(_downButtonClickedToken));
				_tpDownButton.Click -= OnDownButtonClicked;
			}

			if (_tpScrollViewerPrivate is { })
			{
				_tpScrollViewerPrivate.EnableOverpan();
			}

			_tpScrollViewer = null;
			_tpScrollViewerPrivate = null;
			_tpPanel = null;
			_tpUpButton = null;
			_tpDownButton = null;

			//LoopingSelectorGenerated.OnApplyTemplateImpl();

			//QueryInterface(__uuidof(ControlProtected), &spControlProtected);
			//(spControlProtected.GetTemplateChild(
			//	wrl_wrappers.Hstring(c_upButtonTemplatePart),
			//	&spUpButtonAsDO));
			spUpButtonAsDO = GetTemplateChild(c_upButtonTemplatePart);

			if (spUpButtonAsDO is ButtonBase spUpButtonAsButtonBase)
			{
				//IGNOREHR(spUpButtonAsDO.As(spUpButtonAsButtonBase));
				_tpUpButton = spUpButtonAsButtonBase;
			}

			//(spControlProtected.GetTemplateChild(
			//	wrl_wrappers.Hstring(c_downButtonTemplatePart),
			//	&spDownButtonAsDO));
			spDownButtonAsDO = GetTemplateChild(c_downButtonTemplatePart);

			if (spDownButtonAsDO is ButtonBase spDownButtonAsButtonBase)
			{
				//IGNOREHR(spDownButtonAsDO.As(spDownButtonAsButtonBase));
				_tpDownButton = spDownButtonAsButtonBase;
			}

			//(spControlProtected.GetTemplateChild(
			//	wrl_wrappers.Hstring(c_scrollViewerTemplatePart),
			//	&spScrollViewerAsDO));
			spScrollViewerAsDO = GetTemplateChild(c_scrollViewerTemplatePart);

			if (spScrollViewerAsDO is { })
			{
				ScrollViewer spScrollViewer;
				//ScrollViewerPrivate spScrollViewerPrivate;

				// Try to cast to IScrollViewer. If failed
				// just allow to remain null.
				//IGNOREHR(spScrollViewerAsDO.As(spScrollViewer));
				spScrollViewer = spScrollViewerAsDO as ScrollViewer;
				//IGNOREHR(spScrollViewerAsDO.As(spScrollViewerPrivate));
				//IGNOREHR(spScrollViewerAsDO.As(spScrollViewerAsCC));
				spScrollViewerAsCC = spScrollViewerAsDO as ContentControl;

				_tpScrollViewer = spScrollViewer;
				//_tpScrollViewerPrivate = spScrollViewerPrivate;
				_tpScrollViewerPrivate = spScrollViewer;
			}

			if (spScrollViewerAsCC is { })
			{
				LoopingSelectorPanel spPanel;
				DependencyObject spLoopingSelectorPanelAsInspectable;
				//wrl.MakeAndInitialize<LoopingSelectorPanel>(spPanel);
				spPanel = new LoopingSelectorPanel();
				//spPanel.As(spLoopingSelectorPanelAsInspectable);
				spLoopingSelectorPanelAsInspectable = spPanel;
				spScrollViewerAsCC.Content = spLoopingSelectorPanelAsInspectable;
				_tpPanel = spPanel;
			}

			if (_tpPanel is { })
			{
				FrameworkElement spPanelAsFE;
				//_tpPanel.As(spPanelAsFE);
				spPanelAsFE = _tpPanel;
				spPanelAsFE.Height = 1000000;
			}

			if (_tpUpButton is { })
			{
				//(_tpUpButton.add_Click(
				//	wrl.Callback<RoutedEventHandler>(this, &LoopingSelector.OnUpButtonClicked),
				//	&_upButtonClickedToken));
				_tpUpButton.Click += OnUpButtonClicked;
			}

			if (_tpDownButton is { })
			{
				//(_tpDownButton.add_Click(
				//	wrl.Callback<RoutedEventHandler>(this, &LoopingSelector.OnDownButtonClicked),
				//	&_downButtonClickedToken));
				_tpDownButton.Click += OnDownButtonClicked;
			}

			if (_tpScrollViewer is { })
			{
				//(_tpScrollViewer.add_ViewChanged(
				//	wrl.Callback<wf.IEventHandler<xaml_controls.ScrollViewerViewChangedEventArgs>>
				//		(this, &LoopingSelector.OnViewChanged),
				//	&_viewChangedToken));
				_tpScrollViewer.ViewChanged += OnViewChanged;

				//(_tpScrollViewer.add_ViewChanging(
				//	wrl.Callback<wf.IEventHandler<xaml_controls.ScrollViewerViewChangingEventArgs>>
				//		(this, &LoopingSelector.OnViewChanging),
				//	&_viewChangingToken));
				_tpScrollViewer.ViewChanging += OnViewChanging;
			}

			if (_tpScrollViewerPrivate is { })
			{
				_tpScrollViewerPrivate.DisableOverpan();
			}
		}

		#endregion

		#region Helpers

#if __ANDROID__
		new
#endif
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
				spFocusedElt = FocusManager.GetFocusedElement(xamlRoot) as DependencyObject;

				if (spFocusedElt is { })
				{
					//spFocusedElt.As(spFocusedEltAsDO);
					spFocusedEltAsDO = spFocusedElt;
					IsAscendantOfTarget(spFocusedEltAsDO, out pHasFocus);
				}
			}
		}

		void IsAscendantOfTarget(DependencyObject pChild, out bool pIsChildOfTarget)
		{
			var spCurrentDO = pChild;
			DependencyObject spParentDO;
			DependencyObject spThisAsDO;
			//xaml_media.IVisualTreeHelperStatics> spVTHStatics;

			var isFound = false;

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
					//spCurrentDO.Attach(spParentDO.Detach());
					spCurrentDO = spParentDO;
				}
			}

			pIsChildOfTarget = isFound;
		}

		void ShiftChildren(double delta)
		{
			if (delta == 0)
			{
				return;
			}
			//LoopingSelectorItem.iterator iter;

			//for (iter = _realizedItems.begin(); iter != _realizedItems.end(); iter++)
			foreach (var iter in _realizedItems)
			{
				LoopingSelectorItem spChild;
				UIElement spChildAsUI;
				var currentPosition = 0.0;
				// This keeps the count unchanged. Attach doesn't
				// AddRef, and Detech doesn't Release.
				//spChild.Attach(iter);
				spChild = iter;
				//spChild.As(spChildAsUI);
				spChildAsUI = spChild;
				//NT_global.System.Diagnostics.Debug.Assert(_spCanvasStatics);
				//_spCanvasStatics.GetTop(spChildAsUI, &currentPosition);
				currentPosition = Canvas.GetTop(spChildAsUI);
				//_spCanvasStatics.SetTop(spChildAsUI, currentPosition + delta);
				Canvas.SetTop(spChildAsUI, currentPosition + delta);
				//spChild.Detach();
			}
		}

		void MeasureExtent(out double extentTop, out double extentBottom)
		{
			var viewportHeight = 0.0;
			var verticalOffset = 0.0;

			viewportHeight = _tpScrollViewer.ViewportHeight;
			verticalOffset = _tpScrollViewer.VerticalOffset;
			extentTop = verticalOffset;
			extentBottom = verticalOffset + viewportHeight;
		}

		void ClearAllItems()
		{
			IList<object> spItems;
			//LoopingSelectorItem.iterator iter;

			//for (iter = _realizedItems.begin(); iter != _realizedItems.end(); iter++)
			foreach (var iter in _realizedItems)
			{
				RecycleItem(iter);
				_realizedBottom -= _scaledItemHeight;
				_realizedBottomIdx--;
			}

			_realizedItems.Clear();
			_realizedItemsForAP.Clear();

			spItems = Items;
			if (spItems is { })
			{
				// We reset the logical indices to not contain any extra multiples of
				// the item count. This makes scenarios where the items collection is
				// changed while the user is manipulating the control do the 'expected'
				// thing and not jump around.
				var itemCount = 0;
				var indexDelta = 0;
				itemCount = spItems.Count;
				_itemCount = (uint)itemCount;
				indexDelta = _realizedMidpointIdx - (int)PositiveMod(_realizedMidpointIdx, (int)_itemCount);

				_realizedMidpointIdx = _realizedMidpointIdx - indexDelta;
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
			ppChildren = spChildren; //.Detach();
		}

		void SizePanel()
		{
			FrameworkElement spPanelAsFE;
			UIElement spThisAsUI;

#if HAS_UNO // Uno specific: Due to lifecycle differences, the ScrollViewer is may not be initialized at this point.
			// If ViewportHeight is 0, we would temporarily size the panel incorrectly, which could cause the selected
			// item to be changed.
			if (_tpScrollViewer is null || _tpScrollViewer.ViewportHeight == 0)
			{
				return;
			}
#endif

			var shouldLoop = false;
			shouldLoop = ShouldLoop;
			//QueryInterface(__uuidof(UIElement), &spThisAsUI);
			spThisAsUI = this;
			//_tpPanel.As(spPanelAsFE);
			spPanelAsFE = _tpPanel;
			if (shouldLoop)
			{
				var scrollViewerHeight = 0.0;
				scrollViewerHeight = _tpScrollViewer.ViewportHeight;
				// This is a large number. It is large enough to ensure for any
				// item size the panel size exceeds that which is reasonable
				// to expect the user to flick to the end of continuously while
				// not allowing a manipulation to complete.
				//
				// It is odd so the panel sizes correctly. The
				// midpoint aligns with the snap points and visual item realization
				// position.
				_panelSize = scrollViewerHeight + 1001 * _scaledItemHeight;
			}
			else
			{
				var scrollViewerHeight = 0.0;

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

			spPanelAsFE.Height = _panelSize;
		}

		void SetScrollPosition(double offset, bool useAnimation)
		{
			//DependencyObject spVerticalOffsetAsInspectable;
			double spVerticalOffset;

			//Private.ValueBoxer.Createdouble(offset, &spVerticalOffsetAsInspectable);
			//spVerticalOffsetAsInspectable.As(spVerticalOffset);
			spVerticalOffset = offset;
			//LSTRACE("[%d] Setting scroll position %f", (((int)this) >> 8) & 0xFF, offset);

			//_skipNextBalance = true;

			if (!useAnimation)
			{
				var didSucceed = false;
				// We use this booleaen as a performance optimization. When
				// this function is called with useAnimation set to false it
				// is an instantaneous jump, and balance will happen afterwards.
				_isWithinScrollChange = true;
				didSucceed = _tpScrollViewer.ChangeViewWithOptionalAnimation(
					null, spVerticalOffset, null, true /* disableAnimation */);

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
				//DispatcherQueueStatics> spDispatcherQueueStatics;
				DispatcherQueue spDispatcherQueue;
				bool enqueued;
				var spThis = this;
				//wrl.WeakRef wrThis;

				//spThis.AsWeak(wrThis);
				//(wf.GetActivationFactory(
				//	wrl_wrappers.Hstring(RuntimeClass_Windows_System_DispatcherQueue),
				//	&spDispatcherQueueStatics));
				//spDispatcherQueueStatics.GetForCurrentThread(spDispatcherQueue);
				spDispatcherQueue = Windows.System.DispatcherQueue.GetForCurrentThread();
				//(spDispatcherQueue.TryEnqueue(
				//	WRLHelper.MakeAgileCallback<wsy.IDispatcherQueueHandler>([wrThis, spVerticalOffset]() mutable {

				//	bool returnValue = false;
				//	ILoopingSelector> spThis;
				//	wrThis.As(spThis);
				//	if (spThis)
				//	{
				//		(((LoopingSelector)(spThis))._tpScrollViewer.ChangeViewWithOptionalAnimation(
				//			null, spVerticalOffset, null, false /* disableAnimation */, &returnValue));
				//	}
				//}),
				//&enqueued));
				//IFCEXPECT(enqueued);
				enqueued = spDispatcherQueue.TryEnqueue(() =>
				{
#if __ANDROID__
					// UNO-TODO: Animations are disabled because of https://github.com/unoplatform/uno/issues/5845
					_tpScrollViewer.ChangeViewWithOptionalAnimation(null, spVerticalOffset, null,
						true /* disableAnimation */);
#else
					_tpScrollViewer.ChangeViewWithOptionalAnimation(null, spVerticalOffset, null,
						false /* disableAnimation */);
#endif
				});
			}
		}

		void SetupSnapPoints(double offset, double size)
		{
			LoopingSelectorPanel lsp = null;
			lsp = _tpPanel;

			lsp.SetOffsetInPixels((float)offset);
			lsp.SetSizeInPixels((float)size);
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
			//LoopingSelectorItem.iterator iter;

			//for (iter = _realizedItems.begin(); iter != _realizedItems.end(); iter++)
			foreach (var iter in _realizedItems)
			{
				var lsi = iter;

				if (state == ItemState.ManipulationInProgress)
				{
					lsi.SetState(LoopingSelectorItem.State.Expanded, true);
				}
				else if (state == ItemState.Expanded)
				{
					if (_realizedTopIdx + (_realizedItems.Count - 1 - childIdx) == _realizedMidpointIdx)
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
					if (_realizedTopIdx + (_realizedItems.Count - 1 - childIdx) == _realizedMidpointIdx)
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

		internal void AutomationGetSelectedItem(out LoopingSelectorItem ppItemNoRef)
		{
			ppItemNoRef = null;

			//for (var iter = _realizedItems.begin(); iter != _realizedItems.end(); iter++)
			foreach (var iter in _realizedItems)
			{
				var pLSINoRef = iter;

				var itemVisualIndex = 0;
				pLSINoRef.GetVisualIndex(out itemVisualIndex);

				uint itemIndex = 0;
				VisualIndexToItemIndex((uint)itemVisualIndex, out itemIndex);

				// We need to make sure that we check against the selected index,
				// not the midpoint index.  Normally, the item in the center of the view
				// is also the selected item, but in the case of UI automation, it
				// is not always guaranteed to be.
				var selectedIndex = 0;
				selectedIndex = SelectedIndex;

				if (itemIndex == (uint)selectedIndex)
				{
					ppItemNoRef = pLSINoRef;
					break;
				}
			}
		}

		void RetreiveItemFromAPRealizedItems(uint moddeItemdIdx, out LoopingSelectorItem ppItem)
		{
			ppItem = null;
			//map<int, LoopingSelectorItem>>.iterator iter;

			//iter = _realizedItemsForAP.find(moddeItemdIdx);
			//var iter = _realizedItemsForAP[(int)moddeItemdIdx];
			//if (iter != _realizedItemsForAP.Last().Value)
			if (_realizedItemsForAP.TryGetValue((int)moddeItemdIdx, out var iter))
			{
				//ppItem = iter; //.Detach();
				ppItem = iter;
				_realizedItemsForAP.Remove((int)moddeItemdIdx);
			}
		}

		#endregion

		#region AutomationInternalInterface

		internal void AutomationScrollToVisualIdx(int visualIdx, bool ignoreScrollingState = false)
		{
			var isFullySetup = false;
			IsSetupForAutomation(out isFullySetup);
			if (isFullySetup && _itemState == ItemState.Expanded)
			{
				var idxMovement = visualIdx - _realizedMidpointIdx;
				var pixelsToMove = idxMovement * _scaledItemHeight;

				SetScrollPosition(_unpaddedExtentTop + pixelsToMove, false /* use animation */);
				Balance(true /* isOnSnapPoint */);
				// If we aren't going to scroll at all, then we need to update the selected index,
				// since we won't get a ViewChanged event during which to do that.
				if (pixelsToMove == 0)
				{
					UpdateSelectedItem(ignoreScrollingState);
				}
			}
		}

		internal void AutomationGetIsScrollable(out bool pIsScrollable)
		{
			// LoopingSelector doesn't currently have a disabled
			// state so as long as the itemCount is greater than
			// zero it is scrollable.
			pIsScrollable = _itemCount > 0;
		}

		internal void AutomationGetScrollPercent(out double pScrollPercent)
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
				pScrollPercent = selectedIndex / (double)_itemCount * 100.0;
			}
			else
			{
				pScrollPercent = 0.0;
			}
		}

		internal void AutomationGetScrollViewSize(out double pScrollPercent)
		{
			var isSetup = false;
			pScrollPercent = 100.0;
			IsSetupForAutomation(out isSetup);
			if (isSetup)
			{
				var viewportHeight = _unpaddedExtentBottom - _unpaddedExtentTop;
				if (viewportHeight > 0)
				{
					pScrollPercent = viewportHeight / (_itemCount * _scaledItemHeight) * 100;
				}
			}
		}

		internal void AutomationSetScrollPercent(double scrollPercent)
		{
			var isSetup = false;

			if (scrollPercent < 0.0 || scrollPercent > 100.0)
			{
				//IFC_NOTRACE(UIA_E_INVALIDOPERATION);
				throw new InvalidOperationException();
			}

			IsSetupForAutomation(out isSetup);
			if (isSetup && _itemState == ItemState.Expanded)
			{
				var itemIdxOffset = (int)((_itemCount - 1) * scrollPercent / 100.0);
				var currentItemIdx = (int)PositiveMod(_realizedMidpointIdx, (int)_itemCount);
				var idxMovement = itemIdxOffset - currentItemIdx;
				var pixelsToMove = idxMovement * _scaledItemHeight;
				SetScrollPosition(_unpaddedExtentTop + pixelsToMove, false /* use animation */);
				Balance(true /* isOnSnapPoint */);
			}
		}

		internal void AutomationTryGetSelectionUIAPeer(out AutomationPeer ppPeer)
		{
			ppPeer = null;

			LoopingSelectorItem pLSINoRef = null;
			AutomationGetSelectedItem(out pLSINoRef);
			if (pLSINoRef is { })
			{
				//FrameworkElementAutomationPeerStatics spAutomationPeerStatics;
				AutomationPeer spAutomationPeer;
				UIElement spChildAsUI;

				//(pLSINoRef.QueryInterface(
				//	__uuidof(UIElement),
				//	&spChildAsUI));
				spChildAsUI = this;
				//(wf.GetActivationFactory(
				//	wrl_wrappers.Hstring(
				//		RuntimeClass_Microsoft_UI_Xaml_Automation_Peers_FrameworkElementAutomationPeer),
				//	&spAutomationPeerStatics));
				//spAutomationPeerStatics.CreatePeerForElement(spChildAsUI, &spAutomationPeer);
				spAutomationPeer = FrameworkElementAutomationPeer.CreatePeerForElement(spChildAsUI);
				//spAutomationPeer.CopyTo(ppPeer);
				ppPeer = spAutomationPeer;
			}
		}

		internal void AutomationScroll(ScrollAmount scrollAmount)
		{
			var isSetup = false;
			IsSetupForAutomation(out isSetup);
			// We don't allow automation interaction when the ScrollViewer is undergoing
			// a manipulation.
			if (isSetup && _itemState == ItemState.Expanded)
			{
				var pixelsToMove = 0.0;
				var itemsToMove = 0;

				switch (scrollAmount)
				{
					case ScrollAmount.LargeDecrement:
						itemsToMove = -c_automationLargeIncrement;
						break;
					case ScrollAmount.LargeIncrement:
						itemsToMove = c_automationLargeIncrement;
						break;
					case ScrollAmount.SmallDecrement:
						itemsToMove = -c_automationSmallIncrement;
						break;
					case ScrollAmount.SmallIncrement:
						itemsToMove = c_automationSmallIncrement;
						break;
				}

				var currentIndex = (int)PositiveMod(_realizedMidpointIdx, (int)_itemCount);

				if (currentIndex + itemsToMove > (int)(_itemCount - 1))
				{
					itemsToMove = (int)_itemCount - currentIndex - 1;
				}
				else if (currentIndex + itemsToMove < 0)
				{
					itemsToMove = -currentIndex;
				}

				pixelsToMove = itemsToMove * _scaledItemHeight;

				SetScrollPosition(_unpaddedExtentTop + pixelsToMove, false /* use animation */);
				Balance(true /* isOnSnapPoint */);
			}
		}

		//internal void AutomationFillCollectionWithRealizedItems(IList<DependencyObject> pVector)
		//{
		//	// When the number of items is smaller than the number of items LoopingSelector calculates it
		//	// needs to realize to ensure gapless scrolling the realizedItem list will contain duplicate items.
		//	// This will cause infinite loops when UIA clients attempt to find an element because of the logic UIAutomationCore
		//	// uses to traverse the UIA tree. Because the realizedItem list is in order we simply add items until we reach
		//	// a point where we've added either all available items or added the number of items in the data list.
		//	uint counter = 0;
		//	for (var iter = _realizedItems.begin(); iter != _realizedItems.end() && counter < _itemCount; iter++)
		//	{
		//		ContentControl> spChildAsCC;
		//		DependencyObject spChildContent;

		//		counter++;

		//		((iter).QueryInterface<ContentControl>(
		//			&spChildAsCC));
		//		spChildContent = spChildAsCC.Content;
		//		pVector.Append(spChildContent);
		//	}
		//}

		internal void AutomationTryScrollItemIntoView(DependencyObject pItem)
		{
			var index = 0;
			var found = false;
			IList<object> spVector;
			spVector = Items;
			//spVector.IndexOf(pItem, &index, &found);
			index = spVector.IndexOf(pItem);
			found = index >= 0;
			if (found)
			{
				_skipSelectionChangeUntilFinalViewChanged = true;

				// The _realizedMidpointIdx points to the currently selected item's visual index. Because the visual index
				// always starts at the first item we subtract it from itself modded with the item count to obtain the
				// nearest first item visual index.
				var desiredVisualIdx =
					(int)(_realizedMidpointIdx - PositiveMod(_realizedMidpointIdx, (int)_itemCount) + index);
				AutomationScrollToVisualIdx(desiredVisualIdx);
			}
		}

		void AutomationRaiseSelectionChanged()
		{
			UIElement spThisAsUI;
			//ScrollPatternIdentifiersStatics spScrollPatternStatics;
			AutomationProperty spAutomationScrollProperty;

			UIElement spSelectedItemAsUI;
			LoopingSelectorItem pLSINoRef = null;

			//DependencyObject spNewScrollValue;

			var scrollPercent = 0.0;

			//(QueryInterface(
			//	__uuidof(UIElement),
			//	&spThisAsUI));
			spThisAsUI = this;

			//(wf.GetActivationFactory(
			//	wrl_wrappers.Hstring(RuntimeClass_Microsoft_UI_Xaml_Automation_ScrollPatternIdentifiers),
			//	&spScrollPatternStatics));
			//spAutomationScrollProperty = spScrollPatternStatics.VerticalScrollPercentProperty;
			spAutomationScrollProperty = ScrollPatternIdentifiers.VerticalScrollPercentProperty;
			AutomationGetScrollPercent(out scrollPercent);
			//Private.ValueBoxer.Createdouble(scrollPercent, &spNewScrollValue);
			AutomationGetSelectedItem(out pLSINoRef);
			if (pLSINoRef is { })
			{
				//(pLSINoRef.QueryInterface(
				//	__uuidof(UIElement),
				//	&spSelectedItemAsUI));
				spSelectedItemAsUI = this;

				//(AutomationHelper.RaiseEventIfListener(
				//	spSelectedItemAsUI,
				//	Peers.AutomationEvents_SelectionItemPatternOnElementSelected));
				AutomationHelper.RaiseEventIfListener(spSelectedItemAsUI,
					AutomationEvents.SelectionItemPatternOnElementSelected);
				AutomationHelper.SetAutomationFocusIfListener(spSelectedItemAsUI);
			}

			//(Private.AutomationHelper.RaisePropertyChangedIfListener(
			//	spThisAsUI,
			//	spAutomationScrollProperty,
			//	_tpPreviousScrollPosition,
			//	spNewScrollValue));
			AutomationHelper.RaisePropertyChangedIfListener(
				spThisAsUI,
				spAutomationScrollProperty,
				_tpPreviousScrollPosition,
				scrollPercent);

			_tpPreviousScrollPosition = scrollPercent;
		}

		void AutomationRaiseExpandCollapse()
		{
			UIElement spThisAsUI;
			//ExpandCollapsePatternIdentifiersStatics spExpandCollapsePatternStatics;
			AutomationProperty spAutomationProperty;

			//DependencyObject spOldValue;
			//DependencyObject spNewValue;
			ExpandCollapseState spOldValueAsPV;
			ExpandCollapseState spNewValueAsPV;

			//(QueryInterface(
			//	__uuidof(UIElement),
			//	&spThisAsUI));
			spThisAsUI = this;

			//(wf.GetActivationFactory(
			//	wrl_wrappers.Hstring(RuntimeClass_Microsoft_UI_Xaml_Automation_ExpandCollapsePatternIdentifiers),
			//	&spExpandCollapsePatternStatics));

			//spAutomationProperty = spExpandCollapsePatternStatics.ExpandCollapseStateProperty;
			spAutomationProperty = ExpandCollapsePatternIdentifiers.ExpandCollapseStateProperty;
			//(Private.ValueBoxer.CreateReference<ExpandCollapseState>
			//	(ExpandCollapseState_Collapsed, &spOldValueAsPV));
			spOldValueAsPV = ExpandCollapseState.Collapsed;
			//(Private.ValueBoxer.CreateReference<ExpandCollapseState>
			//	(ExpandCollapseState_Expanded, &spNewValueAsPV));
			spNewValueAsPV = ExpandCollapseState.Expanded;

			//spOldValueAsPV.As(spOldValue);
			//spNewValueAsPV.As(spNewValue);
			AutomationHelper.RaisePropertyChangedIfListener(spThisAsUI, spAutomationProperty, spOldValueAsPV,
				spNewValueAsPV);
		}

		void AutomationRaiseStructureChanged()
		{
			UIElement spThisAsUI;
			//(QueryInterface(
			//	__uuidof(UIElement),
			//	&spThisAsUI));
			spThisAsUI = this;

			// The visible children has changed. Notify UIA of
			// a new structure.
			AutomationHelper.RaiseEventIfListener(
				spThisAsUI,
				AutomationEvents.StructureChanged);
		}

		internal void AutomationGetContainerUIAPeerForItem(
			DependencyObject pItem,
			out LoopingSelectorItemAutomationPeer ppPeer)
		{
			ppPeer = null;
			LoopingSelectorItem spChild = default;

			//for (var iter = _realizedItems.begin(); iter != _realizedItems.end(); iter++)
			foreach (var iter in _realizedItems)
			{
				var spTentativeChild = iter;
				ContentControl spTentativeChildAsCC;
				DependencyObject spItem;
				//spTentativeChild.As(spTentativeChildAsCC);
				spTentativeChildAsCC = spTentativeChild;

				spItem = spTentativeChildAsCC.Content as DependencyObject;

				if (spItem == pItem)
				{
					spChild = spTentativeChild;
					break;
				}
			}

			if (spChild == null)
			{
				//for (var iter = _realizedItemsForAP.begin(); iter != _realizedItemsForAP.end(); iter++)
				foreach (var iter in _realizedItemsForAP)
				{
					var spTentativeChild = iter.Value;
					ContentControl spTentativeChildAsCC;
					DependencyObject spItem;
					//spTentativeChild.As(spTentativeChildAsCC);
					spTentativeChildAsCC = spTentativeChild;

					spItem = spTentativeChildAsCC.Content as DependencyObject;

					if (spItem == pItem)
					{
						spChild = spTentativeChild;
						break;
					}
				}
			}

			if (spChild is { })
			{
				UIElement spChildAsUIElt;
				AutomationPeer spChildAP;
				LoopingSelectorItemAutomationPeer spChildAPAsLSIAP;
				//spChild.As(spChildAsUIElt);
				spChildAsUIElt = spChild;
				spChildAP = AutomationHelper.CreatePeerForElement(spChildAsUIElt);
				//spChildAP.As(spChildAPAsLSIAP);
				spChildAPAsLSIAP = spChildAP as LoopingSelectorItemAutomationPeer;

				ppPeer = spChildAPAsLSIAP; //.Detach();
			}
		}

		internal void AutomationClearPeerMap()
		{
			LoopingSelectorAutomationPeer spLSAP = default;
			//_wrAP.CopyTo<LoopingSelectorAutomationPeer>(spLSAP);
			//if (spLSAP)
			if (_wrAP?.TryGetTarget(out spLSAP) ?? false)
			{
				spLSAP.ClearPeerMap();
			}
		}

		internal void AutomationRealizeItemForAP(uint itemIdxToRealize)
		{
			LoopingSelectorItem spItem;
			RealizeItem(itemIdxToRealize, out spItem);
			_realizedItemsForAP[(int)itemIdxToRealize] = spItem;
		}

		#endregion
	}
}

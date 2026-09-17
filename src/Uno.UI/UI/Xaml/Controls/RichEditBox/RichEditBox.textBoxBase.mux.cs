// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference dxaml/xcp/core/native/text/Controls/TextBoxBase.cpp, commit 3c9c168844f06c6ac000a97977f0bb3f4c90fd75
#nullable enable

using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Input;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls;

partial class RichEditBox
{
	// TextBoxBase.cpp, lines 972-980.
	private void EnableEnsureRectVisible() => m_ensureRectVisibleEnabled = true;

	private void DisableEnsureRectVisible() => m_ensureRectVisibleEnabled = false;

	// TextBoxBase.cpp, lines 1130-1146. Geometry and deferred display updates belong to the managed view.
	private void RaisePendingBringLastVisibleRectIntoView(bool forceIntoView, bool focusChanged)
	{
		EnableEnsureRectVisible();
#if HAS_UNO
		DispatchUpdateScrolling();
#else
		// TODO Uno: The managed view queues the last pending caret/selection geometry instead of CTextBoxView.
		// if ((focusChanged && m_shouldBringLastVisibleRectIntoView) || forceIntoView)
		// {
		//     IFC_RETURN(BringLastVisibleRectIntoView(true));
		// }
#endif
	}

	// TextBoxBase.cpp, lines 1889-1911.
	//------------------------------------------------------------------------
	//
	//  Synopsis:
	//      Event handler for PointerEntered event.
	//
	//------------------------------------------------------------------------
	private void OnPointerEnteredCore(PointerRoutedEventArgs pEventArgs)
	{
		_isPointerOver = true;
		UpdateVisualState();
	}

	//------------------------------------------------------------------------
	//
	//  Synopsis:
	//      Event handler for PointerExited event.
	//
	//------------------------------------------------------------------------
	private void OnPointerExitedCore(PointerRoutedEventArgs pEventArgs)
	{
		_isPointerOver = false;
		UpdateVisualState();
		// Need to send a message to stop the interaction context if we leave a text box
#if HAS_UNO
		HandleLinkNavigation(false, pEventArgs.KeyModifiers);
#else
		// TODO Uno: Pointer capture and hyperlink hit testing replace the native interaction context.
		// IFC_RETURN(SendPointerMessage(WM_POINTERLEAVE, pEventArgs));
#endif
	}

	// TextBoxBase.cpp, lines 2097-2105.
	//------------------------------------------------------------------------
	//
	//  Synopsis:
	//      Event handler for PointerCaptureLost event.
	//
	//------------------------------------------------------------------------
	private void OnPointerCaptureLostCore(PointerRoutedEventArgs pEventArgs)
	{
		_isPointerOver = false;
		UpdateVisualState();
#if HAS_UNO
		ResetManagedPointerState();
		HandleLinkNavigation(false, pEventArgs.KeyModifiers);
#else
		// TODO Uno: Pointer capture belongs to UIElement rather than WinUIEdit.
		// IFC_RETURN(SendPointerMessage(WM_POINTERCAPTURECHANGED, pEventArgs));
#endif
	}

	// TextBoxBase.cpp, lines 2186-2210.
	//------------------------------------------------------------------------
	//
	//  Synopsis:
	//      Tap reported by gesture engine. Mark as handled so controls
	//      up the tree ignores it. RichEdit handles this using
	//      OnPointerPressed and OnPointerReleased.
	//
	//------------------------------------------------------------------------
	private void OnTappedCore(TappedRoutedEventArgs pEventArgs)
	{
		if (FocusState != FocusState.Unfocused)
		{
			if (PreventKeyboardDisplayOnProgrammaticFocus && !IsReadOnly)
			{
				ActivateImeForUserInteraction(FocusState.Pointer);
			}
#if !HAS_UNO
			// TODO Uno: The platform IME adapter and PointerReleased already handle software-keyboard reactivation and touch selection.
			// IFC_RETURN(ForceNotifyFocusEnterIfNeeded());
			// const bool inputIsTouch = pPointerEventArgs->m_pointerDeviceType == DirectUI::PointerDeviceType::Touch;
			// if (inputIsTouch)
			// {
			//     IFC_RETURN(SendGripperHostTapMessage(pPointerEventArgs));
			// }
#endif
		}

		pEventArgs.Handled = true;
	}

	// TextBoxBase.cpp, lines 2575-2653.
	//------------------------------------------------------------------------
	//
	//  Synopsis:
	//      Event handler for GotFocus event.
	//
	//------------------------------------------------------------------------
	private void OnGotFocusCore(RoutedEventArgs pEventArgs)
	{
		if (!IsEnabled && AllowFocusWhenDisabled)
		{
			UpdateVisualState();
			return;
		}

		// Verify that this textbox is still the focused element.
		// This is to account for a scenario where a new element gains focus
		// between focus manager queueing the GotFocus event and it being raised.
		if (XamlRoot is not null)
		{
			var focusedElement = FocusManager.GetFocusedElement(XamlRoot);
			var originalSource = pEventArgs.OriginalSource;
			// Only return if this TextBox is the original source of the OnGotFocus event.
			// Other controls may directly call this OnGotFocus function and we do not want
			// to block those calls.
			if (ReferenceEquals(originalSource, this) && !ReferenceEquals(focusedElement, this))
			{
				return;
			}
		}
#if HAS_UNO
		OnGotFocusManaged(pEventArgs);
#else
		// TODO Uno: TextBoxView/ImeSessionCoordinator replace native view focus, SendRichEditFocus and Windows TSF input settings.
		// if (m_forceFocusedVisualState)
		// {
		//     m_forceFocusedVisualState = false;
		//     if (m_shouldHideGrippersOnFlyoutOpening) { IFC_RETURN(GetView()->OnContextMenuDismiss()); }
		// }
		// else
		// {
		//     IFC_RETURN(SendRichEditFocus());
		//     IFC_RETURN(GetView()->OnGotFocus());
		// }
		// IFC_RETURN(UpdateVisualState());
		// We call UpdateLastSelectedTextElement here to make sure our state is updated
		// in case the focus was gained as a result on mouse or pen input.
		// IFC_RETURN(UpdateLastSelectedTextElement());
		// On Phone, we do not want to scroll headers into view if the input pane is not showing, since we
		// would not try to bring the last rect into view.
		// However, on Desktop, bring in to view logic does not depend on the input pane.
		// As a result, we always want to attempt to scroll headers into view.
		// m_shouldScrollHeaderIntoView = true;
		// We intentionally did not send WM_KILLFOCUS to RichEdit at OnLostFocus, and it will skip notifying TSF3 of
		// focus enter in WM_SETFOCUS call, so we notify TSF3 directly here.
		// IFC_RETURN(ForceNotifyFocusEnterIfNeeded());
		// if (m_spTextServiceManager) { IFC_RETURN(OnCurrentInputLanguageChanged(nullptr, nullptr)); }
		// else if (CInputServices* inputServices = GetContext()->GetInputServices())
		// {
		//     LCID lcid = MAKELCID(LANGIDFROMHKL(inputServices->GetInputLanguage()), SORT_DEFAULT);
		//     UpdateKeyboardLCID(lcid);
		// }
#endif
	}

	// TextBoxBase.cpp, lines 2655-2747. Native TSF focus exceptions are handled by the platform session adapter.
	private void OnLostFocusCore(RoutedEventArgs pEventArgs)
	{
#if HAS_UNO
		OnLostFocusManaged(pEventArgs);
#endif
		// if there was candidate windown bound changed event fired earlier, we should always fire a zero sized candidate window event when focus lost
		if (m_firedCandidateWindowEventAfterFocus)
		{
			m_firedCandidateWindowEventAfterFocus = false;
			_candidateWindowBoundsChanged?.Invoke(this, new CandidateWindowBoundsChangedEventArgs(default));
		}

		// Lost focus does not necessary trigger redrawing of contents, sometimes it only needs to redraw border
		// Force redraw if SelectionHighlightColorWhenNotFocused is set by app
		if (SelectionHighlightColorWhenNotFocused is not null)
		{
			InvalidateView();
		}
	}

	// TextBoxBase.cpp, lines 2827-2868.
	//------------------------------------------------------------------------
	//
	//  Synopsis:
	//      Event handler for Holding event.
	//
	//------------------------------------------------------------------------
	private void OnHoldingCore(HoldingRoutedEventArgs pEventArgs)
	{
		var inputIsPen = pEventArgs.PointerDeviceType == PointerDeviceType.Pen;
		var inputIsTouch = pEventArgs.HoldingState == HoldingState.Started && pEventArgs.PointerDeviceType == PointerDeviceType.Touch;
		var holdStarted = pEventArgs.HoldingState == HoldingState.Started;
		var holdCompleted = pEventArgs.HoldingState == HoldingState.Completed;

		if (holdCompleted && inputIsPen)
		{
			// RichEdit does not support Pen input directly.
			// Convert Holding Completed to Right Mouse Button Up in order to show the context menu.
#if HAS_UNO
			var contextArgs = new ContextRequestedEventArgs();
			contextArgs.SetGlobalPoint(pEventArgs.GetPosition(null));
			OnContextRequested(this, contextArgs);
			pEventArgs.Handled = contextArgs.Handled;
#else
			// TODO Uno: The managed context-menu pipeline replaces EM_MOUSEINPUT.
			// IFC_RETURN(PackageMouseEventParams(WM_RBUTTONUP, pHoldingEventArgs, DirectUI::VirtualKeyModifiers::None, pHoldingEventArgs->m_pointerDeviceType, &reMouseInput));
			// IFC_RETURN(TextBoxBase_Internal::TxSendMessageHelper(this, EM_MOUSEINPUT, WM_RBUTTONUP, 0, reinterpret_cast<LPARAM>(&reMouseInput), &pHoldingEventArgs->m_bHandled, &result));
#endif
		}
		else if (holdStarted && inputIsTouch)
		{
			// Since TextServicesHost exposes IGripperHost, we are working with the immersive version of msftedit touch
			// code. We place the caret in response to a press-and-hold on the RichEdit canvas.
			// Get the pointer position relative to the inner CTextBoxView.
#if HAS_UNO
			if (_textBoxView?.DisplayBlock is { } view)
			{
				var position = pEventArgs.GetPosition(view);
				var cpResult = view.ParsedText.GetIndexAt(position, true, true);
				if (cpResult >= 0)
				{
					SetInteractiveSelection(cpResult, 0);
					CaretMode = RichEditCaretDisplayMode.CaretWithThumbsOnlyEndShowing;
				}
			}
#else
			// TODO Uno: ParsedText supplies caret hit testing instead of the private canvas-touch message.
			// Forward to msftedit (touch.cpp) to handle caret placement.
			// m_pTextServices->TxSendMessage(EM_ONCANVASTOUCHHOLD, (WPARAM) &params, NULL, &lres);
#endif
		}
	}

	// TextBoxBase.cpp, lines 3063-3123.
	private bool ProcessSelectionChangingEvent(ref int selectionStart, ref int selectionLength)
	// SelectionChanging Synchronous event handling
	{
		_isProcessingSelectionChanging = true;
		try
		{
			var originalSelection = _selection;
			var spSelection = Document.Selection;
#if HAS_UNO
			var selectionChangeVersion = Document.SelectionChangeVersion;
#endif
			var selectionChangingCanceled = OnSelectionChangingHandler(selectionStart, selectionLength);

			var selectionStartAfterChangingEvent = spSelection.StartPosition;
			var selectionEndAfterChangingEvent = spSelection.EndPosition;
			var selectionChangedByApp = selectionStartAfterChangingEvent != selectionStart
				|| selectionEndAfterChangingEvent != selectionStart + selectionLength;
#if HAS_UNO
			// Managed SetText rebases the same selection object during this callback; native TOM owns that transaction.
			// Only explicit selection mutations (including an asynchronous paste request) supersede Cancel.
			selectionChangedByApp = Document.SelectionChangeVersion != selectionChangeVersion;
#endif

			if (selectionChangedByApp)
			{
				selectionStart = selectionStartAfterChangingEvent;
				selectionLength = selectionEndAfterChangingEvent - selectionStartAfterChangingEvent;
				selectionChangingCanceled = false; // ignore cancel set by app if selection has been changed in handling event
			}

			if (selectionChangingCanceled)
			{
				// restore the original text selection if cancelled
				RestoreSelectionSilently(originalSelection);
			}

			// Snap the grippers to the current selection if it has been modified by app or because of cancellation
			if (CaretMode is RichEditCaretDisplayMode.CaretWithThumbsBothEndsShowing or RichEditCaretDisplayMode.CaretWithThumbsOnlyEndShowing
				&& (selectionChangedByApp || selectionChangingCanceled))
			{
				// Gripper dragging condition may be in progress, ignore that
				_gripperPresenter?.Update();
			}

			return selectionChangingCanceled;
		}
		finally
		{
			_isProcessingSelectionChanging = false;
		}
	}

	// TextBoxBase.cpp, lines 3272-3291.
	private void UpdateDefaultSpellChecking()
	{
#if HAS_UNO
		// The value's precedence identifies an explicit setting even when it equals the metadata default.
		if (((DependencyObject)this).GetCurrentHighestValuePrecedence(IsSpellCheckEnabledProperty) == DependencyPropertyValuePrecedences.DefaultValue)
		{
			if (GetInputScope() == InputScopeNameValue.PersonalFullName) // default to be consistent with mobile
			{
				SetValue(IsSpellCheckEnabledProperty, false);
			}
		}
#else
		// TODO Uno: The managed display/IME callbacks replace IMF_SPELLCHECKING and private TSF settings.
		// IFC_RETURN(SetLanguageOption(IMF_SPELLCHECKING, isSpellCheckEnabled));
		// if (GetContext()->IsTSF3Enabled())
		// {
		//     IFC_RETURN(EnsureTextInputSettings());
		//     IFC_RETURN(m_pPrivateTextInputSettings->OnSpellCheckEnabledChanged(static_cast<BOOL>(isSpellCheckEnabled)));
		// }
#endif
	}

	// TextBoxBase.cpp, lines 3548-3575.
	//------------------------------------------------------------------------
	//
	//  Synopsis:
	//      Called when the visual state of the control is modified (eg. focus
	//      change). UpdateVisualState performs the visual state transition
	//      for the new state based on managed or native control status. Use
	//      the managed VisualStateManager to perform the transition.
	//
	//------------------------------------------------------------------------
	private void UpdateVisualStateCore(bool useTransitions)
	{
		// CommonStates & FocusStates are combined
		//
		// NOTES: Pressed state is the same as Focused
		//        PointerFocused state is the same as Focused
		if (!IsEnabled)
		{
			VisualStateManager.GoToState(this, "Disabled", useTransitions);
		}
		else if (_forceFocusedVisualState || FocusState != FocusState.Unfocused)
		{
			VisualStateManager.GoToState(this, "Focused", useTransitions);
		}
		else if (_isPointerOver)
		{
			VisualStateManager.GoToState(this, "PointerOver", useTransitions);
		}
		else
		{
			VisualStateManager.GoToState(this, "Normal", useTransitions);
		}
#if !HAS_UNO
		// TODO Uno: Native focusManager->IsPluginFocused() is represented by host activation/focus notifications.
		// TODO Uno: Feature_HeaderPlacement's HeaderStates are outside the supported WinUI contract.
#endif
	}
}

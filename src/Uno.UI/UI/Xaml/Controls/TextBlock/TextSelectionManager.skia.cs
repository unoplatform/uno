// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

// MUX Reference TextSelectionManager.cpp, tag winui3/release/1.8.2, commit 4a1c6184c

#nullable enable

using System;
using Windows.Foundation;
using Windows.System;
using Windows.UI;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Controls.Text.Core;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.ApplicationModel.DataTransfer;
using static Microsoft.UI.Xaml.Controls._Tracing;
using static Microsoft.UI.Xaml.Documents.BlockLayout.TextGravity;
using TextGravity = Microsoft.UI.Xaml.Documents.BlockLayout.TextGravity;

namespace Microsoft.UI.Xaml.Controls;

internal sealed partial class TextSelectionManager
{
	private const TextGravity c_defaultGravity = LineForwardCharacterBackward;

	// MUX Reference free function XAML_SetClipboardText.
	private static void XAML_SetClipboardText(string strText)
	{
		DataPackage dataPackage = new();
		dataPackage.SetText(strText);

		Clipboard.SetContent(dataPackage);
		Clipboard.Flush();
	}

	//---------------------------------------------------------------------------
	//
	//  Member: TextSelectionManager::TextViewChanged
	//
	//---------------------------------------------------------------------------
	public void TextViewChanged(ITextView? pOldTextView, ITextView? pNewTextView)
	{
		// If the old text view is different from the new one, selection and all associated text pointers will be different.
		// Delete the current selection and create a new one.
		if (pOldTextView != pNewTextView)
		{
			m_pTextSelection = null;

			// Create selection object.
			if (pNewTextView != null)
			{
				TextSelection.Create(m_pContainer, pNewTextView, this, out var pSelection);
				m_pTextSelection = pSelection;
			}
		}
	}

	//------------------------------------------------------------------------
	//  Method:   TextSelectionManager::OnSelectionPointerMove
	//------------------------------------------------------------------------
	private void OnSelectionPointerMove(UIElement pSender, PointerRoutedEventArgs pPointerEventArgs, ITextView pTextView)
	{
		TextGravity gravity = LineForwardCharacterForward;

		// MouseMove, etc. are expected to be called only when we know we have a selection.
		MUX_ASSERT(m_pTextSelection != null);

		Point hitPoint = pPointerEventArgs.GetCurrentPoint(null).Position;

		// If we have mouse capture, in a linked scenario the mouse may be captured by
		// one link but actually be over another link. In that case, we should hit test the
		// linked view, because we want to extend selection.
		IsPointOverLinkedView(pSender, pTextView, hitPoint, out var mouseMovePoint, out var pPointerOverView, out _);

		// Always use the mouse over view for hit testing.
		uint mouseMoveOffset = pPointerOverView!.PixelPositionToTextPosition(
			mouseMovePoint,   // pixel offset
			false,            // Recognise hits after newline
			out gravity);

		// Get previous selection offsets to pass to NotifySelectionChanged.
		GetSelectionStartEndOffsets(out var previousSelectionStartOffset, out var previousSelectionEndOffset);

		// Use the TextView passed in to create the position. Assume it's the same one that selection was created with,
		// it's the callers responsibility to notify us if TextView changed.
		TextPosition mouseMovePosition = new(new PlainTextPosition(m_pContainer, mouseMoveOffset, gravity));
		m_pTextSelection!.ExtendSelectionByMouse(mouseMovePosition);

		pPointerEventArgs.Handled = true;

		bool hideGrippers = true;
		bool interactionIsPen = m_penPressed;
		if (interactionIsPen)
		{
			ShowGrippers(true /* Animate */);
			hideGrippers = false;
		}

		// Assuming we have capture and are handling the move, we can assume selection will change.
		NotifySelectionChanged(
			previousSelectionStartOffset,
			previousSelectionEndOffset,
			hideGrippers /* fHideGrippers */);
	}

	//------------------------------------------------------------------------
	//  Method:   TextSelectionManager::OnSelectionPointerPressed
	//
	//  Synopsis: Event handler for MouseLeftButtonDown event on TextBlock.
	//------------------------------------------------------------------------
	private void OnSelectionPointerPressed(UIElement pSender, PointerRoutedEventArgs pPointerEventArgs, ITextView pTextView)
	{
		MUX_ASSERT(m_pTextSelection != null);
		bool selectionPreviouslyEmpty = m_pTextSelection!.IsEmpty();

		bool isInitiallyFocused = IsSelectionVisible();
		if (!m_owner.IsFocused)
		{
			bool focusUpdated = m_owner.Focus(FocusState.Pointer);
			if (!focusUpdated)
			{
				// We can't obtain focus - do not react to the mouse button.
				return;
			}
		}

		GetTextPosition(pPointerEventArgs, pSender, pTextView, out var mouseDownPosition, out _);

		// Get previous selection offsets to pass to NotifySelectionChanged.
		GetSelectionStartEndOffsets(out var previousSelectionStartOffset, out var previousSelectionEndOffset);

		m_isShiftPressed = false;
		if ((pPointerEventArgs.KeyModifiers & VirtualKeyModifiers.Shift) != VirtualKeyModifiers.None)
		{
			m_isShiftPressed = true;
			m_pTextSelection.ExtendSelectionByMouse(mouseDownPosition);
			m_hasPointerCapture = pSender.CapturePointer(pPointerEventArgs.Pointer);
		}
		else
		{
			IsOffsetBetweenSelection(mouseDownPosition, out var isOffsetBetweenSelection);

			// If the pointer is pressed within the the selection we just do nothing.
			// This is the current behavior of IE for mouse.
			// For pen we have to do that. Otherwise, when the user PressHoldAndLift
			// to invoke the Context Menu, the selection will be reset before the CM shows.
			if (!isInitiallyFocused || !isOffsetBetweenSelection)
			{
				m_pTextSelection.SetCaretPositionFromTextPosition(mouseDownPosition);
				m_hasPointerCapture = pSender.CapturePointer(pPointerEventArgs.Pointer);
			}
		}

		HideGrippers(false /* fAnimate */);

		NotifySelectionChanged(
			selectionPreviouslyEmpty,
			previousSelectionStartOffset,
			previousSelectionEndOffset,
			true /* fHideGrippers */);
	}

	//------------------------------------------------------------------------
	//  Method:   TextSelectionManager::GetTextPosition
	//------------------------------------------------------------------------
	private void GetTextPosition(
		PointerRoutedEventArgs pPointEventArgs,
		UIElement pSender,
		ITextView pTextView,
		out TextPosition pTextPosition,
		out bool pbInNextLine)
	{
		Point pointCoords = pPointEventArgs.GetCurrentPoint(pSender).Position;

		uint pointDownOffset = pTextView.PixelPositionToTextPosition(
			pointCoords,   // pixel offset
			false /* bIncludeNewline */, // Get position before any trailing newlines.
			out var gravity);

		pTextPosition = new TextPosition(new PlainTextPosition(m_pContainer, pointDownOffset, gravity));
		pbInNextLine = (gravity & LineBackward) != 0;
	}

	// Overload for the gesture events (Tapped/DoubleTapped) which expose GetPosition(relativeTo).
	private void GetTextPosition(
		Func<UIElement, Point> getPosition,
		UIElement pSender,
		ITextView pTextView,
		out TextPosition pTextPosition,
		out bool pbInNextLine)
	{
		Point pointCoords = getPosition(pSender);

		uint pointDownOffset = pTextView.PixelPositionToTextPosition(
			pointCoords,
			false /* bIncludeNewline */,
			out var gravity);

		pTextPosition = new TextPosition(new PlainTextPosition(m_pContainer, pointDownOffset, gravity));
		pbInNextLine = (gravity & LineBackward) != 0;
	}

	//------------------------------------------------------------------------
	//  Method:   TextSelectionManager::OnDoubleTapped
	//
	//  Synopsis: Event handler for OnDoubleTapped event on TextBlock.
	//------------------------------------------------------------------------
	public void OnDoubleTapped(UIElement pSender, DoubleTappedRoutedEventArgs pPointerEventArgs, ITextView pTextView)
	{
		MUX_ASSERT(m_pTextSelection != null);
		bool selectionPreviouslyEmpty = m_pTextSelection!.IsEmpty();

		// If the event is already handled or doesn't come from the sender, we shouldn't be doing
		// selection updates.
		if (pPointerEventArgs.Handled || pPointerEventArgs.OriginalSource != pSender)
		{
			return;
		}

		if (!m_owner.IsFocused)
		{
			bool focusUpdated = m_owner.Focus(FocusState.Pointer);
			if (!focusUpdated)
			{
				// We can't obtain focus - do not react to the mouse button.
				return;
			}
		}

		m_lastInputDeviceType = pPointerEventArgs.PointerDeviceType;

		// Get previous selection offsets to pass to NotifySelectionChanged.
		GetSelectionStartEndOffsets(out var previousSelectionStartOffset, out var previousSelectionEndOffset);

		GetTextPosition(pPointerEventArgs.GetPosition, pSender, pTextView, out var pointerDownPosition, out var bTextPositionInLineBelow);

		if (pPointerEventArgs.PointerDeviceType == PointerDeviceType.Touch ||
			pPointerEventArgs.PointerDeviceType == PointerDeviceType.Pen)
		{
			// If the given position is in the line below the one hit, don't start the selection,
			// and dismiss the existing one so we don't show it when we tap back into the control when the focus was somewhere else
			if (bTextPositionInLineBelow)
			{
				ClearSelection(pointerDownPosition);
			}
			else
			{
				SetSelectionToWord(pointerDownPosition);
			}
		}
		else
		{
			TextBoxHelpers.SelectWordFromTextPosition(
				m_pContainer,
				pointerDownPosition,
				m_pTextSelection,
				FindBoundaryType.ForwardIncludeTrailingWhitespace,
				TagConversion.None);
		}

		pPointerEventArgs.Handled = true;

		// Due to a limitation the input manager will call OnDoubleTapped before OnPointerReleased
		// hence a PointerMove might happen in between the 2 events and since we do not release the
		// pointer capture until OnPointerRelease is called we mark m_ignorePointerMove = true
		// so that OnPointerMove will have no effect.
		m_ignorePointerMove = true;

		if (pPointerEventArgs.PointerDeviceType == PointerDeviceType.Touch ||
			pPointerEventArgs.PointerDeviceType == PointerDeviceType.Pen)
		{
			ShowGrippers(true /* fAnimate */);
		}
		else
		{
			// Manipulating via a mouse. Ensure the grippers are not shown, no animation.
			HideGrippers(false /* fAnimate */);
		}

		NotifySelectionChanged(
			selectionPreviouslyEmpty,
			previousSelectionStartOffset,
			previousSelectionEndOffset,
			false /* fHideGrippers */); // Already hidden with the code above if necessary
	}

	//------------------------------------------------------------------------
	//  Method:   TextSelectionManager::OnSelectionPointerReleased
	//------------------------------------------------------------------------
	private void OnSelectionPointerReleased(UIElement pSender, PointerRoutedEventArgs pPointerEventArgs, ITextView pTextView)
	{
		// Do not handle the event if we don't have capture.
		// It is OK to call ReleaseCapture unconditionally, but if the event is marked handled,
		// it will not bubble further.
		if (!pPointerEventArgs.Handled && m_hasPointerCapture)
		{
			if (!m_pTextSelection!.IsEmpty() && IsSelectionVisible())
			{
				if (pPointerEventArgs.Pointer.PointerDeviceType != PointerDeviceType.Mouse)
				{
					QueueUpdateSelectionFlyoutVisibility();
				}
			}

			m_hasPointerCapture = false;
			pSender.ReleasePointerCapture(pPointerEventArgs.Pointer);
		}

		// MouseUp currently has no effect on selection - no need to notify owner of change.
	}

	//------------------------------------------------------------------------
	//  Method:   TextSelectionManager::OnPointerMoved
	//------------------------------------------------------------------------
	public void OnPointerMoved(UIElement pSender, PointerRoutedEventArgs pPointerArgs, ITextView pTextView)
	{
		if (pPointerArgs.Handled || !m_hasPointerCapture || m_ignorePointerMove)
		{
			return;
		}

		var pPointer = pPointerArgs.Pointer;

		if ((pPointer.PointerDeviceType == PointerDeviceType.Mouse
			 && m_leftClickPressed
			 && m_leftClickPointerId == (int)pPointer.PointerId)
			||
			(pPointer.PointerDeviceType == PointerDeviceType.Pen
			 && m_penPressed
			 && m_penPointerId == (int)pPointer.PointerId))
		{
			OnSelectionPointerMove(pSender, pPointerArgs, pTextView);
		}
	}

	//------------------------------------------------------------------------
	//  Method:   TextSelectionManager::OnPointerPressed
	//------------------------------------------------------------------------
	public void OnPointerPressed(UIElement pSender, PointerRoutedEventArgs pPointerArgs, ITextView pTextView)
	{
		var pPointer = pPointerArgs.Pointer;

		// If the event is already handled or doesn't come from the sender, we shouldn't be doing
		// selection updates.
		if (pPointerArgs.Handled || pPointerArgs.OriginalSource != pSender)
		{
			return;
		}

		// Due to a limitation the input manager will call OnDoubleTapped before OnPointerReleased
		// hence a PointerMove might happen in between the 2 events and since we do not release the
		// pointer capture until OnPointerRelease is called we mark m_ignorePointerMove = true
		// so that OnPointerMove will have no effect.
		m_ignorePointerMove = false;
		m_lastInputDeviceType = pPointer.PointerDeviceType;

		var properties = pPointerArgs.GetCurrentPoint(pSender).Properties;

		if (pPointer.PointerDeviceType == PointerDeviceType.Mouse && properties.IsLeftButtonPressed)
		{
			m_leftClickPressed = true;
			m_leftClickPointerId = (int)pPointer.PointerId;

			OnSelectionPointerPressed(pSender, pPointerArgs, pTextView);
		}
		else if (pPointer.PointerDeviceType == PointerDeviceType.Pen && properties.IsBarrelButtonPressed)
		{
			m_penPressed = true;
			m_penPointerId = (int)pPointer.PointerId;

			OnSelectionPointerPressed(pSender, pPointerArgs, pTextView);
		}

		// TODO Uno (Stage 7 P2): caret browsing — ShowCaretElement() when CaretBrowsingMode is enabled.

		pPointerArgs.Handled = true;
	}

	//------------------------------------------------------------------------
	//  Method:   TextSelectionManager::OnPointerReleased
	//------------------------------------------------------------------------
	public void OnPointerReleased(UIElement pSender, PointerRoutedEventArgs pPointerArgs, ITextView pTextView)
	{
		var pPointer = pPointerArgs.Pointer;

		if (pPointerArgs.Handled)
		{
			return;
		}

		if (pPointer.PointerDeviceType == PointerDeviceType.Mouse
			&& m_leftClickPressed
			&& (int)pPointer.PointerId == m_leftClickPointerId)
		{
			OnSelectionPointerReleased(pSender, pPointerArgs, pTextView);

			m_leftClickPressed = false;
			m_leftClickPointerId = -1;
		}
		else if (pPointer.PointerDeviceType == PointerDeviceType.Pen
				 && m_penPressed
				 && (int)pPointer.PointerId == m_penPointerId)
		{
			OnSelectionPointerReleased(pSender, pPointerArgs, pTextView);

			m_penPointerId = -1;
			m_penPressed = false;

			ShowGrippers(true /*animate*/);
		}

		// Stops the event from bubbling up.
		// When TB or RTB is inside a scroll viewer, not marking this event as handled
		// will cause the control to go out of focus. This will prevent us from
		// successfully showing the CM when the user taps again on a selected word.
		pPointerArgs.Handled = true;
	}

	//------------------------------------------------------------------------
	//  Method:   TextSelectionManager::OnHolding
	//------------------------------------------------------------------------
	public void OnHolding(UIElement pSender, HoldingRoutedEventArgs pPointerEventArgs, ITextView pTextView)
	{
		MUX_ASSERT(m_pTextSelection != null);

		// If the event is already handled or doesn't come from the sender, we shouldn't be doing
		// selection updates.
		if (pPointerEventArgs.Handled || pPointerEventArgs.OriginalSource != pSender)
		{
			return;
		}

		if (!m_owner.IsFocused && !m_owner.ShouldForceFocusedVisualState())
		{
			bool focusUpdated = m_owner.Focus(FocusState.Pointer);
			if (!focusUpdated)
			{
				// We can't obtain focus - do not react to the mouse button.
				return;
			}
		}

		if (!m_pTextSelection!.IsEmpty())
		{
			if (pPointerEventArgs.PointerDeviceType == PointerDeviceType.Pen)
			{
				ShowContextMenu(
					pPointerEventArgs.GetPosition(null),
					false /*isSelectionEmpty*/,
					true /* showGrippersOnDismiss */);

				pPointerEventArgs.Handled = true;
			}
		}
	}

	//------------------------------------------------------------------------
	//  Method:   TextSelectionManager::OnRightTapped
	//------------------------------------------------------------------------
	public void OnRightTapped(UIElement pSender, RightTappedRoutedEventArgs pRightTappedEventArgs, ITextView pTextView)
	{
		MUX_ASSERT(m_pTextSelection != null);

		// If the event is already handled or doesn't come from the sender, we shouldn't be doing
		// selection updates.
		if (pRightTappedEventArgs.Handled || pRightTappedEventArgs.OriginalSource != pSender)
		{
			return;
		}

		if (!m_owner.IsFocused && !m_owner.ShouldForceFocusedVisualState())
		{
			bool focusUpdated = m_owner.Focus(FocusState.Pointer);
			if (!focusUpdated)
			{
				// We can't obtain focus - do not react to the mouse button.
				return;
			}
		}

		if (!m_pTextSelection!.IsEmpty())
		{
			bool reshowGrippers = pRightTappedEventArgs.PointerDeviceType == PointerDeviceType.Touch ||
								  pRightTappedEventArgs.PointerDeviceType == PointerDeviceType.Pen;

			ShowContextMenu(
				pRightTappedEventArgs.GetPosition(null),
				false /*isSelectionEmpty*/,
				reshowGrippers);

			pRightTappedEventArgs.Handled = true;
		}
	}

	//------------------------------------------------------------------------
	//  Method:   TextSelectionManager::OnGotFocus
	//------------------------------------------------------------------------
	public void OnGotFocus(UIElement pSender, RoutedEventArgs pEventArgs, ITextView pTextView)
	{
		// We call UpdateLastSelectedTextElement here to make sure our state is updated
		// in case the focus was gained as a result on mouse or pen input.
		UpdateLastSelectedTextElement();

		// GotFocus can't change the selection, only make it visible, so only invalidate render if it's non-empty.
		if (m_pTextSelection != null && !m_pTextSelection.IsEmpty())
		{
			if (m_lastInputDeviceType == PointerDeviceType.Touch ||
				m_lastInputDeviceType == PointerDeviceType.Pen)
			{
				ShowGrippers(true /* fAnimate */);
			}

			// Selection went from invisible to visible, notify visibility changed.
			NotifySelectionVisibilityChanged();

			m_forceFocusedVisualState = false;
		}

		// TODO Uno (Stage 7 P2): caret browsing — caret-on-focus handling when CaretBrowsingMode is enabled.
	}

	//------------------------------------------------------------------------
	//  Method:   TextSelectionManager::OnLostFocus
	//------------------------------------------------------------------------
	public void OnLostFocus(UIElement pSender, RoutedEventArgs pEventArgs, ITextView pTextView)
	{
		// LostFocus can't change the selection, only make it invisible, so only invalidate render if it's non-empty.
		if (m_pTextSelection != null && !m_pTextSelection.IsEmpty())
		{
			// If we're losing focus to the Selection or Context flyouts, then we should not hide the selection or grippers.
			m_forceFocusedVisualState = m_owner.ShouldForceFocusedVisualState();

			if (!m_forceFocusedVisualState)
			{
				HideGrippers(false /* fAnimate */);
			}

			NotifySelectionVisibilityChanged();
		}

		RemoveCaret();
	}

	//------------------------------------------------------------------------
	//  Method:   TextSelectionManager::OnTapped
	//------------------------------------------------------------------------
	public void OnTapped(UIElement pSender, TappedRoutedEventArgs pTappedEventArgs, ITextView pTextView)
	{
		MUX_ASSERT(m_pTextSelection != null);
		bool selectionPreviouslyEmpty = m_pTextSelection!.IsEmpty();

		// If the event is already handled or doesn't come from the sender, we shouldn't be doing
		// selection updates.
		if (pTappedEventArgs.Handled || pTappedEventArgs.OriginalSource != pSender)
		{
			return;
		}

		// If one gripper is already being manipulated, ignore the tap with any other pointer.
		if (AreGrippersBeingManipulated())
		{
			pTappedEventArgs.Handled = true;
			return;
		}

		m_lastInputDeviceType = pTappedEventArgs.PointerDeviceType;

		// Get previous selection offsets to pass to NotifySelectionChanged.
		GetSelectionStartEndOffsets(out var previousSelectionStartOffset, out var previousSelectionEndOffset);

		GetTextPosition(pTappedEventArgs.GetPosition, pSender, pTextView, out var tapPosition, out var bTextPositionInLineBelow);

		if (pTappedEventArgs.PointerDeviceType == PointerDeviceType.Touch ||
			pTappedEventArgs.PointerDeviceType == PointerDeviceType.Pen)
		{
			EnsureGrippers();
			if (!m_pTextSelection.IsEmpty() && IsSelectionVisible())
			{
				IsOffsetBetweenSelection(tapPosition, out var isOffsetBetweenSelection);

				if (isOffsetBetweenSelection)
				{
					// Tapped inside the selection. Show grippers if not shown yet.
					ShowGrippers(true /* fAnimate */);
				}
				else
				{
					// If text is already selected, a tap outside the selected text should
					// just clear the selection.
					ClearSelection(tapPosition);
				}
			}
			else if (!m_owner.CanSelectText)
			{
				ClearSelection(tapPosition);
			}
			else
			{
				// If the given position is in the line below the one hit, don't start the selection,
				// and dismiss the existing one so we don't show it when we tap back into the control when the focus was somewhere else
				if (bTextPositionInLineBelow)
				{
					ClearSelection(tapPosition);
				}
				else
				{
					SetSelectionToWord(tapPosition);
				}
			}
		}
		else
		{
			if (!m_isShiftPressed)
			{
				m_pTextSelection.SetCaretPositionFromTextPosition(tapPosition);
			}
		}

		if (!m_owner.IsFocused)
		{
			m_owner.Focus(FocusState.Pointer);
		}

		pTappedEventArgs.Handled = true;
		m_ignorePointerMove = true;
		NotifySelectionChanged(
			selectionPreviouslyEmpty,
			previousSelectionStartOffset,
			previousSelectionEndOffset,
			false /* fHideGrippers */);
	}

	private void GetSelectionStartEndOffsets(out uint pStartOffset, out uint pEndOffset)
	{
		pStartOffset = 0;
		pEndOffset = 0;

		m_pTextSelection!.GetStartTextPosition(out var startPosition);
		m_pTextSelection.GetEndTextPosition(out var endPosition);

		startPosition.GetOffset(out pStartOffset);
		endPosition.GetOffset(out pEndOffset);
	}

	//------------------------------------------------------------------------
	//  Method:   TextSelectionManager::NotifySelectionVisibilityChanged
	//
	//  Synopsis: Notifies TextContainer owner that selection visibility changed.
	//------------------------------------------------------------------------
	private void NotifySelectionVisibilityChanged()
	{
		// Get updated selection start/end offsets.
		GetSelectionStartEndOffsets(out var selectionStartOffset, out var selectionEndOffset);

		m_owner.OnSelectionVisibilityChanged(selectionStartOffset, selectionEndOffset);
	}

	//------------------------------------------------------------------------
	//  Method:   TextSelectionManager::NotifySelectionChanged
	//
	//  Synopsis: Notifies TextContainer owner that selection changed.
	//            Deletes stored SelectedText string.
	//------------------------------------------------------------------------
	private void NotifySelectionChanged(
		uint previousSelectionStartOffset,
		uint previousSelectionEndOffset,
		bool fHideGrippers)
	{
		// Delete the current stored value of selected text.
		m_strSelectedText = null;

		// Get updated selection start/end offsets.
		GetSelectionStartEndOffsets(out var newSelectionStartOffset, out var newSelectionEndOffset);

		m_owner.OnSelectionChanged(
			previousSelectionStartOffset,
			previousSelectionEndOffset,
			newSelectionStartOffset,
			newSelectionEndOffset);

		UpdateLastSelectedTextElement();
		if (fHideGrippers)
		{
			HideGrippers(false /* fAnimate */);
		}
	}

	//------------------------------------------------------------------------
	//  Method:   TextSelectionManager::NotifySelectionChanged (empty-aware overload)
	//------------------------------------------------------------------------
	private void NotifySelectionChanged(
		bool selectionPreviouslyEmpty,
		uint previousSelectionStartOffset,
		uint previousSelectionEndOffset,
		bool fHideGrippers)
	{
		bool selectionChanged = true;

		// If the selection was empty before and still is, there is no need for callers to re-render
		// highlight, etc. Further optimization is possible by comparing anchor/moving positions, etc.
		// but is not really worth it.
		if (m_pTextSelection!.IsEmpty() && selectionPreviouslyEmpty)
		{
			selectionChanged = false;
		}

		if (selectionChanged)
		{
			NotifySelectionChanged(
				previousSelectionStartOffset,
				previousSelectionEndOffset,
				fHideGrippers);
		}
	}

	//------------------------------------------------------------------------
	//  Method:   TextSelectionManager::IsOffsetBetweenSelection
	//
	//  Synopsis: Checks whether a given offset is between the start
	//            and end of the selected text.
	//------------------------------------------------------------------------
	private void IsOffsetBetweenSelection(TextPosition currentOffsetPosition, out bool pIsBetweenSelection)
	{
		m_pTextSelection!.GetStartTextPosition(out var startPosition);
		m_pTextSelection.GetEndTextPosition(out var endPosition);

		currentOffsetPosition.GetOffset(out var currentOffset);
		startPosition.GetOffset(out var startOffset);
		endPosition.GetOffset(out var endOffset);

		pIsBetweenSelection = currentOffset >= startOffset && currentOffset < endOffset;
	}

	//------------------------------------------------------------------------
	//  Method:   TextSelectionManager::OnKeyUp
	//------------------------------------------------------------------------
	public void OnKeyUp(UIElement pSender, KeyRoutedEventArgs pKeyEventArgs, ITextView pTextView)
	{
		if (pKeyEventArgs.Handled)
		{
			return;
		}

		// TODO Uno (Stage 7 P2): the ContextMenu key (Apps key) maps to OnKeyDown Shift+F10 below.
	}

	//------------------------------------------------------------------------
	//  Method:   TextSelectionManager::OnKeyDown
	//------------------------------------------------------------------------
	public void OnKeyDown(UIElement pSender, KeyRoutedEventArgs pKeyEventArgs, ITextView pTextView)
	{
		bool handled = false;

		if (pKeyEventArgs.Handled)
		{
			return;
		}

		m_owner.CloseSelectionFlyoutIfOpen();

		if (pKeyEventArgs.Key == VirtualKey.F10 && IsShiftPressed(pKeyEventArgs))
		{
			if (m_pTextSelection != null && !m_pTextSelection.IsEmpty() &&
				m_owner.TryGetLastPointerPosition(out var worldPoint))
			{
				ShowContextMenu(
					worldPoint,
					false /*isSelectionEmpty*/,
					false /*isTouchInput*/);

				handled = true;
			}
		}

		if (IsCtrlPressed(pKeyEventArgs))
		{
			switch (pKeyEventArgs.Key)
			{
				case VirtualKey.C:
					CopySelectionToClipboard();
					handled = true;
					break;
				case VirtualKey.A:
					SelectAll();
					handled = true;
					break;
			}
		}

		// TODO Uno (Stage 7 P2): caret browsing — arrow/Home/End handling via CaretOnKeyDown.

		if (handled)
		{
			pKeyEventArgs.Handled = true;
		}
	}

	private static bool IsShiftPressed(KeyRoutedEventArgs args)
		=> args.KeyboardModifiers.HasFlag(VirtualKeyModifiers.Shift);

	private static bool IsCtrlPressed(KeyRoutedEventArgs args)
		=> args.KeyboardModifiers.HasFlag(VirtualKeyModifiers.Control);

	private void UpdateLastSelectedTextElement()
	{
		// If UpdateLastSelectedTextElement() was called as a result of
		// programatically setting the selection then we will not update
		// the last selected text element to this one since the selection
		// will not be visible if the control was not in focus.
		if (IsSelectionVisible())
		{
			if (m_pTextSelection == null || m_pTextSelection.IsEmpty())
			{
				m_owner.ClearLastSelectedTextElement();
			}
			else
			{
				m_owner.SetLastSelectedTextElement();
			}
		}
	}

	//------------------------------------------------------------------------
	//  Method:   TextSelectionManager::NotifyContextMenuOpening
	//
	//  Synopsis: Notifies TextContainer owner that context menu is showing.
	//------------------------------------------------------------------------
	private void NotifyContextMenuOpening(Point point, bool isSelectionEmpty)
		=> m_owner.RaiseContextMenuOpening(point, isSelectionEmpty);

	//------------------------------------------------------------------------
	//  Method:   TextSelectionManager::GetSelectedText
	//
	//  Synopsis: Gets selection serialized as plain text string.
	//------------------------------------------------------------------------
	public string GetSelectedText()
	{
		if (m_strSelectedText == null && m_pTextSelection != null)
		{
			m_pTextSelection.GetText(out m_strSelectedText);
		}

		return m_strSelectedText ?? string.Empty;
	}

	//------------------------------------------------------------------------
	//  Method:   TextSelectionManager::GetSelectionBoundingRect
	//
	//  Synopsis: Gets the bounding rect for the selected text.
	//------------------------------------------------------------------------
	public Rect GetSelectionBoundingRect()
	{
		// Uno adaptation: WinUI aggregates per-overflow highlight rects and transforms them
		// into the owner's coordinate space. Overflow chaining lands at Stage 9; for now we
		// union the highlight rects of the owner's single view.
		Rect selectionBoundingRectLocal = RectUtilCreateEmptyRect();

		ITextView? view = m_owner.GetTextView();
		if (view != null)
		{
			GetSelectionHighlightRects(view, out var highlightRects);
			foreach (var highlightRect in highlightRects)
			{
				selectionBoundingRectLocal.X = Math.Min(selectionBoundingRectLocal.X, highlightRect.X);
				selectionBoundingRectLocal.Y = Math.Min(selectionBoundingRectLocal.Y, highlightRect.Y);
				selectionBoundingRectLocal.Width = Math.Max(selectionBoundingRectLocal.Width, highlightRect.Width);
				selectionBoundingRectLocal.Height = Math.Max(selectionBoundingRectLocal.Height, highlightRect.Height);
			}
		}

		return selectionBoundingRectLocal;
	}

	private static Rect RectUtilCreateEmptyRect()
		=> new(float.PositiveInfinity, float.PositiveInfinity, float.NegativeInfinity, float.NegativeInfinity);

	//------------------------------------------------------------------------
	//  Method:   TextSelectionManager::GetSelectionHighlightRects
	//
	//  Synopsis: Gets rectangles for selection highlight.
	//------------------------------------------------------------------------
	public void GetSelectionHighlightRects(ITextView pTextView, out Rect[] ppRectangles)
		=> GetSelectionHighlightRectsInternal(pTextView, out ppRectangles, m_pTextSelection);

	//------------------------------------------------------------------------
	//  Method:   TextSelectionManager::GetBackPlateSelectionHighlightRects
	//
	//  Synopsis: Gets rectangles for Back Plate.
	//------------------------------------------------------------------------
	public void GetBackPlateSelectionHighlightRects(ITextView pTextView, out Rect[] ppRectangles)
	{
		TextSelection.Create(m_pContainer, pTextView, null, out var pTextSelection);

		SelectAllText(pTextSelection, false);

		GetSelectionHighlightRectsInternal(pTextView, out ppRectangles, pTextSelection);
	}

	private void GetSelectionHighlightRectsInternal(
		ITextView pTextView,
		out Rect[] ppRectangles,
		IJupiterTextSelection? pTextSelection)
	{
		if (pTextSelection != null && !pTextSelection.IsEmpty())
		{
			// Generate an array of text bound rectangles from the selection object.
			ppRectangles = pTextView.TextSelectionToTextBounds(pTextSelection);
		}
		else
		{
			ppRectangles = Array.Empty<Rect>();
		}
	}

	//------------------------------------------------------------------------
	//  Method:   TextSelectionManager::SetSelectionHighlightColor
	//------------------------------------------------------------------------
	public void SetSelectionHighlightColor(Color selectionColor)
	{
		if (m_pSelectionBackground != null)
		{
			m_pSelectionBackground.Color = selectionColor;
		}
		// Gripper stroke color adjustment is handled by the presenter.
	}

	//------------------------------------------------------------------------
	//  Method:   TextSelectionManager::GetSelectionHighlightColor
	//
	//  Synopsis: Gets the selection color appropriate for normal and high contrast modes.
	//------------------------------------------------------------------------
	private SolidColorBrush GetSelectionHighlightColor(bool isHighContrast)
	{
		// If high contrast, ignore the property set by user. Always return the system theme color.
		if (isHighContrast)
		{
			return m_owner.GetSystemTextSelectionBackgroundBrush();
		}

		return m_pSelectionBackground!;
	}

	private SolidColorBrush GetSystemColorWindowBrush() => m_owner.GetSystemColorWindowBrush();

	//------------------------------------------------------------------------
	//  Method:   TextSelectionManager::CopySelectionToClipboard
	//------------------------------------------------------------------------
	public void CopySelectionToClipboard()
	{
		string strSelectedText = GetSelectedText();
		if (!string.IsNullOrEmpty(strSelectedText))
		{
			XAML_SetClipboardText(strSelectedText);
		}
	}

	//------------------------------------------------------------------------
	//  Method:   TextSelectionManager::GetSelectionHighlightRegion
	//
	//  Synopsis: Gets start/end offsets + foreground/background brushes for a
	//            selected region. If nothing is selected, returns null.
	//------------------------------------------------------------------------
	public HighlightRegion? GetSelectionHighlightRegion(bool isHighContrast)
	{
		if (IsSelectionVisible())
		{
			// Get start and end offsets.
			GetTextSelection()!.GetStartTextPosition(out var startPosition);
			GetTextSelection()!.GetEndTextPosition(out var endPosition);
			startPosition.GetOffset(out var startOffset);
			endPosition.GetOffset(out var endOffset);

			// Get foreground brush.
			SolidColorBrush selectionForegroundBrush = m_owner.GetSystemTextSelectionForegroundBrush();

			// Get background brush -- accounts for high contrast mode.
			SolidColorBrush selectionBackgroundBrush = GetSelectionHighlightColor(isHighContrast);

			return new HighlightRegion(
				(int)startOffset,
				(int)endOffset - 1, // Highlight ends are inclusive, selection ends are not.
				selectionForegroundBrush,
				selectionBackgroundBrush);
		}

		return null;
	}

	//------------------------------------------------------------------------
	//  Method:   TextSelectionManager::SelectAll
	//
	//  Synopsis: Select all content in TextContainer.
	//------------------------------------------------------------------------
	public void SelectAll() => SelectAllText(m_pTextSelection, true);

	private void SelectAllText(IJupiterTextSelection? textSelection, bool sendSelectionChangedNotification)
	{
		MUX_ASSERT(textSelection != null);

		m_pContainer.GetPositionCount(out var containerEndPosition);
		uint containerStartPosition = 0;
		if (containerEndPosition > 0)
		{
			containerEndPosition--;
		}

		textSelection!.GetStartTextPosition(out var startTextPosition);
		textSelection.GetEndTextPosition(out var endTextPosition);
		startTextPosition.GetOffset(out var startOffset);
		endTextPosition.GetOffset(out var endOffset);

		if (startOffset == containerStartPosition &&
			endOffset == containerEndPosition)
		{
			return;
		}

		// Select all content.
		textSelection.Select(
			containerStartPosition,
			containerEndPosition,
			LineForwardCharacterBackward);

		if (sendSelectionChangedNotification)
		{
			NotifySelectionChanged(
				startOffset,
				endOffset,
				true /* fHideGrippers*/);
		}
	}

	//------------------------------------------------------------------------
	//  Method:   TextSelectionManager::Select
	//
	//  Synopsis: Select text in the specified range.
	//------------------------------------------------------------------------
	public void Select(PlainTextPosition anchorTextPosition, PlainTextPosition movingTextPosition)
	{
		MUX_ASSERT(m_pTextSelection != null);

		anchorTextPosition.GetOffset(out var startOffset);
		movingTextPosition.GetOffset(out var endOffset);
		if (!TextBoxHelpers.VerifyPositionPair(m_pContainer, startOffset, endOffset))
		{
			return;
		}
		MUX_ASSERT(anchorTextPosition.GetTextContainer() == m_pContainer);
		MUX_ASSERT(movingTextPosition.GetTextContainer() == m_pContainer);

		m_pTextSelection!.GetStartTextPosition(out var startTextPosition);
		m_pTextSelection.GetEndTextPosition(out var endTextPosition);
		startTextPosition.GetOffset(out var selectionStartOffset);
		endTextPosition.GetOffset(out var selectionEndOffset);

		if (startOffset == selectionStartOffset &&
			endOffset == selectionEndOffset)
		{
			// Everything's already selected.
			return;
		}

		m_pTextSelection.Select(
			startOffset,
			endOffset,
			LineForwardCharacterBackward);

		NotifySelectionChanged(
			selectionStartOffset,
			selectionEndOffset,
			true /* fHideGrippers*/);
	}

	//------------------------------------------------------------------------
	//  Method:   TextSelectionManager::IsPointOverLinkedView
	//
	//  Synopsis: Check a global mouse over point and establish whether it's
	//            over a linked view of the sender.
	//
	//  Uno adaptation: overflow chaining lands at Stage 9. For the single-element
	//  RichTextBlock/TextBlock case this resolves to the sender's own view.
	//------------------------------------------------------------------------
	private void IsPointOverLinkedView(
		UIElement pSender,
		ITextView pSenderView,
		Point pHitPoint,
		out Point pLocalMousePoint,
		out ITextView? ppPointerOverView,
		out UIElement? ppTargetUIElement)
	{
		// TODO Uno (Stage 9): walk the RichTextBlockOverflow chain to hit-test linked views.
		pLocalMousePoint = pSender.TransformToVisual(null).Inverse?.TransformPoint(pHitPoint) ?? pHitPoint;
		ppPointerOverView = pSenderView;
		ppTargetUIElement = pSender;
	}

	//------------------------------------------------------------------------
	//  Method:   TextSelectionManager::ShowContextMenu
	//------------------------------------------------------------------------
	public void ShowContextMenu(Point point, bool isSelectionEmpty, bool showGrippersOnDismiss)
	{
		HideGrippers(true /* fAnimate */);

		NotifyContextMenuOpening(point, isSelectionEmpty);
		m_showGrippersOnCMDismiss = showGrippersOnDismiss;
	}

	//------------------------------------------------------------------------
	//  Method:   TextSelectionManager::OnContextMenuDismiss
	//------------------------------------------------------------------------
	public void OnContextMenuDismiss()
	{
		if (m_showGrippersOnCMDismiss)
		{
			m_showGrippersOnCMDismiss = false;
			ShowGrippers(true /* fAnimate */);
		}
	}

	//------------------------------------------------------------------------
	//  Method:   TextSelectionManager::IsSelectionVisible
	//------------------------------------------------------------------------
	public bool IsSelectionVisible()
	{
		MUX_ASSERT(m_pOwnerUIElement != null);
		// Uno adaptation: overflow focus chaining lands at Stage 9; selection is visible
		// when the owner is focused (or temporarily force-focused for a flyout).
		return m_owner.IsFocused || m_forceFocusedVisualState;
	}

	//------------------------------------------------------------------------
	//  Method:   AreSelectionHighlightOffsetsEqual
	//
	//  Synopsis: Compares two selection regions. Returns true if both sets
	//            match exactly, including if both are empty.
	//------------------------------------------------------------------------
	public static bool AreSelectionHighlightOffsetsEqual(HighlightRegion? oldSelection, HighlightRegion? newSelection)
	{
		if (oldSelection == null && newSelection == null)
		{
			return true;
		}
		if (oldSelection == null || newSelection == null)
		{
			return false;
		}
		if (oldSelection.StartIndex != newSelection.StartIndex)
		{
			return false;
		}
		if (oldSelection.EndIndex != newSelection.EndIndex)
		{
			return false;
		}

		return true;
	}

	//------------------------------------------------------------------------
	//  Method:   TextSelectionManager::ClearSelection
	//
	//  Synopsis: Clears the current selection and places the caret at the given position.
	//------------------------------------------------------------------------
	private void ClearSelection(TextPosition caretPosition)
	{
		m_pTextSelection!.SetCaretPositionFromTextPosition(caretPosition);
		HideGrippers(true /* fAnimate */);
	}

	// Sets selection to the word at 'position'.
	private void SetSelectionToWord(TextPosition position)
	{
		TextBoxHelpers.GetClosestNonWhitespaceWordBoundary(
			m_pContainer,
			position,
			TagConversion.Default,
			out var closestNonWhitespace);

		TextBoxHelpers.SelectWordFromTextPosition(
			m_pContainer,
			closestNonWhitespace,
			m_pTextSelection!,
			FindBoundaryType.ForwardExact,
			TagConversion.Default);

		ShowGrippers(true /* fAnimate */);
		QueueUpdateSelectionFlyoutVisibility();
	}

	private void QueueUpdateSelectionFlyoutVisibility()
	{
		if (m_owner.TryGetLastPointerPosition(out var lastPointerPosition))
		{
			m_lastPointerPosition = lastPointerPosition;
		}

		if (!m_isSelectionFlyoutUpdateQueued)
		{
			m_isSelectionFlyoutUpdateQueued = true;
			m_owner.QueueUpdateSelectionFlyoutVisibility();
		}
	}

	// Called by the owner once the queued flyout update runs.
	public void UpdateSelectionFlyoutVisibility() => m_isSelectionFlyoutUpdateQueued = false;

	//------------------------------------------------------------------------
	//  Method:   TextSelectionManager::ForceFocusLoss
	//------------------------------------------------------------------------
	public void ForceFocusLoss()
	{
		m_forceFocusedVisualState = false;

		// LostFocus can't change the selection, only make it invisible, so only invalidate render if it's non-empty.
		if (m_pTextSelection != null && !m_pTextSelection.IsEmpty())
		{
			HideGrippers(false /* fAnimate */);

			// Selection went from visible to not, notify visibility changed.
			NotifySelectionVisibilityChanged();
		}
	}

	//------------------------------------------------------------------------
	//  Method:   TextSelectionManager::OnSelectionChanged (ITextSelectionNotify)
	//------------------------------------------------------------------------
	void ITextSelectionNotify.OnSelectionChanged() => UpdateCaretElement();
}

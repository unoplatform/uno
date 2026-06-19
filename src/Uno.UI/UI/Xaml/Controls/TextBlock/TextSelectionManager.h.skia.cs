// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

// MUX Reference TextSelectionManager.h, tag winui3/release/1.8.2, commit 4a1c6184c

#nullable enable

using System;
using Windows.Foundation;
using Windows.UI;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Controls.Text.Core;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Documents.RichTextServices;
using Microsoft.UI.Xaml.Media;
using Uno.UI.Xaml.Media;

namespace Microsoft.UI.Xaml.Controls;

//---------------------------------------------------------------------------
//
//  TextSelectionManager
//
//  Performs basic selection services for RichTextBlock and
//  RichTextBlockOverflow - extending selection on mouse input, etc.
//  These are similar to TextEditor services but without typing handling.
//
//---------------------------------------------------------------------------
//
//  Uno adaptations (see the deviation notes in the Stage-7 port report):
//   * WinUI's CTextSelectionManager owns refcounted CUIElements, popups, grippers and a
//     D2D/HW render path. On Uno the manager is owned by the control (created at Stage 9) and
//     talks to it through ITextSelectionManagerOwner; rendering is funnelled through the existing
//     ParsedText/TextHighlightMerge highlight pipeline rather than HWRender/D2DRender.
//   * Pointer/keyboard events arrive as Uno *RoutedEventArgs and are consumed faithfully where
//     the C++ consumed the equivalent CEventArgs subclasses.
//   * The touch grippers are not a ported CTextSelectionGripper class — the manager drives the
//     existing TextSelectionGripperPresenter (see TextSelectionManager.Gripper.skia.cs).
//
internal sealed partial class TextSelectionManager : ITextSelectionNotify
{
	private IJupiterTextSelection? m_pTextSelection;
	private SolidColorBrush? m_pSelectionBackground;
	private ITextContainer m_pContainer = null!;
	private string? m_strSelectedText;
	private int m_leftClickPointerId;
	private int m_penPointerId;
	private bool m_hasPointerCapture;
	private bool m_leftClickPressed;
	private bool m_penPressed;
	private bool m_isShiftPressed;
	private bool m_showGrippersOnCMDismiss;
	// Part of the faithful WinUI field set used by the gripper-drag / snapping path that the
	// presenter handles owner-side (NotifyGripperPressed/SetActiveGripper not re-ported here).
#pragma warning disable CS0169, CS0414 // field never used / assigned but never used
	private bool m_endGripperLastMoved;
	private bool m_interacting;
#pragma warning restore CS0169, CS0414
	private bool m_forceFocusedVisualState;
	private bool m_isSelectionFlyoutUpdateQueued;

	// Due to a limitation the input manager will call OnDoubleTapped before OnPointerReleased
	// hence a PointerMove might happen in between the 2 events and since we do not release the
	// pointer capture until OnPointerRelease is called we mark m_ignorePointerMove = true
	// so that OnPointerMove will have no effect.
	private bool m_ignorePointerMove;

	private UIElement m_pOwnerUIElement = null!;

	// Owner callback seam — replaces the WinUI down-casts to CRichTextBlock/CTextBlock and the
	// global FxCallbacks/Core services. Wired by the control at Stage 9.
	private ITextSelectionManagerOwner m_owner = null!;

	private PointerDeviceType m_lastInputDeviceType = (PointerDeviceType)(-1);

	private const uint DefaultCaretColor = 0xffffffff;

	private Point m_lastPointerPosition;

	private TextSelectionManager()
	{
		m_leftClickPointerId = -1;
		m_penPointerId = -1;
	}

	//---------------------------------------------------------------------------
	//
	//  Member: TextSelectionManager::Create
	//
	//---------------------------------------------------------------------------
	public static TextSelectionManager Create(
		UIElement pOwnerUIElement,
		ITextContainer pTextContainer,
		ITextSelectionManagerOwner owner)
	{
		SolidColorBrush pBackground = new(GetDefaultSelectionHighlightColor());

		// Finally, create selection manager with background and selection.
		TextSelectionManager pManager = new();
		pManager.m_pSelectionBackground = pBackground;
		pManager.m_pContainer = pTextContainer;
		pManager.m_pOwnerUIElement = pOwnerUIElement;
		pManager.m_owner = owner;

		return pManager;
	}

	//---------------------------------------------------------------------------
	//
	//  Member: TextSelectionManager::Destroy
	//
	//---------------------------------------------------------------------------
	public static void Destroy(ref TextSelectionManager? ppSelectionManager)
	{
		if (ppSelectionManager != null)
		{
			ppSelectionManager.RemoveCaret();
			ppSelectionManager.ReleaseGrippers();
			ppSelectionManager = null;
		}
	}

	// Returns the default text selection highlight color (system accent).
	private static Color GetDefaultSelectionHighlightColor()
		=> DefaultBrushes.SelectionHighlightColor.Color;

	public IJupiterTextSelection? GetTextSelection() => m_pTextSelection;

	public SolidColorBrush? GetSelectionBackgroundBrush() => m_pSelectionBackground;
}

//---------------------------------------------------------------------------
//
//  ITextSelectionManagerOwner
//
//  The control-side surface the manager calls back into. WinUI reached the
//  same behaviors through CRichTextBlock/CTextBlock virtual methods, the core
//  services (CTextCore, system brushes) and FxCallbacks. Implemented by the
//  owning control at Stage 9.
//
//---------------------------------------------------------------------------
internal interface ITextSelectionManagerOwner
{
	// Notifies that the selection changed (offsets are container positions).
	void OnSelectionChanged(
		uint previousSelectionStartOffset,
		uint previousSelectionEndOffset,
		uint newSelectionStartOffset,
		uint newSelectionEndOffset);

	// Notifies that selection visibility changed (e.g. focus gained/lost).
	void OnSelectionVisibilityChanged(
		uint selectionStartOffset,
		uint selectionEndOffset);

	// The ITextView the manager hit-tests / measures against.
	ITextView? GetTextView();

	bool IsFocused { get; }

	// Attempts to focus the owner with the given state; returns whether focus moved.
	bool Focus(FocusState focusState);

	// Whether text selection is permitted (mirrors CTextCore::CanSelectText for a single owner).
	bool CanSelectText { get; }

	// Schedules a re-render of the selection highlight.
	void InvalidateRender();

	// Tracks/clears the "last selected text element" (single-owner equivalent of CTextCore).
	void SetLastSelectedTextElement();
	void ClearLastSelectedTextElement();

	// High-contrast / system selection brushes.
	bool IsHighContrast { get; }
	SolidColorBrush GetSystemTextSelectionBackgroundBrush();
	SolidColorBrush GetSystemTextSelectionForegroundBrush();
	SolidColorBrush GetSystemColorWindowBrush();

	// SelectionFlyout / ContextFlyout hooks.
	void RaiseContextMenuOpening(Point worldPoint, bool isSelectionEmpty);
	void QueueUpdateSelectionFlyoutVisibility();
	void CloseSelectionFlyoutIfOpen();
	bool ShouldForceFocusedVisualState();
	bool TryGetLastPointerPosition(out Point worldPosition);

	// Touch-selection grippers (driven by TextSelectionGripperPresenter).
	void ShowGrippers(bool animate);
	void HideGrippers(bool animate);
	void EnsureGrippers();
	void ReleaseGrippers();
	void UpdateGripperPositions();
	bool AreGrippersBeingManipulated { get; }
}

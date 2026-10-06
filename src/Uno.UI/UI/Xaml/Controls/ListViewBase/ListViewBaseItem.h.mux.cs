// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference ListViewBaseItem_Partial.h, ListViewBaseItem_Partial.cpp (constants), tag winui3/release/2.5.1

#nullable enable

using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls;

partial class ListViewBaseItem
{
	// The ZIndex used by ListViewBaseItem during user interaction.
	// Note that ZIndex==1 is used by opaque headers which should be above rest items but below dragged one
	private const int LISTVIEWBASEITEM_INTERACTION_ZINDEX = 2;
	// The ZIndex used by ListViewBaseItem when no user is performing an interaction.
	private const int LISTVIEWBASEITEM_REST_ZINDEX = 0;
	// The name of the NoSelectionHint VisualState.
	private const string LISTVIEWBASEITEM_NO_SELECTION_HINT_STATE_NAME = "NoSelectionHint";
	// The name of the NoReorderHint VisualState.
	private const string LISTVIEWBASEITEM_NO_REORDER_HINT_STATE_NAME = "NoReorderHint";
	// The name of the content container template part (the bit that becomes the drag visual).
	private const string LISTVIEWBASEITEM_CONTENT_CONTAINER_PART_NAME = "ContentContainer";
	// (phone only) The name of the checkbox container template part (used to determine the source of a PointerPress).
	private const string LISTVIEWBASEITEM_CHECKBOX_CONTAINER_PART_NAME = "CheckboxContainer";

	// The standard Windows mouse drag box size is defined by SM_CXDRAG and SM_CYDRAG.
	// ListViewBaseItem uses the standard box size with dimensions multiplied by this constant.
	// This arrangement is in place as accidentally triggering a drag was deemed too easy while
	// selecting several items with the mouse in quick succession.
	// See bug 103860: Jupiter moco: Reorder  kicks in before DropTargetItemThemeAnimation starts
	private const double LISTVIEWBASEITEM_MOUSE_DRAG_THRESHOLD_MULTIPLIER = 2.0;

	// The delay time before going to the Pressed state after the initial touch
	private const int LISTVIEWBASEITEM_TOUCH_PRESSED_DELAY = 100;

	// The time to stay in a Pressed state if the touch was less than LISTVIEWBASEITEM_TOUCH_PRESSED_DELAY
	// basically, we want to cover the case where the user simply taps
	// a visual should be shown
	private const int LISTVIEWBASEITEM_TOUCH_RELEASED_TIMER = 125;

	// Two variables to define the outside area of the DragOver region
	private const double LISTVIEWITEM_DRAGOVER_OUTSIDE_MARGIN = 0.2;
	private const double GRIDVIEWITEM_DRAGOVER_OUTSIDE_MARGIN = 0.3;

	private static readonly Point s_center = new(0.5f, 0.5f);
	private const double s_holdingVisualOpacity = 0.8;
	private const double s_holdingVisualScale = 1.05;

#pragma warning disable CS0169, CS0414 // Fields used by the not-yet-ported drag, hint and holding paths.

	// Latest position the mouse left button was pushed. Used to initiate a mouse drag and drop.
	private Point m_lastMouseLeftButtonDownPosition;

	// Latest position the user first made contact with the screen.
	private Point m_lastTouchDownPosition;

	// TODO Uno: m_tpNoSelectionHintTimeline / m_tpNoReorderHintTimeline and their Completed handlers (hint storyboards not ported).

	// The ContentContainer template part.
	private UIElement? m_tpContentContainer;

	// The CheckboxContainer template part (phone-only).
	private UIElement? m_tpCheckboxContainer;

	// TODO Uno: m_spDragDropVisual and m_tpHoldingVisual (DragDropVisual / holding LTE not ported).

	// The pArgs received at the latest OnPointerPressed event.
	private Input.PointerRoutedEventArgs? m_tpLastTouchPressedArgs;

	// TODO Uno: m_pendingTapPointerIDs - click and selection stay on SelectorItem (_canRaiseClickOnPointerRelease).

	// Timer and variables to help control the Item's Pressed state using Touch
	private enum TouchTimerState
	{
		NoTimer,
		PressedTimer,
		ReleasedTimer
	}

	private TouchTimerState m_touchTimerState = TouchTimerState.NoTimer;
	private DispatcherTimer? m_tpTouchTimer;

	// Whether or not to display our pointer over visuals.
	private bool m_shouldEnterPointerOver;

	// Whether or not the left mouse button is pressed over this ListViewBaseItem.
	private bool m_isLeftButtonPressed;

	// Whether or not the right mouse button is pressed over this ListViewBaseItem.
	private bool m_isRightButtonPressed;

	// Whether or not we should be in the Pressed VisualState due to touch interactions.
	private bool m_inPressedForTouch;

	// Whether or not we should be in the CheckboxPressed VisualState.
	private bool m_inCheckboxPressedForTouch;

	// Whether or not the user is Holding over this ListViewBaseItem.
	private bool m_isHolding;

	// UIAutomation Property: whether or not this element is currently grabbed (i.e. being dragged).
	private bool m_fDragIsGrabbed;

	// If this is TRUE, this ListViewBaseItem is in a selection hint visual state.
	private bool m_inSelectionHintState;

	// If this is TRUE, this ListViewBaseItem is in a reorder hint visual state.
	private bool m_inReorderHintState;

	// TRUE if this ListViewBaseItem is above its siblings in ListViewBase.
	// Manipulated by MakeTopmost/ClearTopmost.
	private bool m_isTopmost;

	// If this is true, start a drag when the mouse moves far enough away from
	// m_lastMouseLeftButtonDownPosition (while the left button is held down).
	// "Far enough away" is defined via system metrics SM_CXDRAG and SM_CYDRAG.
	private bool m_isCheckingForMouseDrag;

	// Set when we're handing off a drag gesture to our ListViewBase.
	// Used to prevent the release of the CrossSlideViewport on CaptureLost,
	// since we need that viewport even after the swipe has completed (it prevents
	// a pan from occurring). However, we want to release the viewport in other
	// CaptureLost invocations.
	private bool m_isStartingDrag;

	// This boolean is set to true after we take a capture of the drag visual
	private bool m_dragVisualCaptured;

	// This boolean is used to indicate whether the Enter, Space or GamepadA key is down (pressed)
	// We will use this value to show the pressed visual state when one of those key down
	private bool m_isKeyboardPressed;

	// This boolean is used to indicate whether the Item should go into the DragOver visual
	private bool m_isDragOver;

#pragma warning restore CS0169, CS0414

	// Clears the previously cached arguments
	internal void ClearLastTouchPressedArgs() => m_tpLastTouchPressedArgs = null;

	// TODO Uno: ShouldAutomaticDragHelperHandleInputEvents (returns false in WinUI) - no AutomaticDragHelper.
}

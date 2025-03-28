// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference src\dxaml\xcp\dxaml\lib\ComboBox_Partial.h, tag winui3/release/1.6.1, commit f31293f

// The file is not ported completely, some methods are still missing!

using System;
using DirectUI;
using Windows.UI.Xaml.Media.Animation;
using Uno.Disposables;
using Uno.UI.Xaml.Input;
using Windows.Foundation;

namespace Windows.UI.Xaml.Controls;

partial class ComboBox
{
	private const char VK_ESCAPE = (char)0x1B;

#pragma warning disable CS0067 // Unused
#pragma warning disable CS0649 // Unused
#pragma warning disable CS0169 // Unused
#pragma warning disable CS0168 // Unused
#pragma warning disable CS0414 // Unused
#pragma warning disable IDE0051 // Unused
#pragma warning disable IDE0055 // Unused
	internal bool IsSearchResultIndexSet() => m_searchResultIndexSet;

	internal int GetSearchResultIndex() => m_searchResultIndex;

	private bool m_bIsPopupPannable;
	private bool m_bShouldCarousel;
	private bool m_bShouldCenterSelectedItem;
	private bool m_handledGamepadOrRemoteKeyDown;
	private bool m_ignoreCancelKeyDowns;
	private bool m_isEditModeConfigured;
	private bool m_isInSearchingMode;
	private bool m_openedUp;

	// On pointer released we perform some actions depending on control. We decide to whether to perform them
	// depending on some parameters including but not limited to whether released is followed by a pressed, which
	// mouse button is pressed, what type of pointer is it etc. This BOOLEAN keeps our decision.
	private bool m_shouldPerformActions;
	private bool m_IsPointerOverMain;
	private bool m_IsPointerOverPopup;
	private bool m_bIsPressed;
	private bool m_preparingContentPresentersElement;
	private bool m_isDropDownClosing;
	private bool m_bPopupHasBeenArrangedOnce;
	// Used to determine when to open the popup based on touch, we open the popup when TextBox gains
	// focus due to a pointer event.
	private bool m_openPopupOnTouch;

	// Determines if any search has started and has not been committed or reverted.
	private bool m_searchResultIndexSet;
	// Touch input model for Editable ComboBox establishes that second tap should select all text,
	// we use this variable to determine when to select all text on touch.
	private bool m_selectAllOnTouch;
	// Setting Editable Mode configures several event listeners, we use this variable to prevent configuring Editable mode twice.
	// Editable ComboBox is designed to set the focus on TextBox when ComboBox is focused, there are some cases when we don't want
	// this behavior eg(Shift+Tab).
	private bool m_shouldMoveFocusToTextBox;
	private bool m_isExpanded;
	private bool m_isOverlayVisible;
	private bool m_restoreIndexSet;
	private bool m_isClosingDueToCancel;

	private bool m_IsPointerOverDropDownOverlay;

	private InputDeviceType m_inputDeviceTypeUsedToOpen;
	private InputDeviceType m_previousInputDeviceTypeUsedToOpen;

	private FrameworkElement m_tpElementPopupChild;
	private FrameworkElement m_tpElementPopupContent;

	private readonly SerialDisposable m_pElementPopupChildKeyDownToken = new();
	private readonly SerialDisposable m_pElementPopupChildKeyUpToken = new();
	private readonly SerialDisposable m_pElementPopupChildGotFocusToken = new();
	private readonly SerialDisposable m_pElementPopupChildLostFocusToken = new();
	private readonly SerialDisposable m_pElementPopupChildPointerEnteredToken = new();
	private readonly SerialDisposable m_pElementPopupChildPointerExitedToken = new();
	private readonly SerialDisposable m_pElementPopupChildISizeChangedToken = new();
	private readonly SerialDisposable m_pElementOutSidePopupPointerPressedToken = new();
	private readonly SerialDisposable m_closedStateStoryboardCompletedToken = new();
	private readonly SerialDisposable m_pElementPopupChildCharacterReceivedToken = new();
	private readonly SerialDisposable m_pElementPopupChildLoadedToken = new();

	private readonly SerialDisposable m_spEditableTextPointerPressedEventHandler = new();
	private readonly SerialDisposable m_spEditableTextTappedEventHandler = new();
	private readonly SerialDisposable m_spEditableTextKeyDownHandler = new();
	private readonly SerialDisposable m_spEditableTextPreviewKeyDownHandler = new();
	private readonly SerialDisposable m_spEditableTextTextChangedHandler = new();
	private readonly SerialDisposable m_spEditableTextCandidateWindowBoundsChangedEventHandler = new();
	private readonly SerialDisposable m_spEditableTextSizeChangedHandler = new();
	private readonly SerialDisposable m_spDropDownOverlayPointerEnteredHandler = new();
	private readonly SerialDisposable m_spDropDownOverlayPointerExitedHandler = new();

	private object m_tpEmptyContent;
	private TextBlock m_tpEditableContentPresenterTextBlock;
	private ComboBoxItem m_tpSwappedOutComboBoxItem;
	private Canvas m_tpElementPopupChildCanvas;

	private Storyboard m_tpClosedStoryboard;
	private DependencyObject m_tpGeneratedContainerForContentPresenter;
	private int m_iLastGeneratedItemIndexforFaceplate;
	private object m_customValueRef;
	private Rect m_candidateWindowBoundsRect;

	// TypeAhead methods and members
	private string m_searchString = "";
	private DateTime m_timeSinceLastCharacterReceived;
	private PropertyPathListener m_spPropertyPathListener;
	// Keeps track of the item's index that matched the last search.
	private int m_searchResultIndex = -1;

	private bool ShouldMoveFocusToTextBox => m_shouldMoveFocusToTextBox;

	private bool IsSmallFormFactor => false; // TODO Uno: This is currently not supported.

	private bool IsInline => IsSmallFormFactor && m_itemCount <= s_itemCountThreshold;

	private bool IsFullMode => IsSmallFormFactor && m_itemCount > s_itemCountThreshold;

	private IAsyncInfo m_tpAsyncSelectionInfo;
	private int m_itemCount;

	private int m_indexToRestoreOnCancel = -1;
}

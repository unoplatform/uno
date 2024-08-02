using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media.Animation;
using Uno.Disposables;
using Uno.UI.Xaml.Input;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls;

partial class ComboBox
{
// TODO MZ: These disablings should not be required
#pragma warning disable CS0067 // Unused only in reference API.
#pragma warning disable CS0649 // Unused only in reference API.
#pragma warning disable CS0169 // Unused only in reference API.
#pragma warning disable CS0168 // Unused only in reference API.
#pragma warning disable CS0414 // Unused only in reference API.
	internal bool IsSearchResultIndexSet() => m_searchResultIndexSet;

	internal int GetSearchResultIndex() => m_searchResultIndex;

	private bool m_bIsPopupPannable;
	private bool m_bShouldCarousel;
	private bool m_bShouldCenterSelectedItem;
	private bool m_handledGamepadOrRemoteKeyDown;
	private bool m_ignoreCancelKeyDowns;
	private bool m_isEditModeConfigured;
	private bool m_isInSearchingMode;

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

	private Storyboard m_tpClosedStoryboard;
	private DependencyObject m_tpGeneratedContainerForContentPresenter;
	private int m_iLastGeneratedItemIndexforFaceplate;
	private object m_customValueRef;
	private Rect m_candidateWindowBoundsRect;

	private bool ShouldMoveFocusToTextBox => m_shouldMoveFocusToTextBox;

	private bool IsSmallFormFactor => false; // TODO Uno: This is currently not supported.

	private bool IsInline => IsSmallFormFactor && m_itemCount <= s_itemCountThreshold;

	private bool IsFullMode => IsSmallFormFactor && m_itemCount > s_itemCountThreshold;

	private IAsyncInfo m_tpAsyncSelectionInfo;
	private int m_itemCount;

	private int m_indexToRestoreOnCancel = -1;
}

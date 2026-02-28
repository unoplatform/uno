// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference src\dxaml\xcp\dxaml\lib\AutoSuggestBox_Partial.h, tag winui3/release/1.7.1

#if HAS_UNO
using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Uno.Disposables;
using Uno.UI.DataBinding;
using Windows.Foundation;
using Windows.System;
using Windows.UI.ViewManagement;
#endif

namespace Microsoft.UI.Xaml.Controls;

partial class AutoSuggestBox
{
#if HAS_UNO
	// Static constants specific to this control
	private const long TextChangedEventTimerDuration = 1500000L; // 150ms in 100-nanosecond units
	private const int MinSuggestionListHeight = 178; // 178 pixels = 44 pixels * 4 items + 2 pixels

	// Template part names
	private const string c_TextBoxName = "TextBox";
	private const string c_TextBoxQueryButtonName = "QueryButton";
	private const string c_SuggestionsPopupName = "SuggestionsPopup";
	private const string c_SuggestionsListName = "SuggestionsList";
	private const string c_SuggestionsContainerName = "SuggestionsContainer";
	private const string c_UpwardTransformName = "UpwardTransform";
	private const string c_TextBoxScrollViewerName = "ContentElement";
#pragma warning disable IDE0051 // Private member is unused
	private const string c_VisualStateLandscape = "Landscape";
	private const string c_VisualStatePortrait = "Portrait";
#pragma warning restore IDE0051
	private const string c_ListItemOrderTransformName = "ListItemOrderTransform";
	private const string c_LayoutRootName = "LayoutRoot";
#pragma warning disable IDE0051 // Private member is unused
	private const string c_RequiredHeaderName = "RequiredHeaderPresenter";
#pragma warning restore IDE0051
	private const string c_DescriptionPresenterName = "DescriptionPresenter";

	// Enum for controlling the ControlledPeer
	private enum ControlledPeer
	{
		None,
		SuggestionsList
	}

	// Enum for suggestion list position
	private enum SuggestionListPosition
	{
		Above,
		Below
	}

	// Enum for input device type
	private enum InputDeviceType
	{
		None,
		Keyboard,
		GamepadOrRemote
	}

	// Template parts
	private TextBox m_tpTextBoxPart;
#pragma warning disable CS0414 // The field is assigned but its value is never used
	private UIElement m_requiredHeaderPresenterPart;
#pragma warning restore CS0414
	private ButtonBase m_tpTextBoxQueryButtonPart;
	private Selector m_tpSuggestionsPart;
	private Popup m_tpPopupPart;
	private FrameworkElement m_tpSuggestionsContainerPart;
	private TranslateTransform m_tpUpwardTransformPart;
	private Transform m_tpListItemOrderTransformPart;
	private Grid m_tpLayoutRootPart;
	private DispatcherTimer m_tpTextChangedEventTimer;
	private AutoSuggestBoxTextChangedEventArgs m_tpTextChangedEventArgs;

	// Text change reason
	private AutoSuggestionBoxTextChangeReason m_textChangeReason = AutoSuggestionBoxTextChangeReason.UserInput;

	// Counter for text changed events
	// counter++ when Text is changed. TextChangedEvent will use this counter
	// to determine if the current value of the TextBox is unchanged from
	// the point when TextChangedEvent was raised.
	private uint m_textChangedCounter = 0;

	// Event revokers
	private readonly SerialDisposable m_unloadedEventRevoker = new();
	private readonly SerialDisposable m_sizeChangedEventRevoker = new();
	private readonly SerialDisposable m_textBoxTextChangedEventRevoker = new();
	private readonly SerialDisposable m_textChangedEventTimerTickEventRevoker = new();
	private readonly SerialDisposable m_suggestionSelectionChangedEventRevoker = new();
	private readonly SerialDisposable m_suggestionListKeyDownEventRevoker = new();
	private readonly SerialDisposable m_listViewItemClickEventRevoker = new();
	private readonly SerialDisposable m_listViewContainerContentChangingEventRevoker = new();
	private readonly SerialDisposable m_popupOpenedEventRevoker = new();
	private readonly SerialDisposable m_suggestionsContainerLoadedEventRevoker = new();
	private readonly SerialDisposable m_textBoxLoadedEventRevoker = new();
	private readonly SerialDisposable m_textBoxUnloadedEventRevoker = new();
	private readonly SerialDisposable m_queryButtonClickEventRevoker = new();
	private readonly SerialDisposable m_textBoxCandidateWindowBoundsChangedEventRevoker = new();
	private readonly SerialDisposable m_layoutUpdatedEventRevoker = new();
	private readonly SerialDisposable m_sipShowingEventRevoker = new();
	private readonly SerialDisposable m_sipHidingEventRevoker = new();

	// Weak reference to root scroll viewer
	private WeakReference<ScrollViewer> m_wkRootScrollViewer;

	// Scroll actions for adjusting position when SIP is showing
	private struct ScrollAction
	{
		public WeakReference<ScrollViewer> WkScrollViewer;
		public double Target;
		public double Initial;
	}
	private List<ScrollAction> m_scrollActions = new();

	// Suggestion list position state
	private SuggestionListPosition m_suggestionListPosition = SuggestionListPosition.Below;

	// Input device type used
	private InputDeviceType m_inputDeviceTypeUsed = InputDeviceType.None;

	// SIP (Soft Input Panel) state
	// when SIP is showing, we'll get two SIP_Showing events
	// when SIP is hiding, we get one SIP_Showing following by a SIP_Hiding event
	// to avoid redundant adjustment, use this flag to mark the SIP status.
	private bool m_isSipVisible = false;

	// Flag to ignore selection changes
	private bool m_ignoreSelectionChanges = false;

	// Flag to ignore text changes
	private bool m_ignoreTextChanges = false;

	// User typed text
	private string m_userTypedText = "";

	// Available suggestion height
	private double m_availableSuggestionHeight = 0.0;

	// Focus state
	private bool m_hasFocus = false;
	private bool m_keepFocus = false;

	// Flag to suppress suggestion list visibility
	private bool m_suppressSuggestionListVisibility = false;

	// Flag for deferred update
	private bool m_deferringUpdate = false;

	// Property path listener for TextMemberPath
	private BindingPath m_spPropertyPathListener;

	// Static flag for SIP open state
	private static bool s_sipIsOpen = false;

#if DEBUG
	// Debug flag for handling collection change
	private bool m_handlingCollectionChange = false;
#endif

	// Display orientation
	// TODO UNO: Add display orientation tracking if needed
	// private XamlDisplay.Orientation m_displayOrientation = XamlDisplay.Orientation.None;

	// Candidate window bounds rect
	private Rect m_candidateWindowBoundsRect = default;

	// Overlay visibility state
	private bool m_isOverlayVisible = false;

	// Layout transition elements (for light dismiss overlay)
	// TODO UNO: These are unused until LTE support is implemented
#pragma warning disable CS0169, IDE0051 // The field is never used
	private UIElement m_overlayLayoutTransition;
	private UIElement m_layoutTransition;
	private UIElement m_parentElementForLTEs;
	private FrameworkElement m_overlayElement;
#pragma warning restore CS0169, IDE0051

	// InputPane reference
	private InputPane m_tpInputPane;

	// InputPane visibility event args (for deferred showing)
	// TODO UNO: Implement InputPaneVisibilityEventArgs handling if needed
	// private InputPaneVisibilityEventArgs m_tpSipArgs;

	/// <summary>
	/// Helper function to determine if the suggestion list is vertically mirrored.
	/// In WP8.1, we implemented the mode where the suggestion list is above the textbox
	/// by vertically mirroring the Suggestion ListView. Now that ASB needs to support
	/// keyboard and mouse in win10, we now reverse the vector we set on the suggestion
	/// ListView's ItemsSource instead. We keep the old path for compat. If the ASB's
	/// template contains the ScaleTransform that flips the ListView (m_tpListItemOrderTransformPart)
	/// we'll go ahead and use it. Else, we'll do it the new way (reverse the vector).
	/// </summary>
	private bool IsSuggestionListVerticallyMirrored =>
		m_suggestionListPosition == SuggestionListPosition.Above && m_tpListItemOrderTransformPart is not null;

	/// <summary>
	/// This helper function is used to identify the ASBs which have new ASB implementation
	/// where we reverse the vector set on the suggestion ListView's ItemSource.
	/// </summary>
	private bool IsSuggestionListVectorReversed =>
		m_suggestionListPosition == SuggestionListPosition.Above && m_tpListItemOrderTransformPart is null;

	/// <summary>
	/// This helper function is used to identify whether we should move down (look at an item of higher
	/// index) or move up (look at an item of lower index) in the list.
	/// </summary>
	private bool ShouldMoveIndexForwardForKey(VirtualKey key, VirtualKeyModifiers modifiers) =>
		(key == VirtualKey.Down && !IsSuggestionListVerticallyMirrored) ||
		(key == VirtualKey.Up && IsSuggestionListVerticallyMirrored) ||
		(key == VirtualKey.Tab && !modifiers.HasFlag(VirtualKeyModifiers.Shift) && !IsSuggestionListVerticallyMirrored) ||
		(key == VirtualKey.Tab && modifiers.HasFlag(VirtualKeyModifiers.Shift) && IsSuggestionListVerticallyMirrored);

#endif
}

#nullable enable

using System;
using System.Collections.Generic;
using Uno.Disposables;
using Uno.UI.DataBinding;
using Uno.UI.Xaml.Input;
using Windows.Foundation;
using Windows.Graphics.Display;
using Windows.System;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Media;

namespace Windows.UI.Xaml.Controls;

public partial class AutoSuggestBox
{
	private enum ControlledPeer
	{
		None,
        SuggestionsList
	}

	// In WP8.1, we implemented the mode where the suggestion list is above the textbox
	// by veritically mirroring the Suggestion ListView.  Now that ASB needs to support
	// keyboard and mouse in win10, we now reverse the vector we set on the suggestion
	// ListView's ItemsSource instead.  We keep the old path for compat.  If the ASB's
	// template contains the ScaleTransform that flips the ListView (m_tpListItemOrderTransformPart)
	// we'll go ahead and use it. Else, we'll do it the new way (reverse the vector).
	private bool IsSuggestionListVerticallyMirrored()
	{
		return m_suggestionListPosition == SuggestionListPosition.Above
			&& m_tpListItemOrderTransformPart != null;
	}

	// This helper function is used to identify the ASBs which have new ASB implementation
	// where we reverse the vector set on the suggestion ListView's ItemSource.
	private bool IsSuggestionListVectorReversed()
	{
		return m_suggestionListPosition == SuggestionListPosition.Above
			&& m_tpListItemOrderTransformPart == null;
	}

	// This helper function is used to identify whether we should move down (look at an item of higher
	// index) or move up (look at an item of lower index) in the list.
	private bool ShouldMoveIndexForwardForKey(VirtualKey key, VirtualKeyModifiers modifiers)
	{
		return (key == VirtualKey.Down && !IsSuggestionListVerticallyMirrored()) ||
			(key == VirtualKey.Up && IsSuggestionListVerticallyMirrored()) ||
			(key == VirtualKey.Tab && !modifiers.HasFlag(VirtualKeyModifiers.Shift) && !IsSuggestionListVerticallyMirrored()) ||
			(key == VirtualKey.Tab && modifiers.HasFlag(VirtualKeyModifiers.Shift) && IsSuggestionListVerticallyMirrored());
	}

	//	// static constatants specific to this control:
	//	static const INT64 s_textChangedEventTimerDuration;

	//	static const unsigned int s_minSuggestionListHeight;

	// template parts
	private TextBox m_tpTextBoxPart;
	private UIElement? m_requiredHeaderPresenterPart;
	private ButtonBase? m_tpTextBoxQueryButtonPart;

	private Selector? m_tpSuggestionsPart;

	private Popup? m_tpPopupPart;

	private FrameworkElement? m_tpSuggestionsContainerPart;

	// when we need to show suggestion list above the text box, we apply a transform to move the suggestion list.
	private TranslateTransform? m_tpUpwardTransformPart;

	// when we need to show suggestion list above the text box, we invert the order of the items in the list.
	private Transform? m_tpListItemOrderTransformPart;

	private Grid? m_tpLayoutRootPart;

	private DispatcherTimer m_tpTextChangedEventTimer;

	private AutoSuggestBoxTextChangedEventArgs? m_tpTextChangedEventArgs;

	private AutoSuggestionBoxTextChangeReason m_textChangeReason = AutoSuggestionBoxTextChangeReason.UserInput;

	private InputPane? m_tpInputPane;

	private InputPaneVisibilityEventArgs? m_tpSipArgs;

	// counter++ when Text is changed. TextChangedEvent will use this counter
	// to determine if the current value of the TextBox is unchanged from
	// the point when TextChangedEvent was raised.
	private int m_textChangedCounter;

	//	// events:
	//	EventRegistrationToken m_sipEvents[2] {};

	private readonly SerialDisposable m_epUnloadedEventHandler = new();
	private readonly SerialDisposable m_epSizeChangedEventHandler = new();

	private readonly SerialDisposable m_epTextBoxTextChangedEventHandler = new();
	private readonly SerialDisposable m_epTextChangedEventTimerTickEventHandler = new();
	private readonly SerialDisposable m_epSuggestionSelectionChangedEventHandler = new();
	private readonly SerialDisposable m_suggestionListKeyDownEventHandler = new();
	private readonly SerialDisposable m_epListViewItemClickEventHandler = new();
	private readonly SerialDisposable m_epListViewContainerContentChangingEventHandler = new();
	private readonly SerialDisposable m_epPopupOpenedEventHandler = new();
	private readonly SerialDisposable m_epSuggestionsContainerLoadedEventHandler = new();

	private readonly SerialDisposable m_epTextBoxLoadedEventHandler = new();
	private readonly SerialDisposable m_epTextBoxUnloadedEventHandler = new();
	private readonly SerialDisposable m_epQueryButtonClickEventHandler = new();

	private readonly SerialDisposable m_epTextBoxCandidateWindowBoundsChangedEventHandler = new();
	private readonly SerialDisposable m_layoutUpdatedEventHandler = new();

	//wrl::ComPtr<ReversedVector> m_spReversedVector;
	
	private struct ScrollAction
	{
		public ManagedWeakReference WkScrollViewer { get; set; }

		public double Target { get; set; }

		public double Initial { get; set; }
	}

	// save the scroll actions when we adjust the position (typically when this control gets focus)
	// restore them when SIP is gone (not in LoseFocus because user may change the focus to another
	// textbox, in that case the SIP is still on).
	private List<ScrollAction> m_scrollActions = new();

	// RootScrollViewer acts an important role when we adjust the position.
	// when SIP is on, the RootScrollViewer's viewport will be changed so the content inside
	// RootScrollViewer can be scrollable.
	// here there is a known issue that when RootScrollViewer's viewport is being restored and the
	// content becomes non-scrollable, but the vertical offset is not reset back to 0.
	// AutoSuggestBox needs correct offset to determine how much space we can scroll it up or down.
	// the workaround is when SIP is hiding, we manually reset the vertical offset to 0.
	private ManagedWeakReference? m_wkRootScrollViewer;

	private enum SuggestionListPosition
	{
		Above,
		Below
	}

	private SuggestionListPosition m_suggestionListPosition = SuggestionListPosition.Below;

	private InputDeviceType m_inputDeviceTypeUsed = InputDeviceType.None;

	//// when SIP is showing, we'll get two SIP_Showing events
	//// when SIP is hiding, we get one SIP_Showing following by a SIP_Hiding event
	//// to avoid redundant adjustment, use this flag to mark the SIP status.
	private bool m_isSipVisible;

	private bool m_ignoreSelectionChanges;

	private bool m_ignoreTextChanges;

	private string m_userTypedText;

	private double m_availableSuggestionHeight;

	private bool m_hasFocus;

	private bool m_keepFocus;

	private bool m_suppressSuggestionListVisibility;

	private bool m_deferringUpdate;
	//ctl::ComPtr<PropertyPathListener> m_spPropertyPathListener;

	//static bool m_sSipIsOpen;

#if DBG
	bool m_handlingCollectionChange= false;
#endif
	private DisplayOrientations m_displayOrientation = DisplayOrientations.None;

	private Rect m_candidateWindowBoundsRect; //TODO:MZ: Or .Empty?

	private bool m_isOverlayVisible;

	private UIElement? m_overlayLayoutTransition;
	private UIElement? m_layoutTransition;

	private UIElement? m_parentElementForLTEs;
	private FrameworkElement? m_overlayElement;
}

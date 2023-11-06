// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference AutoSuggestBox_Partial.h, tag winui3/release/1.4.2

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using DirectUI;
using Uno.Disposables;
using Uno.Extensions.Specialized;
using Uno.Foundation.Logging;
using Uno.UI;
using Uno.UI.DataBinding;
using Uno.UI.Xaml.Core;
using Uno.UI.Xaml.Input;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Display;
using Windows.Security.Cryptography.Core;
using Windows.System;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml.Automation;
using Windows.UI.Xaml.Automation.Peers;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;
using WinUICoreServices = Uno.UI.Xaml.Core.CoreServices;

namespace Windows.UI.Xaml.Controls;

public partial class AutoSuggestBox : ItemsControl
{
	private enum ControlledPeer
	{
		None,
		SuggestionsList,
	}

	// In WP8.1, we implemented the mode where the suggestion list is above the textbox
	// by veritically mirroring the Suggestion ListView.  Now that ASB needs to support
	// keyboard and mouse in win10, we now reverse the vector we set on the suggestion
	// ListView's ItemsSource instead.  We keep the old path for compat.  If the ASB's
	// template contains the ScaleTransform that flips the ListView (m_tpListItemOrderTransformPart)
	// we'll go ahead and use it.  Else, we'll do it the new way (reverse the vector).
	private bool IsSuggestionListVerticallyMirrored()
	{
		return m_suggestionListPosition == SuggestionListPosition.Above
			&& m_tpListItemOrderTransformPart is not null;
	}

	// This helper function is used to identify the ASBs which have new ASB implementation
	// where we reverse the vector set on the suggestion ListView's ItemSource.
	private bool IsSuggestionListVectorReversed()
	{
		return m_suggestionListPosition == SuggestionListPosition.Above
			&& m_tpListItemOrderTransformPart is null;
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

	// template parts
	private TextBox m_tpTextBoxPart;
	//private UIElement m_requiredHeaderPresenterPart;
	private ButtonBase m_tpTextBoxQueryButtonPart;

	private Selector m_tpSuggestionsPart;

	private Popup m_tpPopupPart;

	private FrameworkElement m_tpSuggestionsContainerPart;

	// when we need to show suggestion list above the text box, we apply a transform to move the suggestion list.
	private TranslateTransform m_tpUpwardTransformPart;

	// when we need to show suggestion list above the text box, we invert the order of the items in the list.
	private Transform m_tpListItemOrderTransformPart;

	private Grid m_tpLayoutRootPart;

	private DispatcherTimer m_tpTextChangedEventTimer;

	private AutoSuggestBoxTextChangedEventArgs m_tpTextChangedEventArgs;

	private AutoSuggestionBoxTextChangeReason m_textChangeReason = AutoSuggestionBoxTextChangeReason.UserInput;

	private InputPane m_tpInputPane;

	private InputPaneVisibilityEventArgs m_tpSipArgs;

	// counter++ when Text is changed. TextChangedEvent will use this counter
	// to determine if the current value of the TextBox is unchanged from
	// the point when TextChangedEvent was raised.
	// UNO TODO:
	//private uint m_textChangedCounter;

	// events:
	private TypedEventHandler<InputPane, InputPaneVisibilityEventArgs>[] m_sipEvents = new TypedEventHandler<InputPane, InputPaneVisibilityEventArgs>[2];

	//ctl::EventPtr<FrameworkElementUnloadedEventCallback> m_epUnloadedEventHandler;
	//ctl::EventPtr<FrameworkElementSizeChangedEventCallback> m_epSizeChangedEventHandler;

	//ctl::EventPtr<TextBoxTextChangedEventCallback> m_epTextBoxTextChangedEventHandler;
	//ctl::EventPtr<DispatcherTimerTickEventCallback> m_epTextChangedEventTimerTickEventHandler;
	//ctl::EventPtr<SelectionChangedEventCallback> m_epSuggestionSelectionChangedEventHandler;
	//ctl::EventPtr<UIElementKeyDownEventCallback> m_suggestionListKeyDownEventHandler;
	//ctl::EventPtr<ListViewBaseItemClickEventCallback> m_epListViewItemClickEventHandler;
	//ctl::EventPtr<ListViewBaseContainerContentChangingEventCallback> m_epListViewContainerContentChangingEventHandler;
	//ctl::EventPtr<PopupOpenedEventCallback> m_epPopupOpenedEventHandler;
	//ctl::EventPtr<FrameworkElementLoadedEventCallback> m_epSuggestionsContainerLoadedEventHandler;

	//ctl::EventPtr<FrameworkElementLoadedEventCallback> m_epTextBoxLoadedEventHandler;
	//ctl::EventPtr<FrameworkElementUnloadedEventCallback> m_epTextBoxUnloadedEventHandler;
	//ctl::EventPtr<ButtonBaseClickEventCallback> m_epQueryButtonClickEventHandler;

	//ctl::EventPtr<TextBoxCandidateWindowBoundsChangedEventCallback> m_epTextBoxCandidateWindowBoundsChangedEventHandler;
	//ctl::EventPtr<FrameworkElementLayoutUpdatedEventCallback> m_layoutUpdatedEventHandler;

	//ReversedVector m_spReversedVector;

	private struct ScrollAction
	{
		public WeakReference<ScrollViewer> wkScrollViewer;
		public double target;
		public double initial;
	}

	// save the scroll actions when we adjust the position (typically when this control gets focus)
	// restore them when SIP is gone (not in LoseFocus because user may change the focus to another
	// textbox, in that case the SIP is still on).
	private List<ScrollAction> m_scrollActions = new();

	private WeakReference<ScrollViewer> m_wkRootScrollViewer;

	private enum SuggestionListPosition
	{
		Above,
		Below,
	}

	private SuggestionListPosition m_suggestionListPosition = SuggestionListPosition.Below;

	private InputDeviceType m_inputDeviceTypeUsed;

	// when SIP is showing, we'll get two SIP_Showing events
	// when SIP is hiding, we get one SIP_Showing following by a SIP_Hiding event
	// to avoid redundant adjustment, use this flag to mark the SIP status.
	private bool m_isSipVisible;

	private bool m_ignoreSelectionChanges;

	private bool m_ignoreTextChanges;

	private string m_userTypedText;

	private double m_availableSuggestionHeight;

	private bool m_hasFocus;

	private bool m_keepFocus;

	private bool m_suppressSuggestionListVisibility;

	private bool m_deferringUpdate;

	// UNO TODO:
	//private PropertyPathListener m_spPropertyPathListener;

	private bool m_sSipIsOpen;

#if DEBUG
	private bool m_handlingCollectionChange;
#endif
	private DisplayOrientations m_displayOrientation;

	// UNO TODO:
	//private Rect m_candidateWindowBoundsRect;

	private bool m_isOverlayVisible;

	// UNO TODO:
	//private UIElement m_overlayLayoutTransition;
	//private UIElement m_layoutTransition;

	//private UIElement m_parentElementForLTEs;
	private FrameworkElement m_overlayElement;
}

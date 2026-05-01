// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference AutoSuggestBox_Partial.h, commit 5f9e85113

//      The AutoSuggestBox control is a standard textbox that provides contextual
//      suggestions in a temporary drop-down list while users type in characters.
//
//      Suggestions change as the user types more characters.

#nullable disable

#pragma warning disable CS0169 // Field never used — placeholder fields awaiting impl
#pragma warning disable CS0414 // Unused field — placeholder fields awaiting impl
#pragma warning disable CS0649 // Unassigned field — placeholder fields awaiting impl
#pragma warning disable IDE0044 // Add readonly modifier — placeholder fields awaiting impl
#pragma warning disable IDE0051 // Unused private member — placeholder members awaiting impl
#pragma warning disable IDE0052 // Read but unused — placeholder fields awaiting impl

using System;
using System.Collections.Generic;
using DirectUI;
using Uno.UI.Xaml.Input;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Uno.Disposables;
using Windows.Foundation;
using Windows.System;
using Windows.UI.ViewManagement;

namespace Microsoft.UI.Xaml.Controls
{
	partial class AutoSuggestBox
	{
		// private typedefs:
		// typedef wf::ITypedEventHandler<wuv::InputPane*, wuv::InputPaneVisibilityEventArgs*> InputPaneVisibilityEventHandler;

		private enum ControlledPeer
		{
			None,
			SuggestionsList
		}

		private struct ScrollAction
		{
			public WeakReference<ScrollViewer> wkScrollViewer;
			public double target;
			public double initial;
		}

		private enum SuggestionListPosition
		{
			Above,
			Below
		}

		// static constants specific to this control:
		private const long s_textChangedEventTimerDuration = 1500000L; // 150ms

		private const uint s_minSuggestionListHeight = 178; // 178 pixels = 44 pixels * 4 items + 2 pixels

		private const string c_TextBoxName = "TextBox";
		private const string c_TextBoxQueryButtonName = "QueryButton";
		private const string c_SuggestionsPopupName = "SuggestionsPopup";
		private const string c_SuggestionsListName = "SuggestionsList";
		private const string c_SuggestionsContainerName = "SuggestionsContainer";
		private const string c_UpwardTransformName = "UpwardTransform";
		private const string c_TextBoxScrollViewerName = "ContentElement";
		private const string c_VisualStateLandscape = "Landscape";
		private const string c_VisualStatePortrait = "Portrait";
		private const string c_ListItemOrderTransformName = "ListItemOrderTransform";
		private const string c_LayoutRootName = "LayoutRoot";
		private const string c_RequiredHeaderName = "RequiredHeaderPresenter";

		// template parts
		private TextBox m_tpTextBoxPart;
		private UIElement m_requiredHeaderPresenterPart;
		private ButtonBase m_tpTextBoxQueryButtonPart;

		private Primitives.Selector m_tpSuggestionsPart;

		private Primitives.Popup m_tpPopupPart;

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
		private uint m_textChangedCounter;

		// events:
		// EventRegistrationToken m_sipEvents[2] {};
		private IDisposable m_sipEvents0;
		private IDisposable m_sipEvents1;

		private readonly SerialDisposable m_epUnloadedEventHandler = new SerialDisposable();
		private readonly SerialDisposable m_epSizeChangedEventHandler = new SerialDisposable();

		private readonly SerialDisposable m_epTextBoxTextChangedEventHandler = new SerialDisposable();
		private readonly SerialDisposable m_epTextChangedEventTimerTickEventHandler = new SerialDisposable();
		private readonly SerialDisposable m_epSuggestionSelectionChangedEventHandler = new SerialDisposable();
		private readonly SerialDisposable m_suggestionListKeyDownEventHandler = new SerialDisposable();
		private readonly SerialDisposable m_epListViewItemClickEventHandler = new SerialDisposable();
		private readonly SerialDisposable m_epListViewContainerContentChangingEventHandler = new SerialDisposable();
		private readonly SerialDisposable m_epPopupOpenedEventHandler = new SerialDisposable();
		private readonly SerialDisposable m_epSuggestionsContainerLoadedEventHandler = new SerialDisposable();

		private readonly SerialDisposable m_epTextBoxLoadedEventHandler = new SerialDisposable();
		private readonly SerialDisposable m_epTextBoxUnloadedEventHandler = new SerialDisposable();
		private readonly SerialDisposable m_epQueryButtonClickEventHandler = new SerialDisposable();

		private readonly SerialDisposable m_epTextBoxCandidateWindowBoundsChangedEventHandler = new SerialDisposable();
		private readonly SerialDisposable m_layoutUpdatedEventHandler = new SerialDisposable();

		// wrl::ComPtr<ReversedVector> m_spReversedVector;
		// TODO Uno: ReversedVector is a WinRT helper that exposes a reversed view of an IObservableVector.
		// Replace with a managed equivalent when implementing IsSuggestionListVectorReversed path.
		private object m_spReversedVector;

		// save the scroll actions when we adjust the position (typically when this control gets focus)
		// restore them when SIP is gone (not in LoseFocus because user may change the focus to another
		// textbox, in that case the SIP is still on).
		private List<ScrollAction> m_scrollActions = new List<ScrollAction>();

		// RootScrollViewer acts an important role when we adjust the position.
		// when SIP is on, the RootScrollViewer's viewport will be changed so the content inside
		// RootScrollViewer can be scrollable.
		// here there is a known issue that when RootScrollViewer's viewport is being restored and the
		// content becomes non-scrollable, but the vertical offset is not reset back to 0.
		// AutoSuggestBox needs correct offset to determine how much space we can scroll it up or down.
		// the workaround is when SIP is hiding, we manually reset the vertical offset to 0.
		private WeakReference<ScrollViewer> m_wkRootScrollViewer;

		private SuggestionListPosition m_suggestionListPosition = SuggestionListPosition.Below;

		private InputDeviceType m_inputDeviceTypeUsed = InputDeviceType.None;

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
		// TODO Uno: PropertyPathListener — internal WinUI helper that observes property-path changes.
		// Replace with Uno's BindingPath / PropertyPathParser equivalent when wiring TryGetSuggestionValue.
		private object m_spPropertyPathListener;

		private static bool m_sSipIsOpen;

#if DBG
		private bool m_handlingCollectionChange;
#endif
		// TODO Uno: XamlDisplay::Orientation — display orientation tracking helper from DisplayOrientationHelper.h.
		// Map to Windows.Graphics.Display.DisplayOrientations or maintain a private enum for this control.
		private int m_displayOrientation; // XamlDisplay::Orientation::None == 0

		private Rect m_candidateWindowBoundsRect;

		private bool m_isOverlayVisible;

		private UIElement m_overlayLayoutTransition;
		private UIElement m_layoutTransition;

		private UIElement m_parentElementForLTEs;
		private FrameworkElement m_overlayElement;

		// In WP8.1, we implemented the mode where the suggestion list is above the textbox
		// by veritically mirroring the Suggestion ListView.  Now that ASB needs to support
		// keyboard and mouse in win10, we now reverse the vector we set on the suggestion
		// ListView's ItemsSource instead.  We keep the old path for compat.  If the ASB's
		// template contains the ScaleTransform that flips the ListView (m_tpListItemOrderTransformPart)
		// we'll go ahead and use it.  Else, we'll do it the new way (reverse the vector).
		private bool IsSuggestionListVerticallyMirrored() =>
			m_suggestionListPosition == SuggestionListPosition.Above
				&& m_tpListItemOrderTransformPart is not null;

		// This helper function is used to identify the ASBs which have new ASB implementation
		// where we reverse the vector set on the suggestion ListView's ItemSource.
		private bool IsSuggestionListVectorReversed() =>
			m_suggestionListPosition == SuggestionListPosition.Above
				&& m_tpListItemOrderTransformPart is null;

		// This helper function is used to identify whether we should move down (look at an item of higher
		// index) or move up (look at an item of lower index) in the list.
		private bool ShouldMoveIndexForwardForKey(VirtualKey key, VirtualKeyModifiers modifiers) =>
			(key == VirtualKey.Down && !IsSuggestionListVerticallyMirrored()) ||
			(key == VirtualKey.Up && IsSuggestionListVerticallyMirrored()) ||
			(key == VirtualKey.Tab && !modifiers.HasFlag(VirtualKeyModifiers.Shift) && !IsSuggestionListVerticallyMirrored()) ||
			(key == VirtualKey.Tab && modifiers.HasFlag(VirtualKeyModifiers.Shift) && IsSuggestionListVerticallyMirrored());

		internal uint GetTextChangedEventCounter() => m_textChangedCounter;

		private void ResetIgnoreTextChanges()
		{
			m_ignoreTextChanges = false;
		}
	}
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference AutoSuggestBox_Partial.cpp, tag winui3/release/1.4.2

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
using Uno.UI.Helpers;
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

namespace Windows.UI.Xaml.Controls
{
	public partial class AutoSuggestBox : ItemsControl
	{
		private const long s_textChangedEventTimerDuration = 1500000L;    // 150ms
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
		//private const string c_RequiredHeaderName = "RequiredHeaderPresenter";

		public AutoSuggestBox()
		{
			DefaultStyleKey = typeof(AutoSuggestBox);

			// TODO Uno specific: This is called by DirectUI automatically,
			// we have to do it manually here.
			PrepareState();
		}

		// UNO TODO:
		//~AutoSuggestBox()
		//{
		//	if (m_tpInputPane)
		//	{
		//		if (m_sipEvents[0].value != 0)
		//		{
		//			m_tpInputPane.Showing -= m_sipEvents[0];
		//		}

		//		if (m_sipEvents[1].value != 0)
		//		{
		//			m_tpInputPane.Hiding -= m_sipEvents[1];
		//		}
		//	}

		//	// Should have been clean up in the unloaded handler.
		//	Debug.Assert(!m_isOverlayVisible);
		//}

		//------------------------------------------------------------------------------
		// AutoSuggestBox PrepareState
		//
		// Prepares this control by attaching needed event handlers
		//------------------------------------------------------------------------------
		private void PrepareState()
		{
			InputPane spInputPane;
			DispatcherTimer spTextChangedEventTimer;
			TimeSpan interval;

			// base.PrepareState();

			// If we're in the context of XAML islands, then we don't want to use InputPane -
			// that requires a UIContext instance, which is not supported in WinUI 3.
			if (DXamlCore.Current.GetHandle().InitializationType != InitializationType.IslandsOnly)
			{
				// Acquire an instance to IInputPane. It is used to listen on
				// SIP events and queries that type for the size occupied by
				// that SIP when it is visible, so we can position this
				// control correctly.

				spInputPane = InputPane.GetForCurrentView(); //Task 23548475 Get correct input pane without UIContext.

				m_tpInputPane = spInputPane;

				// listen on visibility changes:
				m_sipEvents[0] = OnSipShowing;
				spInputPane.Showing += m_sipEvents[0];

				m_sipEvents[1] = OnSipHiding;
				spInputPane.Hiding += m_sipEvents[1];
			}

			spTextChangedEventTimer = new DispatcherTimer();
			m_tpTextChangedEventTimer = spTextChangedEventTimer;

			interval = new(ticks: s_textChangedEventTimerDuration);
			spTextChangedEventTimer.Interval = interval;

			this.Unloaded += OnUnloaded;

			this.SizeChanged += OnSizeChanged;
		}

		private void OnIsSuggestionListOpenChanged(DependencyPropertyChangedEventArgs e)
		{
			if (e.NewValue is true)
			{
				Focus(FocusState.Programmatic);
			}
		}

		protected override void OnApplyTemplate()
		{
			TextBox spTextBoxPart;
			Selector spSuggestionsPart;
			Popup spPopupPart;
			FrameworkElement spSuggestionsContainerPart;
			TranslateTransform spUpwardTransformPart;
			Transform spListItemOrderTransformPart;
			Grid layoutRoot;
			//UIElement requiredHeaderPart = null;
			m_wkRootScrollViewer = null;

			// unwire old template part (if existing)
			if (m_tpSuggestionsPart is not null)
			{
				ListViewBase spListViewPart = m_tpSuggestionsPart as ListViewBase; ;

				if (spListViewPart is not null)
				{
					spListViewPart.ItemClick -= OnListViewItemClick;
					spListViewPart.ContainerContentChanging -= OnListViewContainerContentChanging;
				}

				m_tpSuggestionsPart.SelectionChanged -= OnSuggestionSelectionChanged;
				m_tpSuggestionsPart.KeyDown -= OnSuggestionListKeyDown;
			}

			if (m_tpPopupPart is not null)
			{
				m_tpPopupPart.Opened -= OnPopupOpened;
			}

			if (m_tpSuggestionsContainerPart is not null)
			{
				m_tpSuggestionsContainerPart.Loaded -= OnSuggestionsContainerLoaded;
			}

			if (m_tpTextBoxPart is not null)
			{
				m_tpTextBoxPart.TextChanged -= OnTextBoxTextChanged;
				m_tpTextBoxPart.Loaded -= OnTextBoxLoaded;
#if !HAS_UNO // Not yet implemented in Uno.
				m_tpTextBoxPart.CandidateWindowBoundsChanged -= OnTextBoxCandidateWindowBoundsChanged;
#endif
			}

			if (m_tpTextBoxQueryButtonPart is not null)
			{
				ClearTextBoxQueryButtonIcon();
				m_tpTextBoxQueryButtonPart.Click -= OnTextBoxQueryButtonClick;
			}

			m_tpSuggestionsPart = null;
			m_tpPopupPart = null;
			m_tpSuggestionsContainerPart = null;
			m_tpTextBoxPart = null;
			m_tpUpwardTransformPart = null;
			m_tpListItemOrderTransformPart = null;
			m_tpLayoutRootPart = null;
			//m_requiredHeaderPresenterPart = null;

			base.OnApplyTemplate();

			HookToRootScrollViewer();

			spTextBoxPart = GetTemplateChild<TextBox>(c_TextBoxName);
			spSuggestionsPart = GetTemplateChild<Selector>(c_SuggestionsListName);
			spPopupPart = GetTemplateChild<Popup>(c_SuggestionsPopupName);
			spSuggestionsContainerPart = GetTemplateChild<FrameworkElement>(c_SuggestionsContainerName);
			spUpwardTransformPart = GetTemplateChild<TranslateTransform>(c_UpwardTransformName);
			spListItemOrderTransformPart = GetTemplateChild<Transform>(c_ListItemOrderTransformName);
			layoutRoot = GetTemplateChild<Grid>(c_LayoutRootName);

			// UNO TODO:
			//if (IsValueRequired(this))
			//{
			//	requiredHeaderPart = GetTemplateChild<UIElement>(c_RequiredHeaderName);
			//}
			//m_requiredHeaderPresenterPart = requiredHeaderPart;

			m_tpTextBoxPart = spTextBoxPart;
			m_tpSuggestionsPart = spSuggestionsPart;
			m_tpPopupPart = spPopupPart;
			m_tpSuggestionsContainerPart = spSuggestionsContainerPart;
			m_tpUpwardTransformPart = spUpwardTransformPart;
			m_tpListItemOrderTransformPart = spListItemOrderTransformPart;
			m_tpLayoutRootPart = layoutRoot;

			if (m_tpTextBoxPart is not null)
			{
				string originalText;

				m_tpTextBoxPart.TextChanged += OnTextBoxTextChanged;

				// added code to initialize text box
				// retrieving original text set by user in the "Text" field
				// updating the text box text with the original text
				originalText = Text;
				UpdateTextBoxText(originalText, AutoSuggestionBoxTextChangeReason.ProgrammaticChange);

				m_tpTextBoxPart.Loaded += OnTextBoxLoaded;

				m_tpTextBoxPart.Unloaded += OnTextBoxUnloaded;

#if !HAS_UNO // Not yet implemented in Uno.
				m_tpTextBoxPart.CandidateWindowBoundsChanged += OnTextBoxCandidateWindowBoundsChanged;
#endif

				// Add the automation name from the group to the edit box.
				string automationName = AutomationProperties.GetName(this);
				if (automationName is not null)
				{
					AutomationProperties.SetName(m_tpTextBoxPart, automationName);
				}

				// Pass our validation context and command onto the editable textbox
				//ctl::ComPtr<xaml_controls::IInputValidationContext> context;
				//IFC_RETURN(get_ValidationContext(&context));
				//ctl::ComPtr<xaml_controls::IInputValidationControl> textBoxValidation;
				//IFC_RETURN(spTextBoxPart.As(&textBoxValidation));
				//IFC_RETURN(textBoxValidation->put_ValidationContext(context.Get()));

				//ctl::ComPtr<xaml_controls::IInputValidationCommand> command;
				//IFC_RETURN(get_ValidationCommand(&command));
				//ctl::ComPtr<xaml_controls::IInputValidationControl2> textBoxValidation2;
				//IFC_RETURN(spTextBoxPart.As(&textBoxValidation2));
				//IFC_RETURN(textBoxValidation2->put_ValidationCommand(command.Get()));
			}

			if (m_tpSuggestionsPart is not null)
			{
				ListViewBase spListViewPart;

				UpdateSuggestionListItemsSource();

				spListViewPart = m_tpSuggestionsPart as ListViewBase;

				if (spListViewPart is not null)
				{
					spListViewPart.ItemClick += OnListViewItemClick;

					spListViewPart.ContainerContentChanging += OnListViewContainerContentChanging;
				}

				m_tpSuggestionsPart.SelectionChanged += OnSuggestionSelectionChanged;

				m_tpSuggestionsPart.KeyDown += OnSuggestionListKeyDown;

				// UNO TODO
				//ListView spListView = m_tpSuggestionsPart as ListView;
				//if (spListView is not null)
				//{
				//	spListView.SetAllowItemFocusFromUIA(false);
				//}
			}

			if (m_tpPopupPart is not null)
			{
				m_tpPopupPart.Opened += OnPopupOpened;
			}

			if (m_tpSuggestionsContainerPart is not null)
			{
				m_tpSuggestionsContainerPart.Loaded += OnSuggestionsContainerLoaded;
			}

			if (m_tpTextChangedEventTimer is not null)
			{
				m_tpTextChangedEventTimer.Tick += OnTextChangedEventTimerTick;
			}

			ReevaluateIsOverlayVisible();
		}

		internal override void OnPropertyChanged2(DependencyPropertyChangedEventArgs args)
		{
			base.OnPropertyChanged2(args);

			if (args.Property == AutoSuggestBox.TextProperty)
			{
				string strQueryText = (string)args.NewValue;
				UpdateTextBoxText(strQueryText, AutoSuggestionBoxTextChangeReason.ProgrammaticChange);
			}
			else if (args.Property == AutoSuggestBox.QueryIconProperty)
			{
				SetTextBoxQueryButtonIcon();
			}
			else if (args.Property == AutoSuggestBox.IsSuggestionListOpenProperty)
			{
				bool isOpen = (bool)args.NewValue;

				// Only proceed if there is at least one island that's still alive. Otherwise we can crash when opening the
				// windowed popup when it tries to get its island in CPopup::EnsureWindowForWindowedPopup to check hwnds.
				// Note: Tests running in UWP mode don't need this check, so count them as having islands.
				WinUICoreServices core = DXamlCore.Current.GetHandle();
				// Uno TODO: We don't have "HasXamlIslands"
				if (core.InitializationType != InitializationType.IslandsOnly /*|| core.HasXamlIslands*/)
				{
					if (m_tpPopupPart is not null)
					{
						m_tpPopupPart.IsOpen = isOpen;

						if (!isOpen)
						{
							// In the desktop window, the focus moves into the AutoSuggestBox when the suggestion
							// popup list is closed so m_suppressSuggestionListVisibility flag ensures the popup
							// close when the AutoSuggestBox got the focus by closing the popup.
							m_suppressSuggestionListVisibility = true;

							// We should ensure that no element in the suggestion list is selected
							// when the popup isn't open, since otherwise that opens up the possibility
							// of interacting with the suggestion list even when it's closed.
							m_ignoreSelectionChanges = true;
							m_tpSuggestionsPart.SelectedIndex = -1;
							m_ignoreSelectionChanges = false;
						}
					}

					ReevaluateIsOverlayVisible();

					//SetCurrentControlledPeer(isOpen ? ControlledPeer.SuggestionsList : ControlledPeer.None);
				}
			}
			else if (args.Property == AutoSuggestBox.LightDismissOverlayModeProperty)
			{
				ReevaluateIsOverlayVisible();
			}
			else if (args.Property == AutoSuggestBox.TextMemberPathProperty)
			{
				// TextMemberPath updated, release existing PropertyPathListener
				// UNO TODO:
				//m_spPropertyPathListener = null;
			}
		}

		private void OnUnloaded(object sender, RoutedEventArgs args)
		{
			if (!IsInLiveTree && m_isOverlayVisible)
			{
				m_isOverlayVisible = false;
				TeardownOverlayState();
			}
		}

		private void OnTextBoxTextChanged(object sender, TextChangedEventArgs e)
		{
			string strQueryText;
			AutoSuggestBoxTextChangedEventArgs spEventArgs = new();

			//m_textChangedCounter++;

			//spEventArgs.SetCounter(m_textChangedCounter);
			//spEventArgs.SetOwner(this));

			spEventArgs.Reason = m_textChangeReason;

			m_tpTextChangedEventArgs = spEventArgs;

			m_tpTextChangedEventTimer.Stop();
			m_tpTextChangedEventTimer.Start();

			strQueryText = m_tpTextBoxPart.Text;

			UpdateText(strQueryText);

			if (!m_ignoreTextChanges)
			{
				if (m_textChangeReason == AutoSuggestionBoxTextChangeReason.UserInput)
				{
					m_userTypedText = strQueryText;
					// make sure the suggestion list is shown when user inputs
					UpdateSuggestionListVisibility();
				}

				if (m_tpSuggestionsPart is not null)
				{
					int selectedIndex = m_tpSuggestionsPart.SelectedIndex;

					if (-1 != selectedIndex)
					{
						m_ignoreSelectionChanges = true;
						m_tpSuggestionsPart.SelectedIndex = -1;
						m_ignoreSelectionChanges = false;
					}
				}
			}

			//Cleanup:
			m_textChangeReason = AutoSuggestionBoxTextChangeReason.UserInput;
		}

#if !HAS_UNO
		private void OnTextBoxCandidateWindowBoundsChanged(TextBox sender, CandidateWindowBoundsChangedEventArgs args)
		{
			Rect candidateWindowBounds = args.Bounds;

			// do nothing if the candidate windows bound did not change
			if (m_candidateWindowBoundsRect == candidateWindowBounds)
			{
				return;
			}

			m_candidateWindowBoundsRect = candidateWindowBounds;

			// When the candidate window bounds change, there are three things that we need to do,
			// since this changes the rect in which we need to draw the suggestion list:
			//
			// 1. Adjust the available height for and position of the suggestion list;
			// 2. Adjust the size of the suggestion list; and
			// 3. Adjust the position of the suggestion list.
			//
			// #1 must be done in a different way depending on whether or not the SIP is currently open,
			// hence the if statement.  The others are the same regardless.

			if (m_tpInputPane is not null && m_sSipIsOpen)
			{
				Rect sipOverlayArea;

				sipOverlayArea = m_tpInputPane.OccludedRect);
				AlignmentHelper(sipOverlayArea);
			}
			else
			{
				MaximizeSuggestionAreaWithoutInputPane();
			}

			UpdateSuggestionListPosition();
			UpdateSuggestionListSize();
		}
#endif

		//------------------------------------------------------------------------------
		// Handler of the SizeChanged event.
		//------------------------------------------------------------------------------
		private void OnSizeChanged(object sender, SizeChangedEventArgs args)
		{
			if (m_tpSuggestionsContainerPart is not null)
			{
				double actualWidth = 0;
				FrameworkElement spThisAsFE = this;

				actualWidth = spThisAsFE.ActualWidth;
				m_tpSuggestionsContainerPart.Width = actualWidth;
			}
		}

		// This event handler is only for Gamepad or Remote cases, where Focus and Selection are tied.
		// This is never invoked for Keyboard cases since Focus stays on the TextBox but Selection moves
		// down the ListView so the key down events go to OnKeyDown event handler.
		private void OnSuggestionListKeyDown(object sender, KeyRoutedEventArgs args)
		{
			var key = args.Key;

			m_inputDeviceTypeUsed = InputDeviceType.GamepadOrRemote;

			bool wasHandledLocally = false;

			switch (key)
			{
				case VirtualKey.Left:
				case VirtualKey.Right:
					// Since the SuggestionList is open, we don't allow horizontal movement.
					wasHandledLocally = true;
					break;

				case VirtualKey.Up:
				case VirtualKey.Down:
					{
						int selectedIndex = m_tpSuggestionsPart.SelectedIndex;

						uint count;
						var spItems = Items;
						count = spItems.Size;
						int lastIndex = (int)(count - 1);

						// If we are already at the Suggestion that is adjacent to the TextBox, then set SelectedIndex to -1.
						if ((selectedIndex == 0 && !IsSuggestionListVectorReversed()) ||
							(selectedIndex == lastIndex && IsSuggestionListVectorReversed()))
						{
							UpdateTextBoxText(m_userTypedText, AutoSuggestionBoxTextChangeReason.ProgrammaticChange);
							m_tpSuggestionsPart.SelectedIndex = -1;

							m_tpTextBoxPart.Focus(FocusState.Keyboard);
						}

						wasHandledLocally = true;
						break;
					}

				case VirtualKey.Space:
				case VirtualKey.Enter:
					{
						var spSelectedItem = m_tpSuggestionsPart.SelectedItem;
						SubmitQuery(spSelectedItem);
						IsSuggestionListOpen = false;
						wasHandledLocally = true;
					}
					break;

				case VirtualKey.Escape:
					// Reset the text in the TextBox to what the user had typed.
					UpdateTextBoxText(m_userTypedText, AutoSuggestionBoxTextChangeReason.ProgrammaticChange);
					// Close the suggestion list.
					IsSuggestionListOpen = false;
					// Return the focus to the TextBox.
					m_tpTextBoxPart.Focus(FocusState.Keyboard);

					wasHandledLocally = true;
					break;
			}

			if (wasHandledLocally)
			{
				args.Handled = true;
			}
		}

		//------------------------------------------------------------------------------
		// Handler of the suggestions Popup's Opened event.
		//
		// Updates the suggestions list's position as its position can be changed before
		// the previous Open operation.
		//------------------------------------------------------------------------------
		private void OnPopupOpened(object sender, object args)
		{
			//TraceLoggingActivity<g_hTraceProvider, MICROSOFT_KEYWORD_TELEMETRY> traceLoggingActivity;

			// Telemetry marker for suggestion list opening popup.
			//TraceLoggingWriteStart(traceLoggingActivity,
			//	"ASBSuggestionListOpened");

			// Apply a shadow effect to the popup's immediate child
			// UNO TODO:
			//ApplyElevationEffect(m_tpPopupPart);

			UpdateSuggestionListPosition();
		}

		//------------------------------------------------------------------------------
		// Handler of the suggestions container's Loaded event.
		//
		// Sets the position of the suggestions list as soon as the container is loaded.
		//------------------------------------------------------------------------------
		private void OnSuggestionsContainerLoaded(object sender, RoutedEventArgs args)
		{
			UpdateSuggestionListPosition();
			UpdateSuggestionListSize();
		}

		//------------------------------------------------------------------------------
		// Handler of the text box's Loaded event.
		//
		// Retrieves the query button if it exists and attaches a handler to its Click event.
		//------------------------------------------------------------------------------
		private void OnTextBoxLoaded(object sender, RoutedEventArgs args)
		{
			ButtonBase spTextBoxQueryButtonPart;

			spTextBoxQueryButtonPart = m_tpTextBoxPart.GetTemplateChild<ButtonBase>(c_TextBoxQueryButtonName);

			m_tpTextBoxQueryButtonPart = spTextBoxQueryButtonPart;

			if (m_tpTextBoxQueryButtonPart is not null)
			{
				SetTextBoxQueryButtonIcon();
				m_tpTextBoxQueryButtonPart.Click += OnTextBoxQueryButtonClick;
				// Update query button's AutomationProperties.Name to "Search" by default
				string automationName = AutomationProperties.GetName(m_tpTextBoxQueryButtonPart);
				if (automationName is null)
				{
					automationName = "Search";
					// UNO TODO:
					//automationName = DXamlCore.GetCurrentNoCreate().GetLocalizedResourceString(UIA_AUTOSUGGESTBOX_QUERY);
					AutomationProperties.SetName(m_tpTextBoxQueryButtonPart, automationName);
				}
			}
		}

		//------------------------------------------------------------------------------
		// Handler of the text box's Unloaded event.
		//
		// Removes the handler to the query button
		//------------------------------------------------------------------------------
		private void OnTextBoxUnloaded(object sender, RoutedEventArgs e)
		{
			// Checking IsActive because Unloaded is async and we might have reloaded before this fires
			if (m_tpTextBoxQueryButtonPart is not null /*&& m_epQueryButtonClickEventHandler && !GetHandle()->IsActive()*/)
			{
				m_tpTextBoxQueryButtonPart.Click -= OnTextBoxQueryButtonClick;
			}
		}

		//------------------------------------------------------------------------------
		// Handler of the query button's Click event.
		//
		// Raises the QuerySubmitted event.
		//------------------------------------------------------------------------------
		private void OnTextBoxQueryButtonClick(object sender, RoutedEventArgs args)
		{
			ProgrammaticSubmitQuery();
		}

		private void ProgrammaticSubmitQuery()
		{
			// Clicking the query button should always submit the query solely with the text
			// in the TextBox, and should ignore any selected item in the suggestion list.
			// To ensure that, we'll deselect any item in the suggestion list that might be selected
			// before submitting the query.
			m_ignoreSelectionChanges = true;
			var cleanupGuard = Disposable.Create(() => m_ignoreSelectionChanges = false);

			m_tpSuggestionsPart.SelectedIndex = -1;
			cleanupGuard.Dispose();

			SubmitQuery(null);
		}

		//------------------------------------------------------------------------------
		// Sets the value of QueryButton.Content to the current value of QueryIcon.
		//------------------------------------------------------------------------------
		private void SetTextBoxQueryButtonIcon()
		{
			if (m_tpTextBoxQueryButtonPart is not null)
			{
				//static_cast<CFrameworkElement*>(m_tpTextBoxQueryButtonPart.Cast<ButtonBase>()->GetHandle())->SetCursor(MouseCursorArrow);
				IconElement spQueryIcon = QueryIcon;

				if (spQueryIcon is not null)
				{
					SymbolIcon spQueryIconAsSymbolIcon = spQueryIcon as SymbolIcon;

					if (spQueryIconAsSymbolIcon is not null)
					{
						// Setting FontSize to zero prevents SymbolIcon from setting a static FontSize on it's child TextBlock,
						// allowing the binding to AutoSuggestBoxIconFontSize to be inherited properly.
						//spQueryIconAsSymbolIcon.Cast<SymbolIcon>->SetFontSize(0);
					}

					m_tpTextBoxQueryButtonPart.Visibility = Visibility.Visible;
					m_tpTextBoxQueryButtonPart.Content = spQueryIcon;
				}
				else
				{
					m_tpTextBoxQueryButtonPart.Visibility = Visibility.Collapsed;
				}
			}
		}

		//------------------------------------------------------------------------------
		// Sets the value of QueryButton.Content to the current value of QueryIcon.
		//------------------------------------------------------------------------------
		private void ClearTextBoxQueryButtonIcon()
		{
			if (m_tpTextBoxQueryButtonPart is not null)
			{
				m_tpTextBoxQueryButtonPart.Content = null;
			}
		}

		//------------------------------------------------------------------------------
		// Raises the QuerySubmitted event using the current content of the TextBox.
		//------------------------------------------------------------------------------
		private void SubmitQuery(object chosenSuggestion)
		{
			string strQueryText;
			AutoSuggestBoxQuerySubmittedEventArgs spEventArgs = new();

			strQueryText = m_tpTextBoxPart.Text;
			spEventArgs.QueryText = strQueryText;
			spEventArgs.ChosenSuggestion = chosenSuggestion;

			QuerySubmitted?.Invoke(this, spEventArgs);

			IsSuggestionListOpen = false;
		}

		//------------------------------------------------------------------------------
		// AutoSuggestBox OnLostFocus event handler
		//
		// The suggestions drop-down will never be displayed when this control loses focus.
		//------------------------------------------------------------------------------
		private void OnLostFocus()
		{
			if (!m_keepFocus)
			{
				m_hasFocus = false;

				if (m_isSipVisible)
				{
					// checking to see if the focus went to a new element that contains a textbox
					// in this case, we leave it up to the new element to handle scrolling
					DependencyObject spFocusedElement = FocusManager.GetFocusedElement(XamlRoot) as DependencyObject;
					if (spFocusedElement is not null)
					{
						TextBox spFocusedElementAsTextBox = spFocusedElement as TextBox;
						if (spFocusedElementAsTextBox is null)
						{
							// This is for the case when the focus is changed to something that is not ASB or textbox
							OnSipHidingInternal();
						}
					}
					else
					{
						// This is for the case when focus is lost and no other control is in focus
						OnSipHidingInternal();
					}
				}
				m_scrollActions.Clear();
			}

			// When Gamepad or Remote is used, we move the focus from the AutoSuggestBox TextBox to the ListView.
			// But in other cases (when using keyboard), where we get OnLostFocus, we want to make sure to close suggestions list.
			// Note that Left/Right keys are handled by OnKeyDown and OnSuggestionListKeyDown handlers so the only time we are here
			// is when we "Tab" out of, or the focus is moved programmatically off of, the AutoSuggestBox TextBox.
			if (m_inputDeviceTypeUsed != InputDeviceType.GamepadOrRemote)
			{
				IsSuggestionListOpen = false;
			}
		}

		//------------------------------------------------------------------------------
		// AutoSuggestBox OnGotFocus event handler
		//
		// The suggestions drop-down should be displayed if items are present and the textbox has text
		//------------------------------------------------------------------------------
		private void OnGotFocus()
		{
			if (!m_keepFocus)
			{
				bool isTextBoxFocused = IsTextBoxFocusedElement();

				if (isTextBoxFocused)
				{
					string strText;

					// this code handles the case when the control receives focus from another control
					// that was using the sip. In this case, OnSipShowing is not guaranteed to be called
					// hence, we align the control to the top or bottom depending on its position at the time
					// it received focus
					if (m_tpInputPane is not null && m_sSipIsOpen)
					{
						Rect sipOverlayArea = m_tpInputPane.OccludedRect;

						AlignmentHelper(sipOverlayArea);
					}

					// Expands the suggestion list if there is already text in the textbox
					strText = m_tpTextBoxPart.Text;

					if (strText.Length > 0 && !m_suppressSuggestionListVisibility)
					{
						UpdateSuggestionListVisibility();
						m_suppressSuggestionListVisibility = false;
					}
				}
			}
			else
			{
				// making sure the ASB is aligned to where it should be
				ApplyScrollActions(true);
			}

			m_keepFocus = false;
		}

		//------------------------------------------------------------------------------
		// AutoSuggestBox OnKeyDown event handler
		//
		// Handle the proper KeyDown event to process Key_Down, KeyUp, Key_Enter and
		// Key_Escape.
		//
		//  Key_Down/Key_Up is for navigating the suggestionlist.
		//  Key_Enter is for choosing the current selection if there is a proper select item.
		//   otherwise, do nothing.
		//  Key_Escape is for closing the suggestion list or clear the current text.
		//
		//------------------------------------------------------------------------------
		protected override void OnKeyDown(KeyRoutedEventArgs args)
		{
			var key = args.Key;

			var originalKey = args.OriginalKey;

			m_inputDeviceTypeUsed = XboxUtility.IsGamepadNavigationInput(originalKey) ? InputDeviceType.GamepadOrRemote : InputDeviceType.Keyboard;

			base.OnKeyDown(args);

			if (m_tpSuggestionsPart is not null)
			{
				bool wasHandledLocally = false;

				int selectedIndex = m_tpSuggestionsPart.SelectedIndex;

				bool isSuggestionListOpen = IsSuggestionListOpen;

				switch (key)
				{
					case VirtualKey.Left:
					case VirtualKey.Right:
						if (isSuggestionListOpen && m_inputDeviceTypeUsed == InputDeviceType.GamepadOrRemote)
						{
							// If SuggestionList is open, we don't allow horizontal movement
							// when using Gamepad or Remote.
							wasHandledLocally = true;
						}
						break;

					case VirtualKey.Up:
					case VirtualKey.Down:
						// If the suggestion list isn't open, we shouldn't be able to keyboard through it.
						if (isSuggestionListOpen)
						{
							var spItems = Items;

							uint count = spItems.Size;
							if (count > 0)
							{
								int lastIndex = (int)(count - 1);

								GetKeyboardModifiers(out var modifiers);
								bool isForward = ShouldMoveIndexForwardForKey(key, modifiers);

								selectedIndex = m_tpSuggestionsPart.SelectedIndex;

								// The meaning of this conditional is really hard to parse by itself, so here's
								// a much simpler way of saying the same thing:
								//
								// "If we only have one item (lastIndex == 0), and if it's already selected (selectedIndex == 0),
								// then the arrow keys should do nothing."
								//
								// Basically, if lastIndex != 0, then since count > 0, lastIndex must be greater than 0.
								// Conversely, if lastIndex == 0, then selectedIndex can only be equal to -1 (nothing selected)
								// or 0 (the first and only item is selected).
								//
								// Note that lastIndex means "the index of the last item in the suggestion list", not
								// "the previous index".
								//
								if (selectedIndex != 0 || lastIndex != 0)
								{
									// The following conditional was written to satisfy the table below which identifies the indices we
									// need to go to for the different types of AutoSuggestBoxes (Suggestion List below, Suggestion List
									// above with new implementation where vector is reversed, Suggestion List above with old implementation).
									// Note that the "/NA" indicates that the index does not move when the input is coming from Gamepad/Remote
									// because there is no looping behavior.
									//
									//                        ArrowKey  isForward   VectorReversed  indexToGoTo
									// ASB_SuggestionsBelow     Down        1           0               0
									//                          Up          0           0               lastIndex/NA
									// ASB_SuggestionsAbove     Down        1           1               0/NA
									//                (New)     Up          0           1               lastIndex
									// ASB_SuggestionsAbove     Down        0           0               lastIndex/NA
									//                (Old)     Up          1           0               0
									if (selectedIndex == -1)
									{
										if (isForward)
										{
											if (!(IsSuggestionListVectorReversed() && m_inputDeviceTypeUsed == InputDeviceType.GamepadOrRemote))
											{
												m_tpSuggestionsPart.SelectedIndex = 0;
											}
										}
										else
										{
											if (!(!IsSuggestionListVectorReversed() && m_inputDeviceTypeUsed == InputDeviceType.GamepadOrRemote))
											{
												m_tpSuggestionsPart.SelectedIndex = lastIndex;
											}
										}
									}
									else if (selectedIndex == 0)
									{
										if (isForward)
										{
											m_tpSuggestionsPart.SelectedIndex = selectedIndex + 1;
										}
										else
										{
											UpdateTextBoxText(m_userTypedText, AutoSuggestionBoxTextChangeReason.ProgrammaticChange);
											m_tpSuggestionsPart.SelectedIndex = -1;
										}
									}
									else if (selectedIndex == lastIndex)
									{
										if (isForward)
										{
											UpdateTextBoxText(m_userTypedText, AutoSuggestionBoxTextChangeReason.ProgrammaticChange);
											m_tpSuggestionsPart.SelectedIndex = -1;
										}
										else
										{
											m_tpSuggestionsPart.SelectedIndex = lastIndex - 1;
										}
									}
									else
									{
										m_tpSuggestionsPart.SelectedIndex = isForward ? selectedIndex + 1 : selectedIndex - 1;
									}
								}
							}
							wasHandledLocally = true;
						}
						break;

					case VirtualKey.Tab:
						// Only close the suggestion list and reset the user text with Tab if the suggestion
						// list is open.
						if (!isSuggestionListOpen)
						{
							break;
						}
						else
						{
							goto case VirtualKey.Escape;
						}
					case VirtualKey.Escape:
						{
							// Reset the text in the TextBox to what the user had typed.
							UpdateTextBoxText(m_userTypedText, AutoSuggestionBoxTextChangeReason.ProgrammaticChange);
							// Close the suggestion list.
							IsSuggestionListOpen = false;
							// Return the focus to the TextBox.
							m_tpTextBoxPart.Focus(FocusState.Keyboard);

							// Handle the key for Escape, but not for tab so that the default tab processing can take place.
							wasHandledLocally = (key != VirtualKey.Tab);
							break;
						}

					case VirtualKey.Enter:
						object spSelectedItem = m_tpSuggestionsPart.SelectedItem;

						// If the AutoSuggestBox supports QueryIcon, then pressing the Enter key
						// will submit the current query - we'll already have set the text in the TextBox
						// in OnSuggestionSelectionChanged().
						SubmitQuery(spSelectedItem);

						IsSuggestionListOpen = false;
						wasHandledLocally = true;
						break;
				}

				if (wasHandledLocally)
				{
					args.Handled = true;
				}
			}
		}

		//------------------------------------------------------------------------------
		// AutoSuggestBox IsFocusedElement event handler
		//
		// Queries the focused element from the focus manager to see if it's the textbox
		//------------------------------------------------------------------------------
		private bool IsTextBoxFocusedElement()
		{
			if (m_tpTextBoxPart is not null)
			{
				DependencyObject spFocusedElement = FocusManager.GetFocusedElement(XamlRoot) as DependencyObject;

				if (spFocusedElement is not null)
				{
					TextBox spFocusedElementAsTextBox = spFocusedElement as TextBox;

					if (spFocusedElementAsTextBox is not null && spFocusedElementAsTextBox == m_tpTextBoxPart)
					{
						return true;
					}
				}
			}

			return false;
		}

		//------------------------------------------------------------------------------
		// AutoSuggestBox OnSipShowing
		//
		// This event handler will be called by the global InputPane when the SIP will
		// be showing up from the bottom of the screen, here we try to scroll AutoSuggestBox
		// to top or bottom with minimum visual glitch
		//------------------------------------------------------------------------------
		private void OnSipShowing(InputPane _, InputPaneVisibilityEventArgs args)
		{
			// setting the static value to true
			m_sSipIsOpen = true;

			// the check here is to ensure that the arguments received are not null hence crashing our application
			// in some stress tests, null was encountered
			if (args is not null)
			{
				OnSipShowingInternal(args);
			}
		}

		//------------------------------------------------------------------------------
		// AutoSuggestBox OnSipHiding
		//
		// This event handler will be called by the global InputPane when the SIP is
		// hiding. It reverts the scroll actions that are applied to bring the ASB
		// to its original position.
		//------------------------------------------------------------------------------
		private void OnSipHiding(InputPane _, InputPaneVisibilityEventArgs _2)
		{
			// setting the static value to false
			m_sSipIsOpen = false;

			ReevaluateIsOverlayVisible();
		}

		//------------------------------------------------------------------------------
		// Updates the suggestion list's size based on the available space and the
		// MaxSuggestionListHeight property.
		//------------------------------------------------------------------------------
		private void UpdateSuggestionListSize()
		{
			if (m_tpSuggestionsContainerPart is not null)
			{
				double maxSuggestionListHeight = MaxSuggestionListHeight;
				double actualWidth = 0;
				FrameworkElement spThisAsFElement = this;

				// if the user specifies a negative value for the maxsuggestionlistsize, we use the available size
				if ((m_availableSuggestionHeight > 0 && maxSuggestionListHeight > m_availableSuggestionHeight) || maxSuggestionListHeight < 0)
				{
					maxSuggestionListHeight = m_availableSuggestionHeight;
				}

				m_tpSuggestionsContainerPart.MaxHeight = maxSuggestionListHeight;

				actualWidth = spThisAsFElement.ActualWidth;
				m_tpSuggestionsContainerPart.Width = actualWidth;
			}
		}

		//------------------------------------------------------------------------------
		// Opens the suggestion list if there is at least one item in the items collection.
		//------------------------------------------------------------------------------
		private void UpdateSuggestionListVisibility()
		{
			//ctl::ComPtr<wfc::IObservableVector<IInspectable*>> spItemsReference;
			//ctl::ComPtr<wfc::IVector<IInspectable*>> spSuggestionsCollectionReference;
			ItemCollection spSuggestionsCollectionReference;
			bool isOpen = false;

			// if the suggestion container exists, we are retrieving its maxsuggestionlistheight
			double maxHeight = 0;
			if (m_tpSuggestionsContainerPart is not null)
			{
				maxHeight = m_tpSuggestionsContainerPart.MaxHeight;
			}

			spSuggestionsCollectionReference = Items;

			if (spSuggestionsCollectionReference is not null && maxHeight > 0)
			{
				uint count = spSuggestionsCollectionReference.Size;

				// the suggestion list is only open when the maxsuggestionlistheight is greater than zero
				// and the count of elements in the list is positive
				if (count > 0)
				{
					isOpen = true;
				}
			}

			// We don't want to necessarily take focus in this case, since we probably already
			// have focus somewhere internal to the AutoSuggestBox, so we'll bypass the custom
			// setter for IsSuggestionListOpen that takes focus.
			IsSuggestionListOpen = isOpen;
		}


		//------------------------------------------------------------------------------
		// Positions the suggestion list based on the value specified in the TextBoxPosition
		//------------------------------------------------------------------------------
		private void UpdateSuggestionListPosition()
		{
			// Don't call this function while processing a collection change.  When a collection change happens,
			// ASB's OnItemsChanged gets called before the inner ListView's OnItemsChanged.  So if ASB calls UpdateLayout
			// on the ListView during its OnItemsChanged, ListView will try to measure itself when it has a stale
			// view of the collection.  Don't let this happen.
			// UNO TODO: This assert fails.
			//Debug.Assert(!m_handlingCollectionChange);

			if (!m_isSipVisible)
			{
				MaximizeSuggestionAreaWithoutInputPane();
			}

			if (m_tpPopupPart is not null && m_tpTextBoxPart is not null && m_tpSuggestionsContainerPart is not null)
			{
				UIElement spThisAsUI = this;
				DependencyObject spTextBoxScrollViewerAsDO;

				double width = 0;
				double height = 0;
				double translateX = 0;
				double translateY = 0;
				double scaleY = 1;

				double candidateWindowXOffset = 0.0;
				double candidateWindowYOffset = 0.0;
				Thickness margin;

				Thickness suggestionListMargin = default;
				if (m_tpSuggestionsPart is not null)
				{
					FrameworkElement spSuggestionAsFE = m_tpSuggestionsPart;
					suggestionListMargin = spSuggestionAsFE.Margin;
				}

				TextBox spTextBoxPeer = m_tpTextBoxPart;

				// scroll viewer location
				// getting the ScrollViewer (child of the textbox)
				// we want to align the popup to the ScrollViewer part of the textbox
				// after getting the ScrollViewer, we find its position relative to the AutoSuggestBox
				// if the ScrollViewer is not present, we align to the textbox itself
				spTextBoxScrollViewerAsDO = m_tpTextBoxPart.GetTemplateChild(c_TextBoxScrollViewerName);

				if (spTextBoxScrollViewerAsDO is null)
				{
					GeneralTransform spTransform;
					Point textBoxLocation = default;

					spTransform = m_tpTextBoxPart.TransformToVisual(spThisAsUI);
					textBoxLocation = spTransform.TransformPoint(textBoxLocation);
					translateY = textBoxLocation.Y;
				}
				else
				{
					UIElement spTextBoxScrollViewerAsUI = (UIElement)spTextBoxScrollViewerAsDO;
					GeneralTransform spTransform;
					Point scrollViewerLocation = default;

					spTransform = spTextBoxScrollViewerAsUI.TransformToVisual(spThisAsUI);
					scrollViewerLocation = spTransform.TransformPoint(scrollViewerLocation);
					translateY = scrollViewerLocation.Y;
				}

				// We need move the popup up (popup's bottom align to textbox) when textbox is at bottom position.
				if (m_suggestionListPosition == SuggestionListPosition.Above)
				{
					m_tpSuggestionsContainerPart.UpdateLayout();
					height = m_tpSuggestionsContainerPart.ActualHeight;

					translateY -= height;

					if (IsSuggestionListVerticallyMirrored())
					{
						scaleY = -1.0;
					}
				}
				else if (m_suggestionListPosition == SuggestionListPosition.Below)
				{
					// If the text box has an active handwritingView or if the ScrollViewer isn't present, get the height of the
					// textbox/handwritingVirew itself. Otherweise add the ScrollViewer's height.
					if (spTextBoxScrollViewerAsDO is null)
					{
						GetActualTextBoxSize(out width, out height);
						translateY += height;
						// bring up the suggestion list to avoid gap caused by margin
						translateY -= suggestionListMargin.Top;
					}
					else
					{
						FrameworkElement spTextBoxScrollViewerAsFE = spTextBoxScrollViewerAsDO as FrameworkElement;
						// ScrollViewer height
						height = spTextBoxScrollViewerAsFE.ActualHeight;

						translateY += height;
					}
				}

				GetCandidateWindowPopupAdjustment(
					false /* ignoreSuggestionListPosition */,
					out candidateWindowXOffset,
					out candidateWindowYOffset);

				if (m_tpUpwardTransformPart is not null)
				{
					m_tpUpwardTransformPart.X = translateX + candidateWindowXOffset;
					m_tpUpwardTransformPart.Y = translateY + candidateWindowYOffset;
				}
				else
				{
					m_tpPopupPart.HorizontalOffset = translateX + candidateWindowXOffset;
					m_tpPopupPart.VerticalOffset = translateY + candidateWindowYOffset;
				}

				// If we've moved the suggestions list popup over in the x-direction, we still want the
				// right side of the popup to be in the same place, so we add the offset to its right margin as well.
				margin = m_tpSuggestionsContainerPart.Margin;
				margin.Right = candidateWindowXOffset;
				m_tpSuggestionsContainerPart.Margin = margin;

				if (IsSuggestionListVerticallyMirrored())
				{
					ScaleTransform scaleTransform = m_tpListItemOrderTransformPart as ScaleTransform;

					if (scaleTransform is not null)
					{
						scaleTransform.ScaleY = scaleY;
					}
				}
				else
				{
					UpdateSuggestionListItemsSource();
				}
			}
		}

		//------------------------------------------------------------------------------
		// Positions the suggestion list based on the value specified in the TextBoxPosition
		//------------------------------------------------------------------------------
		private void ScrollLastItemIntoView()
		{
			if (m_tpSuggestionsPart is ListViewBase listViewBase)
			{
				var items = listViewBase.Items;
				var size = (int)items.Size;

				if (size > 1)
				{
					var lastItem = items[size - 1];
					listViewBase.ScrollIntoView(lastItem);
				}
			}
		}

		// Called when ItemsSource is set, or when the ItemsSource collection chanages
		protected override void OnItemsChanged(object e)
		{
			bool isTextBoxFocused = false;
			bool bAutomationListener = false;

#if DEBUG
			bool wasHandlingCollectionChange = m_handlingCollectionChange;
			m_handlingCollectionChange = true;
#endif

			//IFC(OnItemsChangedImpl(e));

			isTextBoxFocused = IsTextBoxFocusedElement();
			if (isTextBoxFocused)
			{
				// Defer the update until after change notification is fully processed.
				// UpdateSuggestionListPosition must not be called synchronously while handling
				// the change notification (see comment in UpdateSuggestionListPosition).
				// This also has the benefit of doing just one update for a batch of change notifications
				// if we get a bunch at the same time.
				if (!m_deferringUpdate)
				{
					//ctl::WeakRefPtr wpThis;
					//IFC(ctl::AsWeak(this, &wpThis));

					//DXamlCore.Current.GetXamlDispatcherNoRef().RunAsync(
					//	MakeCallback<ctl::WeakRefPtr, ctl::WeakRefPtr>(&AutoSuggestBox::ProcessDeferredUpdateStatic, wpThis)));
					m_deferringUpdate = true;
				}

				// We should immediately update visibility, however, since we want the value of
				// IsSuggestionListOpen to be properly updated by the time this returns.
				UpdateSuggestionListVisibility();
			}

			bAutomationListener = AutomationPeer.ListenerExistsHelper(AutomationEvents.LayoutInvalidated);

			if (bAutomationListener)
			{
				UIElement spListPart;
				AutomationPeer spAutomationPeer;

				if (m_tpSuggestionsPart is not null)
				{
					spListPart = m_tpSuggestionsPart;
					if (spListPart is not null)
					{
						spAutomationPeer = spListPart.GetOrCreateAutomationPeer();
						if (spAutomationPeer is not null)
						{
							spAutomationPeer.RaiseAutomationEvent(AutomationEvents.LayoutInvalidated);
						}
					}
				}
			}

#if DEBUG
			m_handlingCollectionChange = wasHandlingCollectionChange;
#endif
		}

		private void ProcessDeferredUpdate()
		{
			if (m_deferringUpdate)
			{
				m_deferringUpdate = false;
				UpdateSuggestionListPosition();
				UpdateSuggestionListSize();
				UpdateSuggestionListVisibility();
			}
		}

		private void UpdateSuggestionListItemsSource()
		{
			if (m_tpSuggestionsPart is not null && m_tpListItemOrderTransformPart is null)
			{
				// If we have a m_tpListItemOrderTransformPart, we implement SuggestionListPosition::Above
				// by applying a scale transform.  Also, in the win8.1 template where we do have a
				// m_tpListItemOrderTransformPart, the suggestion list's ItemsSource is bound to the
				// ASB's ItemsSource, so no need to update it.

				if (m_suggestionListPosition == SuggestionListPosition.Above)
				{
					var spObservable = Items;

					if (spObservable is not null)
					{
						//if (m_spReversedVector is null || !m_spReversedVector.IsBoundTo(spObservable))
						//{
						//	m_spReversedVector = new();
						//	m_spReversedVector.SetSource(spObservable);

						//	m_tpSuggestionsPart.ItemsSource = m_spReversedVector;
						//	ScrollLastItemIntoView();
						//}
						return;
					}
				}

				// We can't reverse the vector, fall back to propagating ItemsSource from ASB to the suggestion list
				//m_spReversedVector = null;

				var spItemsSource = ItemsSource;
				m_tpSuggestionsPart.ItemsSource = spItemsSource;
			}
			return;
		}

		private void TryGetSuggestionValue(object @object, /*PropertyPathListener pathListener,*/ out string value)
		{
			if (@object == null)
			{
				value = null;
				return;
			}

			//ASSERT(value != nullptr); // This assert doesn't make sense for Uno. In WinUI, it makes sense because value is a pointer where the value will be stored into.

			object spBoxedValue;
			object spObject = @object;

			//if (spObject is ICustomPropertyProvider spObjectPropertyAccessor)
			//{
			//	if (pathListener is not null)
			//	{
			//		// Our caller has provided us with a PropertyPathListener. By setting the source of the listener, we can pull a value out.
			//		// This is our boxedValue, which we effectively ToString below.
			//		pathListener.SetSource(spObject);
			//		spBoxedValue = pathListener.GetValue();
			//	}
			//	else
			//	{
			//		// "value" property not specified, but this object implements
			//		// ICustomPropertyProvider. Call .ToString on the object:
			//		value = spObjectPropertyAccessor.GetStringRepresentation();
			//		return;
			//	}
			//}
			//else
			{
				spBoxedValue = spObject; // the object itself is the value string, unbox it.
			}

			// calling the ToString function on items that can be represented by a string
			value = spBoxedValue.ToString();
		}

#if !HAS_UNO // UNO TODO: controlled peers not yet implemented.
		private void SetCurrentControlledPeer(ControlledPeer peer)
		{
			if (m_tpTextBoxPart is not null)
			{
				bool bAutomationListener;
				UIElement spPeer = null;
				DependencyObject spTextBoxPartAsDO;

				switch (peer)
				{
					case ControlledPeer.None:
						// Leave spPeer as nullptr.
						break;

					case ControlledPeer.SuggestionsList:
						if (m_tpSuggestionsPart is not null)
						{
							spPeer = m_tpSuggestionsPart;
						}
						break;

					default:
						break;
				}

				spTextBoxPartAsDO = m_tpTextBoxPart;

				// UNO TODO: controlled peers not yet implemented.
				var spControlledPeers = AutomationProperties.GetControlledPeers(spTextBoxPartAsDO);

				spControlledPeers.Clear();
				if (spPeer is not null)
				{
					spControlledPeers.Add(spPeer);
				}

				bAutomationListener = AutomationPeer.ListenerExistsHelper(AutomationEvents.PropertyChanged);

				if (bAutomationListener)
				{
					AutomationPeer spAutomationPeer;

					spAutomationPeer = m_tpTextBoxPart.GetOrCreateAutomationPeer();

					if (spAutomationPeer is not null)
					{
						//XHANDLE handle = spAutomationPeer.Cast<AutomationPeer>()->GetHandle();
						//if (handle)
						//{
						//	IFC(CoreImports::AutomationRaiseAutomationPropertyChanged(static_cast<CAutomationPeer*>(handle), UIAXcp::APAutomationProperties::APControlledPeersProperty, CValue(), CValue()));
						//}
					}
				}
			}
		}
#endif

		private void HookToRootScrollViewer()
		{
			FrameworkElement spRSViewerAsFE = null;
			DependencyObject spCurrentAsDO = this;
			DependencyObject spParentAsDO;
			ScrollViewer spRootScrollViewer;

			while (spCurrentAsDO is not null)
			{
				spParentAsDO = VisualTreeHelper.GetParent(spCurrentAsDO);

				if (spParentAsDO is not null)
				{
					FrameworkElement spParentAsFE = spParentAsDO as FrameworkElement;

					spCurrentAsDO = spParentAsDO;

					// checking to see if the element is of type rootScrollViewer
					// using IFC will cause the application to throw an exception
					spRootScrollViewer = spParentAsFE as ScrollViewer;
					if (spRootScrollViewer is not null)
					{
						spRSViewerAsFE = spParentAsFE;
					}
					else // if (hr != E_NOINTERFACE)
					{
						//return;
					}
				}
				else
				{
					break;
				}
			}

			if (spRSViewerAsFE is not null)
			{
				m_wkRootScrollViewer = new WeakReference<ScrollViewer>((ScrollViewer)spRSViewerAsFE);
			}
		}

		//------------------------------------------------------------------------------
		// AutoSuggestBox ChangeVisualState
		//
		// Applies the necessary visual state
		//------------------------------------------------------------------------------
		private void ChangeVisualState()
		{
			Control spThisAsControl = this;

			if (m_displayOrientation == DisplayOrientations.Landscape ||
				m_displayOrientation == DisplayOrientations.LandscapeFlipped)
			{
				VisualStateManager.GoToState(spThisAsControl, c_VisualStateLandscape, useTransitions: true);
			}
			else if (m_displayOrientation == DisplayOrientations.Portrait ||
				m_displayOrientation == DisplayOrientations.PortraitFlipped)
			{
				VisualStateManager.GoToState(spThisAsControl, c_VisualStatePortrait, useTransitions: true);
			}

			// Uno TODO:
			//checked_cast<CControl>(GetHandle())->EnsureValidationVisuals();
		}

		//------------------------------------------------------------------------------
		// AutoSuggestBox AlignmentHelper
		//
		// Performs the alignment to the top or bottom depending on the location of the control
		//------------------------------------------------------------------------------
		private void AlignmentHelper(Rect sipOverlay)
		{
			// query the ScrollViewer.BringIntoViewOnFocusChange property
			// if it's set to false, the control should not move
			DependencyObject spThisAsDO = this;
			bool bringIntoViewOnFocusChange;

			// In case when the app is not occluded by the SIP, we should just calculate the max suggestion area same way as if SIP is not deployed.
			// Otherwise sipOverlay.Y is 0 and it will erroneously throw the calculation off, it is same as if SIP is deployed at the top of the app.
			if (sipOverlay.Y == 0)
			{
				MaximizeSuggestionAreaWithoutInputPane();
				return;
			}

			if (m_wkRootScrollViewer is null)
			{
				return;
			}

			bringIntoViewOnFocusChange = ScrollViewer.GetBringIntoViewOnFocusChange(spThisAsDO);

			if (bringIntoViewOnFocusChange)
			{
				if (m_scrollActions.Count == 0)
				{
					_ = m_wkRootScrollViewer.TryGetTarget(out var spRootScrollViewerAsUIElement);

					if (spRootScrollViewerAsUIElement is not null)
					{
						Point point = default;
						Rect layoutBounds = GetAdjustedLayoutBounds();

						// getting the position with respect to the root ScrollViewer
						TransformPoint(spRootScrollViewerAsUIElement, ref point);

						GetActualTextBoxSize(out var actualTextBoxWidth, out var actualTextBoxHeight);

						double bottomY = point.Y + actualTextBoxHeight;

						// updates the visual state of the ASB depending on the phone orientation
						ChangeVisualState();

						MaximizeSuggestionArea(point.Y, bottomY, sipOverlay.Y, layoutBounds);
					}
				}
				else
				{
					ApplyScrollActions(true);
				}

				UpdateSuggestionListPosition();
				UpdateSuggestionListSize();
			}
		}

		//------------------------------------------------------------------------------
		// AutoSuggestBox MaximizeSuggestionArea
		//
		// Maximizes the suggestion list area if the AutoMaximizeSuggestionArea is enabled.
		//------------------------------------------------------------------------------
		private void MaximizeSuggestionArea(double topY, double bottomY, double sipOverlayY, Rect layoutBounds)
		{
			double deltaTop = 0;
			double deltaBottom = 0;
			bool autoMaximizeSuggestionArea = AutoMaximizeSuggestionArea;

			//DBG_TRACE(L"DBASB[0x%p]: topY: %f, bottomY: %f, sipOverlayAreaY: %f, windowsBoundsHeight",
			//	this, topY, bottomY, sipOverlayY, windowsBoundsHeight);

			GetCandidateWindowPopupAdjustment(
				true /* ignoreSuggestionListPosition */,
				out _,
				out var candidateWindowYOffset);

			// distance from top of asb (or candidate window, whichever is higher) to bottom of system chrome
			deltaTop = topY + (candidateWindowYOffset < 0 ? candidateWindowYOffset : 0) - layoutBounds.Y;
			// distance from bottom of asb (or candidate window, whichever is lower) to top of sip
			deltaBottom = sipOverlayY - (bottomY + (candidateWindowYOffset > 0 ? candidateWindowYOffset : 0));

			if (deltaBottom < 0)
			{
				double actualTextBoxHeight = bottomY - topY;

				// Scrolls the textbox up above the SIP if it is covered by the SIP, put the suggestions
				// list on top of the ASB and maximizes its height based on the available space left.
				deltaBottom *= -1;
				Scroll(ref deltaBottom);
				m_suggestionListPosition = SuggestionListPosition.Above;

				// scroll function changes the deltaBottom to 0 if the ASB reached its location
				// otherwise it will contain the remaining distance to the desired destination
				// subtracting the positive deltabottom value (bottomY - sipOverlayY)
				// then subtracting deltaBottom which will either contain 0 or the remaining distance
				m_availableSuggestionHeight = sipOverlayY - (deltaBottom + actualTextBoxHeight + layoutBounds.Y);
			}
			else if (autoMaximizeSuggestionArea &&
					 (deltaTop < s_minSuggestionListHeight && deltaBottom < s_minSuggestionListHeight))
			{
				// Scrolls the textbox to the top of the page, this makes use of ScrollableArea
				// of the RootScrollViewer if needed. Put the suggestions list to the bottom of the
				// ASB and maximizes its height based on the available space, the height of the
				// suggestion list shouldn't go beyond the SIP area.

				double actualTextBoxHeight = bottomY - topY;

				Scroll(ref deltaTop);

				// in case we cannot scroll all the way to the top due to ScrollViewer scrollableheight restrictions,
				// we maximize the suggestions list position and size
				// deltaTop will only be greater than zero in case we couldn't scroll all the way to the top
				if (deltaTop > 0.0)
				{
					topY = deltaTop + layoutBounds.Y;
					bottomY = topY + actualTextBoxHeight;

					deltaBottom = sipOverlayY - bottomY;

					if (deltaTop < deltaBottom)
					{
						m_suggestionListPosition = SuggestionListPosition.Below;
						m_availableSuggestionHeight = Math.Abs(deltaBottom);
					}

					else
					{
						m_suggestionListPosition = SuggestionListPosition.Above;
						m_availableSuggestionHeight = deltaTop;
					}
				}

				else
				{
					m_suggestionListPosition = SuggestionListPosition.Below;
					m_availableSuggestionHeight = sipOverlayY - actualTextBoxHeight - layoutBounds.Y;
				}
			}

			else
			{
				if (deltaTop < deltaBottom)
				{
					m_suggestionListPosition = SuggestionListPosition.Below;
					m_availableSuggestionHeight = Math.Abs(deltaBottom);
				}
				else
				{
					m_suggestionListPosition = SuggestionListPosition.Above;
					m_availableSuggestionHeight = deltaTop;
				}
			}

			// Set availabeHeight to zero if there are no space left on the screen, this can happen
			// for instance when the ASB has a great height and when the device is in landscape orientation
			if (m_availableSuggestionHeight < 0)
			{
				m_availableSuggestionHeight = 0;
			}
		}

		private void MaximizeSuggestionAreaWithoutInputPane()
		{
			using var scopeGuard = Disposable.Create(() =>
			{
				// Set availableHeight to zero if there are no space left on the screen, this can happen
				// for instance when the ASB has a great height and when the device is in landscape orientation
				if (m_availableSuggestionHeight < 0)
				{
					m_availableSuggestionHeight = 0;
				}
			});

			double topY;
			double bottomY;
			double deltaTop = 0;
			double deltaBottom = 0;
			Rect layoutBounds = default;
			bool autoMaximizeSuggestionArea = AutoMaximizeSuggestionArea;
			UIElement spRootScrollViewerAsUIElement;
			double candidateWindowYOffset = 0.0;
			Point point = default;

			if (m_wkRootScrollViewer?.TryGetTarget(out var unoTemp) == true && (spRootScrollViewerAsUIElement = unoTemp) is not null)
			{
				// getting the position with respect to the root ScrollViewer
				TransformPoint(spRootScrollViewerAsUIElement, ref point);
			}


			// Instead of determining the alignment of the autosuggest popup using the popup layout bounds, use the adjusted layout
			// bounds of the text box in the case where the autosuggest popup is nullptr. If a windowed popup is created later,
			// the UpdateSuggestionListPosition function will run again and correct the invalid previous alignment.
			// Uno TODO:
			if ((m_tpPopupPart is not null) /*&& (m_tpPopupPart.IsWindowed)*/)
			{
				//layoutBounds = DXamlCore.Current.CalculateAvailableMonitorRect(m_tpPopupPart, point));
			}
			else
			{
				layoutBounds = GetAdjustedLayoutBounds();
			}

			GetActualTextBoxSize(out var actualTextBoxWidth, out var actualTextBoxHeight);

			topY = point.Y;
			bottomY = point.Y + actualTextBoxHeight;

			GetCandidateWindowPopupAdjustment(
				true /* ignoreSuggestionListPosition */,
				out _,
				out candidateWindowYOffset);

			// distance from top of asb (or candidate window, whichever is higher) to bottom of system chrome
			deltaTop = topY + (candidateWindowYOffset < 0 ? candidateWindowYOffset : 0) - layoutBounds.Y;

			// distance from bottom of asb (or candidate window, whichever is lower) to the bottom of the layout bounds
			deltaBottom = layoutBounds.Height - (bottomY + (candidateWindowYOffset > 0 ? candidateWindowYOffset : 0));

			if (deltaTop < deltaBottom)
			{
				m_suggestionListPosition = SuggestionListPosition.Below;
				m_availableSuggestionHeight = Math.Abs(deltaBottom);
			}
			else
			{
				m_suggestionListPosition = SuggestionListPosition.Above;
				m_availableSuggestionHeight = deltaTop;
			}
		}

		//------------------------------------------------------------------------------
		// Walks up the visual tree and find all ScrollViewers, try to scroll them up or down to see
		// if we can let the ASB hit the desired position (Top or Bottom)
		//------------------------------------------------------------------------------
		private void Scroll(ref double totalOffset)
		{
			DependencyObject spCurrentAsDO = this;
			DependencyObject spParentAsDO;

			double previousYLocation = 0;

			Debug.Assert(m_scrollActions.Count == 0);

			do
			{
				spParentAsDO = VisualTreeHelper.GetParent(spCurrentAsDO);

				if (spParentAsDO is not null)
				{
					ScrollViewer spScrollViewer;

					// checking to see if the element is of type ScrollViewer
					// using IFC will cause the application to throw an exception
					spScrollViewer = spParentAsDO as ScrollViewer;
					if (spScrollViewer is not null)
					{
						UIElement spScrollViewerAsUIE;
						Point asbLocation = default;
						double partialOffset = 0;

						spScrollViewerAsUIE = spScrollViewer;
						TransformPoint(spScrollViewerAsUIE, ref asbLocation);

						asbLocation.Y -= (float)previousYLocation;
						partialOffset = asbLocation.Y;

						// checking to see if the ASB's position within the ScrollViewer is less than the total offset
						// this means that the ASB will scroll out of the ScrollViewer's viewport
						// in this case, we scroll by the ASB's position and let the parent ScrollViewer handle the rest of the move
						if (asbLocation.Y < totalOffset)
						{
							PushScrollAction(spScrollViewer, ref partialOffset);

							totalOffset -= asbLocation.Y - partialOffset;
						}
						else
						{
							PushScrollAction(spScrollViewer, ref totalOffset);
						}

						// if ASB cannot scroll by the partial offset value (ScrollViewer height restrictions)
						// the difference is added to previous location so that the parent ScrollViewer handles the rest of the move
						previousYLocation = asbLocation.Y - (float)partialOffset;
					}
					else //if (hr != E_NOINTERFACE)
					{
						return;
					}

					spCurrentAsDO = spParentAsDO;
				}

			} while (spParentAsDO is not null);

			ApplyScrollActions(true /* hasNewScrollActions */);
		}

		//------------------------------------------------------------------------------
		// Push scroll actions for the given ScrollViewer to the internal ScrollAction vector.
		//------------------------------------------------------------------------------
		private void PushScrollAction(ScrollViewer scrollViewer, ref double targetOffset)
		{
			ScrollViewer spScrollViewer = scrollViewer;
			double verticalOffset = 0;
			double scrollableHeight = 0;
			ScrollAction action;

			Debug.Assert(spScrollViewer is not null);

			verticalOffset = spScrollViewer.VerticalOffset;
			scrollableHeight = spScrollViewer.ScrollableHeight;

			action.wkScrollViewer = new WeakReference<ScrollViewer>(spScrollViewer);

			action.initial = verticalOffset;
			if (targetOffset + verticalOffset > scrollableHeight)
			{
				action.target = scrollableHeight;
				targetOffset -= scrollableHeight - verticalOffset;
			}
			else
			{
				action.target = targetOffset + verticalOffset;

				if (action.target < 0)
				{
					action.target = 0;
					targetOffset += verticalOffset;
				}

				else
				{
					targetOffset = 0;
				}
			}

			m_scrollActions.Add(action);
		}

		private void ApplyScrollActions(bool hasNewScrollActions)
		{
			if (m_wkRootScrollViewer is null)
			{
				goto Cleanup;
			}

			foreach (var iter in m_scrollActions)
			{
				iter.wkScrollViewer.TryGetTarget(out var spScrollViewer);

				if (spScrollViewer is not null)
				{
					double offset = iter.target;

					if (!hasNewScrollActions)
					{
						offset = iter.initial;
					}

					m_wkRootScrollViewer.TryGetTarget(out var m_wkRootScrollViewerTarget);
					if (spScrollViewer == m_wkRootScrollViewerTarget)
					{
						// potential bug on RootScrolViewer, there is a visual glitch on the RootScrollViewer
						// when ChangeViewWithOptionalAnimation is used
						spScrollViewer.ScrollToVerticalOffset(offset);
					}
					else
					{
						_ = spScrollViewer.ChangeViewWithOptionalAnimation(
								null,                   // horizontalOffset
								offset,    // verticalOffset
								null,                   // zoomFactor
								false                     // disableAnimation
								);
					}
				}
			}

		Cleanup:
			if (!hasNewScrollActions)
			{
				m_scrollActions.Clear();
			}
		}

		private void OnTextChangedEventTimerTick(object sender, object args)
		{
			m_tpTextChangedEventTimer.Stop();

			if (m_tpTextChangedEventArgs is not null)
			{
				TextChanged?.Invoke(this, m_tpTextChangedEventArgs);
			}

			// We expect apps to modify the ItemsSource in the TextChangedEvent (raised above).
			// If that happened. we'll have a deferred update at this point, let's just process
			// it now so we don't have to wait for the next tick.
			ProcessDeferredUpdate();
		}

		private void OnSuggestionSelectionChanged(object sender, SelectionChangedEventArgs args)
		{
			object spSelectedItem = m_tpSuggestionsPart.SelectedItem;
			//TraceLoggingActivity<g_hTraceProvider, MICROSOFT_KEYWORD_TELEMETRY> traceLoggingActivity;

			// ASB handles keyboard navigation on behalf of the suggestion box.
			// Consequently, the latter won't scroll its viewport to follow the selected item.
			// We have to do that ourselves explicitly.
			{
				object scrollToItem = spSelectedItem;

				// We fallback on the first item in order to bring the viewport to the beginning.
				if (scrollToItem is null)
				{
					var items = Items;
					uint itemsCount = items.Size;

					if (itemsCount > 0)
					{
						scrollToItem = items[0];
					}
				}

				if (scrollToItem is not null)
				{
					if (m_tpSuggestionsPart is ListViewBase suggestionsPartAsLVB)
					{
						suggestionsPartAsLVB.ScrollIntoView(scrollToItem);
					}
				}
			}

			if (m_ignoreSelectionChanges)
			{
				// Ignore the selection change if the change is trigged by the TextBoxText changed event.
				return;
			}

			// Telemetry marker for suggestion selection changed.
			//TraceLoggingWriteStart(traceLoggingActivity,
			//	"ASBSuggestionSelectionChanged");

			// The only time we'll get here is when we're keyboarding through the suggestion list.
			// In this case, we're going to be updating the TextBox's text
			// as the user does that, so we should not be responding to TextChanged
			// events in the TextBox, as they shouldn't be affecting anything.
			// However, TextChanged is an asynchronous event, so we can't just
			// set a boolean value to true at the start of this method
			// and then set it back to false at the end of this method,
			// because the TextChanged will come in after the end of this method.
			// To get around this fact, we'll leverage the fact that, by the time
			// this method returns, the TextChanged events will be added to the
			// event queue.  We'll post a callback to change m_ignoreTextChanges
			// back to false once all of the TextChanged events have been raised.
			m_ignoreTextChanges = true;

			if (spSelectedItem is not null)
			{
				bool updateTextOnSelect = UpdateTextOnSelect;

				if (updateTextOnSelect)
				{
					string strTextMemberPath = TextMemberPath;
					//if (m_spPropertyPathListener is null && strTextMemberPath.Length > 0)
					//{
					//	var pPropertyPathParser = new PropertyPathParser();
					//	pPropertyPathParser.SetSource(strTextMemberPath, null /* context */);
					//	m_spPropertyPathListener = new PropertyPathListener(null /* pOwner */, pPropertyPathParser, false /* fListenToChanges*/, false /* fUseWeakReferenceForSource */);
					//}

					TryGetSuggestionValue(spSelectedItem, /*m_spPropertyPathListener,*/ out string strSelectedItem);
					UpdateTextBoxText(strSelectedItem, AutoSuggestionBoxTextChangeReason.SuggestionChosen);
				}

				// If the item was selected using Gamepad or Remote, move the focus to the selected item.
				if (m_inputDeviceTypeUsed == InputDeviceType.GamepadOrRemote)
				{
					if (m_tpSuggestionsPart is ListViewBase suggestionsPartAsLVB &&
						suggestionsPartAsLVB.ContainerFromItem(spSelectedItem) is UIElement selectedItemAsUIElement)
					{
						selectedItemAsUIElement.Focus(FocusState.Keyboard);
					}
				}

				SuggestionChosen?.Invoke(this, new AutoSuggestBoxSuggestionChosenEventArgs(spSelectedItem));
			}

			// At this point everything that's going to post a TextChanged event
			// to the event queue has done so, so we'll schedule a callback
			// to reset the value of m_ignoreTextChanges to false once they've
			// all been raised.
			//IFC_RETURN(DXamlCore::GetCurrent()->GetXamlDispatcherNoRef()->RunAsync(
			//	MakeCallback(
			//		this,
			//		&AutoSuggestBox::ResetIgnoreTextChanges)
			//	));
		}

		private void OnListViewItemClick(object sender, ItemClickEventArgs args)
		{
			var spClickedItem = args.ClickedItem;

			// When an suggestion is clicked, we want to raise QuerySubmitted using that
			// as the chosen suggestion.  However, clicking on an item may additionally raise
			// SelectionChanged, which will set the value of the TextBox and raise SuggestionChosen,
			// both of which we want to occur before we raise QuerySubmitted.
			// To account for this, we'll register a callback that will cause us to call SubmitQuery
			// after everything else has happened.
			//IFC(DXamlCore::GetCurrent()->GetXamlDispatcherNoRef()->RunAsync(
			//	MakeCallback(
			//		this,
			//		&AutoSuggestBox::SubmitQuery,
			//		spClickedItem.Get())
			//	));
		}

		private void OnListViewContainerContentChanging(ListViewBase listViewBase, ContainerContentChangingEventArgs args)
		{
			if (m_tpListItemOrderTransformPart is not null)
			{
				SelectorItem spContainer = args.ItemContainer;

				if (spContainer is not null)
				{
					UIElement spContainerAsUI = spContainer;
					if (spContainerAsUI is not null)
					{
						Point origin = new Point(0.5, 0.5);

						spContainerAsUI.RenderTransformOrigin = origin;
						spContainerAsUI.RenderTransform = m_tpListItemOrderTransformPart;
					}
				}
			}
		}

		//------------------------------------------------------------------------------
		// Transforms coordinates to the target element's space.
		//------------------------------------------------------------------------------
		private void TransformPoint(UIElement targetElement, ref Point point)
		{
			UIElement spThisAsUIElement = this;

			GeneralTransform spGeneralTransform;
			Point inPoint = point;

			Debug.Assert(targetElement is not null);

			spGeneralTransform = spThisAsUIElement.TransformToVisual(targetElement);
			point = spGeneralTransform.TransformPoint(inPoint);
		}

		//------------------------------------------------------------------------------
		// Internal helper function to handle the Hiding event from InputPane.
		//------------------------------------------------------------------------------
		private void OnSipHidingInternal()
		{
			m_isSipVisible = false;

			// scroll all ScrollViewers back if we changed.
			ApplyScrollActions(false /* hasNewScrollActions */);

			// potential bug in RootScrollViewer: when SIP is hiding, the viewport of RootScrollViewer will be restored to
			// the screen size and the content of RootScrollViewer will not able to scroll, in this case, the vertical offset
			// should be reset to 0, however it doesn't.
			// wrong vertical offset will cause suggestionlist in wrong positon next time when SIP is showing.
			if (m_wkRootScrollViewer is not null)
			{
				m_wkRootScrollViewer.TryGetTarget(out var spRootScrollViewer);

				if (spRootScrollViewer is not null)
				{
					double verticalOffset = spRootScrollViewer.VerticalOffset;
					if (verticalOffset != 0)
					{
						spRootScrollViewer.ScrollToVerticalOffset(0);
					}
				}
			}
		}

		private static bool s_fDeferredShowing;

		//------------------------------------------------------------------------------
		// Internal helper function to handle the Showing event from InputPane.
		// The InputPane.Showing event could be called multiple times and the order
		// of the internal and public Sip events are not guaranted.
		//------------------------------------------------------------------------------
		private void OnSipShowingInternal(InputPaneVisibilityEventArgs args)
		{
			if (m_tpTextBoxPart is not null)
			{
				InputPaneVisibilityEventArgs spSipArgs = args;
				Rect sipOverlayArea;
				bool isTextBoxFocused;

				// Hold a reference to the eventarg
				m_tpSipArgs = spSipArgs;

				sipOverlayArea = args.OccludedRect;

				isTextBoxFocused = IsTextBoxFocusedElement();
				if (isTextBoxFocused)
				{
					m_hasFocus = true;

					var currentOrientation = DisplayOrientations.None;
					// UNO TODO:
					//IFC_RETURN(XamlDisplay::GetDisplayOrientation(GetHandle(), currentOrientation));
					if (currentOrientation != m_displayOrientation)
					{
						m_displayOrientation = currentOrientation;
						if (m_scrollActions.Count != 0)
						{
							OnSipHidingInternal();
						}
					}
				}

				if (!m_isSipVisible && m_hasFocus && m_wkRootScrollViewer is not null)
				{
					m_wkRootScrollViewer.TryGetTarget(out ScrollViewer spRootScrollViewer);

					if (spRootScrollViewer is not null)
					{
						double scrollableHeight = spRootScrollViewer.ScrollableHeight;

						if (scrollableHeight == 0 && !s_fDeferredShowing)
						{
							// Wait for next OnSIPShowing event as the RootScrollViewer is not adjusted yet.
							//DBG_TRACE(L"DBASB[0x%p]: RootScrollViewer not yet adjusted", this);

							// There is no guarantee that the Jupiter (InputPane::Showing) will gets call
							// first, the native side invokes the Windows.UI.ViewManagement.InputPane.Showing/Hiding
							// separately from the Jupiter internal events. RootScrollViewer will get
							// notified about the InputPane state (through the callback NotifyInputPaneStateChange)
							// when Jupiter gets the Showing event. Defer the SipEvent if the RootScrollViewer
							// has not yet been notified about the SIP state change.

							DispatcherQueue spDispatcherQueue = DispatcherQueue.GetForCurrentThread();
							AutoSuggestBox spThis = this;
							WeakReference<AutoSuggestBox> wrThis = new(spThis);

							bool enqueued = spDispatcherQueue.TryEnqueue(() =>
							{
								spThis.OnSipShowingInternal(spThis.m_tpSipArgs);

								// clearing the placeholder sip arguments stored in the asb after being used
								spThis.m_tpSipArgs = null;
							});

							Debug.Assert(enqueued);

							s_fDeferredShowing = true;

							return;
						}

						string strText;

						m_isSipVisible = true;
						s_fDeferredShowing = false;

						AlignmentHelper(sipOverlayArea);

						// Expands the suggestion list if there is already text in the textbox
						strText = m_tpTextBoxPart.Text;
						if (strText.Length > 0)
						{
							UpdateSuggestionListVisibility();
						}
					}
				}
			}

			ReevaluateIsOverlayVisible();
		}

		private void UpdateText(string value)
		{
			string strText = Text;

			if (value != strText)
			{
				Text = value;
			}

			// UNO TODO:
			//InvokeValidationCommand(this, strText);
		}

		private void UpdateTextBoxText(string value, AutoSuggestionBoxTextChangeReason reason)
		{
			string strText;

			if (m_tpTextBoxPart is not null)
			{
				strText = m_tpTextBoxPart.Text;
				if (value != strText)
				{
					// when TextBox text is changing, we need to let user know the reason so user can
					// respond correctly.
					// however the TextChanged event is raised asynchronously so we need to reset the
					// reason in the TextChangedEvent.
					// here we are sure the TextChangedEvent will be raised because the old content
					// and new content are different.
					m_textChangeReason = reason;
					m_tpTextBoxPart.Text = value;
					m_tpTextBoxPart.SelectionStart = value.Length;
				}
			}
		}

		protected override AutomationPeer OnCreateAutomationPeer()
		{
			return new AutoSuggestBoxAutomationPeer(this);
		}

		private protected override string GetPlainText()
		{
			object spHeader = Header;

			if (spHeader != null)
			{
				return FrameworkElement.GetStringFromObject(spHeader);
			}

			return null;
		}

		private void GetCandidateWindowPopupAdjustment(bool ignoreSuggestionListPosition, out double xOffset, out double yOffset)
		{
			// UNO TODO: This should be a field that is set properly, but OnTextBox.CandidateWindowBoundsChanged isn't yet implemented in Uno.
			Rect m_candidateWindowBoundsRect = default;
			xOffset = 0.0;
			yOffset = 0.0;
			bool shouldOffsetInXDirection;

			GetActualTextBoxSize(out double textBoxWidth, out double textBoxHeight);

			// There are two cases in which we need to return a nonzero offset:
			// either in the case where the candidate window is appearing above the top
			// of the TextBox, or the case where the candidate window stretches below
			// the bottom of the TextBox.  In either case, we only care if the candidate window
			// height and width are not both zero, since if they are then the candidate window
			// isn't actually being shown.
			//
			// Once we determine that we do need to return a nonzero offset, the next question
			// is whether the suggestion list is being displayed in the position in question
			// (i.e., either above or below the AutoSuggestBox).  If it's not, then we'll return
			// offsets that are all zero, since we don't need to do anything,
			// unless we were instructed to ignore the suggestion list position
			// (used when we're setting the suggestion list position).
			//
			// Finally, if we do need to offset, we then see whether to offset in the x-direction or the y-direction.
			// The general heuristic used is that if the candidate window spans more than half
			// of the width of the TextBox, then we'll offset in the y-direction, since
			// otherwise the popup will be squished into an unacceptably small width.
			// Otherwise, we'll offset in the x-direction and apply a margin
			// that will cause the popup to appear side-by-side with the candidate window.
			shouldOffsetInXDirection = (m_candidateWindowBoundsRect.X + m_candidateWindowBoundsRect.Width) < (textBoxWidth / 2);

			if (m_candidateWindowBoundsRect.Y < 0 && m_candidateWindowBoundsRect.Height > 0 &&
				(m_suggestionListPosition == SuggestionListPosition.Above || ignoreSuggestionListPosition))
			{
				if (shouldOffsetInXDirection)
				{
					xOffset = m_candidateWindowBoundsRect.X + m_candidateWindowBoundsRect.Width;
				}
				else
				{
					yOffset = m_candidateWindowBoundsRect.Y;
				}
			}
			else if (m_candidateWindowBoundsRect.Y + m_candidateWindowBoundsRect.Height > textBoxHeight &&
				(m_suggestionListPosition == SuggestionListPosition.Below || ignoreSuggestionListPosition))
			{
				if (shouldOffsetInXDirection)
				{
					xOffset = m_candidateWindowBoundsRect.X + m_candidateWindowBoundsRect.Width;
				}
				else
				{
					// m_candidateWindowBoundsRect.Y - textBoxHeight gets us the starting point of the
					// candidate window with respect to the lower bound of the TextBox's height,
					// and then from there we add on m_candidateWindowBoundsRect.Height in order to ensure
					// that the popup is flush with the bottom of the candidate window.
					yOffset = m_candidateWindowBoundsRect.Y - textBoxHeight + m_candidateWindowBoundsRect.Height;
				}
			}
		}

		private void ReevaluateIsOverlayVisible()
		{
			if (!IsInLiveTree)
			{
				return;
			}

			// UNO TODO
			bool isOverlayVisible = this.LightDismissOverlayMode == LightDismissOverlayMode.Auto;
			//IFC_RETURN(LightDismissOverlayHelper::ResolveIsOverlayVisibleForControl(this, &isOverlayVisible));

			bool isSuggestionListOpen = IsSuggestionListOpen;

			isOverlayVisible &= !!isSuggestionListOpen;  // Overlay should only be visible when the suggestion list is.
			isOverlayVisible &= !m_sSipIsOpen;           // Except if the SIP is also visible.

			if (isOverlayVisible != m_isOverlayVisible)
			{
				m_isOverlayVisible = isOverlayVisible;

				if (m_isOverlayVisible)
				{
					SetupOverlayState();
				}
				else
				{
					TeardownOverlayState();
				}
			}
		}

		private void SetupOverlayState()
		{
			Debug.Assert(m_isOverlayVisible);
			//Debug.Assert(!m_layoutUpdatedEventHander);

			if (m_tpLayoutRootPart is not null)
			{
				// Create our overlay element if necessary.
				if (m_overlayElement is null)
				{
					Rectangle rectangle = new Rectangle();
					rectangle.Width = 1;
					rectangle.Height = 1;
					rectangle.IsHitTestVisible = false;

					// Create a theme resource for the overlay brush.
					{
						//var core = DXamlCore.Current.GetHandle();
						//var dictionary = core.GetThemeResources();

						//DependencyObject initialValueNoRef = dictionary.GetKeyNoRef("AutoSuggestBoxLightDismissOverlayBackground");

						//CREATEPARAMETERS cp(core);
						//xref_ptr<CThemeResourceExtension> themeResourceExtension;
						//IFC_RETURN(CThemeResourceExtension::Create(
						//	reinterpret_cast<CDependencyObject**>(themeResourceExtension.ReleaseAndGetAddressOf()),
						//	&cp));

						//themeResourceExtension->m_strResourceKey = themeBrush;

						//IFC_RETURN(themeResourceExtension->SetInitialValueAndTargetDictionary(initialValueNoRef, dictionary));

						//IFC_RETURN(themeResourceExtension->SetThemeResourceBinding(
						//	rectangle->GetHandle(),
						//	DirectUI::MetadataAPI::GetPropertyByIndex(KnownPropertyIndex::Shape_Fill))
						//	);
					}

					m_overlayElement = rectangle;
				}

				// Add our overlay element to our layout root panel.
				var layoutRootChildren = m_tpLayoutRootPart.Children;
				layoutRootChildren.Insert(0, m_overlayElement);
			}

			CreateLTEs();

			LayoutUpdated += (_, _) =>
			{
				if (m_isOverlayVisible)
				{
					PositionLTEs();
				}
			};
		}

		private void TeardownOverlayState()
		{
			Debug.Assert(!m_isOverlayVisible);
			//Debug.Assert(m_layoutUpdatedEventHandler is not null);

			DestroyLTEs();

			// Remove our light-dismiss element from our layout root panel.
			if (m_tpLayoutRootPart is not null)
			{
				var layoutRootChildren = m_tpLayoutRootPart.Children;

				var indexOfOverlayElement = layoutRootChildren.IndexOf(m_overlayElement);
				Debug.Assert(indexOfOverlayElement >= 0);
				layoutRootChildren.RemoveAt(indexOfOverlayElement);
			}

			// UNO TODO
			//IFC_RETURN(m_layoutUpdatedEventHandler.DetachEventHandler(ctl::iinspectable_cast(this)));
		}

		private void CreateLTEs()
		{
			//Debug.Assert(m_layoutTransition is null);
			//Debug.Assert(m_overlayLayoutTransition is null);
			//Debug.Assert(m_parentElementForLTEs is null);

			// If we're under the PopupRoot or FullWindowMediaRoot, then we'll explicitly set
			// our LTE's parent to make sure the LTE doesn't get placed under the TransitionRoot,
			// which is lower in z-order than these other roots.
			//if (ShouldUseParentedLTE())
			//{
			//	var parent = VisualTreeHelper.GetParent(this);

			//	m_parentElementForLTEs = parent as UIElement;
			//}

			//UIElement spNativeLTE;
			//DependencyObject spNativeLTEAsDO;

			if (m_overlayElement is not null)
			{
				// Create an LTE for our overlay element.
				//IFC_RETURN(CoreImports::LayoutTransitionElement_Create(
				//	DXamlCore::GetCurrent()->GetHandle(),
				//	m_overlayElement.Cast<FrameworkElement>()->GetHandle(),
				//	m_parentElementForLTEs ? m_parentElementForLTEs.Cast<UIElement>()->GetHandle() : nullptr,
				//	false /*isAbsolutelyPositioned*/,
				//	spNativeLTE.ReleaseAndGetAddressOf()
				//	));

				// Configure the overlay LTE with a rendertransform that we'll use to position/size it.
				{
					//spNativeLTEAsDO = DXamlCore.Current.GetPeer(spNativeLTE, KnownTypeIndex::UIElement);
					//m_overlayLayoutTransition = spNativeLTEAsDO;

					//CompositeTransform compositeTransform = new();

					//m_overlayLayoutTransition.RenderTransform = compositeTransform;
				}
			}

			//IFC_RETURN(CoreImports::LayoutTransitionElement_Create(
			//	DXamlCore::GetCurrent()->GetHandle(),
			//	GetHandle(),
			//	m_parentElementForLTEs ? m_parentElementForLTEs.Cast<UIElement>()->GetHandle() : nullptr,
			//	false /*isAbsolutelyPositioned*/,
			//	spNativeLTE.ReleaseAndGetAddressOf()
			//));
			//spNativeLTEAsDO = DXamlCore.Current.GetPeer(spNativeLTE, KnownTypeIndex::UIElement));
			//m_layoutTransition = spNativeLTEAsDO;

			PositionLTEs();
		}

		private void PositionLTEs()
		{
			//Debug.Assert(m_layoutTransition is not null);

			DependencyObject parentDO = VisualTreeHelper.GetParent(this);
			UIElement parent;

			// If we don't have a parent, then there's nothing for us to do.
			if (parentDO is not null)
			{
				parent = parentDO as UIElement;

				GeneralTransform transform = TransformToVisual(parent);

				Point offset = transform.TransformPoint(default);

				//IFC_RETURN(CoreImports::LayoutTransitionElement_SetDestinationOffset(m_layoutTransition.Cast<UIElement>()->GetHandle(), offset.X, offset.Y));
			}

			// Since AutoSuggestBox's suggestion list does not dismiss on window resize, we have to make sure
			// we update the overlay element's size.
			//if (m_overlayLayoutTransition is not null)
			//{
			//	Transform transform = m_overlayLayoutTransition.RenderTransform;

			//	CompositeTransform compositeTransform = transform as CompositeTransform;

			//	// Uno TODO
			//	//Rect windowBounds = DXamlCore.Current.GetContentBoundsForelement(GetHandle());

			//	//compositeTransform.ScaleX = windowBounds.Width;
			//	//compositeTransform.ScaleY = windowBounds.Height;

			//	GeneralTransform transformToVisual = TransformToVisual(null);

			//	Point offsetFromRoot = transformToVisual.TransformPoint(default);

			//	var flowDirection = FlowDirection;

			//	// Translate the light-dismiss layer so that it is positioned at the top-left corner of the window (for LTR cases)
			//	// or the top-right corner of the window (for RTL cases).
			//	// TransformToVisual(nullptr) will return an offset relative to the top-left corner of the window regardless of
			//	// flow direction, so for RTL cases subtract the window width from the returned offset.x value to make it relative
			//	// to the right edge of the window.
			//	// UNO TODO:
			//	//compositeTransform.TranslateX = flowDirection == FlowDirection.LeftToRight ? -offsetFromRoot.X : offsetFromRoot.X - windowBounds.Width;
			//	compositeTransform.TranslateX = -offsetFromRoot.X;
			//	compositeTransform.TranslateY = -offsetFromRoot.Y;
			//}
		}

		private void DestroyLTEs()
		{
			//Debug.Assert(m_layoutTransition is not null);

			//IFC_RETURN(CoreImports::LayoutTransitionElement_Destroy(
			//	DXamlCore::GetCurrent()->GetHandle(),
			//	GetHandle(),
			//	m_parentElementForLTEs ? m_parentElementForLTEs.Cast<UIElement>()->GetHandle() : nullptr,
			//	m_layoutTransition.Cast<UIElement>()->GetHandle()
			//	));

			//m_layoutTransition.Clear();

			//if (m_overlayLayoutTransition is not null)
			//{
			//	// Destroy our light-dismiss element's LTE.
			//	IFC_RETURN(CoreImports::LayoutTransitionElement_Destroy(
			//	DXamlCore::GetCurrent()->GetHandle(),
			//		m_overlayElement.Cast<FrameworkElement>()->GetHandle(),
			//		m_parentElementForLTEs ? m_parentElementForLTEs.Cast<UIElement>()->GetHandle() : nullptr,
			//		m_overlayLayoutTransition.Cast<UIElement>()->GetHandle()
			//	));

			//	m_overlayLayoutTransition.Clear();
			//}

			//m_parentElementForLTEs.Clear();
		}

#if !HAS_UNO // UNO TODO:
		private bool ShouldUseParentedLTE()
		{
			return VisualTreeHelper.GetRoot(this) is PopupRoot or FullWindowMediaRoot;
		}
#endif

		private Rect GetAdjustedLayoutBounds()
		{
			// UNO TODO:
			return default;
			//layoutBounds = DXamlCore.Current.GetContentLayoutBoundsForElement(GetHandle());

			// TODO: 12949603 -- re-enable this in XamlOneCoreTransforms mode using OneCore-friendly APIs
			// It's disabled today because ClientToScreen deals in screen coordinates, which isn't allowed in strict mode.
			// AutoSuggestBox effectively acts as though the client window is always at the very top of the screen.
			//if (!XamlOneCoreTransforms::IsEnabled())
			//{
			//	Point point = DXamlCore.Current.ClientToScreen();
			//	layoutBounds.Y -= point.Y;
			//}
		}

		private void GetActualTextBoxSize(out double actualWidth, out double actualHeight)
		{
			if (m_tpTextBoxPart is not null)
			{
				TextBox spTextBoxPeer = m_tpTextBoxPart;
				//float fWidth, fHeight;
				//static_cast<CTextBoxBase*>(spTextBoxPeer->GetHandle())->GetActualSize(fWidth, fHeight);
				//actualWidth = static_cast<double>(fWidth);
				//actualHeight = static_cast<double>(fHeight);
				actualWidth = spTextBoxPeer.ActualWidth;
				actualHeight = spTextBoxPeer.ActualHeight;
			}
			else
			{
				actualWidth = 0;
				actualHeight = 0;
			}
		}
	}
}

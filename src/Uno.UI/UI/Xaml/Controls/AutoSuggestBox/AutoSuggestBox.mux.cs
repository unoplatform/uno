#nullable enable

using System;
using DirectUI;
using Uno.Disposables;
using Uno.UI.DataBinding;
using Uno.UI.DirectUI.Components;
using Uno.UI.UI.Xaml.Data;
using Uno.UI.Xaml.Core;
using Uno.UI.Xaml.Input;
using Windows.Foundation;
using Windows.Graphics.Display;
using Windows.System;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml.Automation;
using Windows.UI.Xaml.Automation.Peers;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;
using static Microsoft.UI.Xaml.Controls._Tracing;

namespace Windows.UI.Xaml.Controls;

public partial class AutoSuggestBox : ItemsControl
{
	private const long s_textChangedEventTimerDuration = 1500000L; // 150ms

	private const int s_minSuggestionListHeight = 178; // 178 pixels = 44 pixels * 4 items + 2 pixels

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

	private bool m_sSipIsOpen = false;

	//~AutoSuggestBox()
	//{
	//	if (m_tpInputPane)
	//	{
	//		if (m_sipEvents[0].value != 0)
	//		{
	//			/*VERIFYHR*/
	//			(m_tpInputPane.remove_Showing(m_sipEvents[0]));
	//		}
	//		if (m_sipEvents[1].value != 0)
	//		{
	//			/*VERIFYHR*/
	//			(m_tpInputPane.remove_Hiding(m_sipEvents[1]));
	//		}
	//	}

	//	// Should have been clean up in the unloaded handler.
	//	MUX_ASSERT(!m_isOverlayVisible);
	//}


	//------------------------------------------------------------------------------
	// AutoSuggestBox PrepareState
	//
	// Prepares this control by attaching needed event handlers
	//------------------------------------------------------------------------------
	private void PrepareState()
	{
		TimeSpan interval = new TimeSpan();

		base.PrepareState();

		// If we're in the context of XAML islands, then we don't want to use InputPane -
		// that requires a UIContext instance, which is not supported in WinUI 3.
		if (DXamlCore.Current.GetInitializationType() != InitializationType.IslandsOnly)
		{
			// Acquire an instance to IInputPane. It is used to listen on
			// SIP events and queries that type for the size occupied by
			// that SIP when it is visible, so we can position this
			// control correctly.

			(GetActivationFactory(
				stringReference(RuntimeClass_Windows_UI_ViewManagement_InputPane),
				&spInputPaneStatics));

			var spInputPane = InputPane.GetForCurrentView(); //Task 23548475 Get correct input pane without UIContext.

			m_tpInputPane = spInputPane;

			// listen on visibility changes:
			spInputPane.Showing += OnSipShowing; //&m_sipEvents[0])); TODO MZ

			spInputPane.Hiding += OnSipHiding; //&m_sipEvents[1])); TODO MZ
		}

		DispatcherTimer spTextChangedEventTimer = new DispatcherTimer();
		m_tpTextChangedEventTimer = spTextChangedEventTimer;

		interval = new TimeSpan(s_textChangedEventTimerDuration); //TODO MZ: What units?
		spTextChangedEventTimer.Interval = interval;

		Unloaded += OnUnloaded;
		SizeChanged += OnSizeChanged;
	}

	private void SetIsSuggestionListOpen(bool value)
	{
		this.SetValue(IsSuggestionListOpenProperty, value);

		// When we programmatically set IsSuggestionListOpen to true, we want to take focus.
		if (value)
		{
			Focus(FocusState.Programmatic);
		}
	}

	protected override void OnApplyTemplate()
	{
		base.OnApplyTemplate();
	}

	internal override void OnPropertyChanged2(DependencyPropertyChangedEventArgs args)
	{
		base.OnPropertyChanged2(args);

		if (args.Property == TextProperty)
		{
			UpdateTextBoxText((string)args.NewValue, AutoSuggestionBoxTextChangeReason.ProgrammaticChange);
		}
		else if (args.Property == QueryIconProperty)
		{
			SetTextBoxQueryButtonIcon();
		}
		else if (args.Property == IsSuggestionListOpenProperty)
		{
			bool isOpen = (bool)args.NewValue;

			// Only proceed if there is at least one island that's still alive. Otherwise we can crash when opening the
            // windowed popup when it tries to get its island in CPopup::EnsureWindowForWindowedPopup to check hwnds.
            // Note: Tests running in UWP mode don't need this check, so count them as having islands.
            CCoreServices* core = DXamlCore::GetCurrent()->GetHandle();
            if (core->GetInitializationType() != InitializationType::IslandsOnly || core->HasXamlIslands())
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
						if (m_tpSuggestionsPart is not null)
						{
							m_tpSuggestionsPart.SelectedIndex = -1;
						}
						m_ignoreSelectionChanges = false;
					}
				}
			}

			ReevaluateIsOverlayVisible();

			SetCurrentControlledPeer(isOpen ? ControlledPeer.SuggestionsList : ControlledPeer.None);
		}
		else if (args.Property == LightDismissOverlayModeProperty)
		{
			ReevaluateIsOverlayVisible();
		}
		else if (args.Property == TextMemberPathProperty)
		{
			// TextMemberPath updated, release existing PropertyPathListener
			m_spPropertyPathListener = null;
		}
	}

	private protected override void OnUnloaded()
	{
		base.OnUnloaded();

		if (!IsInLiveTree() && m_isOverlayVisible)
		{
			m_isOverlayVisible = false;
			TeardownOverlayState();
		}
	}

	private void OnTextBoxTextChanged(object sender, TextChangedEventArgs args)
	{
		try
		{
			string strQueryText;
			AutoSuggestBoxTextChangedEventArgs spEventArgs = new();

			m_textChangedCounter++;

			spEventArgs.SetCounter(m_textChangedCounter);
			spEventArgs.SetOwner(this);

			spEventArgs.Reason = m_textChangeReason;

			var m_tpTextChangedEventArgs = spEventArgs;

			m_tpTextChangedEventTimer.Stop();
			m_tpTextChangedEventTimer.Start();

			strQueryText = m_tpTextBoxPart.Text;

			UpdateText(strQueryText);

			if (!m_ignoreTextChanges)
			{
				if (m_textChangeReason == AutoSuggestionBoxTextChangeReason.UserInput)
				{
					m_userTypedText.Duplicate(strQueryText);
					// make sure the suggestion list is shown when user inputs
					UpdateSuggestionListVisibility();
				}

				if (m_tpSuggestionsPart != null)
				{
					int selectedIndex = 0;

					selectedIndex = m_tpSuggestionsPart.SelectedIndex;

					if (-1 != selectedIndex)
					{
						m_ignoreSelectionChanges = true;
						m_tpSuggestionsPart.SelectedIndex = -1;
						m_ignoreSelectionChanges = false;
					}
				}
			}

		}
		finally
		{
			m_textChangeReason = AutoSuggestionBoxTextChangeReason.UserInput;
		}
	}

	private void OnTextBoxCandidateWindowBoundsChanged(TextBox sender, CandidateWindowBoundsChangedEventArgs args)
	{
		Rect candidateWindowBounds = args.Bounds;

		// do nothing if the candidate windows bound did not change
		if (RectUtil.AreEqual(m_candidateWindowBoundsRect, candidateWindowBounds))
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

		if (m_tpInputPane != null && m_sSipIsOpen)
		{
			Rect sipOverlayArea;

			sipOverlayArea = m_tpInputPane.OccludedRect;
			AlignmentHelper(sipOverlayArea);
		}
		else
		{
			MaximizeSuggestionAreaWithoutInputPane();
		}

		UpdateSuggestionListPosition();
		UpdateSuggestionListSize();
	}

	private void OnSizeChanged(object sender, SizeChangedEventArgs args)
	{
		if (m_tpSuggestionsContainerPart != null)
		{
			double actualWidth = ActualWidth;
			m_tpSuggestionsContainerPart.Width = actualWidth;
		}
	}

	// This event handler is only for Gamepad or Remote cases, where Focus and Selection are tied.
	// This is never invoked for Keyboard cases since Focus stays on the TextBox but Selection moves
	// down the ListView so the key down events go to OnKeyDown event handler.
	private void OnSuggestionListKeyDown(
		object pSender,
		KeyRoutedEventArgs pArgs)
	{
		var key = VirtualKey.None;
		key = pArgs.Key;

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

					var spItems = Items;
					var count = spItems.Count;
					int lastIndex = count - 1;

					// If we are already at the Suggestion that is adjacent to the TextBox, then set SelectedIndex to -1.
					if ((selectedIndex == 0 && !IsSuggestionListVectorReversed()) ||
						(selectedIndex == lastIndex && IsSuggestionListVectorReversed()))
					{
						UpdateTextBoxText(m_userTypedText, AutoSuggestionBoxTextChangeReason.ProgrammaticChange);
						m_tpSuggestionsPart.SelectedIndex = -1;

						(m_tpTextBoxPart as UIElement)?.Focus(FocusState.Keyboard);
					}

					wasHandledLocally = true;
					break;
				}

			case VirtualKey.Space:
			case VirtualKey.Enter:
				{
					object spSelectedItem = m_tpSuggestionsPart.SelectedItem;
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
				bool succeeded = false;
				(m_tpTextBoxPart as UIElement)?.Focus(FocusState.Keyboard);

				wasHandledLocally = true;
				break;
		}

		if (wasHandledLocally)
		{
			pArgs.Handled = true;
		}
	}

	//------------------------------------------------------------------------------
	// Handler of the suggestions Popup's Opened event.
	//
	// Updates the suggestions list's position as its position can be changed before
	// the previous Open operation.
	//------------------------------------------------------------------------------
	private void OnPopupOpened(
		object pSender,
		object pArgs)
	{
		// Apply a shadow effect to the popup's immediate child
		ApplyElevationEffect(m_tpPopupPart as UIElement);

		UpdateSuggestionListPosition();
	}

	//------------------------------------------------------------------------------
	// Handler of the suggestions container's Loaded event.
	//
	// Sets the position of the suggestions list as soon as the container is loaded.
	//------------------------------------------------------------------------------
	private void OnSuggestionsContainerLoaded(
		object pSender,
		RoutedEventArgs pArgs)
	{
		UpdateSuggestionListPosition();
		UpdateSuggestionListSize();
	}

	//------------------------------------------------------------------------------
	// Handler of the text box's Loaded event.
	//
	// Retrieves the query button if it exists and attaches a handler to its Click event.
	//------------------------------------------------------------------------------
	private void OnTextBoxLoaded(
		object pSender,
		RoutedEventArgs pArgs)
	{
		ButtonBase spTextBoxQueryButtonPart;

		spTextBoxQueryButtonPart = m_tpTextBoxPart.GetTemplateChild<ButtonBase>(c_TextBoxQueryButtonName);

		m_tpTextBoxQueryButtonPart = spTextBoxQueryButtonPart;

		if (m_tpTextBoxQueryButtonPart != null)
		{
			SetTextBoxQueryButtonIcon();
			if (m_epQueryButtonClickEventHandler == null)
			{
				m_tpTextBoxQueryButtonPart.Click += OnTextBoxQueryButtonClick;
				// TODO:MZ: Unsubscribe when appropriate
			}
			// Update query button's AutomationProperties.Name to "Search" by default
			var automationName = AutomationProperties.GetName(m_tpTextBoxQueryButtonPart as ButtonBase);
			if (automationName == null)
			{
				automationName = DXamlCore.GetCurrentNoCreate().GetLocalizedResourceString(UIA_AUTOSUGGESTBOX_QUERY);
				AutomationProperties.SetName(m_tpTextBoxQueryButtonPart as ButtonBase, automationName);
			}
		}
	}

	//------------------------------------------------------------------------------
	// Handler of the text box's Unloaded event.
	//
	// Removes the handler to the query button
	//------------------------------------------------------------------------------
	private void OnTextBoxUnloaded(
		 object pSender,
		 RoutedEventArgs pArgs)
	{
		// Checking IsActive because Unloaded is async and we might have reloaded before this fires
		if (m_tpTextBoxQueryButtonPart is not null &&
			m_epQueryButtonClickEventHandler.Disposable is not null &&
			!GetHandle().IsActive())
		{
			m_epQueryButtonClickEventHandler.Disposable = null;
		}
	}

	/// <summary>
	/// Handler of the query button's Click event.
	/// Raises the QuerySubmitted event.
	/// </summary>
	private void OnTextBoxQueryButtonClick(object sender, RoutedEventArgs args) => ProgrammaticSubmitQuery();

	internal void ProgrammaticSubmitQuery()
	{
		// Clicking the query button should always submit the query solely with the text
		// in the TextBox, and should ignore any selected item in the suggestion list.
		// To ensure that, we'll deselect any item in the suggestion list that might be selected
		// before submitting the query.
		m_ignoreSelectionChanges = true;
		try
		{
			if (m_tpSuggestionsPart != null)
			{
				m_tpSuggestionsPart.SelectedIndex = -1;
			}
		}
		finally
		{
			m_ignoreSelectionChanges = false;
		}

		SubmitQuery(null);
	}

	/// <summary>
	/// Sets the value of QueryButton.Content to the current value of QueryIcon.
	/// </summary>
	private void SetTextBoxQueryButtonIcon()
	{
		if (m_tpTextBoxQueryButtonPart != null)
		{
			m_tpTextBoxQueryButtonPart.SetCursor(MouseCursorArrow);
			IconElement spQueryIcon;

			spQueryIcon = QueryIcon;

			if (spQueryIcon != null)
			{
				SymbolIcon spQueryIconAsSymbolIcon = spQueryIcon as SymbolIcon;

				if (spQueryIconAsSymbolIcon is not null)
				{
					// Setting FontSize to zero prevents SymbolIcon from setting a static FontSize on it's child TextBlock,
					// allowing the binding to AutoSuggestBoxIconFontSize to be inherited properly.
					spQueryIconAsSymbolIcon.SetFontSize(0);
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
		if (m_tpTextBoxQueryButtonPart != null)
		{
			m_tpTextBoxQueryButtonPart.Content = null;
		}
	}

	//------------------------------------------------------------------------------
	// Raises the QuerySubmitted event using the current content of the TextBox.
	//------------------------------------------------------------------------------
	private void SubmitQuery(object? pChosenSuggestion)
	{
		var strQueryText = m_tpTextBoxPart.Text;
		var spEventArgs = new AutoSuggestBoxQuerySubmittedEventArgs(pChosenSuggestion, strQueryText);
		QuerySubmitted?.Invoke(this, spEventArgs);

		IsSuggestionListOpen = false;
	}

	/// <summary>
	/// The suggestions drop-down will never be displayed when this control loses focus.
	/// </summary>
	/// <param name="e">Event args.</param>
	protected override void OnLostFocus(RoutedEventArgs e)
	{
		if (!m_keepFocus)
		{
			m_hasFocus = false;

			if (m_isSipVisible)
			{
				// checking to see if the focus went to a new element that contains a textbox
				// in this case, we leave it up to the new element to handle scrolling
				DependencyObject spFocusedElement = GetFocusedElement();
				if (spFocusedElement != null)
				{
					TextBox spFocusedElementAsTextBox = spFocusedElement;
					if (spFocusedElementAsTextBox == null)
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

	/// <summary>
	/// AutoSuggestBox OnGotFocus event handler.
	/// The suggestions drop-down should be displayed if items are present and the textbox has text.
	/// </summary>
	/// <param name="e">Event args.</param>
	protected override void OnGotFocus(RoutedEventArgs e)
	{
		try
		{
			if (!m_keepFocus)
			{
				var isTextBoxFocused = IsTextBoxFocusedElement;
				if (isTextBoxFocused)
				{
					string strText;

					// this code handles the case when the control receives focus from another control
					// that was using the sip. In this case, OnSipShowing is not guaranteed to be called
					// hence, we align the control to the top or bottom depending on its position at the time
					// it received focus
					if (m_tpInputPane != null && m_sSipIsOpen)
					{
						Rect sipOverlayArea = m_tpInputPane.OccludedRect;
						AlignmentHelper(sipOverlayArea);
					}

					// Expands the suggestion list if there is already text in the textbox
					strText = m_tpTextBoxPart.Text;

					if (!string.IsNullOrEmpty(strText) && !m_suppressSuggestionListVisibility)
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
		}
		finally
		{
			m_keepFocus = false;
		}
	}

	/// <summary>
	/// AutoSuggestBox OnKeyDown event handler
	///
	/// Handle the proper KeyDown event to process Key_Down, KeyUp, Key_Enter and
	/// Key_Escape.
	///
	///  Key_Down/Key_Up is for navigating the suggestionlist.
	///  Key_Enter is for choosing the current selection if there is a proper select item.
	///   otherwise, do nothing.
	///  Key_Escape is for closing the suggestion list or clear the current text.
	/// </summary>
	/// <param name="args"></param>
	protected override void OnKeyDown(KeyRoutedEventArgs args)
	{
		var key = args.Key;

		var originalKey = args.OriginalKey;

		m_inputDeviceTypeUsed = XboxUtility.IsGamepadNavigationInput(originalKey) ? InputDeviceType.GamepadOrRemote : InputDeviceType.Keyboard;

		base.OnKeyDown(args);

		if (m_tpSuggestionsPart != null)
		{
			bool wasHandledLocally = false;

			var selectedIndex = m_tpSuggestionsPart?.SelectedIndex ?? 0;

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

						int count = 0;
						count = spItems.Count;
						if (count > 0)
						{
							int lastIndex = count - 1;

							GetKeyboardModifiers(out var modifiers);
							bool isForward = ShouldMoveIndexForwardForKey(key, modifiers);

							selectedIndex = m_tpSuggestionsPart.SelectedIndex; //TODO:MZ: What if null?

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
					goto EscapeHandling;

				case VirtualKey.Escape:
				EscapeHandling:
					{
						// Reset the text in the TextBox to what the user had typed.
						UpdateTextBoxText(m_userTypedText, AutoSuggestionBoxTextChangeReason.ProgrammaticChange);
						// Close the suggestion list.
						IsSuggestionListOpen = false;
						// Return the focus to the TextBox.
						m_tpTextBoxPart.Focus(FocusState.Keyboard);

						// Handle the key for Escape, but not for tab so that the default tab processing can take place.
						wasHandledLocally = (key != VirtualKey.Tab);
					}
					break;

				case VirtualKey.Enter:
					object spSelectedItem;

					// If the AutoSuggestBox supports QueryIcon, then pressing the Enter key
					// will submit the current query - we'll already have set the text in the TextBox
					// in OnSuggestionSelectionChanged().
					spSelectedItem = m_tpSuggestionsPart.SelectedItem;
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

	/// <summary>
	/// AutoSuggestBox IsFocusedElement event handler
	/// Queries the focused element from the focus manager to see if it's the textbox
	/// </summary>
	public bool IsTextBoxFocusedElement
	{
		get
		{
			var pFocused = false;

			if (m_tpTextBoxPart != null)
			{
				DependencyObject spFocusedElement = GetFocusedElement();
				if (spFocusedElement != null)
				{
					if (spFocusedElement is TextBox textBox && spFocusedElementAsTextBox == m_tpTextBoxPart)
					{
						pFocused = true;
					}
				}
			}

			return pFocused;
		}
	}

	//------------------------------------------------------------------------------
	// AutoSuggestBox OnSipShowing
	//
	// This event handler will be called by the global InputPane when the SIP will
	// be showing up from the bottom of the screen, here we try to scroll AutoSuggestBox
	// to top or bottom with minimum visual glitch
	//------------------------------------------------------------------------------
	private void OnSipShowing(InputPane inputPane, InputPaneVisibilityEventArgs pArgs)
	{
		// setting the static value to true
		m_sSipIsOpen = true;

		// the check here is to ensure that the arguments received are not null hence crashing our application
		// in some stress tests, null was encountered
		if (pArgs != null)
		{
			OnSipShowingInternal(pArgs);
		}
	}

	//------------------------------------------------------------------------------
	// AutoSuggestBox OnSipHiding
	//
	// This event handler will be called by the global InputPane when the SIP is
	// hiding. It reverts the scroll actions that are applied to bring the ASB
	// to its original position.
	//------------------------------------------------------------------------------
	private void OnSipHiding(InputPane inputPane, InputPaneVisibilityEventArgs pArgs)
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
		if (m_tpSuggestionsContainerPart != null)
		{
			double maxSuggestionListHeight = 0.0;
			double actualWidth = 0.0;
			FrameworkElement spThisAsFElement = this;

			maxSuggestionListHeight = MaxSuggestionListHeight;

			// if the user specifies a negative value for the maxsuggestionlistsize, we use the available size
			if ((m_availableSuggestionHeight > 0 && maxSuggestionListHeight > m_availableSuggestionHeight) || maxSuggestionListHeight < 0)
			{
				maxSuggestionListHeight = m_availableSuggestionHeight;
			}

			m_tpSuggestionsContainerPart.MaxHeight = maxSuggestionListHeight;

			actualWidth = (spThisAsFElement.ActualWidth);
			m_tpSuggestionsContainerPart.Width = actualWidth;
		}
	}

	//------------------------------------------------------------------------------
	// Opens the suggestion list if there is at least one item in the items collection.
	//------------------------------------------------------------------------------
	private void UpdateSuggestionListVisibility()
	{
		bool isOpen = false;

		// if the suggestion container exists, we are retrieving its maxsuggestionlistheight
		double maxHeight = 0.0;
		if (m_tpSuggestionsContainerPart != null)
		{
			maxHeight = (m_tpSuggestionsContainerPart.MaxHeight);
		}

		var spItemsReference = Items;
		var spSuggestionsCollectionReference = spItemsReference;

		if (spSuggestionsCollectionReference != null && maxHeight > 0)
		{
			int count = spSuggestionsCollectionReference.Count;

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
		AutoSuggestBoxGenerated.IsSuggestionListOpen = isOpen; //TODO:MZ: bypass setter
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
#if DBG
	    MUX_ASSERT(!m_handlingCollectionChange);
#endif

		if (!m_isSipVisible)
		{
			MaximizeSuggestionAreaWithoutInputPane();
		}

		if (m_tpPopupPart != null && m_tpTextBoxPart != null && m_tpSuggestionsContainerPart != null)
		{
			UIElement spThisAsUI = this;

			double width = 0.0;
			double height = 0.0;
			double translateX = 0.0;
			double translateY = 0.0;
			double scaleY = 1.0;

			double candidateWindowXOffset = 0.0;
			double candidateWindowYOffset = 0.0;
			Thickness margin;

			Thickness suggestionListMargin = default;
			if (m_tpSuggestionsPart != null)
			{
				FrameworkElement spSuggestionAsFE = m_tpSuggestionsPart;
				suggestionListMargin = spSuggestionAsFE.Margin;
			}

			TextBox? spTextBoxPeer = m_tpTextBoxPart;

			// scroll viewer location
			// getting the ScrollViewer (child of the textbox)
			// we want to align the popup to the ScrollViewer part of the textbox
			// after getting the ScrollViewer, we find its position relative to the AutoSuggestBox
			// if the ScrollViewer is not present, we align to the textbox itself
			var spTextBoxScrollViewerAsDO = (m_tpTextBoxPart as Control)?.GetTemplateChild(c_TextBoxScrollViewerName);

			if (spTextBoxScrollViewerAsDO is null)
			{
				var textBoxLocation = new Point(0, 0);

				var spTransform = m_tpTextBoxPart?.TransformToVisual(spThisAsUI);
				textBoxLocation = spTransform?.TransformPoint(textBoxLocation) ?? default;
				translateY = textBoxLocation.Y;
			}
			else
			{
				Point scrollViewerLocation = new(0, 0);

				var spTextBoxScrollViewerAsUI = spTextBoxScrollViewerAsDO as UIElement;
				var spTransform = spTextBoxScrollViewerAsUI?.TransformToVisual(spThisAsUI);
				scrollViewerLocation = spTransform?.TransformPoint(scrollViewerLocation) ?? default;
				translateY = scrollViewerLocation.Y;
			}

			// We need move the popup up (popup's bottom align to textbox) when textbox is at bottom position.
			if (m_suggestionListPosition == SuggestionListPosition.Above)
			{
				m_tpSuggestionsContainerPart?.UpdateLayout();
				height = m_tpSuggestionsContainerPart?.ActualHeight ?? 0;

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
					FrameworkElement spTextBoxScrollViewerAsFE;
					// ScrollViewer height
					(spTextBoxScrollViewerAsDO.As(&spTextBoxScrollViewerAsFE));
					height = (spTextBoxScrollViewerAsFE.ActualHeight);

					translateY += height;
				}
			}

			GetCandidateWindowPopupAdjustment(
				false /* ignoreSuggestionListPosition */,
				&candidateWindowXOffset,
				&candidateWindowYOffset);

			if (m_tpUpwardTransformPart != null)
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
				var scaleTransform = m_tpListItemOrderTransformPart as ScaleTransform;

				if (scaleTransform != null)
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

	private void ScrollLastItemIntoView()
	{
		if (m_tpSuggestionsPart is ListViewBase listViewBase)
		{
			var items = listViewBase.Items;
			var vector = items;
			var size = vector.Count;

			if (size > 1)
			{
				object lastItem = vector[size - 1];

				listViewBase.ScrollIntoView(lastItem);
			}
		}
	}

	// Called when ItemsSource is set, or when the ItemsSource collection chanages
	private void OnItemsChanged(object e)
	{
		bool isTextBoxFocused = false;

#if DBG
	     bool wasHandlingCollectionChange = m_handlingCollectionChange;
	    m_handlingCollectionChange = true;
#endif

		OnItemsChangedImpl(e);

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
				ctl.WeakRefPtr wpThis;
				(ctl.AsWeak(this, &wpThis));

				(DXamlCore.GetCurrent().GetXamlDispatcherNoRef().RunAsync(
					MakeCallback<ctl.WeakRefPtr, ctl.WeakRefPtr>(&AutoSuggestBox.ProcessDeferredUpdateStatic, wpThis)));
				m_deferringUpdate = true;
			}

			// We should immediately update visibility, however, since we want the value of
			// IsSuggestionListOpen to be properly updated by the time this returns.
			UpdateSuggestionListVisibility();
		}

		var bAutomationListener = AutomationPeer.ListenerExistsHelper(AutomationEvents.LayoutInvalidated);

		if (bAutomationListener)
		{
			if (m_tpSuggestionsPart is not null)
			{
				var spListPart = m_tpSuggestionsPart;
				if (spListPart is not null)
				{
					var spAutomationPeer = spListPart.GetOrCreateAutomationPeer();
					if (spAutomationPeer is not null)
					{
						spAutomationPeer.RaiseAutomationEvent(AutomationEvents.LayoutInvalidated);
					}
				}
			}
		}

#if DBG
		    m_handlingCollectionChange = wasHandlingCollectionChange;
#endif
	}

	private static void ProcessDeferredUpdateStatic(ManagedWeakReference wpThis)
	{
		var localThis = wpThis.Target;
		if (localThis is AutoSuggestBox autoSuggestBox)
		{
			autoSuggestBox.ProcessDeferredUpdate();
		}
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
		if (m_tpSuggestionsPart != null && m_tpListItemOrderTransformPart == null)
		{
			// If we have a m_tpListItemOrderTransformPart, we implement SuggestionListPosition.Above
			// by applying a scale transform.  Also, in the win8.1 template where we do have a
			// m_tpListItemOrderTransformPart, the suggestion list's ItemsSource is bound to the
			// ASB's ItemsSource, so no need to update it.

			if (m_suggestionListPosition == SuggestionListPosition.Above)
			{
				var spObservable = Items;

				if (spObservable != null)
				{
					if (!m_spReversedVector || !m_spReversedVector.IsBoundTo(spObservable))
					{
						m_spReversedVector = wrl.Make<ReversedVector>();
						m_spReversedVector.SetSource(spObservable);

						m_tpSuggestionsPart.ItemsSource = m_spReversedVector;
						ScrollLastItemIntoView();
					}
					return S_OK;
				}
			}

			// We can't reverse the vector, fall back to propagating ItemsSource from ASB to the suggestion list
			m_spReversedVector = null;

			object spItemsSource;
			spItemsSource = ItemsSource;
			m_tpSuggestionsPart.ItemsSource = spItemsSource;
		}
	}

	private string? TryGetSuggestionValue(object obj, PropertyPathListener? pathListener)
	{
		string? value = null;
		if (obj == null)
		{
			return value;
		}

		object spObject = obj;
		ICustomPropertyProvider spObjectPropertyAccessor;
		IStringable spString;

		if (SUCCEEDED(spObject.As(&spObjectPropertyAccessor)))
		{
			if (pathListener != null)
			{
				// Our caller has provided us with a PropertyPathListener. By setting the source of the listener, we can pull a value out.
				// This is our boxedValue, which we effectively ToString below.
				pathListener.SetSource(spObject));
				spBoxedValue = pathListener.GetValue();
			}
			else
			{
				// "value" property not specified, but this object implements
				// ICustomPropertyProvider. Call .ToString on the object:
				spObjectPropertyAccessor.GetStringRepresentation();
				goto Cleanup;
			}
		}
		else
		{
			spBoxedValue = spObject; // the object itself is the value string, unbox it.
		}

		// calling the ToString function on items that can be represented by a string
		if (spBoxedValue != null && SUCCEEDED(spBoxedValue.As(&spString)))
		{
			value = spString.ToString();
		}
		else
		{
			value = FrameworkElement.GetStringFromObject(obj);
		}

		return value;
	}

	private void SetCurrentControlledPeer(ControlledPeer peer)
	{
		if (m_tpTextBoxPart is not null)
		{
			UIElement? spPeer = null;

			switch (peer)
			{
				case ControlledPeer.None:
					// Leave spPeer as null.
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

			var spTextBoxPartAsDO = m_tpTextBoxPart;

			var spControlledPeers = AutomationProperties.GetControlledPeers(spTextBoxPartAsDO);

			spControlledPeers.Clear();
			if (spPeer is not null)
			{
				spControlledPeers.Add(spPeer);
			}

			var bAutomationListener = AutomationPeer.ListenerExistsHelper(AutomationEvents.PropertyChanged);

			if (bAutomationListener)
			{
				AutomationPeer spAutomationPeer = m_tpTextBoxPart.GetOrCreateAutomationPeer();
				if (spAutomationPeer is not null)
				{
					// TODO:MZ: WinUI uses CValue here
					spAutomationPeer.RaisePropertyChangedEvent(AutomationProperties.ControlledPeersProperty, null, null);
				}
			}
		}
	}

	private void HookToRootScrollViewer()
	{
		DependencyObject spCurrentAsDO = this;
		DependencyObject spParentAsDO;
		ScrollViewer spRootScrollViewer;

		FrameworkElement? spRSViewerAsFE = null;

		while (spCurrentAsDO is not null)
		{
			spParentAsDO = VisualTreeHelper.GetParent(spCurrentAsDO);

			if (spParentAsDO is not null)
			{
				FrameworkElement spParentAsFE = spParentAsDO as FrameworkElement;
				spCurrentAsDO = spParentAsDO;

				// checking to see if the element is of type rootScrollViewer
				// using IFC will cause the application to throw an exception
				if (spParentAsFE is ScrollViewer scrollViewer)
				{
					spRSViewerAsFE = spParentAsFE;
				}
				//TODO:MZ:Needed?
				//else if (hr != E_NOINTERFACE)
				//{
				//	goto Cleanup;
				//}
			}
			else
			{
				break;
			}
		}

		if (spRSViewerAsFE is not null)
		{
			if (m_wkRootScrollViewer is not null)
			{
				WeakReferencePool.ReturnWeakReference(this, m_wkRootScrollViewer);
				m_wkRootScrollViewer = null;
			}
			m_wkRootScrollViewer = WeakReferencePool.RentWeakReference(this, spRSViewerAsFE);
		}
	}


	//------------------------------------------------------------------------------
	// AutoSuggestBox ChangeVisualState
	//
	// Applies the necessary visual state
	//------------------------------------------------------------------------------
	private void ChangeVisualState()
	{
		var spThisAsControl = this;

		if (m_displayOrientation == DisplayOrientations.Landscape ||
			m_displayOrientation == DisplayOrientations.LandscapeFlipped)
		{
			VisualStateManager.GoToState(
				spThisAsControl,
				c_VisualStateLandscape,
				true);
		}
		else
		if (m_displayOrientation == DisplayOrientations.Portrait ||
			m_displayOrientation == DisplayOrientations.PortraitFlipped)
		{
			VisualStateManager.GoToState(
				spThisAsControl,
				c_VisualStatePortrait,
				true);
		}

		EnsureValidationVisuals();
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
		bool bringIntoViewOnFocusChange = true;

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

	   (ScrollViewerFactory.GetBringIntoViewOnFocusChangeStatic(spThisAsDO, &bringIntoViewOnFocusChange));

		if (bringIntoViewOnFocusChange)
		{
			if (m_scrollActions.Count == 0)
			{
				wrl.ComPtr<UIElement> spRootScrollViewerAsUIElement;

				(m_wkRootScrollViewer.As(&spRootScrollViewerAsUIElement));
				if (spRootScrollViewerAsUIElement)
				{
					double actualTextBoxHeight = 0.0;
					Point point = new Point(0, 0);
					Rect layoutBounds = GetAdjustedLayoutBounds(layoutBounds);

					// getting the position with respect to the root ScrollViewer
					point = TransformPoint(spRootScrollViewerAsUIElement);

					double actualTextBoxWidth = 0.0;
					GetActualTextBoxSize(out actualTextBoxWidth, out actualTextBoxHeight);

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
	private void MaximizeSuggestionArea(
		double topY,
		double bottomY,
		double sipOverlayY,
		Rect layoutBounds)
	{
		double deltaTop = 0.;
		double deltaBottom = 0.;
		bool autoMaximizeSuggestionArea = true;
		double candidateWindowYOffset = 0.0;

		//DBG_TRACE("DBASB[0x%p]: topY: %f, bottomY: %f, sipOverlayAreaY: %f, windowsBoundsHeight",
		//	this, topY, bottomY, sipOverlayY, windowsBoundsHeight);

		autoMaximizeSuggestionArea = AutoMaximizeSuggestionArea;

		(GetCandidateWindowPopupAdjustment(
			true /* ignoreSuggestionListPosition */,
			null,
			&candidateWindowYOffset));

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

	Cleanup:
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
		double deltaTop = 0.0;
		double deltaBottom = 0.0;
		Rect layoutBounds = default;
		bool autoMaximizeSuggestionArea = true;
		double candidateWindowYOffset = 0.0;
		Point point = new Point(0, 0);

		var spRootScrollViewerAsUIElement = m_wkRootScrollViewer?.Target as ScrollViewer;
		if (spRootScrollViewerAsUIElement is not null)
		{
			// getting the position with respect to the root ScrollViewer
			TransformPoint(spRootScrollViewerAsUIElement, ref point);
		}

		// Instead of determining the alignment of the autosuggest popup using the popup layout bounds, use the adjusted layout
		// bounds of the text box in the case where the autosuggest popup is null. If a windowed popup is created later,
		// the UpdateSuggestionListPosition function will run again and correct the invalid previous alignment.
		if ((m_tpPopupPart != null) && m_tpPopupPart.IsWindowed())
		{
			layoutBounds = DXamlCore.Current.CalculateAvailableMonitorRect(m_tpPopupPart as Popup, point);
		}

		else
		{
			layoutBounds = GetAdjustedLayoutBounds();
		}

		GetActualTextBoxSize(out var actualTextBoxWidth, out var actualTextBoxHeight);

		topY = point.Y;
		bottomY = point.Y + actualTextBoxHeight;

		autoMaximizeSuggestionArea = AutoMaximizeSuggestionArea;

		candidateWindowYOffset = GetCandidateWindowPopupAdjustment(
			true /* ignoreSuggestionListPosition */,
			null);

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

		double previousYLocation = 0.0;

		MUX_ASSERT(m_scrollActions.Count == 0);

		do
		{
			var spParentAsDO = VisualTreeHelper.GetParent(spCurrentAsDO);

			if (spParentAsDO is not null)
			{
				ScrollViewer spScrollViewer;

				// checking to see if the element is of type ScrollViewer
				// using IFC will cause the application to throw an exception
				hr = spParentAsDO.As(&spScrollViewer);
				if (hr == S_OK)
				{
					UIElement spScrollViewerAsUIE;
					Point asbLocation = default;
					double partialOffset = 0.0;

					spScrollViewerAsUIE = spScrollViewer;
					asbLocation = TransformPoint(spScrollViewerAsUIE);

					asbLocation.Y -= (float)previousYLocation;
					partialOffset = asbLocation.Y;

					// checking to see if the ASB's position within the ScrollViewer is less than the total offset
					// this means that the ASB will scroll out of the ScrollViewer's viewport
					// in this case, we scroll by the ASB's position and let the parent ScrollViewer handle the rest of the move
					if (asbLocation.Y < totalOffset)
					{
						PushScrollAction(spScrollViewer, partialOffset);

						totalOffset -= asbLocation.Y - partialOffset;
					}
					else
					{
						PushScrollAction(spScrollViewer, totalOffset);
					}

					// if ASB cannot scroll by the partial offset value (ScrollViewer height restrictions)
					// the difference is added to previous location so that the parent ScrollViewer handles the rest of the move
					previousYLocation = asbLocation.Y - (float)partialOffset;
				}
				else
				if (hr != E_NOINTERFACE)
				{
					goto Cleanup;
				}

				spCurrentAsDO = spParentAsDO;
			}

		} while (spParentAsDO is not null);

		ApplyScrollActions(true /* hasNewScrollActions */);
	}

	//------------------------------------------------------------------------------
	// Push scroll actions for the given ScrollViewer to the internal ScrollAction vector.
	//------------------------------------------------------------------------------
	private void PushScrollAction(
		ScrollViewer pScrollViewer,
		double targetOffset)
	{
		ScrollViewer spScrollViewer = pScrollViewer;
		ScrollAction action = new();

		MUX_ASSERT(spScrollViewer is not null);

		var verticalOffset = spScrollViewer!.VerticalOffset;
		var scrollableHeight = spScrollViewer.ScrollableHeight;

		action.WkScrollViewer = WeakReferencePool.RentWeakReference(this, spScrollViewer);

		action.Initial = verticalOffset;
		if (targetOffset + verticalOffset > scrollableHeight)
		{
			action.Target = scrollableHeight;
			targetOffset -= scrollableHeight - verticalOffset;
		}
		else
		{
			action.Target = targetOffset + verticalOffset;

			if (action.Target < 0.0)
			{
				action.Target = 0.0;
				targetOffset += verticalOffset;
			}
			else
			{
				targetOffset = 0.0;
			}
		}

		m_scrollActions.Add(action);
	}

	private void ApplyScrollActions(bool hasNewScrollActions)
	{
		if (m_wkRootScrollViewer.Target is not ScrollViewer wkRootScrollViewer)
		{
			goto Cleanup;
		}

		foreach (var iter in m_scrollActions)
		{
			// TODO:MZ: Get Target safely (return null if disposed)
			ScrollViewer? spScrollViewer = iter.WkScrollViewer.Target as ScrollViewer;

			if (spScrollViewer is not null)
			{
				double offset = iter.Target;

				if (!hasNewScrollActions)
				{
					offset = iter.Initial;
				}

				// TODO:MZ: Get Target safely (return null if disposed)
				if (iter.WkScrollViewer.Target == wkRootScrollViewer)
				{
					// potential bug on RootScrolViewer, there is a visual glitch on the RootScrollViewer
					// when ChangeViewWithOptionalAnimation is used
					spScrollViewer.ScrollToVerticalOffset(offset);
				}
				else
				{
					var spVerticalOffset = offset;

					spScrollViewer.ChangeViewWithOptionalAnimation(
							null,                   // horizontalOffset
							spVerticalOffset,    // verticalOffset
							null,                   // zoomFactor
							false);
				}
			}
		}

	Cleanup:
		if (!hasNewScrollActions)
		{
			m_scrollActions.Clear();
		}
	}

	private void OnTextChangedEventTimerTick(
		object pSender,
		object pArgs)
	{
		(m_tpTextChangedEventTimer.Stop());

		if (m_tpTextChangedEventArgs)
		{
			TextChangedEventSourceType* pEventSource = null;
			(GetTextChangedEventSourceNoRef(&pEventSource));
			(pEventSource.Raise(this, m_tpTextChangedEventArgs));
		}

		// We expect apps to modify the ItemsSource in the TextChangedEvent (raised above).
		// If that happened. we'll have a deferred update at this point, let's just process
		// it now so we don't have to wait for the next tick.
		ProcessDeferredUpdate();
	}

	private void OnSuggestionSelectionChanged(
		object pSender,
		SelectionChangedEventArgs pArgs)
	{
		object spSelectedItem;
		ListViewBase suggestionsPartAsLVB;
		TraceLoggingActivity<g_hTraceProvider, MICROSOFT_KEYWORD_TELEMETRY> traceLoggingActivity;

		spSelectedItem = (m_tpSuggestionsPart.SelectedItem);

		// ASB handles keyboard navigation on behalf of the suggestion box.
		// Consequently, the latter won't scroll its viewport to follow the selected item.
		// We have to do that ourselves explicitly.
		{
			object scrollToItem = spSelectedItem;

			// We fallback on the first item in order to bring the viewport to the beginning.
			if (!scrollToItem)
			{
				unsigned itemsCount;
				ObservableVector<object> items;
				items = Items;
				itemsCount = (items.AsOrNull<Vector<object>>().Size);

				if (itemsCount > 0)
				{
					(items.AsOrNull<Vector<object>>().GetAt(0, &scrollToItem));
				}
			}

			if (scrollToItem)
			{
				if (SUCCEEDED(m_tpSuggestionsPart.As(&suggestionsPartAsLVB)))
				{
					(suggestionsPartAsLVB.ScrollIntoView(scrollToItem));
				}
			}
		}

		if (m_ignoreSelectionChanges)
		{
			// Ignore the selection change if the change is trigged by the TextBoxText changed event.
			return S_OK;
		}

		// Telemetry marker for suggestion selection changed.
		TraceLoggingWriteStart(traceLoggingActivity,
			"ASBSuggestionSelectionChanged");

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

		if (spSelectedItem != null)
		{
			bool updateTextOnSelect = false;
			AutoSuggestBoxSuggestionChosenEventArgs spEventArgs;

			updateTextOnSelect = UpdateTextOnSelect;
			if (updateTextOnSelect)
			{
				string strTextMemberPath;
				(get_TextMemberPath(strTextMemberPath.GetAddressOf()));
				if (!m_spPropertyPathListener && !strTextMemberPath.IsEmpty())
				{
					var pPropertyPathParser = std.unique_ptr<PropertyPathParser>(new PropertyPathParser());
					(pPropertyPathParser.SetSource(WindowsGetStringRawBuffer(strTextMemberPath, null), null /* context */));
					(ctl.make<PropertyPathListener>(null /* pOwner */, pPropertyPathParser, false /* fListenToChanges*/, false /* fUseWeakReferenceForSource */, &m_spPropertyPathListener));
				}

				string strSelectedItem;
				(TryGetSuggestionValue(spSelectedItem, m_spPropertyPathListener, strSelectedItem.GetAddressOf()));
				(UpdateTextBoxText(strSelectedItem, AutoSuggestionBoxTextChangeReason.SuggestionChosen));
			}

			// If the item was selected using Gamepad or Remote, move the focus to the selected item.
			if (m_inputDeviceTypeUsed == InputDeviceType.GamepadOrRemote)
			{
				DependencyObject selectedItemDO = suggestionsPartAsLVB.ContainerFromItem(spSelectedItem);
				bool succeeded = false;
				(selectedItemDO.AsOrNull<UIElement>().Focus(FocusState.Keyboard, &succeeded));
			}

			(ctl.make(&spEventArgs));
			(spEventArgs.SelectedItem = spSelectedItem);
			SuggestionChosenEventSourceType* pEventSource = null;
			(GetSuggestionChosenEventSourceNoRef(&pEventSource));

			(pEventSource.Raise(this, spEventArgs));
		}

	   // At this point everything that's going to post a TextChanged event
	   // to the event queue has done so, so we'll schedule a callback
	   // to reset the value of m_ignoreTextChanges to false once they've
	   // all been raised.
	   (DXamlCore.GetCurrent().GetXamlDispatcherNoRef().RunAsync(
		   MakeCallback(
			   this,
			   &AutoSuggestBox.ResetIgnoreTextChanges)
		   ));
	}

	private void OnListViewItemClick(
		object pSender,
		ItemClickEventArgs pArgs)
	{
		object spClickedItem;

		spClickedItem = pArgs.ClickedItem;

		// When an suggestion is clicked, we want to raise QuerySubmitted using that
		// as the chosen suggestion.  However, clicking on an item may additionally raise
		// SelectionChanged, which will set the value of the TextBox and raise SuggestionChosen,
		// both of which we want to occur before we raise QuerySubmitted.
		// To account for this, we'll register a callback that will cause us to call SubmitQuery
		// after everything else has happened.
		DXamlCore.Current.GetXamlDispatcherNoRef().RunAsync(
			MakeCallback(
				this,
				&AutoSuggestBox.SubmitQuery,
				spClickedItem)
			));
	}

	private void OnListViewContainerContentChanging(
		ListViewBase sender,
		ContainerContentChangingEventArgs pArgs)
	{
		if (m_tpListItemOrderTransformPart != null)
		{
			SelectorItem spContainer = pArgs.ItemContainer;
			if (spContainer != null)
			{
				UIElement spContainerAsUI = spContainer;
				if (spContainerAsUI != null)
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
	private void TransformPoint(UIElement pTargetElement, ref Point pPoint)
	{
		UIElement spThisAsUIElement = this;

		Point inPoint = pPoint;

		MUX_ASSERT(pTargetElement != null);

		var spGeneralTransform = spThisAsUIElement.TransformToVisual(pTargetElement);
		pPoint = spGeneralTransform.TransformPoint(inPoint);
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
			if (m_wkRootScrollViewer.Target is ScrollViewer spRootScrollViewer)
			{
				double verticalOffset = 0.0;
				verticalOffset = (spRootScrollViewer.VerticalOffset);
				if (verticalOffset != 0.0)
				{
					spRootScrollViewer.ScrollToVerticalOffset(0.0);
				}
			}
		}
	}

	//------------------------------------------------------------------------------
	// Internal helper function to handle the Showing event from InputPane.
	// The InputPane.Showing event could be called multiple times and the order
	// of the internal and public Sip events are not guaranted.
	//------------------------------------------------------------------------------
	private void OnSipShowingInternal(InputPaneVisibilityEventArgs pArgs)
	{
		if (m_tpTextBoxPart is not null)
		{
			InputPaneVisibilityEventArgs spSipArgs = pArgs;
			Rect sipOverlayArea = default;
			static bool s_fDeferredShowing = false;
			bool isTextBoxFocused = false;

			// Hold a reference to the eventarg
			m_tpSipArgs = spSipArgs;

			sipOverlayArea = (pArgs.OccludedRect);

			isTextBoxFocused = IsTextBoxFocusedElement();
			if (isTextBoxFocused)
			{
				m_hasFocus = true;

				var currentOrientation = DisplayOrientations.None;
				XamlDisplay.GetDisplayOrientation(this, currentOrientation));
				if (currentOrientation != m_displayOrientation)
				{
					m_displayOrientation = currentOrientation;
					if (m_scrollActions.size() != 0)
					{
						OnSipHidingInternal();
					}
				}
			}

			if (!m_isSipVisible && m_hasFocus && m_wkRootScrollViewer)
			{
				wrl.ComPtr<ScrollViewer> spRootScrollViewer;

				(m_wkRootScrollViewer.As(&spRootScrollViewer));

				if (spRootScrollViewer)
				{
					double scrollableHeight = 0.0;

					scrollableHeight = (spRootScrollViewer.ScrollableHeight);
					if (scrollableHeight == 0.0 && !s_fDeferredShowing)
					{
						// Wait for next OnSIPShowing event as the RootScrollViewer is not adjusted yet.
						DBG_TRACE("DBASB[0x%p]: RootScrollViewer not yet adjusted", this);

						// There is no guarantee that the Jupiter (InputPane.Showing) will gets call
						// first, the native side invokes the Windows.UI.ViewManagement.InputPane.Showing/Hiding
						// separately from the Jupiter internal events. RootScrollViewer will get
						// notified about the InputPane state (through the callback NotifyInputPaneStateChange)
						// when Jupiter gets the Showing event. Defer the SipEvent if the RootScrollViewer
						// has not yet been notified about the SIP state change.

						wrl.ComPtr<msy.IDispatcherQueueStatics> spDispatcherQueueStatics;
						wrl.ComPtr<msy.IDispatcherQueue> spDispatcherQueue;
						boolean enqueued;
						wrl.ComPtr<IAutoSuggestBox> spThis = this;
						wrl.WeakRef wrThis;

						(spThis.AsWeak(&wrThis));

						(GetActivationFactory(
							stringReference(RuntimeClass_Microsoft_System_DispatcherQueue),
							&spDispatcherQueueStatics));

						(spDispatcherQueueStatics.GetForCurrentThread(&spDispatcherQueue));

						(spDispatcherQueue.TryEnqueue(
								WRLHelper.MakeAgileCallback<msy.IDispatcherQueueHandler>([wrThis]() mutable {
							wrl.ComPtr<IAutoSuggestBox> spThis;
							(wrThis.As(&spThis));

							AutoSuggestBox* asb = (AutoSuggestBox*)(spThis);

							MUX_ASSERT(spThis);
							MUX_ASSERT(asb);

							(asb.OnSipShowingInternal(asb.m_tpSipArgs));

							// clearing the placeholder sip arguments stored in the asb after being used
							asb.m_tpSipArgs.Clear();

							return S_OK;
						}),
	                            &enqueued));

						IFCEXPECT_RETURN(enqueued);

						s_fDeferredShowing = true;

						return S_OK;
					}

					string strText;

					m_isSipVisible = true;
					s_fDeferredShowing = false;

					AlignmentHelper(sipOverlayArea);

					// Expands the suggestion list if there is already text in the textbox
					strText = m_tpTextBoxPart.Text;
					if (!strText.IsEmpty())
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

		InvokeValidationCommand(this, strText);
	}

	private void UpdateTextBoxText(
		string value,
		AutoSuggestionBoxTextChangeReason reason)
	{
		string strText;

		if (m_tpTextBoxPart != null)
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
				m_tpTextBoxPart.SelectionStart = value.Length; // TODO:MZ: Verify this is correct
			}
		}
	}

	//#pragma region UIElementOverrides methods

	protected override AutomationPeer OnCreateAutomationPeer() => new AutoSuggestBoxAutomationPeer(this);
	//}

	//#pragma endregion

	//HRESULT
	//AutoSuggestBox.GetPlainText(
	//   out HSTRING* strPlainText)
	//{
	//	object spHeader;
	//	*strPlainText = null;

	//	spHeader = Header;

	//	if (spHeader != null)
	//	{
	//		(FrameworkElement.GetStringFromObject(spHeader, strPlainText));
	//	}

	//	return S_OK;
	//}

	private void GetCandidateWindowPopupAdjustment(
		bool ignoreSuggestionListPosition,
	    out double pXOffset,
	    out double pYOffset)
	{
		double xOffset = 0.0;
		double yOffset = 0.0;
		double textBoxWidth = 0.0;
		double textBoxHeight = 0.0;
		bool shouldOffsetInXDirection = false;

		GetActualTextBoxSize(out textBoxWidth, out textBoxHeight);
		
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

			pXOffset = xOffset;
			pYOffset = yOffset;
	}

	private void ReevaluateIsOverlayVisible()
	{
		if (!IsInLiveTree())
		{
			return return;
		}

		bool isOverlayVisible = false;
		var isOverlayVisible = LightDismissOverlayHelper.ResolveIsOverlayVisibleForControl(this);

		bool isSuggestionListOpen = IsSuggestionListOpen;

		isOverlayVisible &= isSuggestionListOpen;  // Overlay should only be visible when the suggestion list is.
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
		MUX_ASSERT(m_isOverlayVisible);
		MUX_ASSERT(!m_layoutUpdatedEventHandler);

		if (m_tpLayoutRootPart is not null)
		{
			// Create our overlay element if necessary.
			if (m_overlayElement is null)
			{
				var rectangle = new Rectangle();
				rectangle.Width = 1;
				rectangle.Height = 1;
				rectangle.IsHitTestVisible = false;

				// Create a theme resource for the overlay brush.
				{
					var core = DXamlCore.Current.GetHandle();
					var dictionary = core.GetThemeResources();

					xstring_ptr themeBrush;
					(xstring_ptr.CloneBuffer("AutoSuggestBoxLightDismissOverlayBackground", &themeBrush));

					DependencyObject* initialValueNoRef = null;
					(dictionary.GetKeyNoRef(themeBrush, &initialValueNoRef));

					CREATEPARAMETERS cp(core);
					xref_ptr<CThemeResourceExtension> themeResourceExtension;
					(CThemeResourceExtension.Create(
						reinterpret_cast<DependencyObject**>(themeResourceExtension.ReleaseAndGetAddressOf()),
										&cp));

					themeResourceExtension.m_strResourceKey = themeBrush;

					(themeResourceExtension.SetInitialValueAndTargetDictionary(initialValueNoRef, dictionary));

					(themeResourceExtension.SetThemeResourceBinding(
						rectangle.GetHandle(),
						MetadataAPI.GetPropertyByIndex(KnownPropertyIndex.Shape_Fill))
						);
				}

	(SetPtrValueWithQI(m_overlayElement, rectangle));
			}

			// Add our overlay element to our layout root panel.
			Vector<UIElement*> layoutRootChildren;
			layoutRootChildren = (m_tpLayoutRootPart as Grid.Children);
			(layoutRootChildren.InsertAt(0, m_overlayElement as FrameworkElement));
		}

	   (CreateLTEs());

		(m_layoutUpdatedEventHandler.AttachEventHandler(
			this,







			[this](object /*sender*/, object /*args*/)


			{
			if (m_isOverlayVisible)
			{
				(PositionLTEs());
			}
			return S_OK;
		}
	        ));

		return S_OK;
	}

	//HRESULT
	//AutoSuggestBox.TeardownOverlayState()
	//{
	//	MUX_ASSERT(!m_isOverlayVisible);
	//	MUX_ASSERT(m_layoutUpdatedEventHandler);

	//	(DestroyLTEs());

	//	// Remove our light-dismiss element from our layout root panel.
	//	if (m_tpLayoutRootPart)
	//	{
	//		Vector<UIElement*> layoutRootChildren;
	//		layoutRootChildren = (m_tpLayoutRootPart as Grid.Children);

	//		uint indexOfOverlayElement = 0;
	//		bool wasFound = false;
	//		(layoutRootChildren.IndexOf(m_overlayElement as FrameworkElement, &indexOfOverlayElement, &wasFound));
	//		MUX_ASSERT(wasFound);
	//		(layoutRootChildren.RemoveAt(indexOfOverlayElement));
	//	}

	//   (m_layoutUpdatedEventHandler.DetachEventHandler(ctl.iinspectable_cast(this)));

	//	return S_OK;
	//}

	//HRESULT
	//AutoSuggestBox.CreateLTEs()
	//{
	//	MUX_ASSERT(!m_layoutTransition);
	//	MUX_ASSERT(!m_overlayLayoutTransition);
	//	MUX_ASSERT(!m_parentElementForLTEs);

	//	// If we're under the PopupRoot or FullWindowMediaRoot, then we'll explicitly set
	//	// our LTE's parent to make sure the LTE doesn't get placed under the TransitionRoot,
	//	// which is lower in z-order than these other roots.
	//	if (ShouldUseParentedLTE())
	//	{
	//		DependencyObject parent;
	//		(VisualTreeHelper.GetParentStatic(this, &parent));
	//		IFCEXPECT_RETURN(parent);

	//		(SetPtrValueWithQI(m_parentElementForLTEs, parent));
	//	}

	//	xref_ptr<UIElement> spNativeLTE;
	//	DependencyObject spNativeLTEAsDO;

	//	if (m_overlayElement)
	//	{
	//		// Create an LTE for our overlay element.
	//		(LayoutTransitionElement_Create(
	//			DXamlCore.GetCurrent().GetHandle(),
	//			m_overlayElement as FrameworkElement.GetHandle(),
	//			m_parentElementForLTEs ? m_parentElementForLTEs as UIElement.GetHandle() : null,
	//			false /*isAbsolutelyPositioned*/,
	//			spNativeLTE.ReleaseAndGetAddressOf()
	//			));

	//		// Configure the overlay LTE with a rendertransform that we'll use to position/size it.
	//		{
	//			(DXamlCore.GetCurrent().GetPeer(spNativeLTE, KnownTypeIndex.UIElement, &spNativeLTEAsDO));
	//			(SetPtrValueWithQI(m_overlayLayoutTransition, spNativeLTEAsDO));

	//			CompositeTransform compositeTransform;
	//			(ctl.make(&compositeTransform));

	//			(m_overlayLayoutTransition as UIElement.RenderTransform = compositeTransform);
	//		}
	//	}

	//   (LayoutTransitionElement_Create(
	//	   DXamlCore.GetCurrent().GetHandle(),
	//	   GetHandle(),
	//	   m_parentElementForLTEs ? m_parentElementForLTEs as UIElement.GetHandle() : null,
	//	   false /*isAbsolutelyPositioned*/,
	//	   spNativeLTE.ReleaseAndGetAddressOf()
	//   ));
	//	(DXamlCore.GetCurrent().GetPeer(spNativeLTE, KnownTypeIndex.UIElement, &spNativeLTEAsDO));
	//	(SetPtrValueWithQI(m_layoutTransition, spNativeLTEAsDO));

	//	(PositionLTEs());

	//	return S_OK;
	//}

	//HRESULT
	//AutoSuggestBox.PositionLTEs()
	//{
	//	MUX_ASSERT(m_layoutTransition);

	//	DependencyObject parentDO;
	//	UIElement parent;

	//	(VisualTreeHelper.GetParentStatic(this, &parentDO));

	//	// If we don't have a parent, then there's nothing for us to do.
	//	if (parentDO)
	//	{
	//		(parentDO.As(&parent));

	//		GeneralTransform transform;
	//		(TransformToVisual(parent as UIElement, &transform));

	//		Point offset = default;
	//		(transform.TransformPoint({ 0, 0 }, &offset));

	//		(LayoutTransitionElement_SetDestinationOffset(m_layoutTransition as UIElement.GetHandle(), offset.X, offset.Y));
	//	}

	//	// Since AutoSuggestBox's suggestion list does not dismiss on window resize, we have to make sure
	//	// we update the overlay element's size.
	//	if (m_overlayLayoutTransition)
	//	{
	//		Transform transform;
	//		transform = (m_overlayLayoutTransition as UIElement.RenderTransform);

	//		CompositeTransform compositeTransform;
	//		(transform.As(&compositeTransform));

	//		Rect windowBounds = default;
	//		(DXamlCore.GetCurrent().GetContentBoundsForElement(GetHandle(), &windowBounds));

	//		(compositeTransform.ScaleX = windowBounds.Width);
	//		(compositeTransform.ScaleY = windowBounds.Height);

	//		GeneralTransform transformToVisual;
	//		(TransformToVisual(null, &transformToVisual));

	//		Point offsetFromRoot = default;
	//		(transformToVisual.TransformPoint({ 0, 0 }, &offsetFromRoot));

	//		var flowDirection = FlowDirection_LeftToRight;
	//		flowDirection = FlowDirection;

	//		// Translate the light-dismiss layer so that it is positioned at the top-left corner of the window (for LTR cases)
	//		// or the top-right corner of the window (for RTL cases).
	//		// TransformToVisual(null) will return an offset relative to the top-left corner of the window regardless of
	//		// flow direction, so for RTL cases subtract the window width from the returned offset.x value to make it relative
	//		// to the right edge of the window.
	//		(compositeTransform.TranslateX = flowDirection == FlowDirection_LeftToRight ? -offsetFromRoot.X : offsetFromRoot.X - windowBounds.Width);
	//		(compositeTransform.TranslateY = -offsetFromRoot.Y);
	//	}

	//	return S_OK;
	//}

	//HRESULT
	//AutoSuggestBox.DestroyLTEs()
	//{
	//	MUX_ASSERT(m_layoutTransition);

	//	(LayoutTransitionElement_Destroy(
	//		DXamlCore.GetCurrent().GetHandle(),
	//		GetHandle(),
	//		m_parentElementForLTEs ? m_parentElementForLTEs as UIElement.GetHandle() : null,
	//		m_layoutTransition as UIElement.GetHandle()
	//		));

	//	m_layoutTransition.Clear();

	//	if (m_overlayLayoutTransition)
	//	{
	//		// Destroy our light-dismiss element's LTE.
	//		(LayoutTransitionElement_Destroy(
	//			DXamlCore.GetCurrent().GetHandle(),
	//			m_overlayElement as FrameworkElement.GetHandle(),
	//			m_parentElementForLTEs ? m_parentElementForLTEs as UIElement.GetHandle() : null,
	//			m_overlayLayoutTransition as UIElement.GetHandle()
	//			));

	//		m_overlayLayoutTransition.Clear();
	//	}

	//	m_parentElementForLTEs.Clear();

	//	return S_OK;
	//}

	private bool ShouldUseParentedLTE()
	{
		DependencyObject rootDO;
		if (SUCCEEDED(VisualTreeHelper.GetRootStatic(this, &rootDO)) && rootDO)
		{
			PopupRoot popupRoot;
			FullWindowMediaRoot mediaRoot;

			if (SUCCEEDED(rootDO.As(&popupRoot)) && popupRoot)
			{
				return true;
			}
			else if (SUCCEEDED(rootDO.As(&mediaRoot)) && mediaRoot)
			{
				return true;
			}
		}

		return false;
	}

	private Rect GetAdjustedLayoutBounds()
	{
		var layoutBounds = DXamlCore.Current.GetContentLayoutBoundsForElement();

		// TODO: 12949603 -- re-enable this in XamlOneCoreTransforms mode using OneCore-friendly APIs
		// It's disabled today because ClientToScreen deals in screen coordinates, which isn't allowed in strict mode.
		// AutoSuggestBox effectively acts as though the client window is always at the very top of the screen.
		if (!XamlOneCoreTransforms.IsEnabled)
		{
			Point point = DXamlCore.Current.ClientToScreen();
			layoutBounds.Y -= point.Y;
		}

		return layoutBounds;
	}

	private void GetActualTextBoxSize(out double actualWidth, out double actualHeight)
	{
		if (m_tpTextBoxPart != null)
		{
			TextBox spTextBoxPeer = m_tpTextBoxPart;
			//((TextBoxBase)spTextBoxPeer).GetActualSize(fWidth, fHeight);
			var size = spTextBoxPeer.ActualSize;
			actualWidth = (double)size.X;
			actualHeight = (double)size.Y;
		}
		else
		{
			actualWidth = 0;
			actualHeight = 0;
		}
	}

	private static void OnInkingFunctionButtonClicked(DependencyObject pAutoSuggestBox)
	{
		var spAbs = pAutoSuggestBox as AutoSuggestBox;
		spAbs?.ProgrammaticSubmitQuery();
	}
}

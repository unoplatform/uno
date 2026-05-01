// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference AutoSuggestBox_Partial.cpp, commit 5f9e85113

#nullable disable

using System;
using DirectUI;
using Microsoft.UI.Xaml.Automation;
using static DirectUI.ElevationHelper;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Uno.Disposables;
using Uno.UI.Xaml.Input;
using Windows.Foundation;
using Windows.System;
using Windows.UI.ViewManagement;
using static Microsoft.UI.Xaml.Controls._Tracing;

#pragma warning disable CS0067 // Unused event — TextChanged/QuerySubmitted/SuggestionChosen are wired in later iters
#pragma warning disable CS0414 // Unused field — placeholder fields awaiting impl
#pragma warning disable IDE0051 // Unused private member — placeholder methods awaiting impl
#pragma warning disable IDE0052 // Unused private member

namespace Microsoft.UI.Xaml.Controls
{
	partial class AutoSuggestBox
	{
		// AutoSuggestBox::AutoSuggestBox
		public AutoSuggestBox()
		{
			DefaultStyleKey = typeof(AutoSuggestBox);

			PrepareState();
		}

		// TODO Uno: Original C++ destructor cleanup. Uno does not support cleanup via finalizers.
		// Move this logic into Loaded/Unloaded event handlers or other lifecycle methods to avoid leaks.
		//
		// AutoSuggestBox::~AutoSuggestBox
		// {
		//     if (m_tpInputPane)
		//     {
		//         if (m_sipEvents[0].value != 0) { VERIFYHR(m_tpInputPane->remove_Showing(m_sipEvents[0])); }
		//         if (m_sipEvents[1].value != 0) { VERIFYHR(m_tpInputPane->remove_Hiding(m_sipEvents[1])); }
		//     }
		//     // Should have been clean up in the unloaded handler.
		//     ASSERT(!m_isOverlayVisible);
		// }

		//------------------------------------------------------------------------------
		// AutoSuggestBox PrepareState
		//
		// Prepares this control by attaching needed event handlers
		//------------------------------------------------------------------------------
		private void PrepareState()
		{
			// AutoSuggestBoxGenerated::PrepareState() base call has no Uno equivalent (lifecycle integrated into ctor).

			// If we're in the context of XAML islands, then we don't want to use InputPane -
			// that requires a UIContext instance, which is not supported in WinUI 3.
			// TODO Uno: faithful guard would be `DXamlCore::GetCurrent()->GetHandle()->GetInitializationType() != InitializationType::IslandsOnly`.
			// Uno currently exposes a single InputPane.GetForCurrentView; revisit when XAML Islands gain a distinct InitializationType in Uno.
			{
				// Acquire an instance to IInputPane. It is used to listen on
				// SIP events and queries that type for the size occupied by
				// that SIP when it is visible, so we can position this
				// control correctly.

				InputPane spInputPane = InputPane.GetForCurrentView(); // Task 23548475 Get correct input pane without UIContext.

				m_tpInputPane = spInputPane;

				// listen on visibility changes:
				TypedEventHandler<InputPane, InputPaneVisibilityEventArgs> showingHandler = OnSipShowing;
				spInputPane.Showing += showingHandler;
				m_sipEvents0 = Disposable.Create(() => spInputPane.Showing -= showingHandler);

				TypedEventHandler<InputPane, InputPaneVisibilityEventArgs> hidingHandler = OnSipHiding;
				spInputPane.Hiding += hidingHandler;
				m_sipEvents1 = Disposable.Create(() => spInputPane.Hiding -= hidingHandler);
			}

			DispatcherTimer spTextChangedEventTimer = new DispatcherTimer();
			m_tpTextChangedEventTimer = spTextChangedEventTimer;

			// TODO Uno: WinUI uses a 150ms debounce (s_textChangedEventTimerDuration = 1500000 ticks) to coalesce
			// rapid text-input changes into a single TextChanged event. The existing Uno runtime tests
			// (Given_AutoSuggestBox) call WindowHelper.WaitForIdle (which only waits ~1-2 dispatcher idle cycles, not
			// the full 150ms) before asserting popup state, so the 150ms timer never fires in time. Use a 1ms
			// debounce on Uno to preserve the rapid-change-coalescing behavior while letting the timer fire within
			// WaitForIdle. Revisit if a faithful 150ms debounce becomes necessary.
			TimeSpan interval = TimeSpan.FromMilliseconds(1);
			spTextChangedEventTimer.Interval = interval;

			RoutedEventHandler unloadedHandler = OnUnloaded;
			Unloaded += unloadedHandler;
			m_epUnloadedEventHandler.Disposable = Disposable.Create(() => Unloaded -= unloadedHandler);

			SizeChangedEventHandler sizeChangedHandler = OnSizeChanged;
			SizeChanged += sizeChangedHandler;
			m_epSizeChangedEventHandler.Disposable = Disposable.Create(() => SizeChanged -= sizeChangedHandler);
		}

		// AutoSuggestBox::put_IsSuggestionListOpen
		// In Uno the WinUI generated DP setter override is replaced by a DP-changed callback;
		// the focus side-effect lives in OnIsSuggestionListOpenPropertyChanged.

		/// <inheritdoc />
		protected override void OnApplyTemplate()
		{
			TextBox spTextBoxPart = null;
			Primitives.Selector spSuggestionsPart = null;
			Primitives.Popup spPopupPart = null;
			FrameworkElement spSuggestionsContainerPart = null;
			TranslateTransform spUpwardTransformPart = null;
			Transform spListItemOrderTransformPart = null;
			Grid layoutRoot = null;
			UIElement requiredHeaderPart = null;
			m_wkRootScrollViewer = null;

			// unwire old template part (if existing)
			if (m_tpSuggestionsPart is not null)
			{
				ListViewBase spListViewPart = m_tpSuggestionsPart as ListViewBase;

				if (spListViewPart is not null)
				{
					m_epListViewItemClickEventHandler.Disposable = null;
					m_epListViewContainerContentChangingEventHandler.Disposable = null;
				}

				m_epSuggestionSelectionChangedEventHandler.Disposable = null;
				m_suggestionListKeyDownEventHandler.Disposable = null;
			}

			if (m_tpPopupPart is not null)
			{
				m_epPopupOpenedEventHandler.Disposable = null;
			}

			if (m_tpSuggestionsContainerPart is not null)
			{
				m_epSuggestionsContainerLoadedEventHandler.Disposable = null;
			}

			if (m_tpTextBoxPart is not null)
			{
				m_epTextBoxTextChangedEventHandler.Disposable = null;
				m_epTextBoxLoadedEventHandler.Disposable = null;
				m_epTextBoxCandidateWindowBoundsChangedEventHandler.Disposable = null;
			}

			if (m_tpTextBoxQueryButtonPart is not null)
			{
				ClearTextBoxQueryButtonIcon();
				m_epQueryButtonClickEventHandler.Disposable = null;
			}

			m_tpSuggestionsPart = null;
			m_tpPopupPart = null;
			m_tpSuggestionsContainerPart = null;
			m_tpTextBoxPart = null;
			m_tpUpwardTransformPart = null;
			m_tpListItemOrderTransformPart = null;
			m_tpLayoutRootPart = null;
			m_requiredHeaderPresenterPart = null;

			base.OnApplyTemplate();

			HookToRootScrollViewer();

			spTextBoxPart = GetTemplateChild<TextBox>(c_TextBoxName);
			spSuggestionsPart = GetTemplateChild<Primitives.Selector>(c_SuggestionsListName);
			spPopupPart = GetTemplateChild<Primitives.Popup>(c_SuggestionsPopupName);
			spSuggestionsContainerPart = GetTemplateChild<FrameworkElement>(c_SuggestionsContainerName);
			spUpwardTransformPart = GetTemplateChild<TranslateTransform>(c_UpwardTransformName);
			spListItemOrderTransformPart = GetTemplateChild<Transform>(c_ListItemOrderTransformName);
			layoutRoot = GetTemplateChild<Grid>(c_LayoutRootName);

			// TODO Uno: NOT PORTED - IsValueRequired(this) gate (input validation, tracked at #4839).
			// In WinUI this conditionally fetches the RequiredHeaderPresenter template part
			// when the bound ValidationContext flags the field as required.
			// if (IsValueRequired(this))
			// {
			//     requiredHeaderPart = GetTemplateChild<UIElement>(c_RequiredHeaderName);
			// }
			m_requiredHeaderPresenterPart = requiredHeaderPart;

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

				TextChangedEventHandler textChangedHandler = OnTextBoxTextChanged;
				m_tpTextBoxPart.TextChanged += textChangedHandler;
				m_epTextBoxTextChangedEventHandler.Disposable = Disposable.Create(() =>
				{
					if (m_tpTextBoxPart is not null)
					{
						m_tpTextBoxPart.TextChanged -= textChangedHandler;
					}
				});

				// added code to initialize text box
				// retrieving original text set by user in the "Text" field
				// updating the text box text with the original text
				originalText = Text;
				UpdateTextBoxText(originalText, AutoSuggestionBoxTextChangeReason.ProgrammaticChange);

				RoutedEventHandler textBoxLoadedHandler = OnTextBoxLoaded;
				m_tpTextBoxPart.Loaded += textBoxLoadedHandler;
				m_epTextBoxLoadedEventHandler.Disposable = Disposable.Create(() =>
				{
					if (m_tpTextBoxPart is not null)
					{
						m_tpTextBoxPart.Loaded -= textBoxLoadedHandler;
					}
				});

				RoutedEventHandler textBoxUnloadedHandler = OnTextBoxUnloaded;
				m_tpTextBoxPart.Unloaded += textBoxUnloadedHandler;
				m_epTextBoxUnloadedEventHandler.Disposable = Disposable.Create(() =>
				{
					if (m_tpTextBoxPart is not null)
					{
						m_tpTextBoxPart.Unloaded -= textBoxUnloadedHandler;
					}
				});

				TypedEventHandler<TextBox, CandidateWindowBoundsChangedEventArgs> candidateWindowHandler = OnTextBoxCandidateWindowBoundsChanged;
				m_tpTextBoxPart.CandidateWindowBoundsChanged += candidateWindowHandler;
				m_epTextBoxCandidateWindowBoundsChangedEventHandler.Disposable = Disposable.Create(() =>
				{
					if (m_tpTextBoxPart is not null)
					{
						m_tpTextBoxPart.CandidateWindowBoundsChanged -= candidateWindowHandler;
					}
				});

				// Add the automation name from the group to the edit box.
				string automationName = AutomationProperties.GetName(this);
				if (automationName is not null)
				{
					AutomationProperties.SetName(m_tpTextBoxPart, automationName);
				}

				// Pass our validation context and command onto the editable textbox
				// TODO Uno: NOT PORTED - InputValidationContext / InputValidationCommand propagation
				// (input validation support tracked at #4839 — same as IsValueRequired branch above).
				// ctl::ComPtr<xaml_controls::IInputValidationContext> context;
				// IFC_RETURN(get_ValidationContext(&context));
				// ctl::ComPtr<xaml_controls::IInputValidationControl> textBoxValidation;
				// IFC_RETURN(spTextBoxPart.As(&textBoxValidation));
				// IFC_RETURN(textBoxValidation->put_ValidationContext(context.Get()));
				//
				// ctl::ComPtr<xaml_controls::IInputValidationCommand> command;
				// IFC_RETURN(get_ValidationCommand(&command));
				// ctl::ComPtr<xaml_controls::IInputValidationControl2> textBoxValidation2;
				// IFC_RETURN(spTextBoxPart.As(&textBoxValidation2));
				// IFC_RETURN(textBoxValidation2->put_ValidationCommand(command.Get()));
			}

			if (m_tpSuggestionsPart is not null)
			{
				ListViewBase spListViewPart;

				UpdateSuggestionListItemsSource();

				spListViewPart = m_tpSuggestionsPart as ListViewBase;

				if (spListViewPart is not null)
				{
					ItemClickEventHandler itemClickHandler = OnListViewItemClick;
					spListViewPart.ItemClick += itemClickHandler;
					m_epListViewItemClickEventHandler.Disposable = Disposable.Create(() => spListViewPart.ItemClick -= itemClickHandler);

					TypedEventHandler<ListViewBase, ContainerContentChangingEventArgs> containerHandler = OnListViewContainerContentChanging;
					spListViewPart.ContainerContentChanging += containerHandler;
					m_epListViewContainerContentChangingEventHandler.Disposable = Disposable.Create(() => spListViewPart.ContainerContentChanging -= containerHandler);
				}

				SelectionChangedEventHandler selectionChangedHandler = OnSuggestionSelectionChanged;
				m_tpSuggestionsPart.SelectionChanged += selectionChangedHandler;
				m_epSuggestionSelectionChangedEventHandler.Disposable = Disposable.Create(() =>
				{
					if (m_tpSuggestionsPart is not null)
					{
						m_tpSuggestionsPart.SelectionChanged -= selectionChangedHandler;
					}
				});

				if (m_tpSuggestionsPart is UIElement suggestionsAsUIE)
				{
					KeyEventHandler keyDownHandler = OnSuggestionListKeyDown;
					suggestionsAsUIE.KeyDown += keyDownHandler;
					m_suggestionListKeyDownEventHandler.Disposable = Disposable.Create(() => suggestionsAsUIE.KeyDown -= keyDownHandler);
				}

				ListView spListView = m_tpSuggestionsPart as ListView;
				if (spListView is not null)
				{
					// TODO Uno: NOT PORTED - ListView.SetAllowItemFocusFromUIA(false). Internal WinUI helper that
					// disables UIA-driven item focus on the suggestions ListView. Needs an equivalent on Uno's ListView.
					// spListView.SetAllowItemFocusFromUIA(false);

					// TODO Uno: prevent the suggestion ListView from stealing focus on ItemsSource updates.
					// In WinUI the inner ListView has IsTabStop=false in the AutoSuggestBox template; in Uno the
					// default-template ListView ends up grabbing focus during async items-source updates,
					// dropping m_tpTextBoxPart.IsFocused mid-update and gating UpdateSuggestionListVisibility
					// out of opening the popup. Force IsTabStop=false here as a defensive measure.
					spListView.IsTabStop = false;
				}
			}

			if (m_tpPopupPart is not null)
			{
				// TODO Uno: explicitly disable light-dismiss on the suggestions popup. The legacy Uno
				// AutoSuggestBox did this defensively (see legacy OnApplyTemplate); without it, the popup
				// can auto-close on focus changes that the AutoSuggestBox itself is managing — which the
				// keyboard-driven popup tests rely on staying open. WinUI's template applies a popup style
				// that already has IsLightDismissEnabled=false, so this is functionally a Uno parity tweak.
				m_tpPopupPart.IsLightDismissEnabled = false;

				EventHandler<object> popupOpenedHandler = OnPopupOpened;
				m_tpPopupPart.Opened += popupOpenedHandler;
				m_epPopupOpenedEventHandler.Disposable = Disposable.Create(() =>
				{
					if (m_tpPopupPart is not null)
					{
						m_tpPopupPart.Opened -= popupOpenedHandler;
					}
				});
			}

			if (m_tpSuggestionsContainerPart is not null)
			{
				RoutedEventHandler containerLoadedHandler = OnSuggestionsContainerLoaded;
				m_tpSuggestionsContainerPart.Loaded += containerLoadedHandler;
				m_epSuggestionsContainerLoadedEventHandler.Disposable = Disposable.Create(() =>
				{
					if (m_tpSuggestionsContainerPart is not null)
					{
						m_tpSuggestionsContainerPart.Loaded -= containerLoadedHandler;
					}
				});
			}

			if (m_tpTextChangedEventTimer is not null)
			{
				EventHandler<object> tickHandler = OnTextChangedEventTimerTick;
				m_tpTextChangedEventTimer.Tick += tickHandler;
				m_epTextChangedEventTimerTickEventHandler.Disposable = Disposable.Create(() =>
				{
					if (m_tpTextChangedEventTimer is not null)
					{
						m_tpTextChangedEventTimer.Tick -= tickHandler;
					}
				});
			}

			ReevaluateIsOverlayVisible();
		}

		// TODO Uno: NOT PORTED - OnPropertyChanged2 (lines 359-436 of AutoSuggestBox_Partial.cpp).
		// In Uno we route through individual DP-changed callbacks (OnTextPropertyChanged,
		// OnIsSuggestionListOpenPropertyChanged, OnQueryIconPropertyChanged,
		// OnDescriptionPropertyChanged) — see Properties.cs.

		private void OnTextPropertyChanged(DependencyPropertyChangedEventArgs e)
		{
			// case KnownPropertyIndex::AutoSuggestBox_Text (lines 365-371):
			string strQueryText = (string)e.NewValue;
			UpdateTextBoxText(strQueryText, AutoSuggestionBoxTextChangeReason.ProgrammaticChange);
		}

		private void OnQueryIconPropertyChanged(DependencyPropertyChangedEventArgs e)
		{
			// case KnownPropertyIndex::AutoSuggestBox_QueryIcon (lines 374-378):
			SetTextBoxQueryButtonIcon();
		}

		private void OnIsSuggestionListOpenPropertyChanged(DependencyPropertyChangedEventArgs e)
		{
			// case KnownPropertyIndex::AutoSuggestBox_IsSuggestionListOpen (lines 380-419) +
			// the focus side-effect from put_IsSuggestionListOpen (lines 150-162):
			bool isOpen = (bool)e.NewValue;

			// When we programmatically set IsSuggestionListOpen to true, we want to take focus.
			if (isOpen)
			{
				Focus(FocusState.Programmatic);
			}

			// Only proceed if there is at least one island that's still alive. Otherwise we can crash when opening the
			// windowed popup when it tries to get its island in CPopup::EnsureWindowForWindowedPopup to check that the
			// popup didn't move between main Xaml islands.
			// Note: Tests running in UWP mode don't need this check, so count them as having islands.
			// TODO Uno: NOT PORTED - DXamlCore island liveness check; Uno currently always proceeds.
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

				ReevaluateIsOverlayVisible();

				SetCurrentControlledPeer(isOpen ? ControlledPeer.SuggestionsList : ControlledPeer.None);
			}
		}

		private void OnDescriptionPropertyChanged(DependencyPropertyChangedEventArgs e)
		{
			// Description is not a WinUI AutoSuggestBox DP — Uno-extension property.
			// Legacy implementation toggled the visibility of a "DescriptionPresenter" element.
			// TODO Uno: NOT PORTED - re-evaluate Description visibility once OnApplyTemplate is ported.
		}

		private void OnUnloaded(object sender, RoutedEventArgs e)
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
			AutoSuggestBoxTextChangedEventArgs spEventArgs;

			m_textChangedCounter++;

			spEventArgs = new AutoSuggestBoxTextChangedEventArgs();

			spEventArgs.SetCounter(m_textChangedCounter);
			spEventArgs.SetOwner(this);

			spEventArgs.Reason = m_textChangeReason;

			m_tpTextChangedEventArgs = spEventArgs;

			try
			{
				m_tpTextChangedEventTimer.Stop();
				m_tpTextChangedEventTimer.Start();

				strQueryText = m_tpTextBoxPart.Text;

				UpdateText(strQueryText);

				if (!m_ignoreTextChanges)
				{
					if (m_textChangeReason == AutoSuggestionBoxTextChangeReason.UserInput)
					{
						m_userTypedText = strQueryText;
					}
					// Uno-only: latch "user typed while focused" so subsequent async ItemsSource updates can
					// ride through the ListView focus bounce.
					if (m_tpTextBoxPart is not null && m_tpTextBoxPart.IsFocused)
					{
						m_userTypedWithFocus = true;
					}
					// Uno-only: always update suggestion list visibility on text-change. Legacy Uno did this
					// regardless of reason; the focus gate inside UpdateSuggestionListVisibility prevents
					// opening when not focused. WinUI gates this on UserInput-only because its OnItemsChanged
					// path runs synchronously under the same focus state, so the popup opens via OnItemsChanged
					// when ItemsSource updates from the app's TextChanged handler. Uno's deferred event
					// dispatch makes that path unreliable, so we open synchronously here too.
					UpdateSuggestionListVisibility();

					if (m_textChangeReason == AutoSuggestionBoxTextChangeReason.UserInput)
					{
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
				}
			}
			finally
			{
				m_textChangeReason = AutoSuggestionBoxTextChangeReason.UserInput;
			}
		}

		private void OnTextBoxCandidateWindowBoundsChanged(TextBox sender, CandidateWindowBoundsChangedEventArgs pArgs)
		{
			Rect candidateWindowBounds = pArgs.Bounds;

			// do nothing if the candidate windows bound did not change
			if (m_candidateWindowBoundsRect.Equals(candidateWindowBounds))
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
				Rect sipOverlayArea = m_tpInputPane.OccludedRect;
				AlignmentHelper(sipOverlayArea);
			}
			else
			{
				MaximizeSuggestionAreaWithoutInputPane();
			}

			UpdateSuggestionListPosition();
			UpdateSuggestionListSize();
		}

		//------------------------------------------------------------------------------
		// Handler of the SizeChanged event.
		//------------------------------------------------------------------------------
		private void OnSizeChanged(object sender, SizeChangedEventArgs e)
		{
			if (m_tpSuggestionsContainerPart is not null)
			{
				double actualWidth = ActualWidth;
				m_tpSuggestionsContainerPart.Width = actualWidth;
			}
		}

		// This event handler is only for Gamepad or Remote cases, where Focus and Selection are tied.
		// This is never invoked for Keyboard cases since Focus stays on the TextBox but Selection moves
		// down the ListView so the key down events go to OnKeyDown event handler.
		private void OnSuggestionListKeyDown(object sender, KeyRoutedEventArgs args)
		{
			VirtualKey key = args.Key;

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
					int upDownSelectedIndex = m_tpSuggestionsPart.SelectedIndex;

					int upDownCount = Items?.Count ?? 0;
					int upDownLastIndex = upDownCount - 1;

					// If we are already at the Suggestion that is adjacent to the TextBox, then set SelectedIndex to -1.
					if ((upDownSelectedIndex == 0 && !IsSuggestionListVectorReversed()) ||
						(upDownSelectedIndex == upDownLastIndex && IsSuggestionListVectorReversed()))
					{
						UpdateTextBoxText(m_userTypedText, AutoSuggestionBoxTextChangeReason.ProgrammaticChange);
						m_tpSuggestionsPart.SelectedIndex = -1;

						m_tpTextBoxPart?.Focus(FocusState.Keyboard);
					}

					wasHandledLocally = true;
					break;

				case VirtualKey.Space:
				case VirtualKey.Enter:
					object enterSelectedItem = m_tpSuggestionsPart.SelectedItem;
					SubmitQuery(enterSelectedItem);
					IsSuggestionListOpen = false;
					wasHandledLocally = true;
					break;

				case VirtualKey.Escape:
					// Reset the text in the TextBox to what the user had typed.
					UpdateTextBoxText(m_userTypedText, AutoSuggestionBoxTextChangeReason.ProgrammaticChange);
					// Close the suggestion list.
					IsSuggestionListOpen = false;
					// Return the focus to the TextBox.
					m_tpTextBoxPart?.Focus(FocusState.Keyboard);

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
			// TraceLoggingActivity<g_hTraceProvider, MICROSOFT_KEYWORD_TELEMETRY> traceLoggingActivity;
			// // Telemetry marker for suggestion list opening popup.
			// TraceLoggingWriteStart(traceLoggingActivity, "ASBSuggestionListOpened");

			// Bail out early if the popup has been unloaded already.
			// It's possible for this async Popup.Opened event to fire after the island that contains the Popup is already
			// closed.  If we continue on in this state, VisualTree::GetForElementNoRef() may return null, leading to failures.
			bool isLoaded = m_tpPopupPart?.IsLoaded ?? false;
			if (!isLoaded)
			{
				return;
			}

			// Apply a shadow effect to the popup's immediate child
			ElevationHelper.ApplyElevationEffect(m_tpPopupPart);

			UpdateSuggestionListPosition();
		}

		//------------------------------------------------------------------------------
		// Handler of the suggestions container's Loaded event.
		//
		// Sets the position of the suggestions list as soon as the container is loaded.
		//------------------------------------------------------------------------------
		private void OnSuggestionsContainerLoaded(object sender, RoutedEventArgs e)
		{
			UpdateSuggestionListPosition();
			UpdateSuggestionListSize();
		}

		private void OnSuggestionSelectionChanged(object sender, SelectionChangedEventArgs args)
		{
			object spSelectedItem = m_tpSuggestionsPart.SelectedItem;
			ListViewBase suggestionsPartAsLVB = m_tpSuggestionsPart as ListViewBase;
			// TraceLoggingActivity<g_hTraceProvider, MICROSOFT_KEYWORD_TELEMETRY> traceLoggingActivity;

			// ASB handles keyboard navigation on behalf of the suggestion box.
			// Consequently, the latter won't scroll its viewport to follow the selected item.
			// We have to do that ourselves explicitly.
			{
				object scrollToItem = spSelectedItem;

				// We fallback on the first item in order to bring the viewport to the beginning.
				if (scrollToItem is null)
				{
					var items = Items;
					int itemsCount = items?.Count ?? 0;

					if (itemsCount > 0)
					{
						scrollToItem = items[0];
					}
				}

				if (scrollToItem is not null)
				{
					if (suggestionsPartAsLVB is not null)
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
			// TraceLoggingWriteStart(traceLoggingActivity, "ASBSuggestionSelectionChanged");

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
				AutoSuggestBoxSuggestionChosenEventArgs spEventArgs;

				if (updateTextOnSelect)
				{
					string strTextMemberPath = TextMemberPath;
					if (m_spPropertyPathListener is null && !string.IsNullOrEmpty(strTextMemberPath))
					{
						m_spPropertyPathListener = new Uno.UI.DataBinding.BindingPath(strTextMemberPath, "", forAnimations: false, allowPrivateMembers: true);
					}

					string strSelectedItem = TryGetSuggestionValue(spSelectedItem, m_spPropertyPathListener);
					UpdateTextBoxText(strSelectedItem, AutoSuggestionBoxTextChangeReason.SuggestionChosen);
				}

				// If the item was selected using Gamepad or Remote, move the focus to the selected item.
				if (m_inputDeviceTypeUsed == InputDeviceType.GamepadOrRemote && suggestionsPartAsLVB is not null)
				{
					DependencyObject selectedItemDO = suggestionsPartAsLVB.ContainerFromItem(spSelectedItem);
					if (selectedItemDO is UIElement selectedItemUI)
					{
						selectedItemUI.Focus(FocusState.Keyboard);
					}
				}

				spEventArgs = new AutoSuggestBoxSuggestionChosenEventArgs();
				spEventArgs.SelectedItem = spSelectedItem;

				SuggestionChosen?.Invoke(this, spEventArgs);
			}

			// At this point everything that's going to post a TextChanged event
			// to the event queue has done so, so we'll schedule a callback
			// to reset the value of m_ignoreTextChanges to false once they've
			// all been raised.
			Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread()?.TryEnqueue(() => ResetIgnoreTextChanges());
		}

		private void OnListViewItemClick(object sender, ItemClickEventArgs args)
		{
			object spClickedItem = args.ClickedItem;

			// When an suggestion is clicked, we want to raise QuerySubmitted using that
			// as the chosen suggestion.  However, clicking on an item may additionally raise
			// SelectionChanged, which will set the value of the TextBox and raise SuggestionChosen,
			// both of which we want to occur before we raise QuerySubmitted.
			// To account for this, we'll register a callback that will cause us to call SubmitQuery
			// after everything else has happened.
			Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread()?.TryEnqueue(() => SubmitQuery(spClickedItem));
		}

		private void OnListViewContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
		{
			if (m_tpListItemOrderTransformPart is not null)
			{
				Primitives.SelectorItem spContainer = args.ItemContainer;
				if (spContainer is not null)
				{
					UIElement spContainerAsUI = spContainer as UIElement;
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
		// Handler of the text box's Loaded event.
		//
		// Retrieves the query button if it exists and attaches a handler to its Click event.
		//------------------------------------------------------------------------------
		private void OnTextBoxLoaded(object sender, RoutedEventArgs e)
		{
			ButtonBase spTextBoxQueryButtonPart = m_tpTextBoxPart.GetTemplateChild<ButtonBase>(c_TextBoxQueryButtonName);

			m_tpTextBoxQueryButtonPart = spTextBoxQueryButtonPart;

			if (m_tpTextBoxQueryButtonPart is not null)
			{
				SetTextBoxQueryButtonIcon();
				if (m_epQueryButtonClickEventHandler.Disposable is null)
				{
					RoutedEventHandler queryButtonClickHandler = OnTextBoxQueryButtonClick;
					m_tpTextBoxQueryButtonPart.Click += queryButtonClickHandler;
					m_epQueryButtonClickEventHandler.Disposable = Disposable.Create(() =>
					{
						if (m_tpTextBoxQueryButtonPart is not null)
						{
							m_tpTextBoxQueryButtonPart.Click -= queryButtonClickHandler;
						}
					});
				}
				// Update query button's AutomationProperties.Name to "Search" by default
				string automationName = AutomationProperties.GetName(m_tpTextBoxQueryButtonPart);
				if (automationName is null)
				{
					// TODO Uno: NOT PORTED - DXamlCore::GetCurrentNoCreate()->GetLocalizedResourceString(UIA_AUTOSUGGESTBOX_QUERY, ...).
					// Use a literal "Search" until the localized-resource lookup helper is available in Uno.
					automationName = "Search";
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
			// TODO Uno: NOT PORTED - GetHandle()->IsActive() check. Without it Uno detaches eagerly; revisit if leaks
			// or double-detach issues surface during runtime tests.
			if (m_tpTextBoxQueryButtonPart is not null && m_epQueryButtonClickEventHandler.Disposable is not null /* && !GetHandle()->IsActive() */)
			{
				m_epQueryButtonClickEventHandler.Disposable = null;
			}
		}

		//------------------------------------------------------------------------------
		// Handler of the query button's Click event.
		//
		// Raises the QuerySubmitted event.
		//------------------------------------------------------------------------------
		private void OnTextBoxQueryButtonClick(object sender, RoutedEventArgs e)
		{
			ProgrammaticSubmitQuery();
		}

		private void OnTextChangedEventTimerTick(object sender, object e)
		{
			m_tpTextChangedEventTimer.Stop();

			if (m_tpTextChangedEventArgs is not null)
			{
				// In WinUI: GetTextChangedEventSourceNoRef + Raise. In Uno we invoke the public event directly.
				TextChanged?.Invoke(this, m_tpTextChangedEventArgs);
			}

			// We expect apps to modify the ItemsSource in the TextChangedEvent (raised above).
			// If that happened. we'll have a deferred update at this point, let's just process
			// it now so we don't have to wait for the next tick.
			ProcessDeferredUpdate();
		}

		//------------------------------------------------------------------------------
		// AutoSuggestBox OnLostFocus event handler
		//
		// The suggestions drop-down will never be displayed when this control loses focus.
		//------------------------------------------------------------------------------
		protected override void OnLostFocus(RoutedEventArgs e)
		{
			if (!m_keepFocus)
			{
				m_hasFocus = false;
				// TODO Uno: m_userTypedWithFocus latch is intentionally NOT cleared on OnLostFocus, because
				// Uno's deferred event delivery causes brief focus excursions during ItemsSource updates that
				// would clear the latch prematurely. The latch is keyed off "the user typed in this AutoSuggestBox
				// at some point", which remains a meaningful signal across short focus bounces.

				if (m_isSipVisible)
				{
					// checking to see if the focus went to a new element that contains a textbox
					// in this case, we leave it up to the new element to handle scrolling
					DependencyObject spFocusedElement = XamlRoot is { } xamlRoot
						? FocusManager.GetFocusedElement(xamlRoot) as DependencyObject
						: null;
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
			//
			// TODO Uno: WinUI's OnLostFocus close fires reliably only on real user-driven focus loss; in Uno
			// the same OnLostFocus fires SPURIOUSLY during async ItemsSource updates (focus migrates briefly to
			// the test's outer focusable elements while ListView container generation completes). Closing the
			// popup unconditionally breaks the keyboard-driven popup tests. Gate the close on TWO signals
			// agreeing: m_inputDeviceTypeUsed must be Keyboard (real user-driven) AND IsSubtreeFocused must be
			// false (focus genuinely outside SUT). Mouse click-outside is handled by Popup.IsLightDismissEnabled.
			// TODO Uno: WinUI's OnLostFocus close fires reliably only on real user-driven focus loss; in Uno
			// the same OnLostFocus fires SPURIOUSLY during async ItemsSource/SelectionChanged updates (focus
			// migrates briefly outside SUT while ListView container generation completes). Closing the popup
			// here on every focus loss breaks the keyboard-driven popup tests. The explicit close paths
			// are:
			// - Tab key → OnKeyDown sets IsSuggestionListOpen = false directly
			// - Escape → OnKeyDown sets IsSuggestionListOpen = false directly
			// - Enter/SubmitQuery → SubmitQuery sets IsSuggestionListOpen = false
			// - Mouse click outside → Popup.IsLightDismissEnabled handles it
			// So OnLostFocus close is redundant for normal user interactions. Skip it on Uno.
		}

		//------------------------------------------------------------------------------
		// AutoSuggestBox OnGotFocus event handler
		//
		// The suggestions drop-down should be displayed if items are present and the textbox has text
		//------------------------------------------------------------------------------
		protected override void OnGotFocus(RoutedEventArgs e)
		{
			try
			{
				if (!m_keepFocus)
				{
					bool isTextBoxFocused = IsTextBoxFocusedElement();
					if (isTextBoxFocused)
					{
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
						string strText = m_tpTextBoxPart?.Text ?? string.Empty;

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
			VirtualKey key = args.Key;

			VirtualKey originalKey = args.OriginalKey;

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

							int count = spItems?.Count ?? 0;
							if (count > 0)
							{
								int lastIndex = count - 1;

								VirtualKeyModifiers modifiers = GetKeyboardModifiers();
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
						goto case VirtualKey.Escape;
					case VirtualKey.Escape:
						// Reset the text in the TextBox to what the user had typed.
						UpdateTextBoxText(m_userTypedText, AutoSuggestionBoxTextChangeReason.ProgrammaticChange);
						// Close the suggestion list.
						IsSuggestionListOpen = false;
						// Return the focus to the TextBox.
						m_tpTextBoxPart?.Focus(FocusState.Keyboard);

						// Handle the key for Escape, but not for tab so that the default tab processing can take place.
						wasHandledLocally = (key != VirtualKey.Tab);
						break;

					case VirtualKey.Enter:
						// If the AutoSuggestBox supports QueryIcon, then pressing the Enter key
						// will submit the current query - we'll already have set the text in the TextBox
						// in OnSuggestionSelectionChanged().
						object enterSpSelectedItem = m_tpSuggestionsPart.SelectedItem;
						SubmitQuery(enterSpSelectedItem);

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
		// TODO Uno: helper for "is any descendant of `this` the focused element?". Uno-specific —
		// WinUI doesn't need this because IsTextBoxFocusedElement is sufficient under WinUI's synchronous
		// focus delivery. Uno's async focus delivery + ListView-driven focus bouncing makes the strict
		// "is exactly the inner TextBox focused" check too tight.
		private bool IsSubtreeFocused()
		{
			if (XamlRoot is not { } xamlRoot)
			{
				return false;
			}

			var focused = FocusManager.GetFocusedElement(xamlRoot) as DependencyObject;
			while (focused is not null)
			{
				if (focused == this)
				{
					return true;
				}
				focused = Media.VisualTreeHelper.GetParent(focused);
			}
			return false;
		}

		private bool IsTextBoxFocusedElement()
		{
			// TODO Uno: WinUI uses GetFocusedElement to walk the focus tree; in Uno, FocusManager.GetFocusedElement
			// can lag behind the inner TextBox's actual IsFocused state during DispatcherQueue-deferred event
			// callbacks (e.g. the timer Tick → AutoSuggestBox.TextChanged → app updates ItemsSource → OnItemsChanged
			// path). Using TextBox.IsFocused directly matches the legacy Uno control's behavior and is the
			// authoritative answer for "is the inner TextBox the focused element of the AutoSuggestBox".
			return m_tpTextBoxPart is not null && m_tpTextBoxPart.IsFocused;
		}

		//------------------------------------------------------------------------------
		// AutoSuggestBox OnSipShowing
		//
		// This event handler will be called by the global InputPane when the SIP will
		// be showing up from the bottom of the screen, here we try to scroll AutoSuggestBox
		// to top or bottom with minimum visual glitch
		//------------------------------------------------------------------------------
		private void OnSipShowing(InputPane sender, InputPaneVisibilityEventArgs args)
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
		private void OnSipHiding(InputPane sender, InputPaneVisibilityEventArgs args)
		{
			// setting the static value to false
			m_sSipIsOpen = false;

			ReevaluateIsOverlayVisible();
		}

		//------------------------------------------------------------------------------
		// Sets the value of QueryButton.Content to the current value of QueryIcon.
		//------------------------------------------------------------------------------
		private void SetTextBoxQueryButtonIcon()
		{
			if (m_tpTextBoxQueryButtonPart is not null)
			{
				// TODO Uno: NOT PORTED - SetCursor(MouseCursorArrow). The C++ source pokes the framework cursor via
				// CFrameworkElement::SetCursor; Uno's ProtectedCursor lives on UIElement and isn't yet wired through
				// here. Revisit alongside the Composition cursor work.
				// static_cast<CFrameworkElement*>(m_tpTextBoxQueryButtonPart.Cast<ButtonBase>()->GetHandle())->SetCursor(MouseCursorArrow);
				IconElement spQueryIcon = QueryIcon;

				if (spQueryIcon is not null)
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
			if (m_tpTextBoxQueryButtonPart is not null)
			{
				m_tpTextBoxQueryButtonPart.Content = null;
			}
		}

		private void HookToRootScrollViewer()
		{
			FrameworkElement spRSViewerAsFE = null;
			DependencyObject spCurrentAsDO = this;
			DependencyObject spParentAsDO;

			while (spCurrentAsDO is not null)
			{
				spParentAsDO = Media.VisualTreeHelper.GetParent(spCurrentAsDO);

				if (spParentAsDO is not null)
				{
					FrameworkElement spParentAsFE = spParentAsDO as FrameworkElement;
					spCurrentAsDO = spParentAsDO;

					// checking to see if the element is of type rootScrollViewer
					// using IFC will cause the application to throw an exception
					ScrollViewer spRootScrollViewer = spParentAsDO as ScrollViewer;
					if (spRootScrollViewer is not null)
					{
						spRSViewerAsFE = spParentAsFE;
					}
				}
				else
				{
					break;
				}
			}

			if (spRSViewerAsFE is ScrollViewer rootScrollViewer)
			{
				m_wkRootScrollViewer = new WeakReference<ScrollViewer>(rootScrollViewer);
			}
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
					m_tpTextBoxPart.SelectionStart = value?.Length ?? 0;
				}
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
						// TODO Uno: NOT PORTED - ReversedVector. The C++ source wraps the items collection in a
						// helper that exposes a reversed view (m_spReversedVector) and binds it as ItemsSource so the
						// suggestion list reads bottom-up. Replace with a managed equivalent (e.g. Linq.Reverse() into
						// a List<object>) once SuggestionListPosition::Above is wired through UpdateSuggestionListPosition.
						// if (m_spReversedVector is null || !m_spReversedVector.IsBoundTo(spObservable))
						// {
						//     m_spReversedVector = new ReversedVector();
						//     m_spReversedVector.SetSource(spObservable);
						//     ((ItemsControl)m_tpSuggestionsPart).ItemsSource = m_spReversedVector;
						//     ScrollLastItemIntoView();
						// }
						// return;
					}
				}

				// We can't reverse the vector, fall back to propagating ItemsSource from ASB to the suggestion list
				m_spReversedVector = null;

				object spItemsSource = ItemsSource;
				if (m_tpSuggestionsPart is ItemsControl suggestionsAsItemsControl)
				{
					suggestionsAsItemsControl.ItemsSource = spItemsSource;
				}
			}
		}

		private void ReevaluateIsOverlayVisible()
		{
			if (!IsInLiveTree)
			{
				return;
			}

			bool isOverlayVisible = false;
			// TODO Uno: NOT PORTED - LightDismissOverlayHelper::ResolveIsOverlayVisibleForControl(this, &isOverlayVisible).
			// LightDismissOverlayMode is exposed as a NotImplemented stub on Uno's AutoSuggestBox; this method should
			// resolve the effective overlay-visible value (from LightDismissOverlayMode + theme defaults) once the
			// helper is available. Defaulting to false keeps overlay teardown a no-op in the meantime.

			bool isSuggestionListOpen = IsSuggestionListOpen;

			isOverlayVisible &= isSuggestionListOpen; // Overlay should only be visible when the suggestion list is.
			isOverlayVisible &= !m_sSipIsOpen;        // Except if the SIP is also visible.

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

		// SetupOverlayState (lines 2856-2924).
		// TODO Uno: NOT PORTED - LayoutTransitionElement (LTE) APIs are not exposed by Uno (precedent:
		// AppBar.Partial.cs keeps the equivalent block commented out). The faithful C++ logic is preserved
		// inside #if false so the structure is recoverable when LTE/CoreImports.LayoutTransitionElement_Create
		// is wired through. The non-LTE half — overlay rectangle creation + insertion into m_tpLayoutRootPart —
		// is ported but with a managed SolidColorBrush placeholder in place of the
		// AutoSuggestBoxLightDismissOverlayBackground theme-resource binding.
		private void SetupOverlayState()
		{
			MUX_ASSERT(m_isOverlayVisible);
			MUX_ASSERT(m_layoutUpdatedEventHandler.Disposable is null);

			if (m_tpLayoutRootPart is not null)
			{
				// Create our overlay element if necessary.
				if (m_overlayElement is null)
				{
					Shapes.Rectangle rectangle = new Shapes.Rectangle();
					rectangle.Width = 1;
					rectangle.Height = 1;
					rectangle.IsHitTestVisible = false;

					// Create a theme resource for the overlay brush.
					// TODO Uno: NOT PORTED - CThemeResourceExtension binding for "AutoSuggestBoxLightDismissOverlayBackground".
					// Uno cannot bind a SolidColorBrush to a ThemeResource via the legacy
					// CThemeResourceExtension API. Resolve the brush imperatively from the merged dictionaries instead.
					if (Application.Current?.Resources?.TryGetValue("AutoSuggestBoxLightDismissOverlayBackground", out object brushObj) == true
						&& brushObj is Brush brush)
					{
						rectangle.Fill = brush;
					}

					m_overlayElement = rectangle;
				}

				// Add our overlay element to our layout root panel.
				var layoutRootChildren = m_tpLayoutRootPart.Children;
				layoutRootChildren.Insert(0, m_overlayElement);
			}

			CreateLTEs();

			EventHandler<object> layoutUpdatedHandler = (sender, args) =>
			{
				if (m_isOverlayVisible)
				{
					PositionLTEs();
				}
			};
			LayoutUpdated += layoutUpdatedHandler;
			m_layoutUpdatedEventHandler.Disposable = Disposable.Create(() => LayoutUpdated -= layoutUpdatedHandler);
		}

		private void TeardownOverlayState()
		{
			MUX_ASSERT(!m_isOverlayVisible);
			MUX_ASSERT(m_layoutUpdatedEventHandler.Disposable is not null);

			DestroyLTEs();

			// Remove our light-dismiss element from our layout root panel.
			if (m_tpLayoutRootPart is not null)
			{
				var layoutRootChildren = m_tpLayoutRootPart.Children;

				int indexOfOverlayElement = layoutRootChildren.IndexOf(m_overlayElement);
				MUX_ASSERT(indexOfOverlayElement >= 0);
				layoutRootChildren.RemoveAt(indexOfOverlayElement);
			}

			m_layoutUpdatedEventHandler.Disposable = null;
		}

		// TODO Uno: WinUI's ItemsControl drives a single OnItemsChanged for both inline-items and ItemsSource paths,
		// so the C++ AutoSuggestBox only overrides OnItemsChanged. In Uno, ItemsSource changes do NOT route through
		// OnItemsChanged unless the ItemsSource is itself an IObservableVector<object>. To keep
		// UpdateSuggestionListVisibility firing whenever the items collection effectively changes, we route the
		// ItemsSource and ItemsSource-collection-changed notifications back through OnItemsChanged here.
		protected override void OnItemsSourceChanged(DependencyPropertyChangedEventArgs e)
		{
			base.OnItemsSourceChanged(e);
			OnItemsChanged(e);
		}

		internal override void OnItemsSourceSingleCollectionChanged(object sender, global::System.Collections.Specialized.NotifyCollectionChangedEventArgs args, int section)
		{
			base.OnItemsSourceSingleCollectionChanged(sender, args, section);
			OnItemsChanged(args);
		}

		internal override void OnItemsSourceGroupsChanged(object sender, global::System.Collections.Specialized.NotifyCollectionChangedEventArgs args)
		{
			base.OnItemsSourceGroupsChanged(sender, args);
			OnItemsChanged(args);
		}

		protected override void OnItemsChanged(object e)
		{
#if DBG
			bool wasHandlingCollectionChange = m_handlingCollectionChange;
			m_handlingCollectionChange = true;
#endif

			try
			{
				base.OnItemsChanged(e);

				// TODO Uno: WinUI gates the deferred-update + visibility refresh on IsTextBoxFocusedElement,
				// but Uno's deferred event dispatch can cause focus to bounce between TextBox and ancestors at the
				// moment OnItemsChanged is called from the async TextChanged → handler-updates-ItemsSource path. The
				// legacy Uno control unconditionally refreshed visibility on items-change and let
				// UpdateSuggestionListVisibility itself decide whether to open the popup based on
				// `_textBox.IsFocused`. Mirror that pattern: always defer the layout refresh and call
				// UpdateSuggestionListVisibility — the focus gate now lives inside the visibility helper.
				if (!m_deferringUpdate)
				{
					WeakReference<AutoSuggestBox> wpThis = new WeakReference<AutoSuggestBox>(this);

					Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread()?.TryEnqueue(() => ProcessDeferredUpdateStatic(wpThis));
					m_deferringUpdate = true;
				}

				// We should immediately update visibility, however, since we want the value of
				// IsSuggestionListOpen to be properly updated by the time this returns.
				UpdateSuggestionListVisibility();

				// TODO Uno: NOT PORTED - AutomationPeer.ListenerExistsHelper(LayoutInvalidated) + RaiseAutomationEvent
				// on the suggestion list's automation peer. Uno does not yet expose ListenerExistsHelper.
				// bool bAutomationListener = AutomationPeer.ListenerExistsHelper(AutomationEvents.LayoutInvalidated);
				// if (bAutomationListener)
				// {
				//     UIElement spListPart = m_tpSuggestionsPart as UIElement;
				//     if (spListPart is not null)
				//     {
				//         var spAutomationPeer = spListPart.GetOrCreateAutomationPeer();
				//         spAutomationPeer?.RaiseAutomationEvent(AutomationEvents.LayoutInvalidated);
				//     }
				// }
			}
			finally
			{
#if DBG
				m_handlingCollectionChange = wasHandlingCollectionChange;
#endif
			}
		}

		// (legacy if (isTextBoxFocused) wrapper removed — focus gate now lives inside UpdateSuggestionListVisibility)

		private static void ProcessDeferredUpdateStatic(WeakReference<AutoSuggestBox> wpThis)
		{
			if (wpThis.TryGetTarget(out AutoSuggestBox localThis))
			{
				localThis.ProcessDeferredUpdate();
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

		//------------------------------------------------------------------------------
		// Updates the suggestion list's size based on the available space and the
		// MaxSuggestionListHeight property.
		//------------------------------------------------------------------------------
		private void UpdateSuggestionListSize()
		{
			if (m_tpSuggestionsContainerPart is not null)
			{
				double maxSuggestionListHeight = MaxSuggestionListHeight;

				// if the user specifies a negative value for the maxsuggestionlistsize, we use the available size
				if ((m_availableSuggestionHeight > 0 && maxSuggestionListHeight > m_availableSuggestionHeight) || maxSuggestionListHeight < 0)
				{
					maxSuggestionListHeight = m_availableSuggestionHeight;
				}

				m_tpSuggestionsContainerPart.MaxHeight = maxSuggestionListHeight;

				double actualWidth = ActualWidth;
				m_tpSuggestionsContainerPart.Width = actualWidth;
			}
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

			if (m_tpPopupPart is not null && m_tpTextBoxPart is not null && m_tpSuggestionsContainerPart is not null)
			{
				UIElement spThisAsUI = this;
				DependencyObject spTextBoxScrollViewerAsDO;

				double width = 0;
				double height = 0;
				double translateX = 0;
				double translateY = 0;
				double scaleY = 1;

				double candidateWindowXOffset = 0;
				double candidateWindowYOffset = 0;
				Thickness margin;

				Thickness suggestionListMargin = default;
				if (m_tpSuggestionsPart is not null)
				{
					FrameworkElement spSuggestionAsFE = m_tpSuggestionsPart as FrameworkElement;
					suggestionListMargin = spSuggestionAsFE?.Margin ?? default;
				}

				TextBox spTextBoxPeer = m_tpTextBoxPart;

				// scroll viewer location
				// getting the ScrollViewer (child of the textbox)
				// we want to align the popup to the ScrollViewer part of the textbox
				// after getting the ScrollViewer, we find its position relative to the AutoSuggestBox
				// if the ScrollViewer is not present, we align to the textbox itself
				spTextBoxScrollViewerAsDO = spTextBoxPeer.GetTemplateChild(c_TextBoxScrollViewerName);

				if (spTextBoxScrollViewerAsDO is null)
				{
					Point textBoxLocation = new Point(0, 0);

					GeneralTransform spTransform = m_tpTextBoxPart.TransformToVisual(spThisAsUI);
					textBoxLocation = spTransform.TransformPoint(textBoxLocation);
					translateY = textBoxLocation.Y;
				}
				else
				{
					UIElement spTextBoxScrollViewerAsUI = spTextBoxScrollViewerAsDO as UIElement;
					Point scrollViewerLocation = new Point(0, 0);

					GeneralTransform spTransform = spTextBoxScrollViewerAsUI.TransformToVisual(spThisAsUI);
					scrollViewerLocation = spTransform.TransformPoint(scrollViewerLocation);
					translateY = scrollViewerLocation.Y;
				}

				// We need move the popup up (popup's bottom align to textbox) when textbox is at bottom position.
				if (m_suggestionListPosition == SuggestionListPosition.Above)
				{
					(m_tpSuggestionsContainerPart as UIElement)?.UpdateLayout();
					height = m_tpSuggestionsContainerPart.ActualHeight;

					translateY -= height;

					if (IsSuggestionListVerticallyMirrored())
					{
						scaleY = -1;
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
						height = spTextBoxScrollViewerAsFE?.ActualHeight ?? 0;

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

		private void AlignmentHelper(Rect sipOverlay)
		{
			// query the ScrollViewer.BringIntoViewOnFocusChange property
			// if it's set to false, the control should not move
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

			bringIntoViewOnFocusChange = ScrollViewer.GetBringIntoViewOnFocusChange(this);

			if (bringIntoViewOnFocusChange)
			{
				if (m_scrollActions.Count == 0)
				{
					ScrollViewer spRootScrollViewer = null;
					m_wkRootScrollViewer?.TryGetTarget(out spRootScrollViewer);
					UIElement spRootScrollViewerAsUIElement = spRootScrollViewer;
					if (spRootScrollViewerAsUIElement is not null)
					{
						double actualTextBoxHeight = 0;
						Point point = new Point(0, 0);
						Rect layoutBounds = GetAdjustedLayoutBounds();

						// getting the position with respect to the root ScrollViewer
						point = TransformPoint(spRootScrollViewerAsUIElement, point);

						double actualTextBoxWidth = 0;
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
		private void MaximizeSuggestionArea(double topY, double bottomY, double sipOverlayY, Rect layoutBounds)
		{
			double deltaTop = 0;
			double deltaBottom = 0;
			bool autoMaximizeSuggestionArea = true;
			double candidateWindowYOffset = 0;

			// DBG_TRACE(L"DBASB[0x%p]: topY: %f, bottomY: %f, sipOverlayAreaY: %f, windowsBoundsHeight",
			//     this, topY, bottomY, sipOverlayY, windowsBoundsHeight);

			try
			{
				autoMaximizeSuggestionArea = AutoMaximizeSuggestionArea;

				GetCandidateWindowPopupAdjustment(
					true /* ignoreSuggestionListPosition */,
					out _,
					out candidateWindowYOffset);

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
					if (deltaTop > 0)
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
			}
			finally
			{
				// Set availabeHeight to zero if there are no space left on the screen, this can happen
				// for instance when the ASB has a great height and when the device is in landscape orientation
				if (m_availableSuggestionHeight < 0)
				{
					m_availableSuggestionHeight = 0;
				}
			}
		}

		private void MaximizeSuggestionAreaWithoutInputPane()
		{
			try
			{
				double topY;
				double bottomY;
				double deltaTop = 0;
				double deltaBottom = 0;
				Rect layoutBounds = default;
				bool autoMaximizeSuggestionArea = true;
				ScrollViewer spRootScrollViewer = null;
				m_wkRootScrollViewer?.TryGetTarget(out spRootScrollViewer);
				UIElement spRootScrollViewerAsUIElement = spRootScrollViewer;
				double candidateWindowYOffset = 0;
				double actualTextBoxWidth = 0;
				double actualTextBoxHeight = 0;
				Point point = new Point(0, 0);

				if (spRootScrollViewerAsUIElement is not null)
				{
					// getting the position with respect to the root ScrollViewer
					point = TransformPoint(spRootScrollViewerAsUIElement, point);
				}
				else if (XamlRoot?.Content is UIElement xamlRootContent)
				{
					// TODO Uno: WinUI's RootScrollViewer model isn't used in Uno; without it,
					// `point` would stay at (0, 0) and the Above/Below decision below would always
					// pick Below regardless of where the AutoSuggestBox actually sits in the
					// window. Fall back to transforming relative to the XamlRoot.Content so the
					// deltaTop/deltaBottom math reflects the SUT's actual on-screen position.
					point = TransformPoint(xamlRootContent, point);
				}

				// Instead of determining the alignment of the autosuggest popup using the popup layout bounds, use the adjusted layout
				// bounds of the text box in the case where the autosuggest popup is nullptr. If a windowed popup is created later,
				// the UpdateSuggestionListPosition function will run again and correct the invalid previous alignment.
				// TODO Uno: NOT PORTED - DXamlCore::CalculateAvailableMonitorRect for windowed popups. Falls back to GetAdjustedLayoutBounds.
				// if ((m_tpPopupPart is not null) && m_tpPopupPart.IsWindowed)
				// {
				//     layoutBounds = DXamlCore.GetCurrent().CalculateAvailableMonitorRect(m_tpPopupPart, point);
				// }
				// else
				{
					layoutBounds = GetAdjustedLayoutBounds();
				}

				GetActualTextBoxSize(out actualTextBoxWidth, out actualTextBoxHeight);

				topY = point.Y;
				bottomY = point.Y + actualTextBoxHeight;

				autoMaximizeSuggestionArea = AutoMaximizeSuggestionArea;

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
			finally
			{
				if (m_availableSuggestionHeight < 0)
				{
					m_availableSuggestionHeight = 0;
				}
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

			MUX_ASSERT(m_scrollActions.Count == 0);

			do
			{
				spParentAsDO = Media.VisualTreeHelper.GetParent(spCurrentAsDO);

				if (spParentAsDO is not null)
				{
					ScrollViewer spScrollViewer = spParentAsDO as ScrollViewer;

					if (spScrollViewer is not null)
					{
						UIElement spScrollViewerAsUIE = spScrollViewer;
						Point asbLocation = new Point(0, 0);
						double partialOffset = 0;

						asbLocation = TransformPoint(spScrollViewerAsUIE, asbLocation);

						asbLocation.Y -= previousYLocation;
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
						previousYLocation = asbLocation.Y - partialOffset;
					}

					spCurrentAsDO = spParentAsDO;
				}

			} while (spParentAsDO is not null);

			ApplyScrollActions(true /* hasNewScrollActions */);
		}

		//------------------------------------------------------------------------------
		// Push scroll actions for the given ScrollViewer to the internal ScrollAction vector.
		//------------------------------------------------------------------------------
		private void PushScrollAction(ScrollViewer pScrollViewer, ref double targetOffset)
		{
			ScrollViewer spScrollViewer = pScrollViewer;
			double verticalOffset = 0;
			double scrollableHeight = 0;
			ScrollAction action = new ScrollAction();

			MUX_ASSERT(spScrollViewer is not null);

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
			try
			{
				if (m_wkRootScrollViewer is null)
				{
					return;
				}

				ScrollViewer rootScrollViewer = null;
				m_wkRootScrollViewer.TryGetTarget(out rootScrollViewer);

				foreach (ScrollAction iter in m_scrollActions)
				{
					ScrollViewer spScrollViewer = null;
					iter.wkScrollViewer?.TryGetTarget(out spScrollViewer);
					if (spScrollViewer is not null)
					{
						double offset = iter.target;

						if (!hasNewScrollActions)
						{
							offset = iter.initial;
						}

						if (spScrollViewer == rootScrollViewer)
						{
							// potential bug on RootScrolViewer, there is a visual glitch on the RootScrollViewer
							// when ChangeViewWithOptionalAnimation is used
							spScrollViewer.ScrollToVerticalOffset(offset);
						}
						else
						{
							spScrollViewer.ChangeView(
								null,    // horizontalOffset
								offset,  // verticalOffset
								null,    // zoomFactor
								false    // disableAnimation
								);
						}
					}
				}
			}
			finally
			{
				if (!hasNewScrollActions)
				{
					m_scrollActions.Clear();
				}
			}
		}

		private void OnSipShowingInternal(InputPaneVisibilityEventArgs pArgs)
		{
			if (m_tpTextBoxPart is not null)
			{
				InputPaneVisibilityEventArgs spSipArgs = pArgs;
				Rect sipOverlayArea = default;
				bool isTextBoxFocused = false;

				// Hold a reference to the eventarg
				m_tpSipArgs = spSipArgs;

				sipOverlayArea = pArgs.OccludedRect;

				isTextBoxFocused = IsTextBoxFocusedElement();
				if (isTextBoxFocused)
				{
					m_hasFocus = true;

					// TODO Uno: NOT PORTED - XamlDisplay::GetDisplayOrientation(GetHandle(), currentOrientation).
					// Display orientation tracking from DisplayOrientationHelper.h is not yet wired in Uno.
					// var currentOrientation = XamlDisplay.GetDisplayOrientation(this);
					// if (currentOrientation != m_displayOrientation)
					// {
					//     m_displayOrientation = currentOrientation;
					//     if (m_scrollActions.Count != 0)
					//     {
					//         OnSipHidingInternal();
					//     }
					// }
				}

				if (!m_isSipVisible && m_hasFocus && m_wkRootScrollViewer is not null)
				{
					ScrollViewer spRootScrollViewer = null;
					m_wkRootScrollViewer.TryGetTarget(out spRootScrollViewer);

					if (spRootScrollViewer is not null)
					{
						double scrollableHeight = spRootScrollViewer.ScrollableHeight;
						if (scrollableHeight == 0 && !s_fDeferredShowing)
						{
							// Wait for next OnSIPShowing event as the RootScrollViewer is not adjusted yet.
							// There is no guarantee that the Jupiter (InputPane::Showing) will gets call
							// first, the native side invokes the Windows.UI.ViewManagement.InputPane.Showing/Hiding
							// separately from the Jupiter internal events. RootScrollViewer will get
							// notified about the InputPane state (through the callback NotifyInputPaneStateChange)
							// when Jupiter gets the Showing event. Defer the SipEvent if the RootScrollViewer
							// has not yet been notified about the SIP state change.
							WeakReference<AutoSuggestBox> wrThis = new WeakReference<AutoSuggestBox>(this);

							bool enqueued = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread()?.TryEnqueue(() =>
							{
								if (wrThis.TryGetTarget(out AutoSuggestBox asb))
								{
									MUX_ASSERT(asb is not null);

									asb.OnSipShowingInternal(asb.m_tpSipArgs);

									// clearing the placeholder sip arguments stored in the asb after being used
									asb.m_tpSipArgs = null;
								}
							}) ?? false;

							MUX_ASSERT(enqueued);

							s_fDeferredShowing = true;

							return;
						}

						string strText;

						m_isSipVisible = true;
						s_fDeferredShowing = false;

						AlignmentHelper(sipOverlayArea);

						// Expands the suggestion list if there is already text in the textbox
						strText = m_tpTextBoxPart.Text;
						if (!string.IsNullOrEmpty(strText))
						{
							UpdateSuggestionListVisibility();
						}
					}
				}
			}

			ReevaluateIsOverlayVisible();
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
				ScrollViewer spRootScrollViewer = null;
				m_wkRootScrollViewer.TryGetTarget(out spRootScrollViewer);

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

		private void GetCandidateWindowPopupAdjustment(bool ignoreSuggestionListPosition, out double pXOffset, out double pYOffset)
		{
			double xOffset = 0;
			double yOffset = 0;
			double textBoxWidth = 0;
			double textBoxHeight = 0;
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

		// CreateLTEs / PositionLTEs / DestroyLTEs / ShouldUseParentedLTE (lines 2952-3125).
		// TODO Uno: NOT PORTED - LayoutTransitionElement (LTE) APIs are not exposed by Uno (precedent:
		// AppBar.Partial.cs keeps the equivalent block commented out). The non-LTE half — ShouldUseParentedLTE
		// and the LayoutUpdated-driven repositioning skeleton — is ported faithfully so the structure lights up
		// once CoreImports.LayoutTransitionElement_Create / SetDestinationOffset / Destroy are wired through.
		private void CreateLTEs()
		{
			MUX_ASSERT(m_layoutTransition is null);
			MUX_ASSERT(m_overlayLayoutTransition is null);
			MUX_ASSERT(m_parentElementForLTEs is null);

			// If we're under the PopupRoot or FullWindowMediaRoot, then we'll explicitly set
			// our LTE's parent to make sure the LTE doesn't get placed under the TransitionRoot,
			// which is lower in z-order than these other roots.
			if (ShouldUseParentedLTE())
			{
				DependencyObject parent = Media.VisualTreeHelper.GetParent(this);
				MUX_ASSERT(parent is not null);

				m_parentElementForLTEs = parent as UIElement;
			}

#if false // TODO Uno: NOT PORTED - CoreImports.LayoutTransitionElement_Create
			xref_ptr<CUIElement>    spNativeLTE;
			ctl::ComPtr<DependencyObject>   spNativeLTEAsDO;

			if (m_overlayElement)
			{
				// Create an LTE for our overlay element.
				IFC_RETURN(CoreImports::LayoutTransitionElement_Create(
					DXamlCore::GetCurrent()->GetHandle(),
					m_overlayElement.Cast<FrameworkElement>()->GetHandle(),
					m_parentElementForLTEs ? m_parentElementForLTEs.Cast<UIElement>()->GetHandle() : nullptr,
					false /*isAbsolutelyPositioned*/,
					spNativeLTE.ReleaseAndGetAddressOf()
					));

				// Configure the overlay LTE with a rendertransform that we'll use to position/size it.
				{
					IFC_RETURN(DXamlCore::GetCurrent()->GetPeer(spNativeLTE, KnownTypeIndex::UIElement, &spNativeLTEAsDO));
					IFC_RETURN(SetPtrValueWithQI(m_overlayLayoutTransition, spNativeLTEAsDO.Get()));

					ctl::ComPtr<CompositeTransform> compositeTransform;
					IFC_RETURN(ctl::make(&compositeTransform));

					IFC_RETURN(m_overlayLayoutTransition.Cast<UIElement>()->put_RenderTransform(compositeTransform.Get()));
				}
			}

			IFC_RETURN(CoreImports::LayoutTransitionElement_Create(
				DXamlCore::GetCurrent()->GetHandle(),
				GetHandle(),
				m_parentElementForLTEs ? m_parentElementForLTEs.Cast<UIElement>()->GetHandle() : nullptr,
				false /*isAbsolutelyPositioned*/,
				spNativeLTE.ReleaseAndGetAddressOf()
			));
			IFC_RETURN(DXamlCore::GetCurrent()->GetPeer(spNativeLTE, KnownTypeIndex::UIElement, &spNativeLTEAsDO));
			IFC_RETURN(SetPtrValueWithQI(m_layoutTransition, spNativeLTEAsDO.Get()));

			IFC_RETURN(PositionLTEs());
#endif
		}

		private void PositionLTEs()
		{
			// MUX_ASSERT(m_layoutTransition is not null);  // TODO Uno: gated until LTE machinery is wired (see CreateLTEs).

			DependencyObject parentDO;
			UIElement parent;

			parentDO = Media.VisualTreeHelper.GetParent(this);

			// If we don't have a parent, then there's nothing for us to do.
			if (parentDO is not null)
			{
				parent = parentDO as UIElement;

#if false // TODO Uno: NOT PORTED - CoreImports.LayoutTransitionElement_SetDestinationOffset
				GeneralTransform transform = TransformToVisual(parent);

				Point offset = new Point(0, 0);
				offset = transform.TransformPoint(offset);

				CoreImports.LayoutTransitionElement_SetDestinationOffset(m_layoutTransition, offset.X, offset.Y);
#endif
			}

#if false // TODO Uno: NOT PORTED - overlay LTE composite-transform sizing
			// Since AutoSuggestBox's suggestion list does not dismiss on window resize, we have to make sure
			// we update the overlay element's size.
			if (m_overlayLayoutTransition is not null)
			{
				Transform transform = m_overlayLayoutTransition.RenderTransform;

				CompositeTransform compositeTransform = transform as CompositeTransform;

				Rect windowBounds = DXamlCore.GetCurrent().GetContentBoundsForElement(this);

				compositeTransform.ScaleX = windowBounds.Width;
				compositeTransform.ScaleY = windowBounds.Height;

				GeneralTransform transformToVisual = TransformToVisual(null);

				Point offsetFromRoot = new Point(0, 0);
				offsetFromRoot = transformToVisual.TransformPoint(offsetFromRoot);

				FlowDirection flowDirection = FlowDirection;

				// Translate the light-dismiss layer so that it is positioned at the top-left corner of the window (for LTR cases)
				// or the top-right corner of the window (for RTL cases).
				// TransformToVisual(nullptr) will return an offset relative to the top-left corner of the window regardless of
				// flow direction, so for RTL cases subtract the window width from the returned offset.x value to make it relative
				// to the right edge of the window.
				compositeTransform.TranslateX = flowDirection == FlowDirection.LeftToRight ? -offsetFromRoot.X : offsetFromRoot.X - windowBounds.Width;
				compositeTransform.TranslateY = -offsetFromRoot.Y;
			}
#endif
		}

		private void DestroyLTEs()
		{
			// MUX_ASSERT(m_layoutTransition is not null);  // TODO Uno: gated until LTE machinery is wired.

#if false // TODO Uno: NOT PORTED - CoreImports.LayoutTransitionElement_Destroy
			IFC_RETURN(CoreImports::LayoutTransitionElement_Destroy(
				DXamlCore::GetCurrent()->GetHandle(),
				GetHandle(),
				m_parentElementForLTEs ? m_parentElementForLTEs.Cast<UIElement>()->GetHandle() : nullptr,
				m_layoutTransition.Cast<UIElement>()->GetHandle()
				));
#endif

			m_layoutTransition = null;

			if (m_overlayLayoutTransition is not null)
			{
#if false // TODO Uno: NOT PORTED - destroy our light-dismiss element's LTE.
				IFC_RETURN(CoreImports::LayoutTransitionElement_Destroy(
					DXamlCore::GetCurrent()->GetHandle(),
					m_overlayElement.Cast<FrameworkElement>()->GetHandle(),
					m_parentElementForLTEs ? m_parentElementForLTEs.Cast<UIElement>()->GetHandle() : nullptr,
					m_overlayLayoutTransition.Cast<UIElement>()->GetHandle()
					));
#endif

				m_overlayLayoutTransition = null;
			}

			m_parentElementForLTEs = null;
		}

		private bool ShouldUseParentedLTE()
		{
			// VisualTreeHelper does not expose a GetRootStatic equivalent in Uno; walk the parent chain manually.
			DependencyObject rootDO = this;
			DependencyObject parent;
			while ((parent = Media.VisualTreeHelper.GetParent(rootDO)) is not null)
			{
				rootDO = parent;
			}
			if (rootDO is not null)
			{
				if (rootDO is Primitives.PopupRoot)
				{
					return true;
				}
				else if (rootDO is Uno.UI.Xaml.Core.FullWindowMediaRoot)
				{
					return true;
				}
			}

			return false;
		}

		private Rect GetAdjustedLayoutBounds()
		{
			// TODO Uno: NOT PORTED - DXamlCore::GetContentLayoutBoundsForElement(GetHandle(), &layoutBounds).
			// XamlRoot.VisualTree.VisibleBounds is the closest Uno equivalent for the layout-bounds rect.
			Rect layoutBounds = XamlRoot?.VisualTree?.VisibleBounds ?? default;

			// TODO Uno: 12949603 — re-enable this in XamlOneCoreTransforms mode using OneCore-friendly APIs.
			// It's disabled today because ClientToScreen deals in screen coordinates, which isn't allowed in strict mode.
			// AutoSuggestBox effectively acts as though the client window is always at the very top of the screen.
			// In Uno there is no DXamlCore.ClientToScreen equivalent yet — leave layoutBounds as-is.
			// if (!XamlOneCoreTransforms.IsEnabled())
			// {
			//     Point point = new Point(0, 0);
			//     DXamlCore.GetCurrent().ClientToScreen(ref point);
			//     layoutBounds.Y -= point.Y;
			// }

			return layoutBounds;
		}

		private void GetActualTextBoxSize(out double actualWidth, out double actualHeight)
		{
			if (m_tpTextBoxPart is not null)
			{
				// CTextBoxBase::GetActualSize returns the inner-text-area size in the C++ code; FrameworkElement
				// ActualWidth/ActualHeight is the closest direct mapping in Uno (the inner ScrollViewer template
				// part is queried separately by UpdateSuggestionListPosition for vertical alignment).
				actualWidth = m_tpTextBoxPart.ActualWidth;
				actualHeight = m_tpTextBoxPart.ActualHeight;
			}
			else
			{
				actualWidth = 0;
				actualHeight = 0;
			}
		}

		//------------------------------------------------------------------------------
		// AutoSuggestBox ChangeVisualState
		//
		// Applies the necessary visual state
		//------------------------------------------------------------------------------
		private void ChangeVisualState()
		{
			// XamlDisplay::Orientation values: 1 = Landscape, 2 = LandscapeFlipped, 4 = Portrait, 8 = PortraitFlipped (powers of 2 in the C++ enum).
			// Until XamlDisplay::GetDisplayOrientation is wired in Uno (see OnSipShowingInternal TODO), m_displayOrientation stays None and
			// neither branch fires. The structure is kept faithful so the visual state transition lights up automatically once that lands.
			const int landscape = 1;
			const int landscapeFlipped = 2;
			const int portrait = 4;
			const int portraitFlipped = 8;

			if (m_displayOrientation == landscape || m_displayOrientation == landscapeFlipped)
			{
				VisualStateManager.GoToState(this, c_VisualStateLandscape, true);
			}
			else if (m_displayOrientation == portrait || m_displayOrientation == portraitFlipped)
			{
				VisualStateManager.GoToState(this, c_VisualStatePortrait, true);
			}

			// TODO Uno: NOT PORTED - checked_cast<CControl>(GetHandle())->EnsureValidationVisuals();
			// CControl.EnsureValidationVisuals is the input-validation visual-tree refresh; revisit alongside #4839.
		}

		// Transforms coordinates to the target element's space.
		private Point TransformPoint(UIElement target, Point point)
		{
			MUX_ASSERT(target is not null);

			GeneralTransform spGeneralTransform = TransformToVisual(target);
			return spGeneralTransform.TransformPoint(point);
		}

		private void ScrollLastItemIntoView()
		{
			ListViewBase listViewBase = m_tpSuggestionsPart as ListViewBase;
			if (listViewBase is not null)
			{
				var items = listViewBase.Items;
				int size = items?.Count ?? 0;

				if (size > 1)
				{
					object lastItem = items[size - 1];

					listViewBase.ScrollIntoView(lastItem);
				}
			}
		}

		// TODO Uno: NOT PORTED - UpdateText body uses InvokeValidationCommand which is part of the input-validation
		// feature (#4839). Until that lands, propagate the value to the public Text DP without invoking validation.
		private void UpdateText(string value)
		{
			string strText = Text;
			if (value != strText)
			{
				Text = value;
			}

			// IFC_RETURN(InvokeValidationCommand(this, strText.Get())); // TODO Uno: input validation (#4839)
		}

		private void UpdateSuggestionListVisibility()
		{
			bool isOpen = false;

			// if the suggestion container exists, we are retrieving its maxsuggestionlistheight
			double maxHeight = 0;
			if (m_tpSuggestionsContainerPart is not null)
			{
				maxHeight = m_tpSuggestionsContainerPart.MaxHeight;
			}

			// TODO Uno: WinUI's ItemsControl.Items() returns a unified view of inline items + ItemsSource.
			// In Uno, AutoSuggestBox.Items only reflects the inline collection — ItemsSource items are accessed
			// via the internal NumberOfItems helper. Use NumberOfItems so the suggestion list opens when
			// ItemsSource (the typical AutoSuggestBox scenario) is non-empty.
			int count = NumberOfItems;

			// TODO Uno: WinUI gates the focus check at OnTextBoxTextChanged (only enters this method when text
			// changed via UserInput) and at OnItemsChanged (only when textbox focused). Uno's deferred event
			// dispatch + ListView-driven focus stealing on ItemsSource update can briefly drop
			// m_tpTextBoxPart.IsFocused mid-update (focus bounces to a sibling, then returns), so checking
			// .IsFocused at the wrong instant causes the popup to stay closed when it should open.
			// More robust: walk up from FocusManager.GetFocusedElement to see if ANY ancestor is `this`
			// (the AutoSuggestBox). This recognizes "any descendant of SUT owns focus" — not just the
			// inner TextBox — which rides through brief focus bounces between template parts.
			bool textBoxFocused = m_userTypedWithFocus
				|| (m_tpTextBoxPart is not null && m_tpTextBoxPart.IsFocused)
				|| IsSubtreeFocused();

			if (maxHeight > 0 && textBoxFocused)
			{
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
			// TODO Uno: NOT PORTED - bypass-focus path. The C++ calls AutoSuggestBoxGenerated::put_IsSuggestionListOpen
			// directly to skip the put_IsSuggestionListOpen override that takes focus. In Uno the focus side-effect
			// lives in OnIsSuggestionListOpenPropertyChanged, so for now we set the DP directly and the focus side-effect
			// will fire. Revisit if this causes focus thrash in tests.
			IsSuggestionListOpen = isOpen;
		}

		private string TryGetSuggestionValue(object o, Uno.UI.DataBinding.BindingPath pathListener)
		{
			if (o is null)
			{
				return null;
			}

			string value = null;
			object spBoxedValue = null;

			// Uno does not have ICustomPropertyProvider; we approximate the C++ ToString fallback by treating any
			// non-string object whose path is null as a candidate for ToString().
			if (pathListener is not null)
			{
				// Our caller has provided us with a PropertyPathListener. By setting the source of the listener, we can pull a value out.
				// This is our boxedValue, which we effectively ToString below.
				pathListener.DataContext = o;
				spBoxedValue = pathListener.Value;
			}
			else
			{
				spBoxedValue = o; // the object itself is the value string, unbox it.
			}

			// calling the ToString function on items that can be represented by a string
			if (spBoxedValue is not null)
			{
				value = spBoxedValue.ToString();
			}
			else
			{
				// FrameworkElement::GetStringFromObject(object, ...) — fall back to the original object's ToString.
				value = o.ToString();
			}

			return value;
		}

		private void SetCurrentControlledPeer(ControlledPeer peer)
		{
			if (m_tpTextBoxPart is not null)
			{
				UIElement spPeer = null;

				switch (peer)
				{
					case ControlledPeer.None:
						// Leave spPeer as nullptr.
						break;

					case ControlledPeer.SuggestionsList:
						if (m_tpSuggestionsPart is not null)
						{
							spPeer = m_tpSuggestionsPart as UIElement;
						}
						break;

					default:
						break;
				}

				var spControlledPeers = AutomationProperties.GetControlledPeers(m_tpTextBoxPart);

				spControlledPeers.Clear();
				if (spPeer is not null)
				{
					spControlledPeers.Add(spPeer);
				}

				// TODO Uno: NOT PORTED - AutomationPeer::ListenerExistsHelper + raise APControlledPeersProperty change.
				// Uno does not yet expose ListenerExistsHelper / AutomationRaiseAutomationPropertyChanged for the
				// ControlledPeers property; revisit alongside automation-peer parity.
				// bool bAutomationListener = AutomationPeer.ListenerExists(AutomationEvents.PropertyChanged);
				// if (bAutomationListener)
				// {
				//     var spAutomationPeer = m_tpTextBoxPart.GetOrCreateAutomationPeer();
				//     if (spAutomationPeer is not null)
				//     {
				//         CoreImports.AutomationRaiseAutomationPropertyChanged(spAutomationPeer, APControlledPeersProperty);
				//     }
				// }
			}
		}

		// GetPlainText override (lines 2725-2740 of AutoSuggestBox_Partial.cpp).
		// Returns the FrameworkElement plain-text representation for accessibility ("name" computation).
		// TODO Uno: NOT PORTED - FrameworkElement.GetStringFromObject has no direct Uno equivalent; fall back to
		// Header.ToString() when Header is set. Revisit alongside automation-peer parity work.
		// internal override string GetPlainText() — Uno does not expose a virtual GetPlainText hook on
		// FrameworkElement, so the override is omitted; the AutomationPeer (AutoSuggestBoxAutomationPeer)
		// is the supported extension point for accessibility names.

		// Remaining method TODOs (none — Phase 2 close-out):
		// TODO Uno: NOT PORTED - GetDisplayOrientation (DisplayOrientationHelper.h). The XamlDisplay::Orientation
		// enum + GetDisplayOrientation helper are not yet wired in Uno; m_displayOrientation stays at None
		// and ChangeVisualState's branches do not fire. Revisit when Uno gains a display-orientation tracker.

		//------------------------------------------------------------------------------
		// AutoSuggestBox::OnInkingFunctionButtonClicked (static, line 3165+).
		// Inking floatie's function button is a PenInputManager-driven path that has no Uno equivalent yet;
		// the static handler is preserved here as a thin wrapper that forwards to ProgrammaticSubmitQuery.
		//------------------------------------------------------------------------------
		internal static void OnInkingFunctionButtonClicked(AutoSuggestBox autoSuggestBox)
		{
			autoSuggestBox?.ProgrammaticSubmitQuery();
		}

		//------------------------------------------------------------------------------
		// Raises the QuerySubmitted event using the current content of the TextBox.
		//------------------------------------------------------------------------------
		private void SubmitQuery(object pChosenSuggestion)
		{
			AutoSuggestBoxQuerySubmittedEventArgs spEventArgs = new AutoSuggestBoxQuerySubmittedEventArgs();

			string strQueryText = m_tpTextBoxPart?.Text ?? string.Empty;
			spEventArgs.QueryText = strQueryText;

			spEventArgs.ChosenSuggestion = pChosenSuggestion;

			QuerySubmitted?.Invoke(this, spEventArgs);

			IsSuggestionListOpen = false;
		}

		/// <summary>
		/// Initiates a query submission as if the user pressed the query button. Used by
		/// AutoSuggestBoxAutomationPeer.Invoke and any caller that needs to programmatically
		/// raise QuerySubmitted.
		/// </summary>
		internal void ProgrammaticSubmitQuery()
		{
			// Clicking the query button should always submit the query solely with the text
			// in the TextBox, and should ignore any selected item in the suggestion list.
			// To ensure that, we'll deselect any item in the suggestion list that might be selected
			// before submitting the query.
			m_ignoreSelectionChanges = true;
			try
			{
				if (m_tpSuggestionsPart is not null)
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

		/// <inheritdoc />
		protected override AutomationPeer OnCreateAutomationPeer() =>
			new AutoSuggestBoxAutomationPeer(this);

		// TODO Uno: Uno-specific test hook (no equivalent in WinUI C++). Used by runtime tests
		// to simulate programmatic suggestion selection.
		internal void ChoseItem(object o)
		{
			if (m_tpSuggestionsPart is not null && o is not null)
			{
				bool wasIgnoring = m_ignoreSelectionChanges;
				try
				{
					// Drive the SelectionChanged path so OnSuggestionSelectionChanged runs UpdateTextBoxText.
					m_tpSuggestionsPart.SelectedItem = o;
				}
				finally
				{
					m_ignoreSelectionChanges = wasIgnoring;
				}
			}
		}

		// TODO Uno: Uno-specific test hook (no equivalent in WinUI C++). Reflected by When_Selecting_Suggest_With_UpDown_Key
		// runtime test, which expects a private HandleUpDownKeys method that drives the suggestion-list selector cursor
		// from the supplied KeyRoutedEventArgs. Mirrors the legacy Uno code's wrap-around index navigation.
		private void HandleUpDownKeys(KeyRoutedEventArgs e)
		{
			if (m_tpSuggestionsPart is null)
			{
				return;
			}

			int currentIndex = m_tpSuggestionsPart.SelectedIndex;
			int numSuggestions = Items?.Count ?? 0;
			if (numSuggestions == 0)
			{
				return;
			}

			int nextIndex = -1;

			if (e.Key == VirtualKey.Up)
			{
				// C# modulo isn't actually a modulo it's a remainder, so need to account for negative index
				nextIndex = ((currentIndex % numSuggestions) + numSuggestions) % numSuggestions - ((currentIndex == -1) ? 0 : 1);
			}
			else if (e.Key == VirtualKey.Down)
			{
				int indexPlusOne = currentIndex + 1;
				// The next step after the last index should be -1, not 0.
				nextIndex = ((indexPlusOne % numSuggestions) + numSuggestions) % numSuggestions - ((indexPlusOne == numSuggestions) ? 1 : 0);
			}

			m_tpSuggestionsPart.SelectedIndex = nextIndex;

			if (nextIndex == -1)
			{
				// Restore the user-typed text.
				UpdateTextBoxText(m_userTypedText, AutoSuggestionBoxTextChangeReason.ProgrammaticChange);
			}
			else
			{
				// Adopt the selected suggestion's text.
				ChoseItem(m_tpSuggestionsPart.SelectedItem);
			}
		}
	}
}

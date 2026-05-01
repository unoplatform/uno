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

			TimeSpan interval = TimeSpan.FromTicks(s_textChangedEventTimerDuration);
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
				}
			}

			if (m_tpPopupPart is not null)
			{
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
			}
			finally
			{
				m_textChangeReason = AutoSuggestionBoxTextChangeReason.UserInput;
			}
		}

		// TODO Uno: NOT PORTED - OnTextBoxCandidateWindowBoundsChanged (lines 505-548).
		private void OnTextBoxCandidateWindowBoundsChanged(TextBox sender, CandidateWindowBoundsChangedEventArgs args)
		{
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

		// TODO Uno: NOT PORTED - OnSuggestionListKeyDown (lines 576-652).
		private void OnSuggestionListKeyDown(object sender, KeyRoutedEventArgs args)
		{
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

		// TODO Uno: NOT PORTED - OnSuggestionSelectionChanged.
		private void OnSuggestionSelectionChanged(object sender, SelectionChangedEventArgs args)
		{
		}

		// TODO Uno: NOT PORTED - OnListViewItemClick.
		private void OnListViewItemClick(object sender, ItemClickEventArgs args)
		{
		}

		// TODO Uno: NOT PORTED - OnListViewContainerContentChanging.
		private void OnListViewContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
		{
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

		// InputPane.Showing handler — TODO Uno: NOT PORTED body (lines from OnSipShowing in C++).
		private void OnSipShowing(InputPane sender, InputPaneVisibilityEventArgs args)
		{
		}

		// InputPane.Hiding handler — TODO Uno: NOT PORTED body (lines from OnSipHiding in C++).
		private void OnSipHiding(InputPane sender, InputPaneVisibilityEventArgs args)
		{
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

		// TODO Uno: NOT PORTED - SetupOverlayState (lines 2856-).
		private void SetupOverlayState()
		{
		}

		// TODO Uno: NOT PORTED - TeardownOverlayState (lines 2926-).
		private void TeardownOverlayState()
		{
		}

		// TODO Uno: NOT PORTED - ProcessDeferredUpdate. Stub keeps OnTextChangedEventTimerTick callable.
		private void ProcessDeferredUpdate()
		{
		}

		// TODO Uno: NOT PORTED - UpdateSuggestionListPosition (lines 1329+).
		private void UpdateSuggestionListPosition()
		{
		}

		// TODO Uno: NOT PORTED - UpdateSuggestionListSize (lines 1252+).
		private void UpdateSuggestionListSize()
		{
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

			var spItemsReference = Items;

			if (spItemsReference is not null && maxHeight > 0)
			{
				int count = spItemsReference.Count;

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

		// TODO Uno: NOT PORTED - SetCurrentControlledPeer.
		private void SetCurrentControlledPeer(ControlledPeer peer)
		{
		}

		// Remaining method TODOs (deferred to next iter):
		// TODO Uno: NOT PORTED - OnTextBoxQueryButtonClick.
		// TODO Uno: NOT PORTED - SubmitQuery.
		// TODO Uno: NOT PORTED - UpdateText.
		// TODO Uno: NOT PORTED - ChangeVisualState.
		// TODO Uno: NOT PORTED - AlignmentHelper.
		// TODO Uno: NOT PORTED - MaximizeSuggestionArea.
		// TODO Uno: NOT PORTED - MaximizeSuggestionAreaWithoutInputPane.
		// TODO Uno: NOT PORTED - Scroll.
		// TODO Uno: NOT PORTED - PushScrollAction.
		// TODO Uno: NOT PORTED - ApplyScrollActions.
		// TODO Uno: NOT PORTED - UpdateSuggestionListSize.
		// TODO Uno: NOT PORTED - UpdateSuggestionListVisibility.
		// TODO Uno: NOT PORTED - UpdateSuggestionListPosition.
		// TODO Uno: NOT PORTED - TransformPoint.
		// TODO Uno: NOT PORTED - OnSipHidingInternal.
		// TODO Uno: NOT PORTED - OnSipShowingInternal.
		// TODO Uno: NOT PORTED - GetCandidateWindowPopupAdjustment.
		// TODO Uno: NOT PORTED - ScrollLastItemIntoView.
		// TODO Uno: NOT PORTED - ProcessDeferredUpdate.
		// TODO Uno: NOT PORTED - SetupOverlayState / TeardownOverlayState.
		// TODO Uno: NOT PORTED - CreateLTEs / PositionLTEs / DestroyLTEs.
		// TODO Uno: NOT PORTED - ShouldUseParentedLTE.
		// TODO Uno: NOT PORTED - GetAdjustedLayoutBounds / GetActualTextBoxSize / GetDisplayOrientation.
		// TODO Uno: NOT PORTED - OnInkingFunctionButtonClicked (static).
		// TODO Uno: NOT PORTED - OnGotFocus / OnLostFocus / OnKeyDown overrides.
		// TODO Uno: NOT PORTED - OnItemsChanged override.
		// TODO Uno: NOT PORTED - TryGetSuggestionValue.
		// GetPlainText override (lines 2725-2740 of AutoSuggestBox_Partial.cpp).
		// TODO Uno: NOT PORTED - GetPlainText. The C++ override returns the FrameworkElement plain-text representation
		// for accessibility ("name" computation). FrameworkElement::GetStringFromObject(spHeader, ...) has no direct
		// Uno equivalent; revisit alongside automation-peer parity work.

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
		// to simulate programmatic suggestion selection. Will be re-implemented once
		// OnSuggestionSelectionChanged + UpdateText are ported.
		internal void ChoseItem(object o)
		{
		}
	}
}

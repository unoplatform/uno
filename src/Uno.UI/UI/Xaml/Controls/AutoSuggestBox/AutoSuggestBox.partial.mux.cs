// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference AutoSuggestBox_Partial.cpp, commit 5f9e85113

#nullable disable

using System;
using DirectUI;
using Microsoft.UI.Xaml.Automation;
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

		// TODO Uno: NOT PORTED - body of OnUnloaded (lines 438-448).
		// Faithful body: if (!IsInLiveTree() && m_isOverlayVisible) { m_isOverlayVisible = false; TeardownOverlayState(); }
		private void OnUnloaded(object sender, RoutedEventArgs e)
		{
		}

		// TODO Uno: NOT PORTED - OnTextBoxTextChanged (lines 450-503).
		private void OnTextBoxTextChanged(object sender, TextChangedEventArgs e)
		{
		}

		// TODO Uno: NOT PORTED - OnTextBoxCandidateWindowBoundsChanged (lines 505-548).
		private void OnTextBoxCandidateWindowBoundsChanged(TextBox sender, CandidateWindowBoundsChangedEventArgs args)
		{
		}

		// TODO Uno: NOT PORTED - OnSizeChanged (lines 553-571).
		// Faithful body: if (m_tpSuggestionsContainerPart is not null) { m_tpSuggestionsContainerPart.Width = ActualWidth; }
		private void OnSizeChanged(object sender, SizeChangedEventArgs e)
		{
		}

		// TODO Uno: NOT PORTED - OnSuggestionListKeyDown (lines 576-652).
		private void OnSuggestionListKeyDown(object sender, KeyRoutedEventArgs args)
		{
		}

		// TODO Uno: NOT PORTED - OnPopupOpened (lines 660-687).
		private void OnPopupOpened(object sender, object args)
		{
		}

		// TODO Uno: NOT PORTED - OnSuggestionsContainerLoaded (lines 694-).
		private void OnSuggestionsContainerLoaded(object sender, RoutedEventArgs e)
		{
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

		// TODO Uno: NOT PORTED - OnTextBoxLoaded.
		private void OnTextBoxLoaded(object sender, RoutedEventArgs e)
		{
		}

		// TODO Uno: NOT PORTED - OnTextBoxUnloaded.
		private void OnTextBoxUnloaded(object sender, RoutedEventArgs e)
		{
		}

		// TODO Uno: NOT PORTED - OnTextChangedEventTimerTick.
		private void OnTextChangedEventTimerTick(object sender, object e)
		{
		}

		// InputPane.Showing handler — TODO Uno: NOT PORTED body (lines from OnSipShowing in C++).
		private void OnSipShowing(InputPane sender, InputPaneVisibilityEventArgs args)
		{
		}

		// InputPane.Hiding handler — TODO Uno: NOT PORTED body (lines from OnSipHiding in C++).
		private void OnSipHiding(InputPane sender, InputPaneVisibilityEventArgs args)
		{
		}

		// TODO Uno: NOT PORTED - SetTextBoxQueryButtonIcon.
		private void SetTextBoxQueryButtonIcon()
		{
		}

		// TODO Uno: NOT PORTED - ClearTextBoxQueryButtonIcon.
		private void ClearTextBoxQueryButtonIcon()
		{
		}

		// TODO Uno: NOT PORTED - HookToRootScrollViewer.
		private void HookToRootScrollViewer()
		{
		}

		// TODO Uno: NOT PORTED - UpdateTextBoxText.
		private void UpdateTextBoxText(string value, AutoSuggestionBoxTextChangeReason reason)
		{
		}

		// TODO Uno: NOT PORTED - UpdateSuggestionListItemsSource.
		private void UpdateSuggestionListItemsSource()
		{
		}

		// TODO Uno: NOT PORTED - ReevaluateIsOverlayVisible.
		private void ReevaluateIsOverlayVisible()
		{
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
		// TODO Uno: NOT PORTED - GetPlainText override.

		/// <summary>
		/// Initiates a query submission as if the user pressed the query button. Used by
		/// AutoSuggestBoxAutomationPeer.Invoke and any caller that needs to programmatically
		/// raise QuerySubmitted.
		/// </summary>
		// TODO Uno: NOT PORTED - body of ProgrammaticSubmitQuery (declared at AutoSuggestBox_Partial.h:36).
		// The C++ implementation routes through SubmitQuery(nullptr).
		internal void ProgrammaticSubmitQuery()
		{
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

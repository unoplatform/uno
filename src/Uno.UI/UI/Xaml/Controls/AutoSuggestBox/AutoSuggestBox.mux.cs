// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference src\dxaml\xcp\dxaml\lib\AutoSuggestBox_Partial.cpp, tag winui3/release/1.7.1

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Uno.Disposables;
using Uno.Extensions.Specialized;
using Uno.Foundation.Logging;
using Uno.UI;
using Uno.UI.DataBinding;
using Uno.UI.Helpers.WinUI;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.System;
using Windows.UI.ViewManagement;
using WinUICoreServices = Uno.UI.Xaml.Core.CoreServices;

namespace Microsoft.UI.Xaml.Controls;

partial class AutoSuggestBox
{
#if HAS_UNO
	/// <summary>
	/// Initializes a new instance of the AutoSuggestBox class.
	/// </summary>
	public AutoSuggestBox()
	{
		DefaultStyleKey = typeof(AutoSuggestBox);

		PrepareState();
	}

	/// <summary>
	/// Prepares this control by attaching needed event handlers.
	/// </summary>
	private void PrepareState()
	{
		// Create and configure the text changed event timer
		m_tpTextChangedEventTimer = new DispatcherTimer();
		m_tpTextChangedEventTimer.Interval = TimeSpan.FromTicks(TextChangedEventTimerDuration);

		// Attach Unloaded event handler
		void OnUnloaded(object sender, RoutedEventArgs args) => OnUnloaded_Internal(sender, args);
		Unloaded += OnUnloaded;
		m_unloadedEventRevoker.Disposable = Disposable.Create(() => Unloaded -= OnUnloaded);

		// Attach SizeChanged event handler
		void OnSizeChanged(object sender, SizeChangedEventArgs args) => OnSizeChanged_Internal(sender, args);
		SizeChanged += OnSizeChanged;
		m_sizeChangedEventRevoker.Disposable = Disposable.Create(() => SizeChanged -= OnSizeChanged);

		// Set up InputPane events
		SetupInputPaneEvents();

		// Hook up Items.VectorChanged
		Items.VectorChanged += OnItemsVectorChanged;
	}

	private void SetupInputPaneEvents()
	{
		try
		{
			m_tpInputPane = InputPane.GetForCurrentView();
			if (m_tpInputPane is not null)
			{
				void OnSipShowing(InputPane sender, InputPaneVisibilityEventArgs args) => OnSipShowing_Internal(sender, args);
				void OnSipHiding(InputPane sender, InputPaneVisibilityEventArgs args) => OnSipHiding_Internal(sender, args);

				m_tpInputPane.Showing += OnSipShowing;
				m_tpInputPane.Hiding += OnSipHiding;

				m_sipShowingEventRevoker.Disposable = Disposable.Create(() =>
				{
					if (m_tpInputPane is not null)
					{
						m_tpInputPane.Showing -= OnSipShowing;
					}
				});
				m_sipHidingEventRevoker.Disposable = Disposable.Create(() =>
				{
					if (m_tpInputPane is not null)
					{
						m_tpInputPane.Hiding -= OnSipHiding;
					}
				});
			}
		}
		catch
		{
			// InputPane might not be available on all platforms
		}
	}

	/// <summary>
	/// Builds the visual tree for the AutoSuggestBox when a new template is applied.
	/// </summary>
	protected override void OnApplyTemplate()
	{
		// Unwire old template parts (if existing)
		UnwireOldTemplateParts();

		base.OnApplyTemplate();

		HookToRootScrollViewer();

		// Get template parts
		m_tpTextBoxPart = GetTemplateChild(c_TextBoxName) as TextBox;
		m_tpSuggestionsPart = GetTemplateChild(c_SuggestionsListName) as Selector;
		m_tpPopupPart = GetTemplateChild(c_SuggestionsPopupName) as Popup;
		m_tpSuggestionsContainerPart = GetTemplateChild(c_SuggestionsContainerName) as FrameworkElement;
		m_tpUpwardTransformPart = GetTemplateChild(c_UpwardTransformName) as TranslateTransform;
		m_tpListItemOrderTransformPart = GetTemplateChild(c_ListItemOrderTransformName) as Transform;
		m_tpLayoutRootPart = GetTemplateChild(c_LayoutRootName) as Grid;

		// TODO UNO: Add RequiredHeaderPresenter support when validation is implemented
		// m_requiredHeaderPresenterPart = GetTemplateChild(c_RequiredHeaderName) as UIElement;

		// Configure TextBox part
		if (m_tpTextBoxPart is not null)
		{
			void OnTextBoxTextChanged(object sender, TextChangedEventArgs args) => OnTextBoxTextChanged_Internal(sender, args);
			m_tpTextBoxPart.TextChanged += OnTextBoxTextChanged;
			m_textBoxTextChangedEventRevoker.Disposable = Disposable.Create(() =>
			{
				if (m_tpTextBoxPart is not null)
				{
					m_tpTextBoxPart.TextChanged -= OnTextBoxTextChanged;
				}
			});

			// Initialize text box with current Text value
			UpdateTextBoxText(Text, AutoSuggestionBoxTextChangeReason.ProgrammaticChange);

			// TextBox Loaded event
			void OnTextBoxLoaded(object sender, RoutedEventArgs args) => OnTextBoxLoaded_Internal(sender, args);
			m_tpTextBoxPart.Loaded += OnTextBoxLoaded;
			m_textBoxLoadedEventRevoker.Disposable = Disposable.Create(() =>
			{
				if (m_tpTextBoxPart is not null)
				{
					m_tpTextBoxPart.Loaded -= OnTextBoxLoaded;
				}
			});

			// TextBox Unloaded event
			void OnTextBoxUnloaded(object sender, RoutedEventArgs args) => OnTextBoxUnloaded_Internal(sender, args);
			m_tpTextBoxPart.Unloaded += OnTextBoxUnloaded;
			m_textBoxUnloadedEventRevoker.Disposable = Disposable.Create(() =>
			{
				if (m_tpTextBoxPart is not null)
				{
					m_tpTextBoxPart.Unloaded -= OnTextBoxUnloaded;
				}
			});

			// TODO UNO: CandidateWindowBoundsChanged event is not available in Uno
			// m_tpTextBoxPart.CandidateWindowBoundsChanged += OnTextBoxCandidateWindowBoundsChanged;

			// Copy automation name from AutoSuggestBox to TextBox
			var automationName = AutomationProperties.GetName(this);
			if (!string.IsNullOrEmpty(automationName))
			{
				AutomationProperties.SetName(m_tpTextBoxPart, automationName);
			}
		}

		// Configure Suggestions part (ListView)
		if (m_tpSuggestionsPart is not null)
		{
			UpdateSuggestionListItemsSource();

			if (m_tpSuggestionsPart is ListViewBase listViewBase)
			{
				// Item click event
				void OnListViewItemClick(object sender, ItemClickEventArgs args) => OnListViewItemClick_Internal(sender, args);
				listViewBase.ItemClick += OnListViewItemClick;
				m_listViewItemClickEventRevoker.Disposable = Disposable.Create(() =>
				{
					if (m_tpSuggestionsPart is ListViewBase lvb)
					{
						lvb.ItemClick -= OnListViewItemClick;
					}
				});

				// Container content changing event
				void OnListViewContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
					=> OnListViewContainerContentChanging_Internal(sender, args);
				listViewBase.ContainerContentChanging += OnListViewContainerContentChanging;
				m_listViewContainerContentChangingEventRevoker.Disposable = Disposable.Create(() =>
				{
					if (m_tpSuggestionsPart is ListViewBase lvb)
					{
						lvb.ContainerContentChanging -= OnListViewContainerContentChanging;
					}
				});
			}

			// Selection changed event
			void OnSuggestionSelectionChanged(object sender, SelectionChangedEventArgs args) => OnSuggestionSelectionChanged_Internal(sender, args);
			m_tpSuggestionsPart.SelectionChanged += OnSuggestionSelectionChanged;
			m_suggestionSelectionChangedEventRevoker.Disposable = Disposable.Create(() =>
			{
				if (m_tpSuggestionsPart is not null)
				{
					m_tpSuggestionsPart.SelectionChanged -= OnSuggestionSelectionChanged;
				}
			});

			// Key down event on suggestions list
			void OnSuggestionListKeyDown(object sender, KeyRoutedEventArgs args) => OnSuggestionListKeyDown_Internal(sender, args);
			m_tpSuggestionsPart.KeyDown += OnSuggestionListKeyDown;
			m_suggestionListKeyDownEventRevoker.Disposable = Disposable.Create(() =>
			{
				if (m_tpSuggestionsPart is not null)
				{
					m_tpSuggestionsPart.KeyDown -= OnSuggestionListKeyDown;
				}
			});

			// Disable item focus from UIA for the suggestion list
			if (m_tpSuggestionsPart is ListView listView)
			{
				// TODO UNO: SetAllowItemFocusFromUIA is not available in Uno
				// listView.SetAllowItemFocusFromUIA(false);
			}
		}

		// Configure Popup part
		if (m_tpPopupPart is not null)
		{
			void OnPopupOpened(object sender, object args) => OnPopupOpened_Internal(sender, args);
			m_tpPopupPart.Opened += OnPopupOpened;
			m_popupOpenedEventRevoker.Disposable = Disposable.Create(() =>
			{
				if (m_tpPopupPart is not null)
				{
					m_tpPopupPart.Opened -= OnPopupOpened;
				}
			});

			// Uno specific: Disable light dismiss if the legacy behavior is enabled
			if (FeatureConfiguration.Popup.EnableLightDismissByDefault)
			{
				m_tpPopupPart.IsLightDismissEnabled = false;
			}
		}

		// Configure Suggestions Container part
		if (m_tpSuggestionsContainerPart is not null)
		{
			void OnSuggestionsContainerLoaded(object sender, RoutedEventArgs args) => OnSuggestionsContainerLoaded_Internal(sender, args);
			m_tpSuggestionsContainerPart.Loaded += OnSuggestionsContainerLoaded;
			m_suggestionsContainerLoadedEventRevoker.Disposable = Disposable.Create(() =>
			{
				if (m_tpSuggestionsContainerPart is not null)
				{
					m_tpSuggestionsContainerPart.Loaded -= OnSuggestionsContainerLoaded;
				}
			});
		}

		// Configure Text Changed Event Timer
		if (m_tpTextChangedEventTimer is not null)
		{
			void OnTextChangedEventTimerTick(object sender, object args) => OnTextChangedEventTimerTick_Internal(sender, args);
			m_tpTextChangedEventTimer.Tick += OnTextChangedEventTimerTick;
			m_textChangedEventTimerTickEventRevoker.Disposable = Disposable.Create(() =>
			{
				if (m_tpTextChangedEventTimer is not null)
				{
					m_tpTextChangedEventTimer.Tick -= OnTextChangedEventTimerTick;
				}
			});
		}

		ReevaluateIsOverlayVisible();
		UpdateDescriptionVisibility(true);
	}

	/// <summary>
	/// Unwires event handlers from old template parts.
	/// </summary>
	private void UnwireOldTemplateParts()
	{
		m_textBoxTextChangedEventRevoker.Disposable = null;
		m_textBoxLoadedEventRevoker.Disposable = null;
		m_textBoxUnloadedEventRevoker.Disposable = null;
		m_textBoxCandidateWindowBoundsChangedEventRevoker.Disposable = null;
		m_queryButtonClickEventRevoker.Disposable = null;
		m_suggestionSelectionChangedEventRevoker.Disposable = null;
		m_suggestionListKeyDownEventRevoker.Disposable = null;
		m_listViewItemClickEventRevoker.Disposable = null;
		m_listViewContainerContentChangingEventRevoker.Disposable = null;
		m_popupOpenedEventRevoker.Disposable = null;
		m_suggestionsContainerLoadedEventRevoker.Disposable = null;

		// Clear query button icon before clearing reference
		if (m_tpTextBoxQueryButtonPart is not null)
		{
			ClearTextBoxQueryButtonIcon();
		}

		// Clear template part references
		m_tpTextBoxPart = null;
		m_tpTextBoxQueryButtonPart = null;
		m_tpSuggestionsPart = null;
		m_tpPopupPart = null;
		m_tpSuggestionsContainerPart = null;
		m_tpUpwardTransformPart = null;
		m_tpListItemOrderTransformPart = null;
		m_tpLayoutRootPart = null;
		m_requiredHeaderPresenterPart = null;
		m_wkRootScrollViewer = null;
	}

	#region Event Handlers

	/// <summary>
	/// Handler of the Unloaded event.
	/// </summary>
	private void OnUnloaded_Internal(object sender, RoutedEventArgs args)
	{
		if (!IsLoaded && m_isOverlayVisible)
		{
			m_isOverlayVisible = false;
			TeardownOverlayState();
		}
	}

	/// <summary>
	/// Handler of the TextBox's TextChanged event.
	/// </summary>
	private void OnTextBoxTextChanged_Internal(object sender, TextChangedEventArgs args)
	{
		m_textChangedCounter++;

		var eventArgs = new AutoSuggestBoxTextChangedEventArgs();
		eventArgs.SetCounter(m_textChangedCounter);
		eventArgs.Owner = this;
		eventArgs.Reason = m_textChangeReason;

		m_tpTextChangedEventArgs = eventArgs;

		m_tpTextChangedEventTimer?.Stop();
		m_tpTextChangedEventTimer?.Start();

		var queryText = m_tpTextBoxPart?.Text ?? "";
		UpdateText(queryText);

		if (!m_ignoreTextChanges)
		{
			if (m_textChangeReason == AutoSuggestionBoxTextChangeReason.UserInput)
			{
				m_userTypedText = queryText;
				// Make sure the suggestion list is shown when user inputs
				UpdateSuggestionListVisibility();
			}

			if (m_tpSuggestionsPart is not null)
			{
				var selectedIndex = m_tpSuggestionsPart.SelectedIndex;
				if (selectedIndex != -1)
				{
					m_ignoreSelectionChanges = true;
					m_tpSuggestionsPart.SelectedIndex = -1;
					m_ignoreSelectionChanges = false;
				}
			}
		}

		m_textChangeReason = AutoSuggestionBoxTextChangeReason.UserInput;
	}

	/// <summary>
	/// Handler of the SizeChanged event.
	/// </summary>
	private void OnSizeChanged_Internal(object sender, SizeChangedEventArgs args)
	{
		if (m_tpSuggestionsContainerPart is not null)
		{
			m_tpSuggestionsContainerPart.Width = ActualWidth;
		}
	}

	/// <summary>
	/// Handler for key down events on the suggestion list.
	/// This event handler is only for Gamepad or Remote cases, where Focus and Selection are tied.
	/// </summary>
	private void OnSuggestionListKeyDown_Internal(object sender, KeyRoutedEventArgs args)
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
					var selectedIndex = m_tpSuggestionsPart?.SelectedIndex ?? -1;
					var count = Items?.Count ?? 0;
					var lastIndex = count - 1;

					// If we are already at the Suggestion that is adjacent to the TextBox, then set SelectedIndex to -1.
					if ((selectedIndex == 0 && !IsSuggestionListVectorReversed) ||
						(selectedIndex == lastIndex && IsSuggestionListVectorReversed))
					{
						UpdateTextBoxText(m_userTypedText, AutoSuggestionBoxTextChangeReason.ProgrammaticChange);
						if (m_tpSuggestionsPart is not null)
						{
							m_tpSuggestionsPart.SelectedIndex = -1;
						}

						m_tpTextBoxPart?.Focus(FocusState.Keyboard);
					}

					wasHandledLocally = true;
					break;
				}

			case VirtualKey.Space:
			case VirtualKey.Enter:
				{
					var selectedItem = m_tpSuggestionsPart?.SelectedItem;
					SubmitQuery(selectedItem);
					IsSuggestionListOpen = false;
					wasHandledLocally = true;
					break;
				}

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

	/// <summary>
	/// Handler of the suggestions Popup's Opened event.
	/// Updates the suggestions list's position as its position can be changed before the previous Open operation.
	/// </summary>
	private void OnPopupOpened_Internal(object sender, object args)
	{
		// Bail out early if the popup has been unloaded already.
		if (m_tpPopupPart is not FrameworkElement fe || !fe.IsLoaded)
		{
			return;
		}

		// TODO UNO: Apply elevation effect to the popup's immediate child
		// ApplyElevationEffect(m_tpPopupPart);

		UpdateSuggestionListPosition();
	}

	/// <summary>
	/// Handler of the suggestions container's Loaded event.
	/// Sets the position of the suggestions list as soon as the container is loaded.
	/// </summary>
	private void OnSuggestionsContainerLoaded_Internal(object sender, RoutedEventArgs args)
	{
		UpdateSuggestionListPosition();
		UpdateSuggestionListSize();
	}

	/// <summary>
	/// Handler of the text box's Loaded event.
	/// Retrieves the query button if it exists and attaches a handler to its Click event.
	/// </summary>
	private void OnTextBoxLoaded_Internal(object sender, RoutedEventArgs args)
	{
		m_tpTextBoxQueryButtonPart = m_tpTextBoxPart?.GetTemplateChild(c_TextBoxQueryButtonName) as ButtonBase;

		if (m_tpTextBoxQueryButtonPart is not null)
		{
			SetTextBoxQueryButtonIcon();

			if (m_queryButtonClickEventRevoker.Disposable is null)
			{
				void OnQueryButtonClick(object s, RoutedEventArgs e) => OnTextBoxQueryButtonClick_Internal(s, e);
				m_tpTextBoxQueryButtonPart.Click += OnQueryButtonClick;
				m_queryButtonClickEventRevoker.Disposable = Disposable.Create(() =>
				{
					if (m_tpTextBoxQueryButtonPart is not null)
					{
						m_tpTextBoxQueryButtonPart.Click -= OnQueryButtonClick;
					}
				});
			}

			// Update query button's AutomationProperties.Name to "Search" by default
			var automationName = AutomationProperties.GetName(m_tpTextBoxQueryButtonPart);
			if (string.IsNullOrEmpty(automationName))
			{
				// TODO UNO: Use localized resource string
				AutomationProperties.SetName(m_tpTextBoxQueryButtonPart, "Search");
			}
		}
	}

	/// <summary>
	/// Handler of the text box's Unloaded event.
	/// Removes the handler to the query button.
	/// </summary>
	private void OnTextBoxUnloaded_Internal(object sender, RoutedEventArgs args)
	{
		// Checking IsActive because Unloaded is async and we might have reloaded before this fires
		if (m_tpTextBoxQueryButtonPart is not null && m_queryButtonClickEventRevoker.Disposable is not null && !IsLoaded)
		{
			m_queryButtonClickEventRevoker.Disposable = null;
		}
	}

	/// <summary>
	/// Handler of the query button's Click event.
	/// Raises the QuerySubmitted event.
	/// </summary>
	private void OnTextBoxQueryButtonClick_Internal(object sender, RoutedEventArgs args)
	{
		ProgrammaticSubmitQuery();
	}

	/// <summary>
	/// Handler for the TextChanged event timer tick.
	/// </summary>
	private void OnTextChangedEventTimerTick_Internal(object sender, object args)
	{
		m_tpTextChangedEventTimer?.Stop();

		if (m_tpTextChangedEventArgs is not null)
		{
			TextChanged?.Invoke(this, m_tpTextChangedEventArgs);
		}

		// We expect apps to modify the ItemsSource in the TextChangedEvent (raised above).
		// If that happened, we'll have a deferred update at this point, let's just process
		// it now so we don't have to wait for the next tick.
		ProcessDeferredUpdate();
	}

	/// <summary>
	/// Handler for suggestion selection changes.
	/// </summary>
	private void OnSuggestionSelectionChanged_Internal(object sender, SelectionChangedEventArgs args)
	{
		var selectedItem = m_tpSuggestionsPart?.SelectedItem;

		// ASB handles keyboard navigation on behalf of the suggestion box.
		// Consequently, the latter won't scroll its viewport to follow the selected item.
		// We have to do that ourselves explicitly.
		var scrollToItem = selectedItem;
		if (scrollToItem is null && Items?.Count > 0)
		{
			scrollToItem = Items[0];
		}

		if (scrollToItem is not null && m_tpSuggestionsPart is ListViewBase listViewBase)
		{
			listViewBase.ScrollIntoView(scrollToItem);
		}

		if (m_ignoreSelectionChanges)
		{
			// Ignore the selection change if the change is triggered by the TextBoxText changed event.
			return;
		}

		// The only time we'll get here is when we're keyboarding through the suggestion list.
		// In this case, we're going to be updating the TextBox's text as the user does that,
		// so we should not be responding to TextChanged events in the TextBox, as they shouldn't be affecting anything.
		m_ignoreTextChanges = true;

		if (selectedItem is not null)
		{
			var updateTextOnSelect = UpdateTextOnSelect;
			if (updateTextOnSelect)
			{
				var textMemberPath = TextMemberPath;
				if (m_spPropertyPathListener is null && !string.IsNullOrEmpty(textMemberPath))
				{
					m_spPropertyPathListener = new BindingPath(textMemberPath, "", forAnimations: false, allowPrivateMembers: true);
				}

				var selectedItemText = TryGetSuggestionValue(selectedItem, m_spPropertyPathListener);
				UpdateTextBoxText(selectedItemText, AutoSuggestionBoxTextChangeReason.SuggestionChosen);
			}

			// If the item was selected using Gamepad or Remote, move the focus to the selected item.
			if (m_inputDeviceTypeUsed == InputDeviceType.GamepadOrRemote)
			{
				if (m_tpSuggestionsPart is ListViewBase lvb)
				{
					var container = lvb.ContainerFromItem(selectedItem);
					(container as UIElement)?.Focus(FocusState.Keyboard);
				}
			}

			var eventArgs = new AutoSuggestBoxSuggestionChosenEventArgs(selectedItem);
			SuggestionChosen?.Invoke(this, eventArgs);
		}

		// At this point everything that's going to post a TextChanged event
		// to the event queue has done so, so we'll schedule a callback
		// to reset the value of m_ignoreTextChanges to false once they've
		// all been raised.
		_ = Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
		{
			m_ignoreTextChanges = false;
		});
	}

	/// <summary>
	/// Handler for ListView item click.
	/// </summary>
	private void OnListViewItemClick_Internal(object sender, ItemClickEventArgs args)
	{
		var clickedItem = args.ClickedItem;

		// When a suggestion is clicked, we want to raise QuerySubmitted using that
		// as the chosen suggestion. However, clicking on an item may additionally raise
		// SelectionChanged, which will set the value of the TextBox and raise SuggestionChosen,
		// both of which we want to occur before we raise QuerySubmitted.
		// To account for this, we'll register a callback that will cause us to call SubmitQuery
		// after everything else has happened.
		_ = Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
		{
			SubmitQuery(clickedItem);
		});
	}

	/// <summary>
	/// Handler for ListView container content changing.
	/// </summary>
	private void OnListViewContainerContentChanging_Internal(ListViewBase sender, ContainerContentChangingEventArgs args)
	{
		if (m_tpListItemOrderTransformPart is not null)
		{
			var container = args.ItemContainer;
			if (container is UIElement containerUI)
			{
				containerUI.RenderTransformOrigin = new Point(0.5, 0.5);
				containerUI.RenderTransform = m_tpListItemOrderTransformPart;
			}
		}
	}

	/// <summary>
	/// Handler for SIP (Soft Input Panel) Showing event.
	/// </summary>
	private void OnSipShowing_Internal(InputPane sender, InputPaneVisibilityEventArgs args)
	{
		s_sipIsOpen = true;

		if (args is not null)
		{
			OnSipShowingInternal(args);
		}
	}

	/// <summary>
	/// Handler for SIP (Soft Input Panel) Hiding event.
	/// </summary>
	private void OnSipHiding_Internal(InputPane sender, InputPaneVisibilityEventArgs args)
	{
		s_sipIsOpen = false;
		ReevaluateIsOverlayVisible();
	}

	/// <summary>
	/// Handler for Items vector changed.
	/// </summary>
	private void OnItemsVectorChanged(IObservableVector<object> sender, IVectorChangedEventArgs args)
	{
		OnItemsChanged_Internal(args);
	}

	#endregion

	#region Focus Handling

	/// <summary>
	/// AutoSuggestBox OnLostFocus event handler.
	/// The suggestions drop-down will never be displayed when this control loses focus.
	/// </summary>
	protected override void OnLostFocus(RoutedEventArgs e)
	{
		base.OnLostFocus(e);

		if (!m_keepFocus)
		{
			m_hasFocus = false;

			if (m_isSipVisible)
			{
				// checking to see if the focus went to a new element that contains a textbox
				// in this case, we leave it up to the new element to handle scrolling
				var focusedElement = FocusManager.GetFocusedElement(XamlRoot) as DependencyObject;
				if (focusedElement is not null)
				{
					if (focusedElement is not TextBox)
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
		if (m_inputDeviceTypeUsed != InputDeviceType.GamepadOrRemote)
		{
			IsSuggestionListOpen = false;
		}
	}

	/// <summary>
	/// AutoSuggestBox OnGotFocus event handler.
	/// The suggestions drop-down should be displayed if items are present and the textbox has text.
	/// </summary>
	protected override void OnGotFocus(RoutedEventArgs e)
	{
		base.OnGotFocus(e);

		if (!m_keepFocus)
		{
			var isTextBoxFocused = IsTextBoxFocusedElement();
			if (isTextBoxFocused)
			{
				// This code handles the case when the control receives focus from another control
				// that was using the SIP. In this case, OnSipShowing is not guaranteed to be called
				// hence, we align the control to the top or bottom depending on its position at the time
				// it received focus
				if (m_tpInputPane is not null && s_sipIsOpen)
				{
					var sipOverlayArea = m_tpInputPane.OccludedRect;
					AlignmentHelper(sipOverlayArea);
				}

				// Expands the suggestion list if there is already text in the textbox
				var text = m_tpTextBoxPart?.Text ?? "";
				if (!string.IsNullOrEmpty(text) && !m_suppressSuggestionListVisibility)
				{
					UpdateSuggestionListVisibility();
					m_suppressSuggestionListVisibility = false;
				}
			}
		}
		else
		{
			// making sure the ASB is aligned to where it should be
			ApplyScrollActions(hasNewScrollActions: true);
		}

		m_keepFocus = false;
	}

	/// <summary>
	/// AutoSuggestBox OnKeyDown event handler.
	/// Handle the proper KeyDown event to process Key_Down, KeyUp, Key_Enter and Key_Escape.
	/// </summary>
	protected override void OnKeyDown(KeyRoutedEventArgs e)
	{
		var key = e.Key;
		var originalKey = e.OriginalKey;

		// Determine input device type
		m_inputDeviceTypeUsed = IsGamepadNavigationInput(originalKey)
			? InputDeviceType.GamepadOrRemote
			: InputDeviceType.Keyboard;

		base.OnKeyDown(e);

		if (m_tpSuggestionsPart is not null)
		{
			bool wasHandledLocally = false;

			var selectedIndex = m_tpSuggestionsPart.SelectedIndex;
			var isSuggestionListOpen = IsSuggestionListOpen;

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
						var count = Items?.Count ?? 0;
						if (count > 0)
						{
							var lastIndex = count - 1;
							var modifiers = GetKeyboardModifiers();
							var isForward = ShouldMoveIndexForwardForKey(key, modifiers);

							selectedIndex = m_tpSuggestionsPart.SelectedIndex;

							// Handle navigation through the suggestion list
							if (selectedIndex != 0 || lastIndex != 0)
							{
								if (selectedIndex == -1)
								{
									if (isForward)
									{
										if (!(IsSuggestionListVectorReversed && m_inputDeviceTypeUsed == InputDeviceType.GamepadOrRemote))
										{
											m_tpSuggestionsPart.SelectedIndex = 0;
										}
									}
									else
									{
										if (!(!IsSuggestionListVectorReversed && m_inputDeviceTypeUsed == InputDeviceType.GamepadOrRemote))
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
					// Only close the suggestion list and reset the user text with Tab if the suggestion list is open.
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
					var selectedItem = m_tpSuggestionsPart.SelectedItem;
					SubmitQuery(selectedItem);
					IsSuggestionListOpen = false;
					wasHandledLocally = true;
					break;
			}

			if (wasHandledLocally)
			{
				e.Handled = true;
			}
		}
	}

	/// <summary>
	/// Queries the focused element from the focus manager to see if it's the textbox.
	/// </summary>
	private bool IsTextBoxFocusedElement()
	{
		if (m_tpTextBoxPart is not null)
		{
			var focusedElement = FocusManager.GetFocusedElement(XamlRoot) as DependencyObject;
			if (focusedElement is TextBox textBox && textBox == m_tpTextBoxPart)
			{
				return true;
			}
		}
		return false;
	}

	#endregion

	#region Query Submission

	/// <summary>
	/// Programmatically submits the query.
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

	/// <summary>
	/// Raises the QuerySubmitted event using the current content of the TextBox.
	/// </summary>
	private void SubmitQuery(object chosenSuggestion)
	{
		var queryText = m_tpTextBoxPart?.Text ?? "";
		var eventArgs = new AutoSuggestBoxQuerySubmittedEventArgs(chosenSuggestion, queryText);

		QuerySubmitted?.Invoke(this, eventArgs);

		IsSuggestionListOpen = false;
	}

	#endregion

	#region Suggestion List Management

	/// <summary>
	/// Updates the suggestion list's size based on the available space and the MaxSuggestionListHeight property.
	/// </summary>
	private void UpdateSuggestionListSize()
	{
		if (m_tpSuggestionsContainerPart is not null)
		{
			var maxSuggestionListHeight = MaxSuggestionListHeight;

			// If the user specifies a negative value for the maxsuggestionlistsize, we use the available size
			if ((m_availableSuggestionHeight > 0 && maxSuggestionListHeight > m_availableSuggestionHeight) || maxSuggestionListHeight < 0)
			{
				maxSuggestionListHeight = m_availableSuggestionHeight;
			}

			m_tpSuggestionsContainerPart.MaxHeight = maxSuggestionListHeight;
			m_tpSuggestionsContainerPart.Width = ActualWidth;
		}
	}

	/// <summary>
	/// Opens the suggestion list if there is at least one item in the items collection.
	/// </summary>
	private void UpdateSuggestionListVisibility()
	{
		// If the suggestion container exists, we are retrieving its maxsuggestionlistheight
		var maxHeight = m_tpSuggestionsContainerPart?.MaxHeight ?? 0.0;
		var items = Items;

		bool isOpen = false;

		if (items is not null && maxHeight > 0)
		{
			var count = items.Count;
			// The suggestion list is only open when the maxsuggestionlistheight is greater than zero
			// and the count of elements in the list is positive
			if (count > 0)
			{
				isOpen = true;
			}
		}

		// We don't want to necessarily take focus in this case, since we probably already
		// have focus somewhere internal to the AutoSuggestBox, so we'll bypass the custom
		// setter for IsSuggestionListOpen that takes focus.
		SetIsSuggestionListOpenInternal(isOpen);
	}

	/// <summary>
	/// Positions the suggestion list based on the available space.
	/// </summary>
	private void UpdateSuggestionListPosition()
	{
		// Don't call this function while processing a collection change.
#if DEBUG
		if (m_handlingCollectionChange)
		{
			return;
		}
#endif

		if (!m_isSipVisible)
		{
			MaximizeSuggestionAreaWithoutInputPane();
		}

		if (m_tpPopupPart is not null && m_tpTextBoxPart is not null && m_tpSuggestionsContainerPart is not null)
		{
			double translateY = 0;
			double translateX = 0;
			double scaleY = 1.0;

			var suggestionListMargin = (m_tpSuggestionsPart as FrameworkElement)?.Margin ?? default;

			// Get scroll viewer location
			var textBoxScrollViewer = m_tpTextBoxPart.GetTemplateChild(c_TextBoxScrollViewerName) as FrameworkElement;

			if (textBoxScrollViewer is null)
			{
				var transform = m_tpTextBoxPart.TransformToVisual(this);
				var textBoxLocation = transform.TransformPoint(new Point(0, 0));
				translateY = textBoxLocation.Y;
			}
			else
			{
				var transform = textBoxScrollViewer.TransformToVisual(this);
				var scrollViewerLocation = transform.TransformPoint(new Point(0, 0));
				translateY = scrollViewerLocation.Y;
			}

			// We need to move the popup up (popup's bottom align to textbox) when textbox is at bottom position.
			if (m_suggestionListPosition == SuggestionListPosition.Above)
			{
				m_tpSuggestionsContainerPart.UpdateLayout();
				var height = m_tpSuggestionsContainerPart.ActualHeight;
				translateY -= height;

				if (IsSuggestionListVerticallyMirrored)
				{
					scaleY = -1.0;
				}
			}
			else if (m_suggestionListPosition == SuggestionListPosition.Below)
			{
				double height;
				if (textBoxScrollViewer is null)
				{
					height = m_tpTextBoxPart.ActualHeight;
					translateY += height;
					// Bring up the suggestion list to avoid gap caused by margin
					translateY -= suggestionListMargin.Top;
				}
				else
				{
					height = textBoxScrollViewer.ActualHeight;
					translateY += height;
				}
			}

			// Get candidate window popup adjustment
			GetCandidateWindowPopupAdjustment(
				ignoreSuggestionListPosition: false,
				out double candidateWindowXOffset,
				out double candidateWindowYOffset);

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
			var margin = m_tpSuggestionsContainerPart.Margin;
			margin.Right = candidateWindowXOffset;
			m_tpSuggestionsContainerPart.Margin = margin;

			if (IsSuggestionListVerticallyMirrored)
			{
				if (m_tpListItemOrderTransformPart is ScaleTransform scaleTransform)
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

	/// <summary>
	/// Updates the suggestion list's ItemsSource.
	/// </summary>
	private void UpdateSuggestionListItemsSource()
	{
		if (m_tpSuggestionsPart is not null && m_tpListItemOrderTransformPart is null)
		{
			// If we have a m_tpListItemOrderTransformPart, we implement SuggestionListPosition::Above
			// by applying a scale transform. Also, in the win8.1 template where we do have a
			// m_tpListItemOrderTransformPart, the suggestion list's ItemsSource is bound to the
			// ASB's ItemsSource, so no need to update it.

			if (m_suggestionListPosition == SuggestionListPosition.Above)
			{
				// TODO UNO: Implement ReversedVector for above positioning
				// For now, just propagate ItemsSource directly
			}

			// Propagate ItemsSource from ASB to the suggestion list
			if (m_tpSuggestionsPart is ItemsControl itemsControl)
			{
				itemsControl.ItemsSource = ItemsSource;
			}
		}
	}

	/// <summary>
	/// Scrolls the last item into view.
	/// </summary>
	private void ScrollLastItemIntoView()
	{
		if (m_tpSuggestionsPart is ListViewBase listViewBase)
		{
			var items = Items;
			if (items is not null && items.Count > 1)
			{
				var lastItem = items[items.Count - 1];
				listViewBase.ScrollIntoView(lastItem);
			}
		}
	}

	#endregion

	#region Text and Query Icon Management

	/// <summary>
	/// Sets the value of QueryButton.Content to the current value of QueryIcon.
	/// </summary>
	private void SetTextBoxQueryButtonIcon()
	{
		if (m_tpTextBoxQueryButtonPart is not null)
		{
			var queryIcon = QueryIcon;

			if (queryIcon is SymbolIcon symbolIcon)
			{
				// Setting FontSize to zero prevents SymbolIcon from setting a static FontSize on its child TextBlock,
				// allowing the binding to AutoSuggestBoxIconFontSize to be inherited properly.
				symbolIcon.SetFontSize(0);
			}

			if (queryIcon is not null)
			{
				m_tpTextBoxQueryButtonPart.Visibility = Visibility.Visible;
				m_tpTextBoxQueryButtonPart.SetValue(ContentControl.ContentProperty, queryIcon);
			}
			else
			{
				m_tpTextBoxQueryButtonPart.Visibility = Visibility.Collapsed;
			}
		}
	}

	/// <summary>
	/// Clears the value of QueryButton.Content.
	/// </summary>
	private void ClearTextBoxQueryButtonIcon()
	{
		if (m_tpTextBoxQueryButtonPart is not null)
		{
			m_tpTextBoxQueryButtonPart.SetValue(ContentControl.ContentProperty, null);
		}
	}

	/// <summary>
	/// Updates the Text property.
	/// </summary>
	private void UpdateText(string value)
	{
		var currentText = Text;
		if (value != currentText)
		{
			Text = value;
		}
	}

	/// <summary>
	/// Updates the TextBox's text with the specified value and reason.
	/// </summary>
	private void UpdateTextBoxText(string value, AutoSuggestionBoxTextChangeReason reason)
	{
		if (m_tpTextBoxPart is not null)
		{
			var currentText = m_tpTextBoxPart.Text;
			if (value != currentText)
			{
				// When TextBox text is changing, we need to let user know the reason so user can respond correctly.
				// However the TextChanged event is raised asynchronously so we need to reset the reason in the TextChangedEvent.
				// Here we are sure the TextChangedEvent will be raised because the old content and new content are different.
				m_textChangeReason = reason;
				m_tpTextBoxPart.Text = value;
				m_tpTextBoxPart.SelectionStart = value?.Length ?? 0;
			}
		}
	}

	/// <summary>
	/// Tries to get the string value from a suggestion item using the TextMemberPath.
	/// </summary>
	private string TryGetSuggestionValue(object item, BindingPath pathListener)
	{
		if (item is null)
		{
			return "";
		}

		if (item is string s)
		{
			return s;
		}

		object value = item;

		if (pathListener is not null)
		{
			pathListener.DataContext = item;
			value = pathListener.Value;
		}

		return value?.ToString() ?? "";
	}

	#endregion

	#region Items Changed Handling

	/// <summary>
	/// Called when ItemsSource is set, or when the ItemsSource collection changes.
	/// </summary>
	private void OnItemsChanged_Internal(object e)
	{
		var isTextBoxFocused = IsTextBoxFocusedElement();

#if DEBUG
		var wasHandlingCollectionChange = m_handlingCollectionChange;
		m_handlingCollectionChange = true;
#endif

		try
		{
			// Call base implementation
			// base.OnItemsChanged(e);

			if (isTextBoxFocused)
			{
				// Defer the update until after change notification is fully processed.
				if (!m_deferringUpdate)
				{
					_ = Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, ProcessDeferredUpdate);
					m_deferringUpdate = true;
				}

				// We should immediately update visibility, however, since we want the value of
				// IsSuggestionListOpen to be properly updated by the time this returns.
				UpdateSuggestionListVisibility();
			}

			// TODO UNO: Raise automation events for layout invalidated
			// AutomationPeer.RaiseAutomationEvent(AutomationEvents.LayoutInvalidated);
		}
		finally
		{
#if DEBUG
			m_handlingCollectionChange = wasHandlingCollectionChange;
#endif
		}
	}

	/// <summary>
	/// Processes deferred updates after collection changes.
	/// </summary>
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

	/// <summary>
	/// Override for items source changed.
	/// </summary>
	protected override void OnItemsSourceChanged(DependencyPropertyChangedEventArgs e)
	{
		base.OnItemsSourceChanged(e);
		OnItemsChanged_Internal(e);
	}

	#endregion

	#region SIP (Soft Input Panel) Handling

	/// <summary>
	/// Internal helper function to handle the Showing event from InputPane.
	/// </summary>
	private void OnSipShowingInternal(InputPaneVisibilityEventArgs args)
	{
		if (m_tpTextBoxPart is not null && args is not null)
		{
			var sipOverlayArea = args.OccludedRect;
			var isTextBoxFocused = IsTextBoxFocusedElement();

			if (isTextBoxFocused)
			{
				m_hasFocus = true;
			}

			if (!m_isSipVisible && m_hasFocus)
			{
				m_isSipVisible = true;

				AlignmentHelper(sipOverlayArea);

				// Expands the suggestion list if there is already text in the textbox
				var text = m_tpTextBoxPart.Text;
				if (!string.IsNullOrEmpty(text))
				{
					UpdateSuggestionListVisibility();
				}
			}
		}

		ReevaluateIsOverlayVisible();
	}

	/// <summary>
	/// Internal helper function to handle the Hiding event from InputPane.
	/// </summary>
	private void OnSipHidingInternal()
	{
		m_isSipVisible = false;

		// Scroll all ScrollViewers back if we changed.
		ApplyScrollActions(hasNewScrollActions: false);

		// Reset root scroll viewer vertical offset
		if (m_wkRootScrollViewer is not null && m_wkRootScrollViewer.TryGetTarget(out var rootScrollViewer))
		{
			var verticalOffset = rootScrollViewer.VerticalOffset;
			if (verticalOffset != 0)
			{
				rootScrollViewer.ScrollToVerticalOffset(0);
			}
		}
	}

	/// <summary>
	/// Performs the alignment to the top or bottom depending on the location of the control.
	/// </summary>
	private void AlignmentHelper(Rect sipOverlay)
	{
		// Query the ScrollViewer.BringIntoViewOnFocusChange property
		// If it's set to false, the control should not move
		var bringIntoViewOnFocusChange = ScrollViewer.GetBringIntoViewOnFocusChange(this);

		// In case when the app is not occluded by the SIP, we should just calculate the max suggestion area same way as if SIP is not deployed.
		if (sipOverlay.Y == 0)
		{
			MaximizeSuggestionAreaWithoutInputPane();
			return;
		}

		if (m_wkRootScrollViewer is null || !m_wkRootScrollViewer.TryGetTarget(out _))
		{
			return;
		}

		if (bringIntoViewOnFocusChange)
		{
			if (m_scrollActions.Count == 0)
			{
				if (m_wkRootScrollViewer.TryGetTarget(out var rootScrollViewer))
				{
					var transform = TransformToVisual(rootScrollViewer);
					var point = transform.TransformPoint(new Point(0, 0));

					var actualTextBoxHeight = m_tpTextBoxPart?.ActualHeight ?? 0;
					var bottomY = point.Y + actualTextBoxHeight;

					// Updates the visual state of the ASB depending on the orientation
					// ChangeVisualState();

					var layoutBounds = GetAdjustedLayoutBounds();
					MaximizeSuggestionArea(point.Y, bottomY, sipOverlay.Y, layoutBounds);
				}
			}
			else
			{
				ApplyScrollActions(hasNewScrollActions: true);
			}

			UpdateSuggestionListPosition();
			UpdateSuggestionListSize();
		}
	}

	/// <summary>
	/// Maximizes the suggestion list area if the AutoMaximizeSuggestionArea is enabled.
	/// </summary>
	private void MaximizeSuggestionArea(double topY, double bottomY, double sipOverlayY, Rect layoutBounds)
	{
		var autoMaximizeSuggestionArea = AutoMaximizeSuggestionArea;

		GetCandidateWindowPopupAdjustment(
			ignoreSuggestionListPosition: true,
			out _,
			out double candidateWindowYOffset);

		// Distance from top of ASB (or candidate window, whichever is higher) to bottom of system chrome
		var deltaTop = topY + (candidateWindowYOffset < 0 ? candidateWindowYOffset : 0) - layoutBounds.Y;
		// Distance from bottom of ASB (or candidate window, whichever is lower) to top of SIP
		var deltaBottom = sipOverlayY - (bottomY + (candidateWindowYOffset > 0 ? candidateWindowYOffset : 0));

		if (deltaBottom < 0)
		{
			var actualTextBoxHeight = bottomY - topY;

			// Scrolls the textbox up above the SIP if it is covered by the SIP
			deltaBottom *= -1;
			Scroll(ref deltaBottom);
			m_suggestionListPosition = SuggestionListPosition.Above;

			m_availableSuggestionHeight = sipOverlayY - (deltaBottom + actualTextBoxHeight + layoutBounds.Y);
		}
		else if (autoMaximizeSuggestionArea &&
				 (deltaTop < MinSuggestionListHeight && deltaBottom < MinSuggestionListHeight))
		{
			var actualTextBoxHeight = bottomY - topY;

			Scroll(ref deltaTop);

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

		// Set availableHeight to zero if there is no space left on the screen
		if (m_availableSuggestionHeight < 0)
		{
			m_availableSuggestionHeight = 0;
		}
	}

	/// <summary>
	/// Maximizes suggestion area without input pane.
	/// </summary>
	private void MaximizeSuggestionAreaWithoutInputPane()
	{
		try
		{
			var layoutBounds = GetAdjustedLayoutBounds();

			UIElement referenceElement = null;
			if (m_wkRootScrollViewer is not null && m_wkRootScrollViewer.TryGetTarget(out var rootScrollViewer))
			{
				referenceElement = rootScrollViewer;
			}

			var point = new Point(0, 0);
			if (referenceElement is not null)
			{
				var transform = TransformToVisual(referenceElement);
				point = transform.TransformPoint(point);
			}

			var actualTextBoxHeight = m_tpTextBoxPart?.ActualHeight ?? 0;
			var topY = point.Y;
			var bottomY = point.Y + actualTextBoxHeight;

			var autoMaximizeSuggestionArea = AutoMaximizeSuggestionArea;

			GetCandidateWindowPopupAdjustment(
				ignoreSuggestionListPosition: true,
				out _,
				out double candidateWindowYOffset);

			// Distance from top of ASB to bottom of system chrome
			var deltaTop = topY + (candidateWindowYOffset < 0 ? candidateWindowYOffset : 0) - layoutBounds.Y;
			// Distance from bottom of ASB to the bottom of the layout bounds
			var deltaBottom = layoutBounds.Height - (bottomY + (candidateWindowYOffset > 0 ? candidateWindowYOffset : 0));

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

			// Set availableHeight to zero if there is no space left on the screen
			if (m_availableSuggestionHeight < 0)
			{
				m_availableSuggestionHeight = 0;
			}
		}
		catch
		{
			// Fallback to reasonable defaults
			m_suggestionListPosition = SuggestionListPosition.Below;
			m_availableSuggestionHeight = 300;
		}
	}

	#endregion

	#region Scroll Handling

	/// <summary>
	/// Walks up the visual tree and finds all ScrollViewers, tries to scroll them up or down.
	/// </summary>
	private void Scroll(ref double totalOffset)
	{
		m_scrollActions.Clear();

		DependencyObject current = this;
		double previousYLocation = 0;

		while (current is not null)
		{
			var parent = VisualTreeHelper.GetParent(current);

			if (parent is ScrollViewer scrollViewer)
			{
				var transform = TransformToVisual(scrollViewer);
				var asbLocation = transform.TransformPoint(new Point(0, 0));

				asbLocation.Y -= (float)previousYLocation;
				var partialOffset = asbLocation.Y;

				if (asbLocation.Y < totalOffset)
				{
					PushScrollAction(scrollViewer, ref partialOffset);
					totalOffset -= asbLocation.Y - partialOffset;
				}
				else
				{
					PushScrollAction(scrollViewer, ref totalOffset);
				}

				previousYLocation = asbLocation.Y - partialOffset;
			}

			current = parent;
		}

		ApplyScrollActions(hasNewScrollActions: true);
	}

	/// <summary>
	/// Push scroll actions for the given ScrollViewer to the internal ScrollAction list.
	/// </summary>
	private void PushScrollAction(ScrollViewer scrollViewer, ref double targetOffset)
	{
		var verticalOffset = scrollViewer.VerticalOffset;
		var scrollableHeight = scrollViewer.ScrollableHeight;

		var action = new ScrollAction
		{
			WkScrollViewer = new WeakReference<ScrollViewer>(scrollViewer),
			Initial = verticalOffset
		};

		if (targetOffset + verticalOffset > scrollableHeight)
		{
			action.Target = scrollableHeight;
			targetOffset -= scrollableHeight - verticalOffset;
		}
		else
		{
			action.Target = targetOffset + verticalOffset;

			if (action.Target < 0)
			{
				action.Target = 0;
				targetOffset += verticalOffset;
			}
			else
			{
				targetOffset = 0;
			}
		}

		m_scrollActions.Add(action);
	}

	/// <summary>
	/// Applies scroll actions to move this control to desired position or revert back to original position.
	/// </summary>
	private void ApplyScrollActions(bool hasNewScrollActions)
	{
		if (m_wkRootScrollViewer is null || !m_wkRootScrollViewer.TryGetTarget(out var rootScrollViewer))
		{
			return;
		}

		foreach (var action in m_scrollActions)
		{
			if (action.WkScrollViewer.TryGetTarget(out var scrollViewer))
			{
				var offset = hasNewScrollActions ? action.Target : action.Initial;

				if (scrollViewer == rootScrollViewer)
				{
					scrollViewer.ScrollToVerticalOffset(offset);
				}
				else
				{
					scrollViewer.ChangeView(null, offset, null, disableAnimation: false);
				}
			}
		}

		if (!hasNewScrollActions)
		{
			m_scrollActions.Clear();
		}
	}

	/// <summary>
	/// Hooks to the root ScrollViewer.
	/// </summary>
	private void HookToRootScrollViewer()
	{
		m_wkRootScrollViewer = null;

		DependencyObject current = this;
		ScrollViewer rootScrollViewer = null;

		while (current is not null)
		{
			var parent = VisualTreeHelper.GetParent(current);

			if (parent is ScrollViewer sv)
			{
				rootScrollViewer = sv;
			}

			current = parent;
		}

		if (rootScrollViewer is not null)
		{
			m_wkRootScrollViewer = new WeakReference<ScrollViewer>(rootScrollViewer);
		}
	}

	#endregion

	#region Automation

	/// <summary>
	/// Creates an automation peer for this control.
	/// </summary>
	protected override AutomationPeer OnCreateAutomationPeer()
	{
		return new AutoSuggestBoxAutomationPeer(this);
	}

	/// <summary>
	/// Sets the current controlled peer for accessibility.
	/// </summary>
	private void SetCurrentControlledPeer(ControlledPeer peer)
	{
		if (m_tpTextBoxPart is not null)
		{
			UIElement peerElement = peer switch
			{
				ControlledPeer.SuggestionsList => m_tpSuggestionsPart as UIElement,
				_ => null
			};

			var controlledPeers = AutomationProperties.GetControlledPeers(m_tpTextBoxPart);
			controlledPeers?.Clear();

			if (peerElement is not null)
			{
				controlledPeers?.Add(peerElement);
			}

			// TODO UNO: Raise automation property changed event for ControlledPeers
			// This requires AutomationProperty.LookupById which is not available in Uno
		}
	}

	/// <summary>
	/// Gets the plain text representation of the control for accessibility.
	/// </summary>
	internal new string GetPlainText()
	{
		var header = Header;
		if (header is not null)
		{
			return header.ToString();
		}
		return null;
	}

	#endregion

	#region Overlay State

	/// <summary>
	/// Reevaluates whether the overlay should be visible.
	/// </summary>
	private void ReevaluateIsOverlayVisible()
	{
		if (!IsLoaded)
		{
			return;
		}

		// TODO UNO: Implement LightDismissOverlayHelper
		// bool isOverlayVisible = LightDismissOverlayHelper.ResolveIsOverlayVisibleForControl(this);

		bool isOverlayVisible = false;
		isOverlayVisible &= IsSuggestionListOpen; // Overlay should only be visible when the suggestion list is.
		isOverlayVisible &= !s_sipIsOpen;          // Except if the SIP is also visible.

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

	/// <summary>
	/// Sets up the overlay state.
	/// </summary>
	private void SetupOverlayState()
	{
		// TODO UNO: Implement overlay state setup if needed
	}

	/// <summary>
	/// Tears down the overlay state.
	/// </summary>
	private void TeardownOverlayState()
	{
		// TODO UNO: Implement overlay state teardown if needed
	}

	#endregion

	#region Helper Methods

	/// <summary>
	/// Gets the current text changed counter for CheckCurrent method support.
	/// </summary>
	internal uint GetTextChangedCounter() => m_textChangedCounter;

	/// <summary>
	/// Gets the adjusted layout bounds.
	/// </summary>
	private Rect GetAdjustedLayoutBounds()
	{
		var layoutBounds = XamlRoot?.VisualTree.VisibleBounds ?? default;
		return layoutBounds;
	}

	/// <summary>
	/// Gets the candidate window popup adjustment.
	/// </summary>
	private void GetCandidateWindowPopupAdjustment(
		bool ignoreSuggestionListPosition,
		out double xOffset,
		out double yOffset)
	{
		xOffset = 0.0;
		yOffset = 0.0;

		var textBoxWidth = m_tpTextBoxPart?.ActualWidth ?? 0;
		var textBoxHeight = m_tpTextBoxPart?.ActualHeight ?? 0;

		var shouldOffsetInXDirection = (m_candidateWindowBoundsRect.X + m_candidateWindowBoundsRect.Width) < (textBoxWidth / 2);

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
				yOffset = m_candidateWindowBoundsRect.Y - textBoxHeight + m_candidateWindowBoundsRect.Height;
			}
		}
	}

	// GetKeyboardModifiers is inherited from Control

	/// <summary>
	/// Checks if the key is gamepad navigation input.
	/// </summary>
	private static bool IsGamepadNavigationInput(VirtualKey key)
	{
		return key switch
		{
			VirtualKey.GamepadA or
			VirtualKey.GamepadB or
			VirtualKey.GamepadX or
			VirtualKey.GamepadY or
			VirtualKey.GamepadDPadUp or
			VirtualKey.GamepadDPadDown or
			VirtualKey.GamepadDPadLeft or
			VirtualKey.GamepadDPadRight or
			VirtualKey.GamepadLeftShoulder or
			VirtualKey.GamepadRightShoulder or
			VirtualKey.GamepadLeftTrigger or
			VirtualKey.GamepadRightTrigger or
			VirtualKey.GamepadLeftThumbstickUp or
			VirtualKey.GamepadLeftThumbstickDown or
			VirtualKey.GamepadLeftThumbstickLeft or
			VirtualKey.GamepadLeftThumbstickRight or
			VirtualKey.GamepadRightThumbstickUp or
			VirtualKey.GamepadRightThumbstickDown or
			VirtualKey.GamepadRightThumbstickLeft or
			VirtualKey.GamepadRightThumbstickRight => true,
			_ => false
		};
	}

	/// <summary>
	/// Updates description visibility.
	/// </summary>
	private void UpdateDescriptionVisibility(bool initialization)
	{
		if (initialization && Description is null)
		{
			// Avoid loading DescriptionPresenter element in template if not needed.
			return;
		}

		var descriptionPresenter = GetTemplateChild(c_DescriptionPresenterName) as ContentPresenter;
		if (descriptionPresenter is not null)
		{
			descriptionPresenter.Visibility = Description is not null ? Visibility.Visible : Visibility.Collapsed;
		}
	}

	/// <summary>
	/// Internal method to set IsSuggestionListOpen without taking focus.
	/// </summary>
	private void SetIsSuggestionListOpenInternal(bool value)
	{
		// Set the property without the custom logic that takes focus
		SetValue(IsSuggestionListOpenProperty, value);
	}

	#endregion

#endif
}

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using DirectUI;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Uno.Disposables;
using Uno.UI.Xaml.Core;
using Uno.UI.Xaml.Input;
using Windows.Foundation;
using Windows.System;

namespace Microsoft.UI.Xaml.Controls;

partial class ComboBox
{
	private const int s_itemCountThreashold = 5;

	private void PrepareState()
	{
		//base.PrepareState();
		TemplateSettings = new();
		TemplateSettings.SelectedItemDirection = AnimationDirection.Top;
	}

	private void SetupEditableMode()
	{
		if (m_isEditModeConfigured || m_tpEditableTextPart is null)
		{
			return;
		}

		var selectedItem = SelectedItem;
		UpdateEditableTextBox(selectedItem, false /*selectText*/, false /*selectAll*/);

		var pEditableTextPartAsTextBox = m_tpEditableTextPart;

		// We make the TextBox visible so that UIA clients can identify this one as an editable
		// ComboBox, but we keep the TextBox invisible and disabled until is actually needed.
		pEditableTextPartAsTextBox.Visibility = Visibility.Visible;
		pEditableTextPartAsTextBox.Width = 0.0f;
		pEditableTextPartAsTextBox.Height = 0.0f;
		pEditableTextPartAsTextBox.PreviewKeyDown += OnTextBoxPreviewKeyDown; // TODO:MZ: Missing support for PreviewKeyDown - problem?
		m_spEditableTextPreviewKeyDownHandler.Disposable = Disposable.Create(() => pEditableTextPartAsTextBox.PreviewKeyDown -= OnTextBoxPreviewKeyDown);

		pEditableTextPartAsTextBox.KeyDown += OnKeyDownPrivate;
		m_spEditableTextKeyDownHandler.Disposable = Disposable.Create(() => pEditableTextPartAsTextBox.KeyDown -= OnKeyDownPrivate);

		pEditableTextPartAsTextBox.TextChanged += OnTextBoxTextChanged;
		m_spEditableTextTextChangedHandler.Disposable = Disposable.Create(() => pEditableTextPartAsTextBox.TextChanged -= OnTextBoxTextChanged);

		pEditableTextPartAsTextBox.CandidateWindowBoundsChanged += OnTextBoxCandidateWindowBoundsChanged;
		m_spEditableTextCandidateWindowBoundsChangedEventHandler.Disposable = Disposable.Create(() => pEditableTextPartAsTextBox.CandidateWindowBoundsChanged -= OnTextBoxCandidateWindowBoundsChanged);

		pEditableTextPartAsTextBox.SizeChanged += OnTextBoxSizeChanged;
		m_spEditableTextSizeChangedHandler.Disposable = Disposable.Create(() => pEditableTextPartAsTextBox.SizeChanged -= OnTextBoxSizeChanged);

		var pointerPressedHandler = new PointerEventHandler(OnTextBoxPointerPressedPrivate);
		pEditableTextPartAsTextBox.AddHandler(PointerPressedEvent, pointerPressedHandler, true /* handledEventsToo */);
		m_spEditableTextPointerPressedEventHandler.Disposable = Disposable.Create(() => pEditableTextPartAsTextBox.RemoveHandler(PointerPressedEvent, pointerPressedHandler));

		var tappedHandler = new TappedEventHandler(OnTextBoxTapped);
		pEditableTextPartAsTextBox.AddHandler(TappedEvent, tappedHandler, true /* handledEventsToo */);
		m_spEditableTextTappedEventHandler.Disposable = Disposable.Create(() => pEditableTextPartAsTextBox.RemoveHandler(TappedEvent, tappedHandler));

		if (m_tpDropDownOverlayPart is not null)
		{
			m_tpDropDownOverlayPart.PointerEntered += OnDropDownOverlayPointerEntered;
			m_spDropDownOverlayPointerEnteredHandler.Disposable = Disposable.Create(() => m_tpContentPresenterPart.PointerEntered -= OnDropDownOverlayPointerEntered);

			m_tpDropDownOverlayPart.PointerExited += OnDropDownOverlayPointerExited;
			m_spDropDownOverlayPointerExitedHandler.Disposable = Disposable.Create(() => m_tpContentPresenterPart.PointerExited -= OnDropDownOverlayPointerExited);

			m_tpDropDownOverlayPart.Visibility = Visibility.Visible;
		}

		// Tells the selector to allow Custom Values.
		SetAllowCustomValues(true /*allow*/);

		m_restoreIndexSet = false;
		m_indexToRestoreOnCancel = -1;
		ResetSearch();
		ResetSearchString();

		wrl::ComPtr<xaml_controls::IInputValidationContext> context;
		get_ValidationContext(&context));
		pEditableTextPartAsTextBox.ValidationContext(context.Get()));

		wrl::ComPtr<xaml_controls::IInputValidationCommand> command;
		get_ValidationCommand(&command));
		pEditableTextPartAsTextBox->put_ValidationCommand(command.Get()));

		if (m_tpPopupPart is not null)
		{
			m_tpPopupPart.OverlayInputPassThroughElement = this;
		}

		m_isEditModeConfigured = true;
	}

	private void DisableEditableMode()
	{
		if (!m_isEditModeConfigured || m_tpEditableTextPart is null)
		{
			return;
		}

		if (m_tpPopupPart is not null)
		{
			m_tpPopupPart.OverlayInputPassThroughElement = null;
		}

		var pEditableTextPartAsTextBox = m_tpEditableTextPart;

		// We hide the TextBox in order tom_tpEditableTextPart. prevent UIA clients from thinking this is an editable ComboBox.
		pEditableTextPartAsTextBox.Visibility = Visibility.Collapsed;
		pEditableTextPartAsTextBox.Width = 0.0f;
		pEditableTextPartAsTextBox.Height = 0.0f;

		m_spEditableTextPreviewKeyDownHandler.Disposable = null;
		m_spEditableTextKeyDownHandler.Disposable = null;
		m_spEditableTextTextChangedHandler.Disposable = null;
		m_spEditableTextCandidateWindowBoundsChangedEventHandler.Disposable = null;
		m_spEditableTextSizeChangedHandler.Disposable = null;

		if (m_tpDropDownOverlayPart is not null)
		{
			var pDropDownOverlayPartAsI = iinspectable_cast(m_tpDropDownOverlayPart.Cast<Border>());

			m_spDropDownOverlayPointerEnteredHandler.DetachEventHandler(pDropDownOverlayPartAsI));
			m_spDropDownOverlayPointerExitedHandler.DetachEventHandler(pDropDownOverlayPartAsI));

			m_tpDropDownOverlayPart.Visibility = Visibility.Collapsed;
		}

		m_spEditableTextPointerPressedEventHandler.Disposable = null;
		m_spEditableTextTappedEventHandler.Disposable = null;

		ResetSearch();
		ResetSearchString();
		m_selectAllOnTouch = false;
		m_openPopupOnTouch = false;
		m_shouldMoveFocusToTextBox = false;
		m_restoreIndexSet = false;
		m_indexToRestoreOnCancel = -1;

		if (m_customValueRef is not null)
		{
			m_customValueRef = null;
			SetContentPresenter(-1);
			SelectedItem = null;
		}

		// Tells the selector to prevent Custom Values.
		SetAllowCustomValues(false /*allow*/);

		m_tpEditableTextPart.Text = null;

		m_isEditModeConfigured = false;
	}

	private protected override void ChangeVisualState(bool useTransitions)
	{
		bool isPointerOver = m_IsPointerOverMain || m_IsPointerOverPopup;
		bool isEnabled = IsEnabled;
		bool isSelectionActive = IsSelectionActive;
		bool isDropDownOpen = IsDropDownOpen;
		int selectedIndex = SelectedIndex;

		var focusManager = VisualTree.GetFocusManagerForElement(this);

		// Ingores pressed visual over the entire control when pointer is over the DropDown button used for Editable Mode.
		bool ignorePressedVisual = false;

		// EditableModeStates VisualStateGroup.
		if (IsEditable)
		{
			bool editableTextHasFocus = EditableTextHasFocus();

			if (m_IsPointerOverDropDownOverlay)
			{
				if (m_bIsPressed)
				{
					ignorePressedVisual = true;
					GoToState(useTransitions, editableTextHasFocus ? "TextBoxFocusedOverlayPressed" : "TextBoxOverlayPressed");
				}
				else
				{
					GoToState(useTransitions, editableTextHasFocus ? "TextBoxFocusedOverlayPointerOver" : "TextBoxOverlayPointerOver");
				}
			}
			else
			{
				GoToState(useTransitions, editableTextHasFocus ? "TextBoxFocused" : "TextBoxUnfocused");
			}
		}

		if (!isEnabled)
		{
			GoToState(useTransitions, "Disabled");
		}
		else if (IsInline && isDropDownOpen)
		{
			GoToState(useTransitions, "Highlighted");
		}
		else if (m_bIsPressed && !ignorePressedVisual)
		{
			GoToState(useTransitions, "Pressed");
		}
		else if (isPointerOver)
		{
			GoToState(useTransitions, "PointerOver");
		}
		else
		{
			GoToState(useTransitions, "Normal");
		}

		// FocusStates VisualStateGroup.
		if (!isSelectionActive || !isEnabled || (focusManager?.IsPluginFocused() == false))
		{
			GoToState(useTransitions, "Unfocused");
		}
		else if (isDropDownOpen)
		{
			GoToState(useTransitions, "FocusedDropDown");
		}
		else
		{
			var focusVisualState = FocusState switch
			{
				FocusState.Unfocused => "Unfocused",
				FocusState.Pointer => "PointerFocused",
				_ when m_bIsPressed => "FocusedPressed",
				_ => "Focused",
			};

			GoToState(useTransitions, focusVisualState);
		}

		// PresenterStates VisualStateGroup.
		if (!IsInline)
		{
			// Either inline mode is not supported based on the template parts available,
			// or the number of items is too large for use of inline mode.
			GoToState(useTransitions, "Full");
		}
		else if (m_bIsPressed || isDropDownOpen || m_isDropDownClosing || selectedIndex >= 0)
		{
			GoToState(useTransitions, "InlineNormal");
		}
		else
		{
			// drop-down is fully closed and nothing is selected
			GoToState(useTransitions, "InlinePlaceholder");
		}

		// DropDownStates VisualStateGroup.
		if (!IsFullMode())
		{
			if (m_isDropDownClosing)
			{
				GoToState(useTransitions, "Closed");
			}
			else if (isDropDownOpen && (IsSmallFormFactor || m_bPopupHasBeenArrangedOnce))
			{
				// Note: The non-small-form-factor combo box uses a popup to display it's
				// items. We can't transition to the Opened state until the popup has been
				// laid out.
				GoToState(useTransitions, "Opened");
			}
		}

		EnsureValidationVisuals();
	}

	internal override void OnPropertyChanged2(DependencyPropertyChangedEventArgs args)
	{
		base.OnPropertyChanged2(args);

		// TODO Uno: Only IsEditable logic for now.
		if (args.Property == HeaderProperty || args.Property == HeaderTemplateProperty)
		{
			UpdateHeaderPresenterVisibility();
		}
		if (args.Property == TextProperty)
		{
			if (m_tpEditableTextPart is not null && IsEditable)
			{
				var text = Text;

				UpdateEditableContentPresenterTextBlock(text);
			}
		}
		else if (args.Property == IsEditableProperty)
		{
			var isEditable = IsEditable;
			if (isEditable)
			{
				SetupEditableMode();
				CreateEditableContentPresenterTextBlock();
			}
			else
			{
				DisableEditableMode();
			}
		}
		else if (args.Property == SelectedItemProperty)
		{
			if (IsEditable)
			{
				int selectedIndex = SelectedIndex;
				var spSelectedItem = SelectedItem;

				SetSearchResultIndex(selectedIndex);

				// If SelectedItem is a custom value (is valid and has index -1) keep a reference to it. We need this to allow
				// reverting to this value in case selection changes.
				if (spSelectedItem is not null && selectedIndex == -1)
				{
					m_customValueRef = spSelectedItem;
				}

				UpdateEditableTextBox(spSelectedItem, true /*selectText*/, true /*selectAll*/);

				bool isDropDownOpen = IsDropDownOpen;

				// In the case a user has not typed in a custom value, we commit the search and reset the selected index when the drop-down closes,
				// but if it's already closed, that won't happen.  In that case, let's do that now.
				if ((m_searchResultIndex > -1) && !isDropDownOpen)
				{
					CommitRevertEditableSearch(false /* restoreValue */);
				}
			}
		}
	}

	private void UpdateEditableTextBox(object? item, bool selectText, bool selectAll)
	{
		if (item is null)
		{
			return;
		}

		EnsurePropertyPathListener();
		var strItem = TryGetStringValue(item/*, m_spPropertyPathListener.Get()*/);

		UpdateEditableTextBox(strItem, selectText, selectAll);
	}

	private void UpdateEditableTextBox(string str, bool selectText, bool selectAll)
	{
		if (str is null)
		{
			return;
		}

		if (m_tpEditableTextPart is not null)
		{
			string textBoxText = m_tpEditableTextPart.Text;

			if (AreStringsEqual(str, textBoxText))
			{
				return;
			}

			m_searchString = str;

			if (selectAll)
			{
				// Selects all the text.
				m_tpEditableTextPart.Text = m_searchString;

				if (selectText)
				{
					m_tpEditableTextPart.SelectAll();
				}
			}
			else
			{
				// Selects auto-completed text for quick replacement.
				int selectionStart = m_tpEditableTextPart.SelectionStart;
				m_tpEditableTextPart.Text = m_searchString;

				if (selectText)
				{
					m_tpEditableTextPart.Select(selectionStart, str.Length - selectionStart);
				}
			}
		}
	}

	private void UpdateSelectionBoxHighlighted()
	{
		bool isDropDownOpen = IsDropDownOpen;
		var hasFocus = HasFocus();
		var value = isDropDownOpen && hasFocus;
		IsSelectionBoxHighlighted = value;
	}


	private void FocusChanged(bool hasFocus)
	{
		// The OnGotFocus & OnLostFocus are asynchronous and cannot reliably tell you that have the focus.  All they do is
		// let you know that the focus changed sometime in the past.  To determine if you currently have the focus you need
		// to do consult the FocusManager (see HasFocus()).
		UpdateSelectionBoxHighlighted();
		IsSelectionActive = hasFocus;

		if (!hasFocus && !IsFullMode())
		{
			m_isClosingDueToCancel = true;
			IsDropDownOpen = false;
		}

		UpdateVisualState();
	}

	internal override bool HasFocus()
	{
		var pbHasFocus = false;

		if (VisualTree.GetFocusManagerForElement(this) is { } focusManager)

		{
			if (focusManager.FocusedElement is { } spFocused)
			{
				pbHasFocus = IsChildOfTarget(spFocused, true, false);
			}
		}

		return pbHasFocus;
	}

	private bool EditableTextHasFocus()
	{
		if (m_tpEditableTextPart is not null)
		{
			var focusManager = VisualTree.GetFocusManagerForElement(this);
			return (m_tpEditableTextPart == focusManager?.FocusedElement);
		}
		else
		{
			return false;
		}
	}

	private void EnsureTextBoxIsEnabled(bool moveFocusToTextBox)
	{
		if (m_tpEditableTextPart is not null)
		{
			m_tpEditableTextPart.Width = DoubleUtil.NaN;
			m_tpEditableTextPart.Height = DoubleUtil.NaN;

			if (m_tpContentPresenterPart is not null)
			{
				m_tpContentPresenterPart.Visibility = Visibility.Collapsed;
			}

			if (moveFocusToTextBox)
			{
				var isSuccessful = m_tpEditableTextPart.Focus(FocusState.Programmatic);

				m_shouldMoveFocusToTextBox = false;
			}
		}
	}

	internal override TabStopProcessingResult ProcessTabStopOverride(
		DependencyObject? focusedElement,
		DependencyObject? candidateTabStopElement,
		bool isBackward,
		bool didCycleFocusAtRootVisualScope)
	{
		DependencyObject? ppNewTabStop = null;
		bool pIsTabStopOverridden = false;
		// An editable ComboBox has special tab behavior. We want to be able to Tab a single time
		// directly into the TextBox and to Shift + Tab a single time to move focus outside to a
		// different Control, if applicable. In this sense, it is similar to AutoSuggestBox, where the
		// TextBox inside is a tab stop but the AutoSuggestBox itself is not (see IsTabStop is set to
		// false in the AutoSuggestBox's Style). However, contrary to AutoSuggestBox, an editable
		// ComboBox must actually appear as two separate tab stops when using a Gamepad. This is to
		// support behavior such as pressing B while the TextBox is focused, which should move focus
		// back to the ComboBox itself (which would not be possible if the ComboBox had IsTabStop set
		// to false). Given this, we will manipulate the tab stops to force backward navigation to skip
		// the ComboBox.
		if (IsEditable && m_tpEditableTextPart is not null && isBackward)
		{
			var contentRoot = VisualTree.GetContentRootForElement(this);
			var lastInputDeviceType = contentRoot?.InputManager.LastInputDeviceType;
			DependencyObject? newTabStop;

			if (lastInputDeviceType == InputDeviceType.Keyboard)
			{
				newTabStop = contentRoot?.FocusManager.GetPreviousTabStop(this);

				// If we found a candidate, then query its corresponding peer.
				if (newTabStop is not null)
				{
					ppNewTabStop = newTabStop;
					pIsTabStopOverridden = true;
				}
			}
		}

		return new(pIsTabStopOverridden, ppNewTabStop);
	}

	protected override void OnGotFocus(RoutedEventArgs e)
	{
		base.OnGotFocus(e);

		var hasFocus = HasFocus();
		FocusChanged(hasFocus);

		if (IsEditable && m_tpEditableTextPart is not null)
		{
			var contentRoot = VisualTree.GetContentRootForElement(this);
			var lastPointerType = contentRoot?.InputManager.LastInputDeviceType;

			// If EditableText is not focused, make the control visible, focus and return. Next time we receive OnGotFocus we will setup the control.
			if (EditableTextHasFocus())
			{
				m_shouldMoveFocusToTextBox = false;
				EnsureTextBoxIsEnabled(false /* moveFocusToTextBox */);
			}
			else
			{
				// For gamepad TextBox should only be visible when popup is open.
				if (lastPointerType == InputDeviceType.GamepadOrRemote)
				{
					return;
				}

				var editableTextPartWidth = m_tpEditableTextPart.Width;
				var editableTextPartHeight = m_tpEditableTextPart.Height;

				if ((editableTextPartWidth == 0 && editableTextPartHeight == 0) || ShouldMoveFocusToTextBox)
				{
					EnsureTextBoxIsEnabled(true /* moveFocusToTextBox */);
				}

				return;
			}

			if (lastPointerType == InputDeviceType.Touch)
			{
				var str = m_tpEditableTextPart.Text;
				m_tpEditableTextPart.SelectionStart = str.Length;

				// According to the interaction model design, after the control is focused we need to select all the text next time it is touched.
				m_selectAllOnTouch = true;
			}
			else
			{
				m_tpEditableTextPart.SelectAll();

				// ProcessSearch when TextBox gains focus, this ensures TextBox.Text matches an item or a custom value index even if Text was modified when control wasn't focused.
				ProcessSearch(' ');
			}

			bool popupIsOpen = true;

			if (m_tpPopupPart is not null)
			{
				popupIsOpen = m_tpPopupPart.IsOpen;
			}

			// On Touch open DropDown when getting focus.
			if (m_openPopupOnTouch && !popupIsOpen && lastPointerType == InputDeviceType.Touch)
			{
				m_inputDeviceTypeUsedToOpen = InputDeviceType.Touch;
				IsDropDownOpen = true;
			}

			m_openPopupOnTouch = false;
		}
	}

	protected override void OnLostFocus(RoutedEventArgs e)
	{
		base.OnLostFocus(e);

		var hasFocus = HasFocus();
		FocusChanged(hasFocus);

		m_selectAllOnTouch = false;

		if (IsEditable)
		{
			if (!hasFocus)
			{
				// Commit the selected value.
				CommitRevertEditableSearch(false /*restoreValue*/);

				if (m_tpEditableTextPart is not null)
				{
					m_tpEditableTextPart.Width = 0.0f;
					m_tpEditableTextPart.Height = 0.0f;
					if (m_tpContentPresenterPart is not null)
					{
						m_tpContentPresenterPart.Visibility = Visibility.Visible;
					}
				}

				// When ComboBox loses focus, ensure to move the focus over to the TextBox next time ComboBox is focused.
				m_shouldMoveFocusToTextBox = true;
			}
		}
	}

	private void UpdateEditableContentPresenterTextBlock(object item)
	{
		if (item is null)
		{
			return;
		}

		EnsurePropertyPathListener();
		var itemString = TryGetStringValue(item) //, m_spPropertyPathListener); TODO Uno: Missing PropertyPathListener support
		UpdateEditableContentPresenterTextBlock(itemString);
	}

	private void UpdateEditableContentPresenterTextBlock(string text)
	{
		if (m_tpContentPresenterPart is null || m_tpEditableContentPresenterTextBlock is null || text is null)
		{
			return;
		}

		// Reset ContentPresenter in case it is storing a swapped ComboBoxItem.
		SetContentPresenter(-1);

		m_tpEditableContentPresenterTextBlock.Text = text;
		m_tpContentPresenterPart.Content = m_tpEditableContentPresenterTextBlock;

		InvokeValidationCommand(this, text);
	}

	private void CommitRevertEditableSearch(bool restoreValue)
	{
		if (m_tpEditableTextPart is null)
		{
			return;
		}

		if (restoreValue)
		{
			if (m_restoreIndexSet)
			{
				if (m_indexToRestoreOnCancel != -1)
				{
					SelectedIndex = m_indexToRestoreOnCancel;
				}
				else
				{
					// If m_indexToRestoreOnCancel is -1 this means we need to either restore to a custom value or to -1
					// check if we are holding a reference to a custom value.
					if (m_customValueRef is not null)
					{
						SelectedItem = m_customValueRef;
					}
					else
					{
						SelectedIndex = -1;
					}
				}
			}
		}
		else
		{
			// We set SearchResultIndex when typing, arrowing up/down, or if a selection change happened. In those cases we already processed the search.
			// If SearchResultIndex is not set we need to ensure the current TextBox.Text matches the correct Index in case ComboBox.Text or TextBox.Text were
			// changed programatically.
			if (!IsSearchResultIndexSet())
			{
				ProcessSearch(' ');
			}

			// If search has a match within the Data Source select the item.
			if (m_searchResultIndex > -1)
			{
				SelectedIndex = m_searchResultIndex;
				m_customValueRef = null;
			}
			// If searched value is not in the Data Source it means we are trying to commit a value outside the Data Source. Raise a CommitRequest with the new value.
			else
			{
				var searchString = m_tpEditableTextPart.Text;

				// Ensure searchString is not empty or contains only spaces.
				if (IsSearchStringValid(searchString))
				{
					bool sendEvent = true;

					if (m_customValueRef is not null)
					{
						wrl_wrappers::HString storedString;
						IValueBoxer::UnboxValue(m_customValueRef.Get(), storedString.GetAddressOf());

						// Prevent sending the event if the custom value is the same.
						sendEvent = !AreStringsEqual(storedString, searchString);
					}

					if (sendEvent)
					{
						ctl::ComPtr<IInspectable> spInspectable;
						PropertyValue::CreateFromString(searchString, &spInspectable));

						bool isHandled = RaiseTextSubmittedEvent(searchString);

						// If event was not handled we assume we want to keep the current value as active.
						if (!isHandled)
						{
							int foundIndex = SearchItemSourceIndex(' ', false /*startSearchFromCurrentIndex*/, true /*searchExactMatch*/);

							if (foundIndex != -1)
							{
								m_customValueRef = null;
								try
								{
									SelectedIndex = foundIndex;
								}
								catch (Exception ex)
								{
									// After the TextSubmittedEvent we try to match the current Custom Value with a value in our ItemSource in case the value was
									// inserted during the event. In order to select this item, it needs to exist in our ItemContainer. This will not be the case when
									// ItemSource is a List and not an ObservableCollection. Using ObservableCollection is required for this to work as our ItemContainerGenerator
									// relies on INotifyPropertyChanged to update values when the ItemSource has changed.
									// We improve the error message here to match what managed code returns when trying to set the SelectedIndex under the same conditions.
									throw new IndexOutOfRangeException("Index was out of range.", ex);
								}
							}
							else
							{
								SelectedItem = spInspectable;
							}
						}
					}
					else
					{
						int selectedIndex = SelectedIndex;

						// Ensure SelectedIndex is -1, this means the Custom Value is still active.
						if (selectedIndex != -1)
						{
							var spInspectable = searchString;

							SelectedItem = spInspectable;
						}
					}
				}
			}
		}

		var spSelectedItem = SelectedItem;

		// Update ContentPresenter.
		if (spSelectedItem is not null)
		{
			var selectedIndex = SelectedIndex;

			// If SelectedItem exists but SelectedIndex is -1 it means we are using a custom value, clear the ContentPresenter in this case.
			if (selectedIndex > -1)
			{
				SetContentPresenter(selectedIndex);
			}
			else
			{
				UpdateEditableContentPresenterTextBlock(spSelectedItem);
				SelectionBoxItem = spSelectedItem;
				SelectionBoxItemTemplate = null;
			}

			UpdateEditableTextBox(spSelectedItem, true /*selectText*/, true /*selectAll*/);
		}
		else
		{
			m_tpEditableTextPart.Text = null;
			m_tpEditableTextPart.SelectAll();
			SetContentPresenter(-1);
		}

		ResetSearch();
		m_restoreIndexSet = false;
		m_indexToRestoreOnCancel = -1;

	}

	private void ResetSearch()
	{
		m_searchResultIndexSet = false;
		m_searchResultIndex = -1;
	}

	private void SetSearchResultIndex(int index)
	{
		m_searchResultIndexSet = true;
		m_searchResultIndex = index;
	}

	private void OnKeyDownPrivate(object pSender, KeyRoutedEventArgs pArgs)
	{
		base.OnKeyDown(pArgs);

		var key = pArgs.Key;
		var originalKey = pArgs.OriginalKey;

		//Key maps to both gamepad B and Escape key
		if (m_ignoreCancelKeyDowns && key == VirtualKey.Escape)
		{
			pArgs.Handled = true;
			return;
		}

		var eventHandled = pArgs.Handled;

		if (eventHandled)
		{
			return;
		}

		var isEnabled = IsEnabled;

		if (!isEnabled)
		{
			return;
		}

		bool bIsDropDownOpen = IsDropDownOpen;
		if (bIsDropDownOpen)
		{
			PopupKeyDown(pArgs);
		}
		else
		{
			MainKeyDown(pArgs);
		}

		eventHandled = pArgs.Handled;
		m_handledGamepadOrRemoteKeyDown = eventHandled && XboxUtility.IsGamepadNavigationInput(originalKey);
	}


	private void OnTextBoxTextChanged(object sender, TextChangedEventArgs args)
	{
		//DEAD_CODE_REMOVAL
	}

	protected override void OnPointerWheelChanged(PointerRoutedEventArgs e)
	{
		if (!IsEnabled)
		{
			return;
		}

		if (HasFocus())
		{
			if (!IsDropDownOpen)
			{
				var point = e.GetCurrentPoint(this);
				var properties = point.Properties;
				var delta = properties.MouseWheelDelta;
				var selectedIndex = SelectedIndex;
				if (delta < 0)
				{
					SelectNext(ref selectedIndex);
				}
				else
				{
					SelectPrev(ref selectedIndex);
				}

				SelectedIndex = selectedIndex;
			}

			e.Handled = true;
		}

		base.OnPointerWheelChanged(e);
	}

	protected override void OnPointerEntered(PointerRoutedEventArgs e)
	{
		base.OnPointerEntered(e);

		var args = e;
		var isEventSourceTarget = IsEventSourceTarget(args);

		if (isEventSourceTarget)
		{
			m_isPointerOverMain = true;
			m_bIsPressed = false;
			UpdateVisualState();
		}
	}

	protected override void OnPointerMoved(PointerRoutedEventArgs e)
	{
		base.OnPointerMoved(e);

		var args = e;
		var isEventSourceTarget = IsEventSourceTarget(args);

		if (isEventSourceTarget)
		{
			if (!m_IsPointerOverMain)
			{
				// The pointer just entered the target area of the ComboBox
				m_IsPointerOverMain = true;
				UpdateVisualState();
			}
		}
		else if (m_IsPointerOverMain)
		{
			// The pointer just left the target area of the ComboBox
			m_IsPointerOverMain = false;
			m_bIsPressed = false;
			UpdateVisualState();
		}
	}

	protected override void OnPointerExited(PointerRoutedEventArgs e)
	{
		base.OnPointerExited(e);

		m_IsPointerOverMain = false;
		m_bIsPressed = false;
		UpdateVisualState();
	}

	protected override void OnPointerCaptureLost(PointerRoutedEventArgs e)
	{
		base.OnPointerCaptureLost(e);

		var pointer = e.Pointer;

		// For touch, we can clear PointerOver when receiving PointerCaptureLost, which we get when the finger is lifted
		// or from cancellation, e.g. pinch-zoom gesture in ScrollViewer.
		// For mouse, we need to wait for PointerExited because the mouse may still be above the ButtonBase when
		// PointerCaptureLost is received from clicking.
		var pointerPoint = e.GetCurrentPoint(null);

		var pointerDeviceType = pointerPoint.PointerDeviceType;
		if (pointerDeviceType == PointerDeviceType.Touch)
		{
			m_IsPointerOverMain = false;
		}

		m_bIsPressed = false;
		UpdateVisualState();
	}

	private void IsLeftButtonPressed(PointerRoutedEventArgs args, out bool isLeftButtonPressed, out PointerDeviceType pointerDeviceType)
	{
		var pointerPoint = args.GetCurrentPoint(this);
		var pointerProperties = pointerPoint.Properties;
		isLeftButtonPressed = pointerProperties.IsLeftButtonPressed;
		pointerDeviceType = pointerPoint.PointerDeviceType;
	}

	protected override void OnPointerPressed(PointerRoutedEventArgs args)
	{
		base.OnPointerPressed(args);

		bool isHandled = args.Handled;
		if (isHandled)
		{
			return;
		}

		var isEnabled = IsEnabled;
		if (!isEnabled)
		{
			return;
		}

		var isEventSourceTarget = IsEventSourceTarget(args);

		if (isEventSourceTarget)
		{
			IsLeftButtonPressed(args, out var bIsLeftButtonPressed, out var _);

			if (bIsLeftButtonPressed)
			{
				args.Handled = true;

				m_bIsPressed = true;

				// for "Pressed" visual state to render
				UpdateVisualState();
			}
		}

		var pointerPoint = args.GetCurrentPoint(null);
		var pointerDeviceType = pointerPoint.PointerDeviceType;

		bool popupIsOpen = true;

		if (m_tpPopupPart is not null)
		{
			popupIsOpen = m_tpPopupPart.IsOpen;
		}

		if (!popupIsOpen && pointerDeviceType == PointerDeviceType.Touch)
		{
			// Open popup after ComboBox is focused due to the PointerPressed event.
			m_openPopupOnTouch = true;
		}
	}

	private void OnTextBoxPointerPressedPrivate(object sender, PointerRoutedEventArgs args)
	{
		var spPointerPoint = args.GetCurrentPoint(null);
		if (spPointerPoint is null)
		{
			throw new InvalidOperationException("PointerPoint is null");
		}

		var pointerDeviceType = spPointerPoint.PointerDeviceType;

		var popupIsOpen = true;

		if (m_tpPopupPart is not null)
		{
			popupIsOpen = m_tpPopupPart.IsOpen;
		}

		// On Touch open DropDown when getting focus.
		if (!popupIsOpen && pointerDeviceType == PointerDeviceType.Touch)
		{
			m_inputDeviceTypeUsedToOpen = InputDeviceType.Touch;
			IsDropDownOpen = true;
		}

		args.Handled = true;
	}

	private void OnTextBoxTapped(object sender, TappedRoutedEventArgs args)
	{
		var pointerDeviceType = args.PointerDeviceType;

		if (m_selectAllOnTouch && pointerDeviceType == PointerDeviceType.Touch && m_tpEditableTextPart is not null)
		{
			m_tpEditableTextPart.SelectAll();
		}

		// Reset this flag even on mouse click
		m_selectAllOnTouch = false;

		args.Handled = true;
	}

	protected override void OnPointerReleased(PointerRoutedEventArgs args)
	{
		base.OnPointerReleased(args);

		var isHandled = args.Handled;
		if (isHandled)
		{
			return;
		}

		var isEnabled = IsEnabled;
		if (!isEnabled)
		{
			return;
		}

		var isEventSourceTarget = IsEventSourceTarget(args);
		if (isEventSourceTarget)
		{
			IsLeftButtonPressed(args, out var bIsLeftButtonPressed, out var pointerDeviceType);
			m_shouldPerformActions = (m_bIsPressed && !bIsLeftButtonPressed);

			if (m_shouldPerformActions)
			{
				m_bIsPressed = false;
				if (pointerDeviceType == PointerDeviceType.Touch)
				{
					m_inputDeviceTypeUsedToOpen = InputDeviceType.Touch;
				}
				else
				{
					m_inputDeviceTypeUsedToOpen = InputDeviceType.Mouse;
				}
			}

			var gestureFollowing = args.GestureFollowing;
			if (gestureFollowing == GestureModes.RightTapped)
			{
				// We will get a right tapped event for every time we visit here, and
				// we will visit before each time we receive a right tapped event
				return;
			}

			if (m_shouldPerformActions)
			{
				// Note that we are intentionally NOT handling the args
				// if we do not fall through here because basically we are no_opting in that case.
				args.Handled = true;
				m_bIsPressed = false;
				if (pointerDeviceType == PointerDeviceType.Touch)
				{
					m_inputDeviceTypeUsedToOpen = InputDeviceType.Touch;
				}
				else
				{
					m_inputDeviceTypeUsedToOpen = InputDeviceType.Mouse;
				}

				PerformPointerUpAction(IsDropDownOverlay(args));
			}
		}
	}

	private protected override void OnRightTappedUnhandled(RightTappedRoutedEventArgs e)
	{
		base.OnRightTappedUnhandled(e);

		var isHandled = e.Handled;
		if (isHandled)
		{
			return;
		}

		var isEventSourceTarget = IsEventSourceTarget(e);

		if (isEventSourceTarget)
		{
			PerformPointerUpAction(false /*isDropDownOverlay*/);
		}
	}

	private void PerformPointerUpAction(bool isDropDownOverlay)
	{
		if (m_shouldPerformActions)
		{
			m_shouldPerformActions = false;

			Focus(FocusState.Pointer);

			// No need to test bFocused - it is possible no focusable element is present if IsTabStop = FALSE for ComboBox
			// We use isDropDownOverlay to determine if dropdown arrow was clicked on Editable mode.
			if (!IsEditable)
			{
				IsDropDownOpen = true;
			}
			else if (isDropDownOverlay)
			{
				bool isDropDownOpen = IsDropDownOpen;

				// Open/Close the DropDown when clicking the DropDownOverlay for Editable Mode.
				IsDropDownOpen = !isDropDownOpen;
			}
		}
	}

	private void OnTextBoxPreviewKeyDown(object pSender, KeyRoutedEventArgs pArgs)
	{
		VirtualKey keyObject = pArgs.Key;

		if (keyObject == VirtualKey.Up || keyObject == VirtualKey.Down)
		{
			OnKeyDownPrivate(pSender, pArgs);
			pArgs.Handled = true;
		}
	}

	private void OnDropDownOverlayPointerEntered(object sender, PointerRoutedEventArgs args)
	{
		m_IsPointerOverDropDownOverlay = true;
		UpdateVisualState();
	}

	private void OnDropDownOverlayPointerExited(object sender, PointerRoutedEventArgs args)
	{
		m_IsPointerOverDropDownOverlay = false;
		UpdateVisualState();
	}

	private void OnTextBoxSizeChanged(object sender, SizeChangedEventArgs args) => ArrangePopup(false);

	private void OnTextBoxCandidateWindowBoundsChanged(TextBox sender, CandidateWindowBoundsChangedEventArgs args)
	{
		Rect candidateWindowBounds = args.Bounds;

		// Do nothing if the candidate windows bound did not change
		if (RectUtil.AreEqual(m_candidateWindowBoundsRect, candidateWindowBounds))
		{
			return;
		}

		m_candidateWindowBoundsRect = candidateWindowBounds;
		ArrangePopup(false);
	}

	protected override void OnItemsChanged(object e)
	{
		var oldIsInline = IsInline;
		m_itemCount = GetItemCount();
		var isDropDownOpen = IsDropDownOpen;

		base.OnItemsChanged(e);

		if (IsSmallFormFactor)
		{
			if (isDropDownOpen)
			{
				IsDropDownOpen = false;
			}
			else if (IsInline != oldIsInline)
			{
				if (oldIsInline)
				{
					var selectedIndex = SelectedIndex;
					EnsurePresenterReadyForFullMode();
					SetContentPresenter(selectedIndex);
				}
				else
				{
					EnsurePresenterReadyForInlineMode();
					ForceApplyInlineLayoutUpdate();
				}
			}
		}
	}

	private void ProcessSearch(char keyCode)
	{
		int foundIndex = -1;

		if (IsEditable)
		{
			if (m_tpEditableTextPart is null)
			{
				return;
			}

			string textBoxText = m_tpEditableTextPart.Text;

			// Don't process search if new text is equal to previous searched text.
			if (string.Equals(textBoxText, m_searchString))
			{
				return;
			}

			if (textBoxText.Length != 0)
			{
				foundIndex = SearchItemSourceIndex(keyCode, false /*startSearchFromCurrentIndex*/, false /*searchExactMatch*/);
			}
			else
			{
				m_searchString = "";
			}

			SetSearchResultIndex(foundIndex);

			var selectionChangedTrigger = SelectionChangedTrigger;

			if (selectionChangedTrigger == ComboBoxSelectionChangedTrigger.Always && foundIndex > -1)
			{
				SelectedIndex = foundIndex;
			}

			bool isDropDownOpen = IsDropDownOpen;

			// Override selected visuals only if popup is open
			if (isDropDownOpen)
			{
				OverrideSelectedIndexForVisualStates(foundIndex);
			}

			if (foundIndex >= 0)
			{
				if (isDropDownOpen)
				{
					// UNO TODO:
					//ScrollIntoView(
					//	foundIndex,
					//	false /*isGroupItemIndex*/,
					//	false /*isHeader*/,
					//	false /*isFooter*/,
					//	false /*isFromPublicAPI*/,
					//	true  /*ensureContainerRealized*/,
					//	false /*animateIfBringIntoView*/,
					//	ScrollIntoViewAlignment.Default);
				}
			}
		}
		else
		{
			foundIndex = SearchItemSourceIndex(keyCode, true /*startSearchFromCurrentIndex*/, false /*searchExactMatch*/);
			if (foundIndex >= 0)
			{
				SelectedIndex = foundIndex;
			}
		}
	}

	private int SearchItemSourceIndex(char keyCode, bool startSearchFromCurrentIndex, bool searchExactMatch)
	{
		// Get all of the ComboBox items; we'll try to convert them to strings later.
		var itemsVector = Items as IList<object>;
		int itemCount = GetItemCount();

		int searchIndex = -1;

		bool newStringCreated = false;

		// Editable ComboBox uses the text in the TextBox to search for values, Non-Editable ComboBox appends received characters to the current search string
		if (IsEditable)
		{
			if (m_tpEditableTextPart is not null)
			{
				m_searchString = m_tpEditableTextPart.Text;
			}
		}
		else
		{
			newStringCreated = AppendCharToSearchString(keyCode);
		}

		if (startSearchFromCurrentIndex)
		{
			int currentSelectedIndex = SelectedIndex;
			searchIndex = currentSelectedIndex;

			if (newStringCreated)
			{
				// If we've created a new search string, then we shouldn't search at i, but rather at i+1.
				if (searchIndex < itemCount - 1)
				{
					// We have at least one more item after this one to start searching at.
					searchIndex++;
				}
				else
				{
					// We are at the end of the list. Loop the search.
					searchIndex = 0;
				}
			}
			else
			{
				// If we just appended to the search string, then ensure that the search index is valid (>= 0)
				searchIndex = (searchIndex >= 0) ? searchIndex : 0;
			}
		}
		else
		{
			searchIndex = 0;
		}

		global::System.Diagnostics.Debug.Assert(searchIndex >= 0);

		object item;
		string strItem;
		int foundIndex = -1;

		EnsurePropertyPathListener();

		// Iterate through all of the items. Try to get a string out of the item; if it matches, break. If not, keep looking.
		// TODO: [https://task.ms/6720676] Use CoreDispatcher/BuildTree to slice TypeAhead search logic
		for (int i = 0; i < itemCount; i++)
		{
			item = itemsVector![searchIndex];

			if (item is not null)
			{
				strItem = TryGetStringValue(item/*, _propertyPathListener*/);

				if (strItem is null)
				{
					// We couldn't get the string representing this item; it doesn't make sense to continue searching because
					// we're probably not going to be able to get strings from more items in this collection.
					break;
				}

				// Trim leading spaces on the item before comparing.
				strItem = strItem.TrimStart(' ');

				// On Editable mode Backspace should only search for exact matches. This prevents auto-complete from stopping backspacing.
				if (searchExactMatch || IsEditable && keyCode == (char)8)
				{
					if (AreStringsEqual(strItem, m_searchString))
					{
						foundIndex = searchIndex;

						break;
					}
				}
				else if (StartsWithIgnoreLinguisticSemantics(strItem, m_searchString))
				{
					foundIndex = searchIndex;

					// If matching item was found auto-complete word.
					if (IsEditable)
					{
						UpdateEditableTextBox(item, true /*selectText*/, false /*selectAll*/);
					}

					break;
				}
			}

			searchIndex++;

			// If we've gotten to the end of the list, loop the search.
			if (searchIndex == itemCount)
			{
				searchIndex = 0;
			}
		}

		return foundIndex;
	}

	private bool StartsWithIgnoreLinguisticSemantics(string strSource, string strPrefix)
	{
		// The goal of this method is to return true if strPrefix is found at the start of strSource regardless of linguistic semantics.
		// For example, if we've got strSource = "wAsHINGton" and strPrefix = "Wa", we should return true from this method.
		// FindNLSStringEx will return a 0-based index into the source string if it's successful; it will return < 0 if it failed to find a match.
		// We pass in a number of flags to achieve this behavior:
		// FIND_STARTSWITH : Test to find out if the strPrefix value is the first value in the Source string.
		// NORM_IGNORECASE: Ignore case (broader than LINGUISTIC_IGNORECASE)
		// NORM_IGNOREKANATYPE: Do not differentiate between hiragana and katakana characters (corresponding chars compare as equal)
		// NORM_IGNOREWIDTH: Used in Japanese and Chinese scripts, this flag ignores the difference between half- and full-width characters
		// NORM_LINGUISTIC_CASING: Use linguistic rules for casing instead of file system rules
		// LINGUISTIC_IGNOREDIACRITIC: Ignore diacritics (Dotless Turkish i maps to dotted i).
		return strSource.StartsWith(strPrefix, StringComparison.InvariantCultureIgnoreCase);
	}

	private bool AreStringsEqual(string str1, string str2) => string.Equals(str1, str2, StringComparison.OrdinalIgnoreCase);

	private bool IsSearchStringValid(string str)
	{
		if (string.IsNullOrEmpty(str))
		{
			return false;
		}

		var trimmedStr = str.TrimStart(' ');

		return !string.IsNullOrEmpty(trimmedStr);
	}

	private bool IsInSearchingMode()
	{
		if (HasSearchStringTimedOut())
		{
			m_isInSearchingMode = false;
		}
		return IsTextSearchEnabled && m_isInSearchingMode;
	}

	private bool AppendCharToSearchString(char ch)
	{
		var createdNewString = false;
		if (HasSearchStringTimedOut())
		{
			ResetSearchString();
			createdNewString = true;
		}

		m_timeSinceLastCharacterReceived = DateTime.UtcNow;

		const int maxNumCharacters = 256;

		// Only append a new character if we're less than the max string length.
		if (m_searchString.Length <= maxNumCharacters)
		{
			m_searchString += ch;
		}
		m_isInSearchingMode = true;

		return createdNewString;
	}

	private void ResetSearchString()
	{
		m_searchString = "";
	}

	private bool RaiseTextSubmittedEvent(string text)
	{
		// Create and set event args
		ComboBoxTextSubmittedEventArgs eventArgs = new(text);

		// Raise TextSubmitted event
		TextSubmitted?.Invoke(this, eventArgs);

		return eventArgs.Handled;
	}

	private void UpdateHeaderPresenterVisibility()
	{
		var headerTemplate = HeaderTemplate;
		var header = Header;

		ConditionallyGetTemplatePartAndUpdateVisibility(
			"HeaderContentPresenter",
			header is not null || headerTemplate is not null,
			ref m_tpHeaderContentPresenterPart);

		// TODO Uno: Input validation support #4839
		//ConditionallyGetTemplatePartAndUpdateVisibility(
		//	"RequiredHeaderPresenter",
		//	(header is not null || headerTemplate is not null) && IsValueRequired(this),
		//	m_requiredHeaderContentPresenterPart);

#if HAS_UNO // TODO:MZ: Validate if this is still needed!
		if (m_tpHeaderContentPresenterPart is not null)
		{
			// On Windows, all interactions involving the HeaderContentPresenter don't seem to affect the ComboBox.
			// For example, hovering/pressing doesn't trigger the PointOver/Pressed visual states. Tapping on it doesn't open the drop down.
			// This is true even if the Background of the root of ComboBox's template (which contains the HeaderContentPresenter) is set.
			// Interaction with any other part of the control (including the root) triggers the corresponding visual states and actions.
			// It doesn't seem like the HeaderContentPresenter consumes (Handled = true) events because they are properly routed to the ComboBox.

			// My guess is that ComboBox checks whether the OriginalSource of Pointer events is a child of HeaderContentPresenter.

			// Because routed events are not implemented yet, the easy workaround is to prevent HeaderContentPresenter from being hit.
			// This only works if the background of the root of ComboBox's template is null (which is the case by default).
			m_tpHeaderContentPresenterPart.IsHitTestVisible = false;
		}
#endif
	}

	private bool IsEventSourceTarget(RoutedEventArgs args)
	{
		var originalSource = args.OriginalSource;
		return IsChildOfTarget(originalSource as DependencyObject, false, true);
	}

	// Used to determine if DropDown arrow was hit on Editable mode
	private bool IsDropDownOverlay(RoutedEventArgs args)
	{
		if (m_tpDropDownOverlayPart is null)
		{
			return false;
		}

		var originalSource = args.OriginalSource;

		var templatePart = m_tpDropDownOverlayPart;

		return (originalSource is not null && templatePart is not null && originalSource == templatePart);
	}

	private DependencyObject? pMostRecentSearchChildNoRef;
	private bool mostRecentResult;

	// Used in hit-testing for the ComboBox target area, which must exclude the header
	private bool IsChildOfTarget(
		DependencyObject? pChild,
		bool doSearchLogicalParents,
		bool doCacheResult)
	{
		// Simple perf optimization: most pointer events have the same source as the previous
		// event, so we'll cache the most recent result and reuse it whenever possible.

		bool result = mostRecentResult;
		DependencyObject? spHeaderPresenterAsDO;
		DependencyObject? spCurrentDO = pChild;
		DependencyObject pThisAsDONoRef = this;
		bool isFound = false;

		if (pChild is null)
		{
			return false;
		}

		spHeaderPresenterAsDO = m_tpHeaderContentPresenterPart as DependencyObject;

		while (spCurrentDO is not null && !isFound)
		{
			if (spCurrentDO == pMostRecentSearchChildNoRef)
			{
				// use the cached result
				isFound = true;
			}

			else if (spCurrentDO == pThisAsDONoRef)
			{
				result = true;
				isFound = true;
			}
			else if (spHeaderPresenterAsDO is not null && spCurrentDO == spHeaderPresenterAsDO)
			{
				result = false;
				isFound = true;
			}
			else
			{
				var spParentDO = VisualTreeHelper.GetParent(spCurrentDO);

				if (doSearchLogicalParents && spParentDO is PopupRoot spPopup)
				{
					// Try the logical parent. This lets us look through popup boxes
					var spCurrentAsFE = spCurrentDO as FrameworkElement;
					if (spCurrentAsFE is not null)
					{
						spParentDO = spCurrentAsFE.Parent;
					}
				}

				// refcounting note: Attach releases the previously stored ptr, and does not
				// addref the new one.
				spCurrentDO = spParentDO;
			}
		}

		if (!isFound)
		{
			result = false;
		}

		if (doCacheResult)
		{
			pMostRecentSearchChildNoRef = pChild;
			mostRecentResult = result;
		}

		return result;
	}

	private InputDeviceType GetInputDeviceTypeUsedToOpen() => m_inputDeviceTypeUsedToOpen;

	private void OverrideSelectedIndexForVisualStates(int selectedIndexOverride)
	{
		//Debug.Assert(!CanSelectMultiple);

		ClearSelectedIndexOverrideForVisualStates();

		// We only need to override the selected visual if the specified item is not
		// also the selected item.
		int selectedIndex = SelectedIndex;
		if (selectedIndexOverride != selectedIndex)
		{
			DependencyObject container;
			ComboBoxItem? comboBoxItem;

			// Force the specified override  item to appear selected.
			if (selectedIndexOverride != -1)
			{
				container = ContainerFromIndex(selectedIndexOverride);
				comboBoxItem = container as ComboBoxItem;
				if (comboBoxItem is not null)
				{
					comboBoxItem.OverrideSelectedVisualState(true /* appearSelected */);
				}
			}

			m_indexForcedToSelectedVisual = selectedIndexOverride;

			if (selectedIndex != -1)
			{
				// Force the actual selected item to appear unselected.
				container = ContainerFromIndex(selectedIndex);
				comboBoxItem = container as ComboBoxItem;
				if (comboBoxItem is not null)
				{
					comboBoxItem.OverrideSelectedVisualState(false /* appearSelected */);
				}

				m_indexForcedToUnselectedVisual = selectedIndex;
			}
		}
	}

	private void ClearSelectedIndexOverrideForVisualStates()
	{
		//Debug.Assert(!CanSelectMultiple);

		DependencyObject? container;
		ComboBoxItem? comboBoxItem;

		if (m_indexForcedToUnselectedVisual != -1)
		{
			container = ContainerFromIndex(m_indexForcedToUnselectedVisual);
			comboBoxItem = container as ComboBoxItem;
			if (comboBoxItem is not null)
			{
				comboBoxItem.ClearSelectedVisualState();
			}

			m_indexForcedToUnselectedVisual = -1;
		}

		if (m_indexForcedToSelectedVisual != -1)
		{
			container = ContainerFromIndex(m_indexForcedToSelectedVisual);
			comboBoxItem = container as ComboBoxItem;
			if (comboBoxItem is not null)
			{
				comboBoxItem.ClearSelectedVisualState();
			}

			m_indexForcedToSelectedVisual = -1;
		}
	}

	private void EnsurePropertyPathListener()
	{
		// TODO Uno: Property path listener is not implemented yet
		//if (!m_spPropertyPathListener)
		//{
		//	wrl_wrappers::HString strDisplayMemberPath;
		//	IFC_RETURN(get_DisplayMemberPath(strDisplayMemberPath.GetAddressOf()));

		//	if (!strDisplayMemberPath.IsEmpty())
		//	{
		//		// If we don't have one cached, create the property path listener
		//		// If strDisplayMemberPath contains something (a path), then use that to inform our PropertyPathListener.
		//		auto pPropertyPathParser = std::make_unique<PropertyPathParser>();

		//		IFC_RETURN(pPropertyPathParser->SetSource(WindowsGetStringRawBuffer(strDisplayMemberPath.Get(), nullptr), FALSE));

		//		IFC_RETURN(ctl::make<PropertyPathListener>(nullptr, pPropertyPathParser.get(), false /*fListenToChanges*/, false /*fUseWeakReferenceForSource*/, &m_spPropertyPathListener));
		//	}
		//}

		//return S_OK;
	}

	private void CreateEditableContentPresenterTextBlock()
	{
		if (m_tpEditableContentPresenterTextBlock is null)
		{
			var spTextBlock = new TextBlock();

			m_tpEditableContentPresenterTextBlock = spTextBlock;
		}
	}
}

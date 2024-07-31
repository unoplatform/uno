using System.Linq;
using DirectUI;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Uno.Disposables;
using Uno.UI.Xaml.Core;
using Uno.UI.Xaml.Input;
using Windows.System;

namespace Microsoft.UI.Xaml.Controls;

partial class ComboBox
{
	private const int s_itemCountThreashold = 5;

	private void SetupEditableMode()
	{
		if (m_isEditModeConfigured || m_tpEditableTextPart is null)
		{
			return;
		}

		var selectedItem = SelectedItem;
		UpdateEditableTextBox(selectedItem, false /*selectText*/, false /*selectAll*/));

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

		this.PointerPressed += OnTextBoxPointerPressedPrivate; // TODO:MZ: Should be on this on the textbox?
		m_spEditableTextPointerPressedEventHandler.Disposable = Disposable.Create(() => this.PointerPressed -= OnTextBoxPointerPressedPrivate);

		this.Tapped += OnTextBoxTapped; // TODO:MZ: Should be on this on the textbox?
		m_spEditableTextTappedEventHandler.Disposable = Disposable.Create(() => this.Tapped -= OnTextBoxTapped);

		PointerPressedEventSourceType* pPointerPressedEventSource = null;

		EditableTextPartAsTextBox->GetPointerPressedEventSourceNoRef(&pPointerPressedEventSource));
		PointerPressedEventSource->AddHandler(m_spEditableTextPointerPressedEventHandler.Get(), true /* handledEventsToo */));

		TappedEventSourceType* pTappedEventSource = null;

		pEditableTextPartAsTextBox->GetTappedEventSourceNoRef(&pTappedEventSource));
		pTappedEventSource->AddHandler(m_spEditableTextTappedEventHandler.Get(), true /* handledEventsToo */));

		if (m_tpDropDownOverlayPart is not null)
		{
			m_spDropDownOverlayPointerEnteredHandler.AttachEventHandler(
				m_tpDropDownOverlayPart.Cast<Border>(),
				std::bind(&ComboBox::OnDropDownOverlayPointerEntered, this, _1, _2)));

			m_spDropDownOverlayPointerExitedHandler.AttachEventHandler(
				m_tpDropDownOverlayPart.Cast<Border>(),
				std::bind(&ComboBox::OnDropDownOverlayPointerExited, this, _1, _2)));

			m_tpDropDownOverlayPart.Cast<Border>()->put_Visibility(xaml::Visibility_Visible));
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

		if (m_tpPopupPart)

		{
			m_tpPopupPart.Cast<Popup>()->put_OverlayInputPassThroughElement(this));
		}

		m_isEditModeConfigured = true;
	}

	private void DisableEditableMode()
	{
		if (!m_isEditModeConfigured || m_tpEditableTextPart is null)
		{
			return;
		}

		if (m_tpPopupPart)
		{
			m_tpPopupPart.Cast<Popup>()->put_OverlayInputPassThroughElement(null));
		}

		var pEditableTextPartAsTextBox = m_tpEditableTextPart;

		// We hide the TextBox in order tom_tpEditableTextPart. prevent UIA clients from thinking this is an editable ComboBox.
		pEditableTextPartAsTextBox.Visibility = Visibility.Collapsed;
		pEditableTextPartAsTextBox.Width = 0.0f;
		pEditableTextPartAsTextBox.Height = 0.0f;

		m_spEditableTextPreviewKeyDownHandler.DetachEventHandler(pEditableTextPartAsI));
		m_spEditableTextKeyDownHandler.DetachEventHandler(pEditableTextPartAsI));
		m_spEditableTextTextChangedHandler.DetachEventHandler(pEditableTextPartAsI));
		m_spEditableTextPointerPressedHandler.DetachEventHandler(pEditableTextPartAsI));
		m_spEditableTextCandidateWindowBoundsChangedEventHandler.DetachEventHandler(pEditableTextPartAsI));
		m_spEditableTextSizeChangedHandler.DetachEventHandler(pEditableTextPartAsI));

		if (m_tpDropDownOverlayPart)
		{
			auto pDropDownOverlayPartAsI = iinspectable_cast(m_tpDropDownOverlayPart.Cast<Border>());

			m_spDropDownOverlayPointerEnteredHandler.DetachEventHandler(pDropDownOverlayPartAsI));
			m_spDropDownOverlayPointerExitedHandler.DetachEventHandler(pDropDownOverlayPartAsI));

			m_tpDropDownOverlayPart.Cast<Border>()->put_Visibility(xaml::Visibility_Collapsed));
		}

		PointerPressedEventSourceType* pPointerPressedEventSource = null;

		pEditableTextPartAsTextBox->GetPointerPressedEventSourceNoRef(&pPointerPressedEventSource));
		pPointerPressedEventSource->RemoveHandler(m_spEditableTextPointerPressedEventHandler.Get()));

		TappedEventSourceType* pTappedEventSource = null;

		pEditableTextPartAsTextBox->GetTappedEventSourceNoRef(&pTappedEventSource));
		pTappedEventSource->RemoveHandler(m_spEditableTextTappedEventHandler.Get()));

		ResetSearch();
		ResetSearchString();
		m_selectAllOnTouch = false;
		m_openPopupOnTouch = false;
		m_shouldMoveFocusToTextBox = false;
		m_restoreIndexSet = false;
		m_indexToRestoreOnCancel = -1;

		if (m_customValueRef)
		{
			m_customValueRef.Reset();
			SetContentPresenter(-1));
			SelectedItem = null;
		}

		// Tells the selector to prevent Custom Values.
		SetAllowCustomValues(false /*allow*/);

		m_tpEditableTextPart.Text = null;

		m_isEditModeConfigured = false;
	}

	internal override void OnPropertyChanged2(DependencyPropertyChangedEventArgs args)
	{
		base.OnPropertyChanged2(args);

		// TODO Uno: Only IsEditable logic for now.
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
			return (m_tpEditableTextPart == focusManager.FocusedElement);
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
			m_tpContentPresenterPart.Visibility = Visibility.Collapsed;

			if (moveFocusToTextBox)
			{
				var isSuccessful = m_tpEditableTextPart.Focus(FocusState.Programmatic);

				m_shouldMoveFocusToTextBox = false;
			}
		}
	}

	internal override TabStopProcessingResult ProcessTabStopOverride(
		DependencyObject focusedElement,
		DependencyObject candidateTabStopElement,
		bool isBackward,
		bool didCycleFocusAtRootVisualScope)
	{
		DependencyObject ppNewTabStop = null;
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
			ContentRoot contentRoot = VisualTree.GetContentRootForElement(this);
			var lastInputDeviceType = contentRoot.InputManager.LastInputDeviceType;
			DependencyObject newTabStop;

			if (lastInputDeviceType == InputDeviceType.Keyboard)
			{
				newTabStop = contentRoot.FocusManager.GetPreviousTabStop(this);

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
			ContentRoot contentRoot = VisualTree.GetContentRootForElement(this);
			var lastPointerType = contentRoot.InputManager.LastInputDeviceType;

			// If EditableText is not focused, make the control visible, focus and return. Next time we receive OnGotFocus we will setup the control.
			if (EditableTextHasFocus())
			{
				m_shouldMoveFocusToTextBox = false;
				EnsureTextBoxIsEnabled(false /* moveFocusToTextBox */));
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

				if ((editableTextPartWidth == 0 && editableTextPartHeight == 0) || ShouldMoveFocusToTextBox())
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

		m_SelectAllOnTouch = false;

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
					m_tpContentPresenterPart.Visibility = Visibility.Visible;
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

		EnsurePropertyPathListener());
		TryGetStringValue(item, m_spPropertyPathListener.Get(), itemString.GetAddressOf()));
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

						bool isHandled;
						RaiseTextSubmittedEvent(searchString, &isHandled));

						// If event was not handled we assume we want to keep the current value as active.
						if (!isHandled)
						{
							int foundIndex = -1;
							SearchItemSourceIndex(" ", false /*startSearchFromCurrentIndex*/, true /*searchExactMatch*/, foundIndex));

							if (foundIndex != -1)
							{
								m_customValueRef.Reset();
								HRESULT hr = put_SelectedIndex(foundIndex);

								// After the TextSubmittedEvent we try to match the current Custom Value with a value in our ItemSource in case the value was
								// inserted during the event. In order to select this item, it needs to exist in our ItemContainer. This will not be the case when
								// ItemSource is a List and not an ObservableCollection. Using ObservableCollection is required for this to work as our ItemContainerGenerator
								// relies on INotifyPropertyChanged to update values when the ItemSource has changed.
								// We improve the error message here to match what managed code returns when trying to set the SelectedIndex under the same conditions.
								if (hr == E_INVALIDARG)
								{
									ErrorHelper::OriginateErrorUsingResourceID(E_INVALIDARG, ERROR_DEPENDENCYOBJECTCOLLECTION_OUTOFRANGE));
								}
								else
								{
									hr);
								}
							}
							else
							{
								put_SelectedItem(spInspectable.Get()));
							}
						}
					}
					else
					{
						INT selectedIndex = -1;
						get_SelectedIndex(&selectedIndex));

						// Ensure SelectedIndex is -1, this means the Custom Value is still active.
						if (selectedIndex != -1)
						{
							ctl::ComPtr<IInspectable> spInspectable;
							PropertyValue::CreateFromString(searchString, &spInspectable));

							put_SelectedItem(spInspectable.Get()));
						}
					}
				}
			}
		}

		ctl::ComPtr<IInspectable> spSelectedItem;
		get_SelectedItem(&spSelectedItem));

		// Update ContentPresenter.
		if (spSelectedItem)
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

	private void OnTextBoxPreviewKeyDown(object pSender, KeyRoutedEventArgs pArgs)
	{
		VirtualKey keyObject = pArgs.Key;

		if (keyObject == VirtualKey.Up || keyObject == VirtualKey.Down)
		{
			OnKeyDownPrivate(pSender, pArgs);
			pArgs.Handled = true;
		}
	}

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

	private void CreateEditableContentPresenterTextBlock()
	{
		if (m_tpEditableContentPresenterTextBlock is null)
		{
			var spTextBlock = new TextBlock();

			m_tpEditableContentPresenterTextBlock = spTextBlock;
		}
	}
}

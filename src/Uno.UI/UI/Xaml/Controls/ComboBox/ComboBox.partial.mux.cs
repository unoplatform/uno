// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference src\dxaml\xcp\dxaml\lib\ComboBox_Partial.cpp, tag winui3/release/1.6.1, commit f31293f

// The file is not ported completely, some methods are still missing!

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using DirectUI;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Uno.Disposables;
using Uno.UI.Extensions;
using Uno.UI.Xaml.Core;
using Uno.UI.Xaml.Input;
using Windows.Foundation;
using Windows.System;
using static DirectUI.ElevationHelper;

#if __ANDROID__
using Uno.UI;
#endif

#if HAS_UNO_WINUI
using PointerDeviceType = Microsoft.UI.Input.PointerDeviceType;
#else
using PointerDeviceType = Windows.Devices.Input.PointerDeviceType;
#endif

namespace Microsoft.UI.Xaml.Controls;

partial class ComboBox
{
	private const int s_itemCountThreshold = 5;

	private void PrepareState()
	{
		//base.PrepareState();
		TemplateSettings = new();
		TemplateSettings.SelectedItemDirection = AnimationDirection.Top;
	}

	private void ReleaseMembers()
	{
		m_tpElementPopupChild = null;
		m_tpEmptyContent = null;
		m_tpElementPopupChildCanvas = null;
		//m_tpElementOutsidePopup = null;

		//if (m_tpFlyoutButtonPart)
		//{
		//	IFC(DetachHandler(m_epFlyoutButtonClickHandler, m_tpFlyoutButtonPart));
		//	// Cleared by ComboBoxGenerated::OnApplyTemplate
		//}
		//if (m_tpItemsPresenterHostParent)
		//{
		//	IFC(DetachHandler(m_epHostParentSizeChangedHandler, m_tpItemsPresenterHostParent));
		//	m_tpItemsPresenterHostParent = null;
		//}
		//if (m_tpItemsPresenterPart)
		//{
		//	IFC(DetachHandler(m_epItemsPresenterSizeChangedHandler, m_tpItemsPresenterPart));
		//	// Cleared by ComboBoxGenerated::OnApplyTemplate
		//}
		//if (m_tpClosedStoryboard)
		//{
		//	IFC(m_tpClosedStoryboard.Cast<Storyboard>()->remove_Completed(m_closedStateStoryboardCompletedToken));
		//	m_tpClosedStoryboard = null;
		//}
		//if (m_tpPopupPart && m_epPopupClosedHandler)
		//{
		//	IFC(m_epPopupClosedHandler.DetachEventHandler(m_tpPopupPart.Get()));
		//}

		m_tpHeaderContentPresenterPart = null;
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
			m_spDropDownOverlayPointerEnteredHandler.Disposable = Disposable.Create(() => m_tpDropDownOverlayPart.PointerEntered -= OnDropDownOverlayPointerEntered);

			m_tpDropDownOverlayPart.PointerExited += OnDropDownOverlayPointerExited;
			m_spDropDownOverlayPointerExitedHandler.Disposable = Disposable.Create(() => m_tpDropDownOverlayPart.PointerExited -= OnDropDownOverlayPointerExited);

			m_tpDropDownOverlayPart.Visibility = Visibility.Visible;
		}

		// Tells the selector to allow Custom Values.
		SetAllowCustomValues(true /*allow*/);

		m_restoreIndexSet = false;
		m_indexToRestoreOnCancel = -1;
		ResetSearch();
		ResetSearchString();

		// TODO Uno: Input validation support #4839
		//wrl::ComPtr<xaml_controls::IInputValidationContext> context;
		//get_ValidationContext(&context));
		//pEditableTextPartAsTextBox.ValidationContext(context.Get()));

		//wrl::ComPtr<xaml_controls::IInputValidationCommand> command;
		//get_ValidationCommand(&command));
		//pEditableTextPartAsTextBox->put_ValidationCommand(command.Get()));

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
			m_spDropDownOverlayPointerEnteredHandler.Disposable = null;
			m_spDropDownOverlayPointerExitedHandler.Disposable = null;

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

		m_tpEditableTextPart.Text =
#if HAS_UNO_WINUI
			null;
#else // In UWP, setting Text to null will throw an exception.
			"";
#endif

		m_isEditModeConfigured = false;
	}

	private void SetupElementPopupChild()
	{
		if (m_tpElementPopupChild is null)
		{
			m_tpElementPopupChildCanvas = null;
			return;
		}

		// Wire up CharacterReceived in our child so that we can handle typeahead if it's enabled.
		// We know we can do this here, as with the other events, because we explicitly create m_tpElementPopupChild in SetupElementPopup.
		m_tpElementPopupChild.CharacterReceived += OnPopupCharacterReceived;
		m_pElementPopupChildCharacterReceivedToken.Disposable = Disposable.Create(() => m_tpElementPopupChild.CharacterReceived -= OnPopupCharacterReceived);

		//Key down event handler.
		// We have to hook the visual tree in two spots to catch all the keyboard events.  Once at the ComboBox and once for
		// the visual root of the popup control.
		m_tpElementPopupChild.KeyDown += OnKeyDownPrivate;
		m_pElementPopupChildKeyDownToken.Disposable = Disposable.Create(() => m_tpElementPopupChild.KeyDown -= OnKeyDownPrivate);

		m_tpElementPopupChild.KeyUp += OnKeyUpPrivate;
		m_pElementPopupChildKeyUpToken.Disposable = Disposable.Create(() => m_tpElementPopupChild.KeyUp -= OnKeyUpPrivate);

		// Got focus event handler
		m_tpElementPopupChild.GotFocus += OnElementPopupChildGotFocus;
		m_pElementPopupChildGotFocusToken.Disposable = Disposable.Create(() => m_tpElementPopupChild.GotFocus -= OnElementPopupChildGotFocus);

		// Leave focus event handler
		m_tpElementPopupChild.LostFocus += OnElementPopupChildLostFocus;
		m_pElementPopupChildLostFocusToken.Disposable = Disposable.Create(() => m_tpElementPopupChild.LostFocus -= OnElementPopupChildLostFocus);

		m_tpElementPopupChild.PointerEntered += OnElementPopupChildPointerEntered;
		m_pElementPopupChildPointerEnteredToken.Disposable = Disposable.Create(() => m_tpElementPopupChild.PointerEntered -= OnElementPopupChildPointerEntered);

		m_tpElementPopupChild.PointerExited += OnElementPopupChildPointerExited;
		m_pElementPopupChildPointerExitedToken.Disposable = Disposable.Create(() => m_tpElementPopupChild.PointerExited -= OnElementPopupChildPointerExited);

		m_tpElementPopupChild.SizeChanged += OnElementPopupChildSizeChanged;
		m_pElementPopupChildISizeChangedToken.Disposable = Disposable.Create(() => m_tpElementPopupChild.SizeChanged -= OnElementPopupChildSizeChanged);

		m_tpElementPopupChild.Loaded += OnElementPopupChildLoaded;
		m_pElementPopupChildLoadedToken.Disposable = Disposable.Create(() => m_tpElementPopupChild.Loaded -= OnElementPopupChildLoaded);

		Canvas spElementPopupChildCanvas = new();
		m_tpElementPopupChildCanvas = spElementPopupChildCanvas;
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
		if (!IsFullMode)
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

	private void SetContentPresenter(int index, bool forceSelectionBoxToNull = false)
	{
		UpdateContentPresenter(); // TODO MZ: This should not happen
		return; // TODO MZ: This should not happen

		//bool bGeneratedComboBoxItem = false;
		//DependencyObject spContainer;
		//DependencyObject spGeneratedComboBoxItemAsDO;
		//ComboBoxItem? spComboBoxItem;
		//object? spContent;
		//DataTemplate spDataTemplate;
		//DataTemplateSelector spDataTemplateSelector;
		//GeneratorPosition generatorPosition;

		//global::System.Diagnostics.Debug.Assert(!IsInline, "ContentPresenter is not used in inline mode.");

		//// Avoid reentrancy.
		//if (m_preparingContentPresentersElement)
		//{
		//	return;
		//}

		//if (m_tpSwappedOutComboBoxItem is not null)
		//{
		//	if (m_tpContentPresenterPart is not null)
		//	{
		//		spContent = m_tpContentPresenterPart.Content;
		//		{
		//			m_tpContentPresenterPart.Content = null;
		//			m_tpSwappedOutComboBoxItem.Content = spContent;
		//			m_tpSwappedOutComboBoxItem = null;
		//		}
		//	}
		//}

		//ItemContainerGenerator? spGenerator = null; // TODO Uno: Support for ItemContainerGenerator #17808 (Should reference this.ItemContainerGenerator property)
		//ItemContainerGenerator? pItemContainerGenerator = spGenerator;
		//if (m_iLastGeneratedItemIndexforFaceplate > 0 && pItemContainerGenerator is not null)
		//{
		//	// This container was generated just for the purpose of extracting Content and ContentTemplate.
		//	// This is the case where we generated an item which was its own container (e.g. defined in XAML or code behind).
		//	// We keep this until the next item is being put on faceplate or popup is opened so that ItemContainerGenerator.ContainerFromIndex returns the
		//	// correct container for this item which a developer would expect.
		//	// We need to remove this item once popup opens (or another item takes its place on faceplate)
		//	// so that virtualizing panel underneath does not get items out of order.
		//	// We want to remove instead of recycle because we do not want to change the collection order by reusing containers for different data.
		//	generatorPosition = pItemContainerGenerator.GeneratorPositionFromIndex(m_iLastGeneratedItemIndexforFaceplate);
		//	if (generatorPosition.Offset == 0 && generatorPosition.Index >= 0)
		//	{
		//		// Only remove if the position returned by Generator is correct
		//		pItemContainerGenerator.Remove(generatorPosition, 1);
		//	}

		//	m_iLastGeneratedItemIndexforFaceplate = -1;
		//}

		//if (index == -1)
		//{
		//	if (m_tpContentPresenterPart is not null)
		//	{
		//		m_tpContentPresenterPart.ContentTemplateSelector = null;
		//		m_tpContentPresenterPart.ContentTemplate = null;
		//		m_tpContentPresenterPart.Content = m_tpEmptyContent;
		//	}

		//	// Only reset the SelectionBoxItem if a custom value is not selected.
		//	if (forceSelectionBoxToNull || !IsEditable || m_customValueRef is null)
		//	{
		//		SelectionBoxItem = null;
		//	}

		//	SelectionBoxItemTemplate = null;
		//	return;
		//}

		//if (m_tpContentPresenterPart is not null)
		//{
		//	m_tpContentPresenterPart.Content = null;
		//}

		//spContainer = ContainerFromIndex(index);
		//spComboBoxItem = spContainer as ComboBoxItem;

		//if (spComboBoxItem is null && pItemContainerGenerator is not null)
		//{
		//	bool isNewlyRealized = false;
		//	generatorPosition = pItemContainerGenerator.GeneratorPositionFromIndex(index);
		//	pItemContainerGenerator.StartAt(generatorPosition, GeneratorDirection.Forward, true);
		//	spGeneratedComboBoxItemAsDO = pItemContainerGenerator.GenerateNext(out isNewlyRealized);
		//	pItemContainerGenerator.Stop();
		//	m_preparingContentPresentersElement = true;
		//	m_tpGeneratedContainerForContentPresenter = spGeneratedComboBoxItemAsDO;
		//	try
		//	{
		//		pItemContainerGenerator.PrepareItemContainer(spGeneratedComboBoxItemAsDO);
		//	}
		//	finally
		//	{
		//		m_tpGeneratedContainerForContentPresenter = null;
		//		m_preparingContentPresentersElement = false;
		//	}
		//	spComboBoxItem = (ComboBoxItem)spGeneratedComboBoxItemAsDO;
		//	// We dont want to remove the comboBoxItem if it was created explicitly in XAML and exists in Items collection
		//	// TODO Uno: Missing implementation for ItemContainerGenerator #17808
		//	//spItem = null;  spComboBoxItem.ReadLocalValue(ItemContainerGenerator.ItemForItemContainerProperty);
		//	//bGeneratedComboBoxItem = IsItemItsOwnContainer(spItem);
		//	//bGeneratedComboBoxItem = !bGeneratedComboBoxItem;
		//	m_iLastGeneratedItemIndexforFaceplate = index;
		//}

		//if (spComboBoxItem is null)
		//{
		//	return;
		//}

		//spContent = spComboBoxItem.Content;
		//{
		//	// Because we can't keep UIElement in 2 different place
		//	// we need to reset ComboBoxItem.Content property. And we need to do it for UIElement only
		//	if (spContent is UIElement)
		//	{
		//		spComboBoxItem.Content = null;
		//		if (!bGeneratedComboBoxItem)
		//		{
		//			m_tpSwappedOutComboBoxItem = spComboBoxItem;
		//		}
		//	}

		//	spComboBoxItem.IsPointerOver = false;
		//	spComboBoxItem.ChangeVisualStateInternal(true);

		//	// We want the item displayed in the 'selected item' ContentPresenter to have the same visual representation as the
		//	// items in the Popup's StackPanel, to do that we copy the DataTemplate of the ComboBoxItem.
		//	spDataTemplate = spComboBoxItem.ContentTemplate;
		//	spDataTemplateSelector = spComboBoxItem.ContentTemplateSelector;
		//	if (m_tpContentPresenterPart is not null)
		//	{
		//		m_tpContentPresenterPart.Content = spContent;
		//		m_tpContentPresenterPart.ContentTemplate = spDataTemplate;
		//		m_tpContentPresenterPart.ContentTemplateSelector = spDataTemplateSelector;
		//		if (spDataTemplate is null)
		//		{
		//			spDataTemplate = m_tpContentPresenterPart.SelectedContentTemplate;
		//		}
		//	}

		//	SelectionBoxItem = spContent;
		//	SelectionBoxItemTemplate = spDataTemplate;
		//}

		//if (bGeneratedComboBoxItem && pItemContainerGenerator is not null)
		//{
		//	// This container was generated just for the purpose of extracting Content and ContentTemplate
		//	// It is not connected to the visual tree which might have unintended consequences, so remove it
		//	generatorPosition = pItemContainerGenerator.GeneratorPositionFromIndex(index);
		//	pItemContainerGenerator.Recycle(generatorPosition, 1);
		//	m_iLastGeneratedItemIndexforFaceplate = -1;
		//}
	}

	internal void UpdateSelectionBoxItemProperties(int index)
	{
		global::System.Diagnostics.Debug.Assert(IsInline, "When not in inline mode SetContentPresenter should be used instead of UpdateSelectionBoxItemProperties.");

		if (-1 == index)
		{
			SelectionBoxItemTemplate = null;
			SelectionBoxItem = null;
		}
		else
		{
			DataTemplate spDataTemplate;
			object spItem;
			ComboBoxItem spComboBoxItem;

			var spContainer = ContainerFromIndex(index);

			// The item will not have been realized if SelectedItem/Index was set in xaml and we're being called
			// from OnApplyTemplate, but in that case the item will be realized when the ItemsPresenter
			// is added to the visual tree later in the layout pass, and SetContentPresenter will be called
			// again then.

			if (spContainer is not null)
			{
				spComboBoxItem = (ComboBoxItem)spContainer;
				spDataTemplate = spComboBoxItem.ContentTemplate;
				spItem = spComboBoxItem.Content;

				SelectionBoxItem = spItem;
				SelectionBoxItemTemplate = spDataTemplate;
			}
		}
	}

	protected override bool IsItemItsOwnContainerOverride(object item) => item is ComboBoxItem;

	protected override DependencyObject GetContainerForItemOverride() => new ComboBoxItem { IsGeneratedContainer = true };

	protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
	{
		base.PrepareContainerForItemOverride(element, item);

		var isDropDownOpen = IsDropDownOpen;
		var selectedIndex = SelectedIndex;
		var selectedItem = SelectedItem;

		var areEqual = PropertyValue.AreEqualImpl(selectedItem, item);
		if (!isDropDownOpen && m_tpSwappedOutComboBoxItem is null && areEqual && !IsInline)
		{
			SetContentPresenter(selectedIndex);
		}
	}

	protected override void ClearContainerForItemOverride(DependencyObject element, object item)
	{
		var sPassedElement = element as ComboBoxItem;

		var isItemsHostInvalid = IsItemsHostInvalid;

		if (!isItemsHostInvalid && sPassedElement == m_tpSwappedOutComboBoxItem)
		{
			global::System.Diagnostics.Debug.Assert(!IsInline, "m_tpSwappedOutComboBoxItem is not used in inline mode.");
			SetContentPresenter(-1);
		}

		base.ClearContainerForItemOverride(element, item);
	}

	// The first generated container is not part of the visual tree until its prepared
	// During prepare container we set IsSelected property on the item being prepared which calls this method
	// Since the container is not hooked into Visual tree yet, we return False from the base class's method.
	// We cover this case by keeping track of the generated container in m_tpGeneratedContainerForContentPresenter and overriding this method.
	private protected override bool IsHostForItemContainer(DependencyObject pContainer)
	{
		var isHost = base.IsHostForItemContainer(pContainer);
		if (!isHost && m_tpGeneratedContainerForContentPresenter is not null)
		{
			isHost = pContainer == m_tpGeneratedContainerForContentPresenter;
		}

		return isHost;
	}

	internal override void OnPropertyChanged2(DependencyPropertyChangedEventArgs args)
	{
		base.OnPropertyChanged2(args);

		// TODO Uno: Only IsEditable logic for now.
		if (args.Property == IsDropDownOpenProperty)
		{
			OnIsDropDownOpenChanged((bool)args.OldValue, (bool)args.NewValue);
			OnIsDropDownOpenChanged();
		}
		else if (args.Property == HeaderProperty || args.Property == HeaderTemplateProperty)
		{
			UpdateHeaderPresenterVisibility();
		}
		else if (args.Property == VisibilityProperty)
		{
			OnVisibilityChanged();
		}
		else if (args.Property == IsSelectionActiveProperty)
		{
			OnIsSelectionActiveChanged();
		}
		else if (args.Property == DisplayMemberPathProperty)
		{
			m_spPropertyPathListener?.Dispose();
			m_spPropertyPathListener = null;
		}
		else if (args.Property == TextProperty)
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

	private void OnIsDropDownOpenChanged()
	{
		var isDropDownOpen = IsDropDownOpen;

		global::System.Diagnostics.Debug.Assert(!(m_isDropDownClosing && !isDropDownOpen), "The drop down cannot already be closing if IsDropDownOpen was just changed to false.");

		m_skipFocusSuggestion = !isDropDownOpen;

		var spElement = m_tpEmptyContent as UIElement;
		if (spElement is not null)
		{
			int selectedIndex = SelectedIndex;

			// hide default placeholder text is we open dropdown or have anything is selected.
			spElement.Opacity = isDropDownOpen || selectedIndex >= 0 ? 0 : 1;
		}

		if (m_isDropDownClosing && isDropDownOpen)
		{
			// We are opening the drop down before it is fully closed. Wrap up on the closing
			// logic before initiating the opening logic.
			FinishClosingDropDown();
		}

		// TODO Uno: Implement
		if (IsSmallFormFactor)
		{
			// isDropDownOpen? OnOpenSmallFormFactor() : OnCloseSmallFormFactor();
		}
		else
		{
			if (IsDropDownOpen)
			{
				OnOpen();
			}
			else
			{
				OnClose();
			}
		}

		ElementSoundPlayerService.RequestInteractionSoundForElementStatic(isDropDownOpen ? ElementSoundKind.Show : ElementSoundKind.Hide, this);
	}

	private protected override void OnSelectionChanged(
		int oldSelectedIndex,
		int newSelectedIndex,
		object pOldSelectedItem,
		object pNewSelectedItem,
		bool animateIfBringIntoView = false,
		FocusNavigationDirection focusNavigationDirection = FocusNavigationDirection.None)
	{
		bool bIsDropDownOpen = IsDropDownOpen;

		if (bIsDropDownOpen)
		{
			// If is Editable skip focusing the selected item.
			if (!IsEditable)
			{
				base.OnSelectionChanged(oldSelectedIndex, newSelectedIndex, pOldSelectedItem, pNewSelectedItem, animateIfBringIntoView, focusNavigationDirection);
			}
		}
		else
		{
			if (IsInline)
			{
				// In inline mode, we need to update layout in case the size of the selected item
				// has changed
				UpdateSelectionBoxItemProperties(newSelectedIndex);
				ForceApplyInlineLayoutUpdate();
			}
			else if (m_tpContentPresenterPart is not null)
			{
				// The user is cycling through values in the SelectionBox
				SetContentPresenter(newSelectedIndex);
			}

			var spElement = m_tpEmptyContent as UIElement;
			if (spElement is not null)
			{
				int selectedIndex = SelectedIndex;

				// Hide the default placeholder text if we have any selected item,
				// or show it if we don't.
				spElement.Opacity = selectedIndex >= 0 ? 0 : 1;
			}

			// When ComboBox is Editable we need to keep track of the restore index as soon as selection changes, not only
			// on Open as non-editable ComboBox does. This allows us to revert when Selection Trigger is set to Always.
			if (IsEditable)
			{
				if (!m_restoreIndexSet)
				{
					m_restoreIndexSet = true;
					m_indexToRestoreOnCancel = oldSelectedIndex;
				}
			}

			// In Phone 8.1, we relied on visual states to show or hide the default placeholder text.
			// Though that's no longer the case in Threshold, we still call this here for app compat,
			// since existing apps need this call to show or hide the placeholder text.
			UpdateVisualState(false);
		}
	}

	private void UpdateEditableTextBox(object? item, bool selectText, bool selectAll)
	{
		if (item is null)
		{
			return;
		}

		EnsurePropertyPathListener();
		var strItem = TryGetStringValue(item, m_spPropertyPathListener);

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

	private void OnIsSelectionActiveChanged() => UpdateVisualState();

	private protected override void OnIsEnabledChanged(IsEnabledChangedEventArgs e)
	{
		base.OnIsEnabledChanged(e);

		var bIsEnabled = IsEnabled;
		if (!bIsEnabled)
		{
			ClearStateFlags();
		}

		UpdateVisualState();
	}

	// Update the visual states when the Visibility property is changed.
	private protected override void OnVisibilityChanged()
	{
		var visibility = Visibility;
		if (Visibility.Visible != visibility)
		{
			ClearStateFlags();
		}

		UpdateVisualState();
	}

	// Clear flags relating to the visual state.  Called when IsEnabled is set to false
	// or when Visibility is set to Hidden or Collapsed.
	private void ClearStateFlags()
	{
		IsDropDownOpen = false;
		m_IsPointerOverMain = false;
		m_IsPointerOverPopup = false;
		m_IsPointerOverDropDownOverlay = false;
		m_bIsPressed = false;
	}

	private void UpdateSelectionBoxHighlighted()
	{
		bool isDropDownOpen = IsDropDownOpen;
		var hasFocus = HasFocus();
		var value = isDropDownOpen && hasFocus;
		IsSelectionBoxHighlighted = value;
	}

#if HAS_UNO
	private protected override void UpdateItems(NotifyCollectionChangedEventArgs args)
	{
		// With virtualization, the base.UpdateItems won't handle the updates
		// (because ShouldItemsControlManageChildren is false), so we make an
		// explicit call to Refresh here instead.
		base.UpdateItems(args);
		Refresh();
	}
#endif

	private void OnOpen()
	{
		// TODO Uno: BackButton support
		//if (DXamlCore.Current.BackButtonSupported)
		//{
		//	BackButtonIntegration.RegisterListener(this);
		//}

		int selectedItemIndex = SelectedIndex;

		// Save off the selected index when opened so that we can
		// restore it when the user cancels using ESC/GamepadB.
		if (!m_restoreIndexSet)
		{
			m_restoreIndexSet = true;
			m_indexToRestoreOnCancel = selectedItemIndex;
		}

		m_isClosingDueToCancel = false;

		if (m_tpPopupPart is not null)
		{
#if HAS_UNO // Force load children
			// This method will load the itempresenter children
#if __ANDROID__
			SetItemsPresenter((m_tpPopupPart.Child as AViewGroup).FindFirstChild<ItemsPresenter>()!);
#elif __APPLE_UIKIT__
			SetItemsPresenter(m_tpPopupPart.Child.FindFirstChild<ItemsPresenter>()!);
#endif
#endif

			m_tpPopupPart.IsOpen = true;

			bool isDefaultShadowEnabled = IsDefaultShadowEnabled(this);

			// Cast a shadow
			if (isDefaultShadowEnabled)
			{
				ApplyElevationEffect(m_tpElementPopupChild);
			}
			else
			{
				ClearElevationEffect(m_tpElementPopupChild);
			}
		}

		if (m_isOverlayVisible)
		{
			PlayOverlayOpeningAnimation();
		}

		// Before CarouselPanel gets into the measure pass we have to update m_bIsPopupPannable flag
		// and propagate m_bShouldCarousel flag to the CarouselPanel
		// On Edit mode we don't carousel because popup appears aligned to the bottom of the ComboBox
		// instead of centered above the ComboBox, so carouseling doesn't make sense for this design.
		m_bShouldCarousel = m_inputDeviceTypeUsedToOpen == InputDeviceType.Touch && !IsEditable;
		SetIsPopupPannable();
		SetContentPresenter(-1, true /*forceSelectionBoxToNull*/);

		RaiseDropDownOpenChangedEvents(true);

		// At this point, the template settings are holding old values which might
		// not be correct anymore. Thus, if we move to the "Opened" visual state
		// and begin the SplitOpenThemeAnimation right away, the animation parameters
		// might be incorrect. To avoid this, will call UpdateLayout to trigger an
		// arrange pass that will end up calling ComboBox.ArrangePopup. This
		// method will update the template settings appropriately.
		UpdateLayout();
		UpdateVisualState();
		UpdateSelectionBoxHighlighted();

		// Focus is forcibly set to the ComboBox when it is opened.  This is needed to make sure
		// narrator scenarios function as expected.  For example, when an item is selected from
		// an open ComboBox using narrator, the high-light rectangle needs to move back to the
		// ComboBox; this only happens if the ComboBox or one of it's items has focus.  If it
		// doesn't have focus, the high-light rectangle will stay where the item was prior to the
		// popup closing, which is confusing to narrator users.  Another example is when opening
		// the ComboBox, if there is already a selected item, it should get narrator focus as
		// soon as it opens.
		// This does not change the behavior for keyboard/touch/mouse users since interacting
		// with the control using any of those input methods would have set focus to it anyway.
		// This only affects opening the ComboBox programmatically, which is what narrator does.
		if (IsEditable)
		{
			// Ensure focus is in TextBox when popup opens.
			if (!EditableTextHasFocus() && m_tpEditableTextPart is not null)
			{
				m_tpEditableTextPart.Focus(FocusState.Programmatic);
			}
		}
		else
		{

			// If no item is selected when we open the combo box and there's at least one item,
			// give focus to or select the first item to ensure that keyboarding can function normally.
			if (selectedItemIndex >= 0)
			{
				SetFocusedItem(selectedItemIndex, m_bShouldCenterSelectedItem /*shouldScrollIntoView*/, true /*forceFocus*/, FocusState.Programmatic, false);
			}
			else
			{
				var itemCount = GetItemCount();

				if (itemCount > 0)
				{
					ComboBoxSelectionChangedTrigger selectionChangedTrigger = SelectionChangedTrigger;

					if (selectionChangedTrigger == ComboBoxSelectionChangedTrigger.Always)
					{
						SelectedIndex = 0;
					}
					else
					{
						OverrideSelectedIndexForVisualStates(0);
					}

					SetFocusedItem(0, false /*shouldScrollIntoView*/, true /*forceFocus*/, FocusState.Programmatic, false);
				}
			}
		}
	}

	private void OnClose()
	{
		// BackButtonIntegration_UnregisterListener(this);

		m_isDropDownClosing = true;

		if (IsEditable)
		{
			CommitRevertEditableSearch(m_isClosingDueToCancel /*restoreValue*/);
		}
		else if (m_isClosingDueToCancel && m_indexToRestoreOnCancel != -1)
		{
			SelectedIndex = m_indexToRestoreOnCancel;
		}

		m_indexToRestoreOnCancel = -1;
		m_restoreIndexSet = false;
		m_isClosingDueToCancel = false;

		ClearSelectedIndexOverrideForVisualStates();
		SetClosingAnimationDirection();

		if (m_isOverlayVisible)
		{
			PlayOverlayClosingAnimation();
		}

		UpdateVisualState(true);
		if (m_tpClosedStoryboard is null)
		{
			// If we do not have a storyboard for closed state, the completed handler for the
			// animation will never be called, so we need to complete closing the drop down
			// now.
			FinishClosingDropDown();
		}
		else
		{
			// Editable ComboBox handles ContentPresenter changes in CommitRevertEditableSearch.
			if (!IsEditable)
			{
				var selectedIndex = SelectedIndex;
				SetContentPresenter(selectedIndex);
			}
		}

		// Clear pointer over status while the dropdown is closing.
		// Sometimes when the dropdown animates out from under the pointer we don't get a
		// PointerExited so the ComboBox stays in PointerOver state indefinitely.
		m_IsPointerOverPopup = false;
		m_IsPointerOverMain = false;
		m_IsPointerOverDropDownOverlay = false;
		ChangeVisualState(false);
	}


	private void FinishClosingDropDown()
	{
		// Clean up any existing operation. Important to clear this before firing the
		// DropDownClosed event because the event handler may set IsDropDownOpen back to true,
		// which will begin a new async operation. Dropping this will destroy the
		// operation which will prevent the completion event from firing, ensuring that
		// our completion callback doesn't erroneously call FinishClosingDropDown again.
		m_tpAsyncSelectionInfo = null;

		int selectedIndex = SelectedIndex;

		// This returns focus to ComboBox after clicking on a ComboBoxItem.
		SetFocusedItem(-1, false /*shouldScrollIntoView*/);

		// Ensure Focus moves over to Textbox next time ComboBox is focused.
		m_shouldMoveFocusToTextBox = true;

		if (IsInline)
		{
			EnsurePresenterReadyForInlineMode();
			UpdateSelectionBoxItemProperties(selectedIndex);
			ForceApplyInlineLayoutUpdate();
		}
		else
		{
			// Editable ComboBox handles ContentPresenter changes in CommitRevertEditableSearch.
			if (!IsEditable)
			{
				SetContentPresenter(selectedIndex);
			}

			if (m_tpPopupPart is not null)
			{
				m_tpPopupPart.IsOpen = false;
				m_IsPointerOverPopup = false; // closing the popup will not fire a PointerExited
				ResetCarouselPanelState();
				ClearStateFlagsOnItems();
			}
		}

		m_isExpanded = false;
		m_isDropDownClosing = false;
		m_previousInputDeviceTypeUsedToOpen = m_inputDeviceTypeUsedToOpen;
		m_inputDeviceTypeUsedToOpen = InputDeviceType.None;

		RaiseDropDownOpenChangedEvents(false);
		UpdateVisualState();
		UpdateSelectionBoxHighlighted();
	}

	private void RaiseDropDownOpenChangedEvents(bool isDropDownOpen)
	{
		var args = new RoutedEventArgs();
		args.OriginalSource = this;

		if (isDropDownOpen)
		{
			OnDropDownOpened(args);
		}
		else
		{
			OnDropDownClosed(args);
		}
	}

	private void FocusChanged(bool hasFocus)
	{
		// The OnGotFocus & OnLostFocus are asynchronous and cannot reliably tell you that have the focus.  All they do is
		// let you know that the focus changed sometime in the past.  To determine if you currently have the focus you need
		// to do consult the FocusManager (see HasFocus()).
		UpdateSelectionBoxHighlighted();
		IsSelectionActive = hasFocus;

#if __IOS__
		if (_popup is Popup popup && popup.Child.FindFirstChild<Picker>() is not null)
		{
			// If the ComboBox is in a Picker, we don't want to close the ComboBox when it loses focus.
			// The Picker will handle the closing of the ComboBox.
			hasFocus = true;
		}
#endif

		if (!hasFocus && !IsFullMode)
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
		var itemString = TryGetStringValue(item, m_spPropertyPathListener);
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
						var storedString = m_customValueRef as string;

						// Prevent sending the event if the custom value is the same.
						sendEvent = !AreStringsEqual(storedString, searchString);
					}

					if (sendEvent)
					{
						var spInspectable = searchString;

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

	protected override void OnKeyDown(KeyRoutedEventArgs args)
	{
		if (!args.Handled)
		{
			OnKeyDownPrivate(null, args);
		}
	}

	protected override void OnKeyUp(KeyRoutedEventArgs args) => OnKeyUpPrivate(null, args);

	private void OnKeyDownPrivate(object? pSender, KeyRoutedEventArgs pArgs)
	{
		base.OnKeyDown(pArgs);

#if HAS_UNO // TODO Uno: This code is customized version to working navigation handling discrepancies and CharacterReceived support missing.
		pArgs.Handled = TryHandleKeyDown(pArgs, null);
#endif

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

#if HAS_UNO // CharacterReceived is not supported yet, handle it here.
		if (!pArgs.Handled && pArgs.UnicodeKey is not null)
		{
			// Temporary as Uno doesn't yet implement CharacterReceived event.
			// The queuing is necessary because the Text of the TextBox is not updated until after KeyDown is processed.
			DispatcherQueue.TryEnqueue(() => OnCharacterReceived(this, new CharacterReceivedRoutedEventArgs(pArgs.UnicodeKey.Value, default)));
		}
#endif
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

	private void OnKeyUpPrivate(object? sender, KeyRoutedEventArgs args)
	{
		base.OnKeyUp(args);

		var key = args.Key;
		var originalKey = args.OriginalKey;


		//Key maps to both gamepad B and Escape key
		if (m_ignoreCancelKeyDowns && key == VirtualKey.Escape)
		{
			m_ignoreCancelKeyDowns = false;
			//We know some escape key was pressed, so make sure to mark it as handled.
			args.Handled = true;
		}

		bool eventHandled = false;
		eventHandled = args.Handled;

		if (eventHandled)
		{
			return;
		}

		bool isEnabled = IsEnabled;

		if (!isEnabled)
		{
			return;
		}

		// Ideally, we want the ComboBox to execute its functional behavior in response to KeyUp,
		// not KeyDown.  Our not doing so causes problems - for example, if a page listens to the
		// B button to know when to navigate back, it will do so in response to a B button press
		// that was intended just to close the ComboBox drop-down, since the ComboBox does not
		// handle the KeyUp event, so the Page ends up receiving it. However, for the purposes of TH2,
		// we'll scope things to simply unblock this specific Xbox scenario by merely marking KeyUp
		// as handled when KeyDown was handled, to prevent it from bubbling up to a page or
		// something else that might want to respond to it.  Task #4373221 has been filed to track
		// the more general work item to bring controls such as ComboBox in line with the
		// design philosophy that controls should execute functional behavior in response to KeyUp,
		// rather than KeyDown.
		if (m_handledGamepadOrRemoteKeyDown && XboxUtility.IsGamepadNavigationInput(originalKey))
		{
			args.Handled = true;
		}

		m_handledGamepadOrRemoteKeyDown = false;
	}

	private void PopupKeyDown(KeyRoutedEventArgs args)
	{
		bool handled = false;
		int newFocusedIndex = -1;
		var key = VirtualKey.None;
		FocusState focusState = FocusState.Programmatic;
		bool bFocused = false;
		bool skipSelection = false;

		key = args.Key;
		var nModifierKeys = CoreImports.Input_GetKeyboardModifiers();

		var lastPointerType = VisualTree.GetContentRootForElement(this)?.InputManager.LastInputDeviceType;

		switch (key)
		{
			case VirtualKey.Escape:
				// NOTE: GamepadNavigationCancel routes through the _Escape case here.
				// Ensure that the combobox has focus if it's closed via Escape or GamepadNavigationCancel.
				// Use programmatic focus here to be consistent with Tab; FocusManager will resolve to Keyboard focus.
				Focus(focusState);

				m_isClosingDueToCancel = true;
				IsDropDownOpen = false;

				// If we closed the drop-down in response to a cancel key-down,
				// then we should ignore all subsequent cancel key-down messages
				// until we get a key-up, as otherwise we can run into the situation
				// where a user can press B to close a ComboBox, then hold B down
				// long enough for that key press to start repeating, and then at that point
				// the popup has closed and the user might unexpectedly navigate back.
				m_ignoreCancelKeyDowns = true;
				handled = true;
				break;
			case VirtualKey.Tab:
				// Need to enable this to support focusing out of combobox using tab key.
				bFocused = Focus(focusState);
				global::System.Diagnostics.Debug.Assert(bFocused, "Focus could not leave ComboBox.");

				IsDropDownOpen = false;
				break;
			case VirtualKey.Space:
				// If we're in searching mode or in Editable mode, then we shouldn't handle the current space KeyDown. Let the Character event handle it.
				// Gamepad A button maps to VirtualKey.Space here, continue into next condition case to select new item.
				if ((IsEditable || IsInSearchingMode()) && lastPointerType != InputDeviceType.GamepadOrRemote)
				{
					break;
				}
				goto case VirtualKey.Enter;
			case VirtualKey.Enter:
				if (VirtualKeyModifiers.Menu != (nModifierKeys & (VirtualKeyModifiers.Control | VirtualKeyModifiers.Menu)))
				{
					if (IsEditable && EditableTextHasFocus())
					{
						IsDropDownOpen = false;
						handled = true;
					}
					else
					{
						// KeyRoutedEventArgs.OriginalSource (used by WPF) isn't available in Silverlight; use FocusManager.GetFocusedElement instead
						var spFocused = this.GetFocusedElement();
						var spComboBoxItem = spFocused as ComboBoxItem;
						if (spComboBoxItem is not null)
						{
							bool bIsSelected = false;
							bIsSelected = spComboBoxItem.IsSelected;
							if ((VirtualKeyModifiers.Control == (nModifierKeys & VirtualKeyModifiers.Control)) && bIsSelected)
							{
								SelectedIndex = -1;
							}
							else
							{
								SelectedIndex = GetFocusedIndex();
								IsDropDownOpen = false;
							}
							handled = true;
						}
					}
				}
				break;
			case VirtualKey.Up:
			case VirtualKey.Down:
				if (IsEditable
					&& (EditableTextHasFocus() || lastPointerType == InputDeviceType.GamepadOrRemote))
				{
					int currentSelectedIndex = -1;
					if (IsSearchResultIndexSet())
					{
						currentSelectedIndex = m_searchResultIndex;
					}
					else
					{
						currentSelectedIndex = SelectedIndex;
					}

					newFocusedIndex = currentSelectedIndex + (key == VirtualKey.Up ? -1 : 1);

					if (lastPointerType == InputDeviceType.GamepadOrRemote)
					{
						int itemCount = GetItemCount();

						// If Popup opened down and moving above the first element, return focus to TextBox.
						// If Popup opened up and moving below the last element, return focus to TextBox.
						if ((newFocusedIndex == -1 && !m_openedUp) || (newFocusedIndex >= (int)itemCount && m_openedUp))
						{
							if (m_tpEditableTextPart is not null)
							{
								m_tpEditableTextPart.Focus(FocusState.Programmatic);
							}

							skipSelection = true;

							handled = true;
						}
						else
						{
							newFocusedIndex = newFocusedIndex < 0 ? 0 : newFocusedIndex;
						}
					}
					else
					{
						newFocusedIndex = newFocusedIndex < 0 ? 0 : newFocusedIndex;
					}
					break;
				}
				else if (0 != (nModifierKeys & VirtualKeyModifiers.Menu))
				{
					IsDropDownOpen = false;
					handled = true;
					break;
				}
				goto case VirtualKey.Home;
			case VirtualKey.Home:
			case VirtualKey.End:
			case VirtualKey.PageUp:
			case VirtualKey.PageDown:
			case VirtualKey.GamepadLeftTrigger:
			case VirtualKey.GamepadRightTrigger:
				{
					newFocusedIndex = GetFocusedIndex();
					HandleNavigationKey(key, /*scrollViewport*/ true, ref newFocusedIndex);
					// When the user presses a navigation key, we want to mark the key as handled. This prevents
					// the key presses from bubbling up to the parent ScrollViewer and inadvertently scrolling.
					handled = true;
				}
				break;
			case VirtualKey.Left:
			case VirtualKey.Right:
				// Mark left/right keys as handled to prevent navigation out of the popup, but
				// don't actually change selection.
				handled = true;
				break;
			case VirtualKey.F4:
				if (nModifierKeys == VirtualKeyModifiers.None)
				{
					IsDropDownOpen = false;
					handled = true;
				}
				break;
			default:
				global::System.Diagnostics.Debug.Assert(!handled);
				break;
		}

		if (newFocusedIndex != -1 && !skipSelection)
		{
			handled = true;
			int itemCount = GetItemCount();
			newFocusedIndex = (int)(Math.Min(newFocusedIndex, (int)(itemCount) - 1));
			if (0 <= newFocusedIndex)
			{
				var selectionChangedTrigger = SelectionChangedTrigger;

				if (IsEditable
					&& (EditableTextHasFocus() || lastPointerType == InputDeviceType.GamepadOrRemote))
				{
					SetSearchResultIndex(newFocusedIndex);

					var spItems = Items;

					var spItem = spItems[newFocusedIndex];

					UpdateEditableTextBox(spItem, true /*selectText*/, true /*selectAll*/);

					if (lastPointerType == InputDeviceType.GamepadOrRemote)
					{
						SetFocusedItem(newFocusedIndex, true /*shouldScrollIntoView*/, false /*forceFocus*/, FocusState.Keyboard, false);
					}
					else
					{
						// TODO Uno: Support ScrollIntoView
						//ScrollIntoView(
						//	newFocusedIndex,
						//	false /*isGroupItemIndex*/,
						//	false /*isHeader*/,
						//	false /*isFooter*/,
						//	false /*isFromPublicAPI*/,
						//	true  /*ensureContainerRealized*/,
						//	false /*animateIfBringIntoView*/,
						//	ScrollIntoViewAlignment.Default);
					}
				}
				else
				{
					SetFocusedItem(newFocusedIndex, true /*shouldScrollIntoView*/, false /*forceFocus*/, FocusState.Keyboard, false);
				}

				if (selectionChangedTrigger == ComboBoxSelectionChangedTrigger.Always)
				{
					SelectedIndex = newFocusedIndex;
				}
				else
				{
					OverrideSelectedIndexForVisualStates(newFocusedIndex);
				}
			}
		}

		if (handled)
		{
			args.Handled = true;
		}
	}

	private void MainKeyDown(KeyRoutedEventArgs args)
	{
		args.Handled = true;
		int newSelectedIndex = -1;
		VirtualKey keyObject = VirtualKey.None;

		var key = args.OriginalKey;
		m_inputDeviceTypeUsedToOpen = InputDeviceType.Keyboard;

		keyObject = args.Key;
		var nModifierKeys = CoreImports.Input_GetKeyboardModifiers();
		switch (key)
		{
			case VirtualKey.Escape:
				if (IsEditable)
				{
					CommitRevertEditableSearch(true /*restoreValue*/);
					ClearSelectedIndexOverrideForVisualStates();

					break;
				}
				else
				{
					args.Handled = false;
					break;
				}
			case VirtualKey.GamepadA:
				{
					if (IsEditable)
					{
						EnsureTextBoxIsEnabled(true /* moveFocusToTextBox */);
					}

					m_inputDeviceTypeUsedToOpen = InputDeviceType.GamepadOrRemote;
					IsDropDownOpen = true;
					break;
				}
			case VirtualKey.GamepadB:
				{
					if (IsEditable && EditableTextHasFocus())
					{
						Focus(FocusState.Programmatic);
					}
					else
					{
						args.Handled = false;
					}
					break;
				}
			case VirtualKey.Enter:
				if (IsEditable)
				{
					CommitRevertEditableSearch(false /*restoreValue*/);
				}
				else if (!IsInSearchingMode())
				{
					IsDropDownOpen = true;
				}
				break;
			case VirtualKey.Tab:
				if (IsEditable)
				{
					CommitRevertEditableSearch(false /*restoreValue*/);
				}

				args.Handled = false;
				break;
			case VirtualKey.Space:
				{
					if (IsEditable)
					{
						// Allow the CharacterReceived handler to handle space character.
						args.Handled = false;
					}
					else if (IsInSearchingMode())
					{
						// If we're in TextSearch mode, then process the Space key as a character so it won't get eaten by our parent.
						ProcessSearch(' ');
					}
					else
					{
						IsDropDownOpen = true;
					}
					break;
				}
			case VirtualKey.Down:
			case VirtualKey.Up:
				if (IsEditable || 0 != (nModifierKeys & VirtualKeyModifiers.Menu))
				{
					IsDropDownOpen = true;
					break;
				}
				goto case VirtualKey.Home;
			case VirtualKey.Home:
			case VirtualKey.End:
				{
					int currentSelectedIndex = SelectedIndex;
					newSelectedIndex = currentSelectedIndex;
					HandleNavigationKey(keyObject, /*scrollViewport*/ false, ref newSelectedIndex);
				}
				break;

			case VirtualKey.F4:
				if (nModifierKeys == VirtualKeyModifiers.None)
				{
					IsDropDownOpen = true;
				}
				else
				{
					args.Handled = false;
				}
				break;
			default:
				args.Handled = false;
				break;
		}

		if (0 <= newSelectedIndex)
		{
			SelectedIndex = newSelectedIndex;
		}
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
			m_IsPointerOverMain = true;
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

	private void OnElementPopupChildGotFocus(
		object pSender,
		RoutedEventArgs pArgs)
	{

		global::System.Diagnostics.Debug.Assert(!IsSmallFormFactor, "OnElementPopupChildGotFocus is not used in small form factor mode");

		var hasFocus = HasFocus();
		FocusChanged(hasFocus);
	}

	private void OnElementPopupChildLostFocus(
		 object pSender,
		 RoutedEventArgs pArgs)
	{

		global::System.Diagnostics.Debug.Assert(!IsSmallFormFactor, "OnElementPopupChildLostFocus is not used in small form factor mode");

		var hasFocus = HasFocus();
		FocusChanged(hasFocus);
	}

	private void OnElementPopupChildPointerEntered(
		 object pSender,
		 PointerRoutedEventArgs pArgs)
	{

		global::System.Diagnostics.Debug.Assert(!IsSmallFormFactor, "OnElementPopupChildPointerEntered is not used in small form factor mode");

		m_IsPointerOverPopup = true;
		UpdateVisualState();
	}

	private void OnElementPopupChildPointerExited(
		 object pSender,
		 PointerRoutedEventArgs pArgs)
	{

		m_IsPointerOverPopup = false;
		global::System.Diagnostics.Debug.Assert(!IsSmallFormFactor, "OnElementPopupChildPointerExited is not used in small form factor mode");

		UpdateVisualState();
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

	private void OnElementPopupChildSizeChanged(
		object pSender,
		SizeChangedEventArgs pArgs)
	{

		global::System.Diagnostics.Debug.Assert(!IsSmallFormFactor, "OnElementPopupChildSizeChanged is not used in small form factor mode");

		ArrangePopup(false);
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

	private void OnElementPopupChildLoaded(
		object pSender,
		RoutedEventArgs pArgs)
	{
		// Ensure searched item gets selected when the popup opens.
		if (IsEditable)
		{
			var selectionChangedTrigger = SelectionChangedTrigger;

			int currentSelectedIndex = 0;

			if (selectionChangedTrigger == ComboBoxSelectionChangedTrigger.Always || !IsSearchResultIndexSet())
			{
				int selectedIndex = SelectedIndex;

				currentSelectedIndex = selectedIndex;
			}
			else
			{
				// Ensure searched item gets selected.
				OverrideSelectedIndexForVisualStates(m_searchResultIndex);

				currentSelectedIndex = m_searchResultIndex;
			}

			if (currentSelectedIndex >= 0)
			{
				// TODO Uno: Implement ScrollIntoView in Selector
				//ScrollIntoView(
				//	currentSelectedIndex,
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

	private void OnPopupClosed(object? sender, object? args)
	{
		// Under most circumstances, this is unnecessary as it is ComboBox that closes the Popup. However for certain light-dismiss
		// scenarios (i.e. window resize) the Popup will close itself, so we need to close the ComboBox here to keep everything in
		// sync.
		if (m_tpPopupPart is not null)
		{
			// Popup.Closed is an asynchronous event, however, so we should check to make sure that the popup is still closed
			// before we do anything.
			var popupIsOpen = m_tpPopupPart.IsOpen;

			if (!popupIsOpen)
			{
				IsDropDownOpen = false;
			}
		}
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

	// should the keycode be ignored when processing characters for search
	bool ShouldIgnoreKeyCode(char keyCode) => keyCode == VK_ESCAPE;

	private void OnCharacterReceived(UIElement pSender, CharacterReceivedRoutedEventArgs pArgs)
	{
		if (IsTextSearchEnabled)
		{
			char keyCode;
			keyCode = pArgs.Character;

			// Uno specific: In WinUI this condition is only !IsEditable - however this seems to be wrong
			// as the KeyDown handling also considers IsInSearchingMode.
			if (!IsEditable && !IsInSearchingMode())
			{
				// Space should have been handled by now because we handle the Space key in the KeyDown event handler.
				// NOTE: The 2 below specifies the map type, and maps VK to CHAR
				global::System.Diagnostics.Debug.Assert(' ' != keyCode);
			}

			if (!ShouldIgnoreKeyCode(keyCode))
			{
				ProcessSearch((char)(keyCode));
			}
		}
	}

	private void OnPopupCharacterReceived(UIElement pSender, CharacterReceivedRoutedEventArgs pEventArgs)
	{
		UIElement pSenderAsUIE = pSender;

		var parentComboBox = FindParentComboBoxFromDO(pSenderAsUIE);

		if (parentComboBox != null && parentComboBox.IsTextSearchEnabled)
		{
			char keyCode;
			keyCode = pEventArgs.Character;

			if (!ShouldIgnoreKeyCode(keyCode))
			{
				parentComboBox.ProcessSearch(keyCode);
			}
		}
	}

	// Given a DependencyObject, attempt to find a ComboBox ancestor in its logical tree.
	// This method allows us to go from items within the dropdown to the ComboBox, and is useful for
	// scenarios where we need to get to the ComboBox from the items inside (like TypeAhead).
	private ComboBox? FindParentComboBoxFromDO(DependencyObject sender)
	{
		ComboBox? parentComboBox = null;
		var current = sender as FrameworkElement;

		while (current is not null)
		{
			if (current is ComboBox comboBox)
			{
				parentComboBox = comboBox;

				break;
			}

			DependencyObject parent = current.Parent;

			// Try querying for our templated parent if the logical
			// parent is null to handle the case where our target
			// element is a template part.  We don't just use
			// VisualTreeHelper because that wouldn't return our
			// parent popup; it would give us the popup root, which
			// isn't useful.
			if (parent is null)
			{
				DependencyObject parentDO = current.TemplatedParent;
				parent = parentDO;
			}

			current = parent as FrameworkElement;
		}

		return parentComboBox;
	}

	private bool HasSearchStringTimedOut()
	{
		const int timeOutInMilliseconds = 1000;

		var now = DateTime.UtcNow;

		return (now - m_timeSinceLastCharacterReceived).TotalMilliseconds > timeOutInMilliseconds;
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
				strItem = TryGetStringValue(item, m_spPropertyPathListener);

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

	private bool AreStringsEqual(string? str1, string? str2) => string.Equals(str1, str2, StringComparison.OrdinalIgnoreCase);

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

	private string TryGetStringValue(object @object, PropertyPathListener pathListener)
	{
		object spBoxedValue;
		object spObject = @object;

#if !HAS_UNO
		if (spObject is ICustomPropertyProvider spObjectPropertyAccessor)
		{
			if (pathListener != null)
			{
				// Our caller has provided us with a PropertyPathListener. By setting the source of the listener, we can pull a value out.
				// This is our boxedValue, which we effectively ToString below.
				pathListener.SetSource(spObject);
				spBoxedValue = pathListener.GetValue();
			}
			else
			{
				// No PathListener specified, but this object implements
				// ICustomPropertyProvider. Call .ToString on the object:
				return spObjectPropertyAccessor.GetStringRepresentation();
			}
		}
#else
		if (pathListener != null)
		{
			// Our caller has provided us with a PropertyPathListener. By setting the source of the listener, we can pull a value out.
			// This is our boxedValue, which we effectively ToString below.
			pathListener.SetSource(spObject);
			spBoxedValue = pathListener.GetValue();
		}
		else if (spObject is ICustomPropertyProvider spObjectPropertyAccessor)
		{
			// No PathListener specified, but this object implements
			// ICustomPropertyProvider. Call .ToString on the object:
			return spObjectPropertyAccessor.GetStringRepresentation();
		}
#endif
		else
		{
			// Try to get the string value by unboxing the object itself.
			spBoxedValue = spObject;
		}

		if (spBoxedValue != null)
		{
			if (spBoxedValue is IStringable spStringable)
			{
				// We've set a BoxedValue. If it is castable to a string, try to ToString it.
				return spStringable.ToString();
			}
			else
			{
				return FrameworkElement.GetStringFromObject(spBoxedValue);
				// We've set a BoxedValue, but we can't directly ToString it. Try to get a string out of it.
			}
		}
		else
		{
			// If we haven't found a BoxedObject and it's not Stringable, try one last time to get a string out.
			return FrameworkElement.GetStringFromObject(@object);
		}
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

	internal InputDeviceType GetInputDeviceTypeUsedToOpen() => m_inputDeviceTypeUsedToOpen;

	private void OverrideSelectedIndexForVisualStates(int selectedIndexOverride)
	{
		//global::System.Diagnostics.Debug.Assert(!CanSelectMultiple);

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
		//global::System.Diagnostics.Debug.Assert(!CanSelectMultiple);

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

	protected override AutomationPeer OnCreateAutomationPeer() => new ComboBoxAutomationPeer(this);

	private void EnsurePropertyPathListener()
	{
		if (m_spPropertyPathListener is null)
		{
			var strDisplayMemberPath = DisplayMemberPath;

			if (!string.IsNullOrEmpty(strDisplayMemberPath))
			{
				var pPropertyPathParser = new PropertyPathParser();
				pPropertyPathParser.SetSource(strDisplayMemberPath, null);

				m_spPropertyPathListener = new();
				m_spPropertyPathListener.Initialize(pOwner: null, pPropertyPathParser, fListenToChanges: false, fUseWeakReferenceForSource: false);
			}
		}
	}

	private void CreateEditableContentPresenterTextBlock()
	{
		if (m_tpEditableContentPresenterTextBlock is null)
		{
			var spTextBlock = new TextBlock();

			m_tpEditableContentPresenterTextBlock = spTextBlock;
		}
	}


#if HAS_UNO // Not ported yet
	private void ArrangePopup(bool value) { }

	private void EnsurePresenterReadyForFullMode() { }

	private void EnsurePresenterReadyForInlineMode() { }

	private void ForceApplyInlineLayoutUpdate() { }

	private void SetIsPopupPannable() { }

	private void SetClosingAnimationDirection() { }

	private void ResetCarouselPanelState() { }

	private void PlayOverlayClosingAnimation() { }

	private void PlayOverlayOpeningAnimation() { }

	private void ClearStateFlagsOnItems() { }
#endif
}

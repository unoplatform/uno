using System.Linq;
using Microsoft.UI.Xaml.Controls;
using Uno.Disposables;
using Uno.UI.Xaml.Core;

namespace Microsoft.UI.Xaml.Controls;

partial class ComboBox
{
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

		IFC_RETURN(pEditableTextPartAsTextBox->GetPointerPressedEventSourceNoRef(&pPointerPressedEventSource));
		IFC_RETURN(pPointerPressedEventSource->AddHandler(m_spEditableTextPointerPressedEventHandler.Get(), true /* handledEventsToo */));

		TappedEventSourceType* pTappedEventSource = null;

		IFC_RETURN(pEditableTextPartAsTextBox->GetTappedEventSourceNoRef(&pTappedEventSource));
		IFC_RETURN(pTappedEventSource->AddHandler(m_spEditableTextTappedEventHandler.Get(), true /* handledEventsToo */));

		if (m_tpDropDownOverlayPart is not null)
		{
			IFC_RETURN(m_spDropDownOverlayPointerEnteredHandler.AttachEventHandler(
				m_tpDropDownOverlayPart.Cast<Border>(),
				std::bind(&ComboBox::OnDropDownOverlayPointerEntered, this, _1, _2)));

			IFC_RETURN(m_spDropDownOverlayPointerExitedHandler.AttachEventHandler(
				m_tpDropDownOverlayPart.Cast<Border>(),
				std::bind(&ComboBox::OnDropDownOverlayPointerExited, this, _1, _2)));

			IFC_RETURN(m_tpDropDownOverlayPart.Cast<Border>()->put_Visibility(xaml::Visibility_Visible));
		}

		// Tells the selector to allow Custom Values.
		SetAllowCustomValues(true /*allow*/);

		m_restoreIndexSet = false;
		m_indexToRestoreOnCancel = -1;
		ResetSearch();
		ResetSearchString();

		wrl::ComPtr<xaml_controls::IInputValidationContext> context;
		IFC_RETURN(get_ValidationContext(&context));
		IFC_RETURN(pEditableTextPartAsTextBox->put_ValidationContext(context.Get()));

		wrl::ComPtr<xaml_controls::IInputValidationCommand> command;
		IFC_RETURN(get_ValidationCommand(&command));
		IFC_RETURN(pEditableTextPartAsTextBox->put_ValidationCommand(command.Get()));

		if (m_tpPopupPart)

		{
			IFC_RETURN(m_tpPopupPart.Cast<Popup>()->put_OverlayInputPassThroughElement(this));
		}

		m_isEditModeConfigured = true;

		return S_OK;
	}

	private void DisableEditableMode()
	{
		if (!m_isEditModeConfigured || m_tpEditableTextPart is null)
		{
			return;
		}

		if (m_tpPopupPart)
		{
			IFC_RETURN(m_tpPopupPart.Cast<Popup>()->put_OverlayInputPassThroughElement(null));
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

			IFC_RETURN(m_spDropDownOverlayPointerEnteredHandler.DetachEventHandler(pDropDownOverlayPartAsI));
			IFC_RETURN(m_spDropDownOverlayPointerExitedHandler.DetachEventHandler(pDropDownOverlayPartAsI));

			IFC_RETURN(m_tpDropDownOverlayPart.Cast<Border>()->put_Visibility(xaml::Visibility_Collapsed));
		}

		PointerPressedEventSourceType* pPointerPressedEventSource = null;

		IFC_RETURN(pEditableTextPartAsTextBox->GetPointerPressedEventSourceNoRef(&pPointerPressedEventSource));
		IFC_RETURN(pPointerPressedEventSource->RemoveHandler(m_spEditableTextPointerPressedEventHandler.Get()));

		TappedEventSourceType* pTappedEventSource = null;

		IFC_RETURN(pEditableTextPartAsTextBox->GetTappedEventSourceNoRef(&pTappedEventSource));
		IFC_RETURN(pTappedEventSource->RemoveHandler(m_spEditableTextTappedEventHandler.Get()));

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
			IFC_RETURN(SetContentPresenter(-1));
			SelectedItem = null;
		}

		// Tells the selector to prevent Custom Values.
		SetAllowCustomValues(false /*allow*/);

		m_tpEditableTextPart.Text = null;

		m_isEditModeConfigured = false;
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
}

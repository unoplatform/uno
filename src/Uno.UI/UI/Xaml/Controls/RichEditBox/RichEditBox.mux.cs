// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference dxaml/xcp/core/native/text/Controls/RichEditBox.cpp, commit 3c9c168844f06c6ac000a97977f0bb3f4c90fd75
#nullable enable

using System;
using Microsoft.UI.Input;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml.Input;
using Windows.System;

namespace Microsoft.UI.Xaml.Controls;

partial class RichEditBox
{
	//---------------------------------------------------------------------------
	//
	//  Synopsis:
	//      Initializes a new instance.
	//
	//---------------------------------------------------------------------------
	private void InitializeRichEditBoxCore()
	{
		// m_bAcceptsReturn is represented by AcceptsReturnProperty's default value.
		m_linkHoverTarget = LinkMouseHoverTarget.None;
		m_eNonHyperlinkCursor = InputSystemCursorShape.IBeam;
	}

	//---------------------------------------------------------------------------
	//
	//  Synopsis:
	//      Releases resources held by this class.
	//
	//---------------------------------------------------------------------------
#if !HAS_UNO
	// TODO Uno: Native Text Services destruction is replaced by OnUnloaded and managed ownership.
	// CRichEditBox::~CRichEditBox()
	// {
	//     Destroy();
	// }

	//---------------------------------------------------------------------------
	//
	//  Synopsis:
	//      Allocates a new CRichEditBox.
	//
	//---------------------------------------------------------------------------
	// TODO Uno: The C# constructor replaces the native factory and generated peer allocation.
	// xref_ptr<CRichEditBox> pRichEditBox;
	// pRichEditBox.attach(new CRichEditBox(pCreate->m_pCore));
	// IFC_RETURN(pRichEditBox->Initialize());
	// *ppObject = pRichEditBox.detach();
	// return S_OK;

	// TODO Uno: Typed events and DependencyProperty registrations replace native metadata IDs.
	// KnownEventIndex CRichEditBox::GetCopyPropertyID() const { return KnownEventIndex::RichEditBox_CopyingToClipboard; }
	// KnownEventIndex CRichEditBox::GetPastePropertyID() const { return KnownEventIndex::RichEditBox_Paste; }
	// KnownEventIndex CRichEditBox::GetCutPropertyID() const { return KnownEventIndex::RichEditBox_CuttingToClipboard; }
	// KnownPropertyIndex CRichEditBox::GetSelectionHighlightColorPropertyID() const { return KnownPropertyIndex::RichEditBox_SelectionHighlightColor; }
	// KnownPropertyIndex CRichEditBox::GetSelectionHighlightColorWhenNotFocusedPropertyID() const { return KnownPropertyIndex::RichEditBox_SelectionHighlightColorWhenNotFocused; }
	// TextCompositionStage::CompositionStarted -> KnownEventIndex::RichEditBox_TextCompositionStarted
	// TextCompositionStage::CompositionChanged -> KnownEventIndex::RichEditBox_TextCompositionChanged
	// TextCompositionStage::CompositionEnded -> KnownEventIndex::RichEditBox_TextCompositionEnded
#endif

	//---------------------------------------------------------------------------
	//
	//  Synopsis:
	//      Sets a DependencyProperty value.
	//
	//---------------------------------------------------------------------------
	private void SetValueCore(DependencyPropertyChangedEventArgs args)
	{
		// Uno's dependency-property system performs coercion and owns the reentrancy guard before this callback.
		switch (args.Property)
		{
			case var property when property == ClipboardCopyFormatProperty:
#if !HAS_UNO
				// TODO Uno: The clipboard adapter reads ClipboardCopyFormat directly instead of storing a RichEdit language option.
				// IFC(SetLanguageOption(IMF_COPYPLAINTEXTONLY,
				//     static_cast<DirectUI::RichEditClipboardFormat>(tempValue.AsEnum()) == DirectUI::RichEditClipboardFormat::PlainText));
#endif
				break;
			case var property when property == DesiredCandidateWindowAlignmentProperty:
				OnDesiredCandidateWindowAlignmentChanged(this, args);
				break;
			case var property when property == TextWrappingProperty:
				OnTextWrappingChanged(this, args);
				InvalidateView();
				break;
			case var property when property == IsReadOnlyProperty:
				OnIsReadOnlyChanged(this, args);
				break;
			case var property when property == AcceptsReturnProperty:
				OnAcceptsReturnChanged(this, args);
				break;
			case var property when property == TextAlignmentProperty:
				OnTextAlignmentChanged(this, args);
				break;
			case var property when property == IsSpellCheckEnabledProperty:
				OnIsSpellCheckEnabledChanged(this, args);
				break;
			case var property when property == IsTextPredictionEnabledProperty:
				OnIsTextPredictionEnabledChanged(this, args);
				break;
			case var property when property == InputScopeProperty:
				OnInputScopeChanged(this, args);
				break;
			case var property when property == IsColorFontEnabledProperty:
				OnIsColorFontEnabledChanged(this, args);
				break;
			case var property when property == SelectionHighlightColorProperty:
				OnSelectionHighlightColorChanged(this, args);
				break;
			case var property when property == SelectionHighlightColorWhenNotFocusedProperty:
				OnSelectionHighlightColorChanged(this, args);
				break;
			case var property when property == TextReadingOrderProperty:
				OnTextReadingOrderChanged(this, args);
				break;
			case var property when property == MaxLengthProperty:
				OnMaxLengthChanged(this, args);
				break;
			case var property when property == DisabledFormattingAcceleratorsProperty:
				OnFormattingAcceleratorsChanged();
				break;
#if HAS_UNO
			// Both alignment properties share m_textAlignment in the native core.
			case var property when property == HorizontalTextAlignmentProperty:
				OnHorizontalTextAlignmentChanged(this, args);
				break;
#endif
		}
	}

	//---------------------------------------------------------------------------
	//
	//  Synopsis:
	//      Gets the inner RichEdit document.
	//
	//---------------------------------------------------------------------------
	private RichEditTextDocument GetDocument()
	{
#if HAS_UNO
		return _document ??= new RichEditTextDocument(this);
#else
		// TODO Uno: WinUIEdit's TOM implementation is not in the WinUI repository. Use the managed document adapter.
		// return GetTextServices()->QueryInterface(iid, reinterpret_cast<void **>(ppDocument));
#endif
	}

	//---------------------------------------------------------------------------
	//
	//  Synopsis:
	//      Initializes a new instance.
	//
	//---------------------------------------------------------------------------
#if !HAS_UNO
	// TODO Uno: CTextBoxBase::Initialize creates Windows Text Services, replaced by ImeSessionCoordinator and TextBoxView.
	// IFC_RETURN(CTextBoxBase::Initialize());
	// IsTextPredictionEnabled defaults to true, so init that here.
	// IFC_RETURN(OnIsTextPredictionEnabledChanged(m_isTextPredictionEnabled));
	// return S_OK;
#endif

	//---------------------------------------------------------------------------
	//
	//  Synopsis:
	//      Called after RichEdit modifies the backing store.
	//
	//---------------------------------------------------------------------------
	private void OnContentChanged(bool fTextChanged)
	{
		InvalidateView();

		//
		// Raise TextChanging and TextChanged events.
		//
#if HAS_UNO
		// The managed notification adapter preserves synchronous TextChanging and queues the routed TextChanged event.
		// It also coalesces reentrant engine edits before exposing a final document snapshot.
		var change = PrepareTextChangedNotification(fTextChanged);
		QueueTextChangedNotification(change);
#else
		// TODO Uno: Native parser ownership and CEventManager are represented by the managed notification adapter.
		// if (!ParserOwnsParent())
		// {
		//     CEventManager *eventManager = core->GetEventManager();
		//     if (eventManager)
		//     {
		//         // We cannot use EventManager here due to MSFT:1993154 so raising through the peer directly as a workaround
		//         IFC_RETURN(FxCallbacks::RichEditBox_OnTextChangingHandler(this, fTextChanged));
		//         xref_ptr<CRoutedEventArgs> args;
		//         args.attach(new CRoutedEventArgs());
		//         eventManager->RaiseRoutedEvent(EventHandle(KnownEventIndex::RichEditBox_TextChanged), this, args);
		//         if (core->UIAClientsAreListening(UIAXcp::AETextPatternOnTextChanged) == S_OK)
		//         {
		//             if (!m_pAP) { OnCreateAutomationPeer(); }
		//             if (m_pAP) { m_pAP->RaiseAutomationEvent(UIAXcp::AETextPatternOnTextChanged); }
		//         }
		//     }
		// }
#endif

		// TextBox in the XAML layer needs to be notified when pText is changed so it can
		// update Placeholder Text visiblity.
		ShowPlaceholderTextHandler(IsEmpty());
	}

	//------------------------------------------------------------------------
	//
	//  Synopsis:
	//      Called whenever the selection is changed. Fires a selection
	//      changed event.
	//
	//------------------------------------------------------------------------
#if !HAS_UNO
	// TODO Uno: ProcessSelectionChange performs this transaction before synchronizing the managed view.
	// The source cancellation policy is ported in ProcessSelectionChangingEvent in RichEditBox.textBoxBase.mux.cs.
	// IFC_RETURN(GetSelection(&spSelection));
	// IFC_RETURN(spSelection->GetStart(&selectionStart));
	// IFC_RETURN(spSelection->GetEnd(&selectionEnd));
	// selectionLength = selectionEnd - selectionStart;
	// An empty selection with non-zero length is the EOP and we don't want to select it.
	// if (IsEmpty())
	// {
	//     long nLength;
	//     IFC_RETURN(spSelection->GetCch(&nLength));
	//     if (nLength != 0)
	//     {
	//         // SetRange causes re-entrancy back into the function.
	//         // Re-entrancy will occur and the goto Cleanup avoids having us run all the code below the goto twice.
	//         IFC_RETURN(spSelection->SetRange(selectionStart, selectionStart));
	//         return S_OK;
	//     }
	// }
	// BOOLEAN SelectionChangingCanceled;
	// IFC_RETURN(ProcessSelectionChangingEvent(selectionStart, selectionLength, SelectionChangingCanceled));
	// if (SelectionChangingCanceled) { return S_OK; }
	// IFC_RETURN(CTextBoxBase::OnSelectionChanged());
	// Selection changed. Proofing menu is no longer valid.
	// if (m_iSelectionStart != selectionStart || m_iSelectionLength != selectionLength) { m_proofingMenuIsValid = false; }
	// m_iSelectionStart = selectionStart;
	// m_iSelectionLength = selectionLength;
	// Raise SelectionChanged event.
	// pEventManager = GetContext()->GetEventManager();
	// if (pEventManager)
	// {
	//     xref_ptr<CRoutedEventArgs> spArgs;
	//     spArgs.init(new CRoutedEventArgs());
	//     pEventManager->RaiseRoutedEvent(EventHandle(KnownEventIndex::RichEditBox_SelectionChanged), this, spArgs);
	//     if (m_pAP) { m_pAP->RaiseAutomationEvent(UIAXcp::AETextPatternOnTextSelectionChanged); }
	// }
#endif

	//---------------------------------------------------------------------------
	//
	//  Synopsis:
	//      Returns TRUE if this control processes cr/lf from user input.
	//
	//---------------------------------------------------------------------------
	// The AcceptsReturn property projects m_bAcceptsReturn.

	//---------------------------------------------------------------------------
	//
	//  Synopsis:
	//      Returns TRUE if this control contains formatted text, otherwise the
	//      control only contains plaintext Unicode.
	//
	//---------------------------------------------------------------------------
	internal bool AcceptsRichText() => true;

	//---------------------------------------------------------------------------
	//
	//  Synopsis:
	//      Gets the current TextAlignment.
	//
	//---------------------------------------------------------------------------
	private TextAlignment GetAlignment() => TextAlignment;

	//---------------------------------------------------------------------------
	//
	//  Synopsis:
	//      Returns TRUE if this control cannot be modified by the user.
	//
	//---------------------------------------------------------------------------
	// The IsReadOnly property projects m_bIsReadOnly.

	//---------------------------------------------------------------------------
	//
	//  Synopsis:
	//      Returns TRUE if this control has no content, FALSE otherwise.
	//
	//---------------------------------------------------------------------------
	private bool IsEmpty() => Document.GetRange(0, 2).StoryLength <= 1;

	//---------------------------------------------------------------------------
	//
	//  Synopsis:
	//      TextServices callback, notifies the host about content or selection
	//      changes.
	//
	//---------------------------------------------------------------------------
#if !HAS_UNO
	// TODO Uno: EN_LINK and EN_HIDELINKTOOLTIP are produced by managed pointer hit testing instead of WinUIEdit.
	// IFC_RETURN(CTextBoxBase::TxNotify(notification, pData));
	// switch (notification)
	// {
	//     case EN_LINK:
	//         IFC_RETURN(HandleLinkNavigation(notification, static_cast<ENLINK*>(pData)->msg, pData));
	//         break;
	//     case EN_HIDELINKTOOLTIP:
	//         // Invoked when mouse leaves the HyperLink
	//         m_linkHoverTarget = LinkMouseHoverTarget::None;
	//         if (GetView()) { IFC_RETURN(GetView()->SetCursor(m_eNonHyperlinkCursor)); }
	//         break;
	// }
#endif

	//---------------------------------------------------------------------------
	//
	//  Synopsis:
	//      TextServices callback, notifies the host about an input action over a link
	//      This method inspects the input action and performs a navigation if necessary
	//
	//---------------------------------------------------------------------------
	private void HandleLinkNavigation(bool overLink, VirtualKeyModifiers modifierKeys)
	{
		if (!overLink)
		{
			// Invoked when mouse leaves the HyperLink
			m_linkHoverTarget = LinkMouseHoverTarget.None;
			SetLinkCursor(m_eNonHyperlinkCursor);
			return;
		}

		m_linkHoverTarget = LinkMouseHoverTarget.HyperLink;
		var navigateCursor = ShouldNavigateLinkOnMouseClick(modifierKeys)
			? InputSystemCursorShape.Arrow
			: InputSystemCursorShape.IBeam;
		SetLinkCursor(navigateCursor);
#if !HAS_UNO
		// TODO Uno: Pointer-up/Enter/UIA activation comes through TryNavigateLinkAt; no WM_* or ENLINK pointers cross the adapter.
		// Enter key is sent to TxNotify as mouse left button down. Navigate
		// on keyboard Enter key. Also, it supports the case when call is coming from UIA client
		// in which case there is no Mouse/Keyboard related message.
		// IFC_RETURN(CTextBoxBase::GetDocument(spTextDocument.GetAddressOf()));
		// IFC_RETURN(spTextDocument->Range(pEnLink->chrg.cpMin, pEnLink->chrg.cpMax, spTextRange.GetAddressOf()));
		// IFC_RETURN(spTextRange->GetText(&spLinkText));
		// IFC_RETURN(FxCallbacks::RichEditBox_HandleHyperlinkNavigation(spLinkText.get(), SysStringLen(spLinkText.get())));
#endif
	}

	//---------------------------------------------------------------------------
	//
	//  Synopsis:
	//      Determines if current input state should cause a link navigation.
	//
	//---------------------------------------------------------------------------
	private bool ShouldNavigateLinkOnMouseClick(VirtualKeyModifiers modifierKeys)
	{
		// For mouse input, hyperlink navigated with click in readonly mode and
		// Ctrl+Click in editable mode.
		if (IsReadOnly)
		{
			return true;
		}

#if HAS_UNO
		// Use the platform command modifier on macOS, as in the shared text editor.
		return (modifierKeys & _platformCtrlKey) != 0;
#else
		return (modifierKeys & VirtualKeyModifiers.Control) != 0;
#endif
	}

	//---------------------------------------------------------------------------
	//
	//  Synopsis:
	//       Event handler for KeyDown event.
	//
	//---------------------------------------------------------------------------
	private void OnKeyDownCore(KeyRoutedEventArgs pKeyEventArgs)
	{
#if !HAS_UNO
		// TODO Uno: The editor processes text keys in OnPostKeyDown, after routed handlers have had a chance to cancel.
		// IFC_RETURN(CTextBoxBase::OnKeyDown(pEventArgs));
#endif
		// If the mouse is over a hyperlink and Ctrl key is now pressed, change the cursor
		// to hyperlink cursor
		if (m_linkHoverTarget == LinkMouseHoverTarget.HyperLink
			&& !IsReadOnly
			&& _textBoxView is not null
			&& pKeyEventArgs.Key == VirtualKey.Control)
		{
			SetLinkCursor(InputSystemCursorShape.Arrow);
		}
	}

	//---------------------------------------------------------------------------
	//
	//  Synopsis:
	//       Event handler for KeyUp event.
	//
	//---------------------------------------------------------------------------
	private void OnKeyUpCore(KeyRoutedEventArgs pKeyEventArgs)
	{
		// If the mouse is over a hyperlink and the Ctrl key is released, revert the cursor
		// back to non-hyperlink state.
		if (m_linkHoverTarget == LinkMouseHoverTarget.HyperLink
			&& !IsReadOnly
			&& _textBoxView is not null
			&& pKeyEventArgs.Key == VirtualKey.Control)
		{
			SetLinkCursor(m_eNonHyperlinkCursor);
		}
	}

	//---------------------------------------------------------------------------
	//
	//  Synopsis:
	//      Parameter validation for SetValue calls.
	//
	//---------------------------------------------------------------------------
	private static void ValidateSetValueArguments(DependencyProperty pProperty, object value)
	{
#if !HAS_UNO
		// The source tests TextBox_TextWrapping, not RichEditBox_TextWrapping. Do not reject WrapWholeWords on RichEditBox.
		// case KnownPropertyIndex::TextBox_TextWrapping:
		//     // TextWrapping enum is shared between *Block and *Box. As specified,
		//     // WrapWithOverflow is an invalid argument.
		//     if (IsWrapWholeWords(value)) { IFC_RETURN(E_INVALIDARG); }
		//     break;
#endif
		if (pProperty == MaxLengthProperty && (int)value < 0)
		{
			throw new ArgumentException("MaxLength cannot be negative.", nameof(value));
		}
	}

	//------------------------------------------------------------------------
	//
	//  Synopsis:
	//      Performs late template-dependent initialization. Attaches the
	//      control to the container element from the template. Initializes
	//      visual states.
	//
	//------------------------------------------------------------------------
	/// <inheritdoc />
	protected override void OnApplyTemplate()
	{
		// Delay updating richedit for spell checking until OnApplyTemplate, this is to optimize perf (avoid start/stop if spellchecking is turned off in markup)
		UpdateDefaultSpellChecking();
		OnFormattingAcceleratorsChanged();
		base.OnApplyTemplate();
#if HAS_UNO
		ApplyManagedTemplate();
#endif
	}

	//------------------------------------------------------------------------
	//
	//  Synopsis:
	//      Event handler for CharacterReceived event.
	//
	//------------------------------------------------------------------------
#if !HAS_UNO
	// TODO Uno: OnPostKeyDown/IImeSessionHost replace the native character dispatcher; OnContentChanged updates the placeholder.
	// IFC_RETURN(CTextBoxBase::OnCharacterReceived(pEventArgs));
	// TextBox in the XAML layer needs to be notified when pText is changed so it can
	// update Placeholder Text visiblity.
	// IFC_RETURN(FxCallbacks::RichEditBox_ShowPlaceholderTextHandler(this, IsEmpty()));
#endif

	//---------------------------------------------------------------------------
	//
	//  Synopsis:
	//      Sets the RichEdit buffer.
	//
	//  Notes:
	//      This method resets the RichEdit state, clearing undo and the selection
	//      state.
	//
	//---------------------------------------------------------------------------
#if !HAS_UNO
	// TODO Uno: RichEditTextDocument's SetText/LoadFromStream adapters own buffer, selection and history transactions.
	// _In_ const xstring_ptr& strText
	//     // If NULL, removes all content.
	// IFC_RETURN(CTextBoxBase::SetTextServicesBuffer(strText));
	// TextBox in the XAML layer needs to be notified when pText is changed so it can
	// update Placeholder Text visiblity.
	// IFC_RETURN(FxCallbacks::RichEditBox_ShowPlaceholderTextHandler(this, strText.IsNull()));
#endif

	//------------------------------------------------------------------------
	//
	//  Synopsis:
	//      Gets the InputScopeNameValue associated with this text control. If no
	//      InputScopeNameValue has been set, returns InputScopeNameValueDefault.
	//
	//------------------------------------------------------------------------
	private InputScopeNameValue GetInputScope()
	{
		var result = InputScopeNameValue.Default;
		if (InputScope is { Names.Count: > 0 } inputScope)
		{
			result = inputScope.Names[0].NameValue;
		}
		return result;
	}

	private void OnFormattingAcceleratorsChanged()
	{
		var disabledFormattingAccelerators = DisabledFormattingAccelerators;
		var lparam = DisabledFormattingAccelerators.Bold | DisabledFormattingAccelerators.Italic | DisabledFormattingAccelerators.Underline;
		if ((disabledFormattingAccelerators & DisabledFormattingAccelerators.Bold) != 0)
		{
			lparam &= ~DisabledFormattingAccelerators.Bold;
		}
		if ((disabledFormattingAccelerators & DisabledFormattingAccelerators.Italic) != 0)
		{
			lparam &= ~DisabledFormattingAccelerators.Italic;
		}
		if ((disabledFormattingAccelerators & DisabledFormattingAccelerators.Underline) != 0)
		{
			lparam &= ~DisabledFormattingAccelerators.Underline;
		}
#if HAS_UNO
		_enabledFormattingAccelerators = lparam;
#else
		// TODO Uno: The managed key dispatcher consumes the mask instead of EM_SETFORMATACCELERATORS.
		// IFC_RETURN(GetTextServices()->TxSendMessage(EM_SETFORMATACCELERATORS, 0, lparam, nullptr));
#endif
	}

	internal override void UpdateVisualState(bool useTransitions = true) => UpdateVisualStateCore(useTransitions);
}

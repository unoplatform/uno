// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Windows.ApplicationModel.DataTransfer;
using Windows.UI.Text;
using Windows.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Controls;

partial class TextCommandBarFlyout
{
	public TextCommandBarFlyout()
	{
		//__RP_Marker_ClassById(RuntimeProfiler.ProfId_TextCommandBarFlyout);

		Opening({
			[this](var &, var &)


		{
				UpdateButtons();
			}
		});

		Opened({
			[this](var &, var &)


		{
				// If there aren't any primary commands and we aren't opening expanded,
				// or if there are just no commands at all, then we'll have literally no UI to show. 
				// We'll just close the flyout in that case - nothing should be opening us
				// in this state anyway, but this makes sure we don't have a light-dismiss layer
				// with nothing visible to light dismiss.

				IFlyoutBase5 thisAsFlyoutBase5 = this;
				var showModeIsStandard = [thisAsFlyoutBase5]()

			{
					if (thisAsFlyoutBase5)
					{
						return thisAsFlyoutBase5.ShowMode() == FlyoutShowMode.Standard;
					}
					return true;
				} ();

				if (PrimaryCommands.Count == 0 &&
					(SecondaryCommands.Count == 0 || (!m_commandBar.IsOpen && !showModeIsStandard)))
				{
					Hide();
				}
			}
		});
	}

	//void InitializeButtonWithUICommand(
	//	ButtonBase & button,
	//	XamlUICommand & uiCommand,
	//	std.function<void()> & executeFunc)
	//{
	//	m_buttonCommandRevokers.push_back(uiCommand.ExecuteRequested(auto_revoke, [executeFunc](var &, var &) { executeFunc(); }));
	//	button.Command(uiCommand);
	//}

	//void InitializeButtonWithProperties(
	//	ButtonBase & button,
	//	ResourceIdType labelResourceId,
	//	ResourceIdType acceleratorKeyResourceId,
	//	ResourceIdType descriptionResourceId,
	//	std.function<void()> & executeFunc)
	//{
	//	AppBarButton elementAsButton = button as AppBarButton;
	//	AppBarToggleButton elementAsToggleButton = button as AppBarToggleButton;

	//	MUX_MUX_ASSERT(elementAsButton || elementAsToggleButton);

	//	if (!ResourceAccessor.IsResourceIdNull(labelResourceId))
	//	{
	//		hstring label{ ResourceAccessor.GetLocalizedStringResource(labelResourceId) };

	//		if (elementAsButton)
	//		{
	//			elementAsButton.Label(label);
	//		}
	//		else
	//		{
	//			elementAsToggleButton.Label(label);
	//		}
	//	}

	//	if (!ResourceAccessor.IsResourceIdNull(descriptionResourceId))
	//	{
	//		hstring description{ ResourceAccessor.GetLocalizedStringResource(descriptionResourceId) };

	//		AutomationProperties.SetHelpText(button, description);
	//		ToolTipService.SetToolTip(button, description);
	//	}

	//	if (elementAsToggleButton)
	//	{
	//		m_toggleButtonCheckedRevokers.push_back(elementAsToggleButton.Checked(auto_revoke, [executeFunc](var &, var &) { executeFunc(); }));
	//		m_toggleButtonUncheckedRevokers.push_back(elementAsToggleButton.Unchecked(auto_revoke, [executeFunc](var &, var &) { executeFunc(); }));
	//	}
	//	else
	//	{
	//		m_buttonClickRevokers.push_back(button.Click(auto_revoke, [executeFunc](var &, var &) { executeFunc(); }));
	//	}

	//	if (!ResourceAccessor.IsResourceIdNull(acceleratorKeyResourceId))
	//	{
	//		if (UIElement7 elementAsUIElement7 = button)
	//       {
	//			hstring acceleratorKeyString{ ResourceAccessor.GetLocalizedStringResource(acceleratorKeyResourceId) };

	//			if (acceleratorKeyString.Count > 0)
	//			{
	//				char acceleratorKeyChar = acceleratorKeyString[0.0;
	//				VirtualKey acceleratorKey = SharedHelpers.GetVirtualKeyFromChar(acceleratorKeyChar);

	//				if (acceleratorKey != VirtualKey.None)
	//				{
	//					KeyboardAccelerator keyboardAccelerator;
	//					keyboardAccelerator.Key(acceleratorKey);
	//					keyboardAccelerator.Modifiers(VirtualKeyModifiers.Control);

	//					elementAsUIElement7.KeyboardAccelerators().Add(keyboardAccelerator);
	//				}
	//			}
	//		}
	//	}
	//}

	//void InitializeButtonWithProperties(
	//	ButtonBase & button,
	//	ResourceIdType labelResourceId,
	//	Symbol & symbol,
	//	ResourceIdType acceleratorKeyResourceId,
	//	ResourceIdType descriptionResourceId,
	//	std.function<void()> & executeFunc)
	//{
	//	InitializeButtonWithProperties(
	//		button,
	//		labelResourceId,
	//		acceleratorKeyResourceId,
	//		descriptionResourceId,
	//		executeFunc);

	//	AppBarButton elementAsButton = button as AppBarButton;
	//	AppBarToggleButton elementAsToggleButton = button as AppBarToggleButton;

	//	MUX_MUX_ASSERT(elementAsButton || elementAsToggleButton);

	//	SymbolIcon symbolIcon{ SymbolIcon(symbol) };

	//	if (elementAsButton)
	//	{
	//		elementAsButton.Icon(symbolIcon);
	//	}
	//	else
	//	{
	//		elementAsToggleButton.Icon(symbolIcon);
	//	}
	//}

	//void UpdateButtons()
	//{
	//	PrimaryCommands.Clear();
	//	SecondaryCommands.Clear();

	//	var buttonsToAdd = GetButtonsToAdd();
	//	var addButtonToCommandsIfPresent =

	//	   [buttonsToAdd, this](var buttonType, var commandsList)

	//	{
	//		if ((buttonsToAdd & buttonType) != TextControlButtons.None)
	//		{
	//			commandsList.Add(GetButton(buttonType));
	//		}
	//	};
	//	var addRichEditButtonToCommandsIfPresent =

	//	   [buttonsToAdd, this](var buttonType, var commandsList, var getIsChecked)

	//	{
	//		if ((buttonsToAdd & buttonType) != TextControlButtons.None)
	//		{
	//			var richEditBoxTarget = Target() as RichEditBox;
	//			var toggleButton{ GetButton(buttonType).as< AppBarToggleButton > () };
	//			var selection{ SharedHelpers.GetRichTextSelection(richEditBoxTarget) };

	//			if (selection)
	//			{
	//				var initializingButtons = gsl.finally([this]()

	//				{
	//					m_isSettingToggleButtonState = false;
	//				});
	//				m_isSettingToggleButtonState = true;
	//				toggleButton.IsChecked(getIsChecked(selection));
	//				}

	//				commandsList.Add(toggleButton);
	//			}
	//		};

	//		FlyoutBase proofingFlyout{ null };

	//		if (var textBoxTarget = Target() as ITextBox8())
	//   {
	//			proofingFlyout = textBoxTarget.ProofingMenuFlyout();
	//		}

	//else if (var richEditBoxTarget = Target() as IRichEditBox8())
	//   {
	//			proofingFlyout = richEditBoxTarget.ProofingMenuFlyout();
	//		}

	//		MenuFlyout proofingMenuFlyout = proofingFlyout as MenuFlyout;

	//		bool shouldIncludeProofingMenu =
	//			(bool)(proofingFlyout) &&
	//			!proofingMenuFlyout || proofingMenuFlyout.Items.Count > 0;

	//		if (shouldIncludeProofingMenu)
	//		{
	//			m_proofingButton = AppBarButton{ };
	//			m_proofingButton.Label(ResourceAccessor.GetLocalizedStringResource(SR_ProofingMenuItemLabel));
	//			m_proofingButton.Flyout(proofingFlyout);

	//			m_proofingButtonLoadedRevoker = m_proofingButton.Loaded(auto_revoke,

	//				[this](var &, var &)

	//		{
	//				// If we have a proofing menu, we'll start with it open by invoking the button
	//				// as soon as the CommandBar opening animation completes.
	//				// We invoke the button instead of just showing the flyout to make the button
	//				// properly update its visual state as well.
	//				// If we have an open animation that we'll be executing, we'll postpone showing
	//				// the proofing menu until it's given a chance to get underway.
	//				// Otherwise, we'll just show the proofing menu immediately.
	//				if (var commandBar = get_self<CommandBarFlyoutCommandBar>(m_commandBar))
	//               {
	//					var strongThis = get_strong();
	//					var openProofingMenuAction = [strongThis]()

	//				{
	//						// There isn't likely to be any way that the proofing button was deleted
	//						// between us scheduling this action and us actually executing it,
	//						// but it doesn't hurt to be resilient when we're scheduling an action
	//						// to occur in the future.
	//						if (strongThis.m_proofingButton)
	//						{
	//							var peer = strongThis.m_proofingButton.OnCreateAutomationPeer().as< ButtonAutomationPeer > ();
	//							peer.Invoke();
	//						}
	//					};

	//					if (commandBar.HasOpenAnimation() && commandBar.IsOpen)
	//					{
	//						// Allowing 100 ms to elapse before we open the proofing menu gives the proofing menu enough time
	//						// to be mostly open when the proofing menu opens (so the menu doesn't look detached from the CommandBarFlyout UI),
	//						// but doesn't wait long enough that there's a noticeable pause in between the user feeling like
	//						// the CommandBarFlyout is fully open and the proofing menu finally opening.
	//						// The exact number comes from design.
	//						SharedHelpers.ScheduleActionAfterWait(openProofingMenuAction, 100.0;
	//					}
	//					else
	//					{
	//						openProofingMenuAction();
	//					}
	//				}
	//			});

	//			// We want interactions with any proofing menu element to close the entire flyout,
	//			// same as interactions with secondary commands, so we'll attach click event handlers
	//			// to the proofing menu items (if they exist) to handle that.
	//			if (proofingMenuFlyout)
	//			{
	//				m_proofingMenuItemClickRevokers.clear();
	//				m_proofingMenuToggleItemClickRevokers.clear();

	//				var closeFlyoutFunc = [this](var sender, var args) { Hide(); };

	//				// We might encounter MenuFlyoutSubItems, so we'll add them to this list
	//				// in order to ensure that we hook up handlers to their entries as well.
	//				std.list<IVector<MenuFlyoutItemBase>> itemsList;
	//				itemsList.push_back(proofingMenuFlyout.Items);

	//				while (!itemsList.empty())
	//				{
	//					var currentItems = itemsList.front();
	//					itemsList.pop_front();

	//					for (uint i = 0; i < currentItems.Count; i++)
	//					{
	//						if (var menuFlyoutItem = currentItems[i] as MenuFlyoutItem)
	//                   {
	//						m_proofingMenuItemClickRevokers.push_back(menuFlyoutItem.Click(auto_revoke, closeFlyoutFunc));
	//					}

	//				else if (var toggleMenuFlyoutItem = currentItems[i] as ToggleMenuFlyoutItem)
	//                   {
	//						m_proofingMenuToggleItemClickRevokers.push_back(toggleMenuFlyoutItem.Click(auto_revoke, closeFlyoutFunc));
	//					}

	//				else if (var menuFlyoutSubItem = currentItems[i] as MenuFlyoutSubItem)
	//                   {
	//						itemsList.push_back(menuFlyoutSubItem.Items);
	//					}
	//				}
	//			}
	//		}

	//		SecondaryCommands.Add(m_proofingButton);
	//	}

	//else
	//	{
	//		m_proofingButton = null;
	//	}

	//	IFlyoutBase5 thisAsFlyoutBase5 = this;

	//	var commandListForCutCopyPaste =
	//		thisAsFlyoutBase5 && thisAsFlyoutBase5.InputDevicePrefersPrimaryCommands ?
	//		PrimaryCommands :
	//		SecondaryCommands;

	//	addButtonToCommandsIfPresent(TextControlButtons.Cut, commandListForCutCopyPaste);
	//	addButtonToCommandsIfPresent(TextControlButtons.Copy, commandListForCutCopyPaste);
	//	addButtonToCommandsIfPresent(TextControlButtons.Paste, commandListForCutCopyPaste);

	//	addRichEditButtonToCommandsIfPresent(TextControlButtons.Bold, PrimaryCommands,

	//		[](ITextSelection textSelection) { return textSelection.CharacterFormat().Bold() == FormatEffect.On; });
	//	addRichEditButtonToCommandsIfPresent(TextControlButtons.Italic, PrimaryCommands,

	//		[](ITextSelection textSelection) { return textSelection.CharacterFormat().Italic() == FormatEffect.On; });
	//	addRichEditButtonToCommandsIfPresent(TextControlButtons.Underline, PrimaryCommands,

	//		[](ITextSelection textSelection)

	//{
	//		var underline = textSelection.CharacterFormat().Underline();
	//		return (underline != UnderlineType.None) && (underline != UnderlineType.Undefined);
	//	});

	//	addButtonToCommandsIfPresent(TextControlButtons.Undo, SecondaryCommands);
	//	addButtonToCommandsIfPresent(TextControlButtons.Redo, SecondaryCommands);
	//	addButtonToCommandsIfPresent(TextControlButtons.SelectAll, SecondaryCommands);
	//}

	//TextControlButtons GetButtonsToAdd()
	//{
	//	TextControlButtons buttonsToAdd = TextControlButtons.None;
	//	var target = Target();

	//	if (target is TextBox textBoxTarget)
	//	{
	//		buttonsToAdd = GetTextBoxButtonsToAdd(textBoxTarget);
	//	}
	//	else if (target is TextBlock textBlockTarget)
	//	{
	//		buttonsToAdd = GetTextBlockButtonsToAdd(textBlockTarget);
	//	}
	//	else if (target is RichEditBox richEditBoxTarget)
	//	{
	//		buttonsToAdd = GetRichEditBoxButtonsToAdd(richEditBoxTarget);
	//	}
	//	else if (target is RichTextBlock richTextBlockTarget)
	//	{
	//		buttonsToAdd = GetRichTextBlockButtonsToAdd(richTextBlockTarget);
	//	}
	//	else if (target is RichTextBlockOverflow richTextBlockOverflowTarget)
	//	{
	//		if (var richTextBlockSource = richTextBlockOverflowTarget.ContentSource())
	//       {
	//			buttonsToAdd = GetRichTextBlockButtonsToAdd(richTextBlockSource);
	//		}
	//	}
	//	else if (target is PasswordBox passwordBoxTarget)
	//	{
	//		buttonsToAdd = GetPasswordBoxButtonsToAdd(passwordBoxTarget);
	//	}

	//	return buttonsToAdd;
	//}

	private TextControlButtons GetTextBoxButtonsToAdd(TextBox textBox)
	{
		TextControlButtons buttonsToAdd = TextControlButtons.None;

		if (!textBox.IsReadOnly)
		{
			if (textBox.SelectionLength > 0)
			{
				buttonsToAdd |= TextControlButtons.Cut;
			}

			var textBox8 = textBox;

			if (textBox8 is not null)
			{
				if (textBox8.CanPasteClipboardContent)
				{
					buttonsToAdd |= TextControlButtons.Paste;
				}
			}
			else
			{
				DataPackageView clipboardContent = Clipboard.GetContent();

				if (clipboardContent.Contains(StandardDataFormats.Text))
				{
					buttonsToAdd |= TextControlButtons.Paste;
				}
			}

			// There's no way to polyfill undo and redo - those are black-box operations that we need
			// the TextBox to tell us about.
			if (textBox8 is not null && textBox8.CanUndo)
			{
				buttonsToAdd |= TextControlButtons.Undo;
			}

			if (textBox8 is not null && textBox8.CanRedo)
			{
				buttonsToAdd |= TextControlButtons.Redo;
			}
		}

		if (textBox.SelectionLength > 0)
		{
			buttonsToAdd |= TextControlButtons.Copy;
		}

		if (textBox.Text.Length > 0)
		{
			buttonsToAdd |= TextControlButtons.SelectAll;
		}

		return buttonsToAdd;
	}

	private TextControlButtons GetTextBlockButtonsToAdd(TextBlock textBlock)
	{
		TextControlButtons buttonsToAdd = TextControlButtons.None;

		if (textBlock.SelectedText.Length > 0)
		{
			buttonsToAdd |= TextControlButtons.Copy;
		}

		if (textBlock.Text.Length > 0)
		{
			buttonsToAdd |= TextControlButtons.SelectAll;
		}

		return buttonsToAdd;
	}

	private TextControlButtons GetRichEditBoxButtonsToAdd(RichEditBox richEditBox)
	{
		TextControlButtons buttonsToAdd = TextControlButtons.None;
		var document = richEditBox.Document;
		var selection = document?.Selection;

		if (!richEditBox.IsReadOnly)
		{
			if (richEditBox is { } richEditBox6)
	       {
				var disabledFormattingAccelerators = richEditBox6.DisabledFormattingAccelerators;

				if ((disabledFormattingAccelerators & DisabledFormattingAccelerators.Bold) != DisabledFormattingAccelerators.Bold)
				{
					buttonsToAdd |= TextControlButtons.Bold;
				}

				if ((disabledFormattingAccelerators & DisabledFormattingAccelerators.Italic) != DisabledFormattingAccelerators.Italic)
				{
					buttonsToAdd |= TextControlButtons.Italic;
				}

				if ((disabledFormattingAccelerators & DisabledFormattingAccelerators.Underline) != DisabledFormattingAccelerators.Underline)
				{
					buttonsToAdd |= TextControlButtons.Underline;
				}
			}

		else
			{
				buttonsToAdd |= TextControlButtons.Bold | TextControlButtons.Italic | TextControlButtons.Underline;
			}

			if (document is not null && document.CanCopy && selection && selection.Length != 0)
			{
				buttonsToAdd |= TextControlButtons.Cut;
			}

			if (selection is not null && selection.CanPaste(0))
			{
				buttonsToAdd |= TextControlButtons.Paste;
			}
			
			if (document is not null && document.CanUndo)
			{
				buttonsToAdd |= TextControlButtons.Undo;
			}

			if (document is not null && document.CanRedo)
			{
				buttonsToAdd |= TextControlButtons.Redo;
			}
		}

		if (document is not null && document.CanCopy && selection && selection.Length != 0)
		{
			buttonsToAdd |= TextControlButtons.Copy;
		}

		if (document is not null)
		{
			document.GetText(TextGetOptions.None, out var text);

			if (text.Length > 0)
			{
				buttonsToAdd |= TextControlButtons.SelectAll;
			}
		}

		return buttonsToAdd;
	}

	private TextControlButtons GetRichTextBlockButtonsToAdd(RichTextBlock richTextBlock)
	{
		TextControlButtons buttonsToAdd = TextControlButtons.None;

		if (richTextBlock.SelectedText.Count > 0)
		{
			buttonsToAdd |= TextControlButtons.Copy;
		}

		if (richTextBlock.Blocks.Count > 0)
		{
			buttonsToAdd |= TextControlButtons.SelectAll;
		}

		return buttonsToAdd;
	}

	private TextControlButtons GetPasswordBoxButtonsToAdd(PasswordBox passwordBox)
	{
		TextControlButtons buttonsToAdd = TextControlButtons.None;

		// There isn't any way to get the selection in a PasswordBox, so there's no way to polyfill pasting.
		if (var passwordBox5 = passwordBox as IPasswordBox5())
	   {
			if (passwordBox5.CanPasteClipboardContent())
			{
				buttonsToAdd |= TextControlButtons.Paste;
			}
		}

		if (passwordBox.Password().Count > 0)
		{
			buttonsToAdd |= TextControlButtons.SelectAll;
		}

		return buttonsToAdd;
	}

	//bool IsButtonInPrimaryCommands(TextControlButtons button)
	//{
	//	uint buttonIndex = 0;
	//	bool wasFound = PrimaryCommands.IndexOf(GetButton(button), buttonIndex);
	//	return wasFound;
	//}

	//void ExecuteCutCommand()
	//{
	//	var target = Target();

	//	try
	//	{
	//		if (target is TextBox textBoxTarget)
	//		{
	//			if (var textBox8 = textBoxTarget as ITextBox8())
	//           {
	//				textBox8.CutSelectionToClipboard();
	//			}

	//		else
	//			{
	//				DataPackage cutPackage;

	//				cutPackage.RequestedOperation(DataPackageOperation.Move);
	//				cutPackage.SetText(textBoxTarget.SelectedText());

	//				Clipboard.SetContent(cutPackage);

	//				textBoxTarget.SelectedText("");
	//			}
	//		}
	//		else if (target is RichEditBox richEditBoxTarget)
	//		{
	//			var selection{ SharedHelpers.GetRichTextSelection(richEditBoxTarget) };

	//			if (selection)
	//			{
	//				selection.Cut();
	//			}
	//		}
	//	}
	//	catch (hresult_error e)
	//	{
	//		// If we got a clipboard error, we don't want to crash as a result of that - that can happen (e.g.)
	//		// if the app isn't the foreground window when we try to execute a clipboard operation.
	//		if (e.code().value < CLIPBRD_E_FIRST || e.code().value > CLIPBRD_E_LAST)
	//		{
	//			throw;
	//		}
	//	}

	//	if (IsButtonInPrimaryCommands(TextControlButtons.Cut))
	//	{
	//		UpdateButtons();
	//	}
	//}

	//void ExecuteCopyCommand()
	//{
	//	var target = Target();

	//	try
	//	{
	//		var executeRichTextBlockCopyCommand =

	//		   [this](RichTextBlock & richTextBlockTarget)

	//	{
	//			if (var richTextBlock6 = richTextBlockTarget as IRichTextBlock6())
	//           {
	//				richTextBlock6.CopySelectionToClipboard();
	//			}

	//		else
	//			{
	//				DataPackage copyPackage;

	//				copyPackage.RequestedOperation(DataPackageOperation.Copy);
	//				copyPackage.SetText(richTextBlockTarget.SelectedText());

	//				Clipboard.SetContent(copyPackage);
	//			}
	//		};

	//		if (target is TextBox textBoxTarget)
	//		{
	//			if (var textBox8 = textBoxTarget as ITextBox8())
	//           {
	//				textBox8.CopySelectionToClipboard();
	//			}

	//		else
	//			{
	//				DataPackage copyPackage;

	//				copyPackage.RequestedOperation(DataPackageOperation.Copy);
	//				copyPackage.SetText(textBoxTarget.SelectedText());

	//				Clipboard.SetContent(copyPackage);
	//			}
	//		}
	//		else if (target is TextBlock textBlockTarget)
	//		{
	//			if (var textBlock7 = textBlockTarget as ITextBlock7())
	//           {
	//				textBlock7.CopySelectionToClipboard();
	//			}

	//		else
	//			{
	//				DataPackage copyPackage;

	//				copyPackage.RequestedOperation(DataPackageOperation.Copy);
	//				copyPackage.SetText(textBlockTarget.SelectedText());

	//				Clipboard.SetContent(copyPackage);
	//			}
	//		}
	//		else if (target is RichEditBox richEditBoxTarget)
	//		{
	//			var selection{ SharedHelpers.GetRichTextSelection(richEditBoxTarget) };

	//			if (selection)
	//			{
	//				selection.Copy();
	//			}
	//		}
	//		else if (target is RichTextBlock richTextBlockTarget)
	//		{
	//			executeRichTextBlockCopyCommand(richTextBlockTarget);
	//		}
	//		else if (target is RichTextBlockOverflow richTextBlockOverflowTarget)
	//		{
	//			if (var richTextBoxSource = richTextBlockOverflowTarget.ContentSource())
	//           {
	//				executeRichTextBlockCopyCommand(richTextBoxSource);
	//			}
	//		}
	//	}
	//	catch (hresult_error e)
	//	{
	//		// If we got a clipboard error, we don't want to crash as a result of that - that can happen (e.g.)
	//		// if the app isn't the foreground window when we try to execute a clipboard operation.
	//		if (e.code().value < CLIPBRD_E_FIRST || e.code().value > CLIPBRD_E_LAST)
	//		{
	//			throw;
	//		}
	//	}

	//	if (IsButtonInPrimaryCommands(TextControlButtons.Copy))
	//	{
	//		UpdateButtons();
	//	}
	//}

	//void ExecutePasteCommand()
	//{
	//	var target = Target();

	//	try
	//	{
	//		if (target is TextBox textBoxTarget)
	//		{
	//			if (var textBox8 = textBoxTarget as ITextBox8())
	//           {
	//				textBox8.PasteFromClipboard();
	//			}

	//		else
	//			{
	//				var strongThis = get_strong();

	//				Clipboard.GetContent().GetTextAsync().Completed(
	//					AsyncOperationCompletedHandler<hstring>([strongThis, textBoxTarget](IAsyncOperation < hstring > asyncOperation, AsyncStatus asyncStatus)

	//					{
	//					if (asyncStatus != AsyncStatus.Completed)
	//					{
	//						return;
	//					}

	//					var textToPaste = asyncOperation.GetResults();

	//					strongThis.m_dispatcherHelper.RunAsync(

	//						[strongThis, textBoxTarget, textToPaste]()

	//							{
	//						textBoxTarget.SelectedText(textToPaste);
	//						textBoxTarget.SelectionStart(textBoxTarget.SelectionStart() + textToPaste.Count);
	//						textBoxTarget.SelectionLength(0.0;

	//						strongThis.UpdateButtons();
	//					});
	//				}));
	//			}
	//		}
	//		else if (target is RichEditBox richEditBoxTarget)
	//		{
	//			var selection{ SharedHelpers.GetRichTextSelection(richEditBoxTarget) };

	//			if (selection)
	//			{
	//				selection.Paste(0.0;
	//			}
	//		}
	//		else if (target is PasswordBox passwordBoxTarget)
	//		{
	//			passwordBoxTarget.PasteFromClipboard();
	//		}
	//	}
	//	catch (hresult_error e)
	//	{
	//		// If we got a clipboard error, we don't want to crash as a result of that - that can happen (e.g.)
	//		// if the app isn't the foreground window when we try to execute a clipboard operation.
	//		if (e.code().value < CLIPBRD_E_FIRST || e.code().value > CLIPBRD_E_LAST)
	//		{
	//			throw;
	//		}
	//	}

	//	if (IsButtonInPrimaryCommands(TextControlButtons.Paste))
	//	{
	//		UpdateButtons();
	//	}
	//}

	//void ExecuteBoldCommand()
	//{
	//	if (!m_isSettingToggleButtonState)
	//	{
	//		var target = Target();

	//		if (target is RichEditBox richEditBoxTarget)
	//		{
	//			var selection{ SharedHelpers.GetRichTextSelection(richEditBoxTarget) };

	//			if (selection)
	//			{
	//				var characterFormat = selection.CharacterFormat();
	//				if (characterFormat.Bold() == FormatEffect.On)
	//				{
	//					characterFormat.Bold(FormatEffect.Off);
	//				}
	//				else
	//				{
	//					characterFormat.Bold(FormatEffect.On);
	//				}
	//			}
	//		}
	//	}
	//}

	//void ExecuteItalicCommand()
	//{
	//	if (!m_isSettingToggleButtonState)
	//	{
	//		var target = Target();

	//		if (target is RichEditBox richEditBoxTarget)
	//		{
	//			var selection{ SharedHelpers.GetRichTextSelection(richEditBoxTarget) };

	//			if (selection)
	//			{
	//				var characterFormat = selection.CharacterFormat();
	//				if (characterFormat.Italic() == FormatEffect.On)
	//				{
	//					characterFormat.Italic(FormatEffect.Off);
	//				}
	//				else
	//				{
	//					characterFormat.Italic(FormatEffect.On);
	//				}
	//			}
	//		}
	//	}
	//}

	//void ExecuteUnderlineCommand()
	//{
	//	if (!m_isSettingToggleButtonState)
	//	{
	//		var target = Target();

	//		if (target is RichEditBox richEditBoxTarget)
	//		{
	//			var selection{ SharedHelpers.GetRichTextSelection(richEditBoxTarget) };

	//			if (selection)
	//			{
	//				var characterFormat = selection.CharacterFormat();
	//				if (characterFormat.Underline() == UnderlineType.None || characterFormat.Underline() == UnderlineType.Undefined)
	//				{
	//					characterFormat.Underline(UnderlineType.Single);
	//				}
	//				else
	//				{
	//					characterFormat.Underline(UnderlineType.None);
	//				}
	//			}
	//		}
	//	}
	//}

	//void ExecuteUndoCommand()
	//{
	//	var target = Target();

	//	if (target is TextBox textBoxTarget)
	//	{
	//		textBoxTarget.Undo();
	//	}
	//	else if (target is RichEditBox richEditBoxTarget)
	//	{
	//		richEditBoxTarget.Document().Undo();
	//	}

	//	if (IsButtonInPrimaryCommands(TextControlButtons.Undo))
	//	{
	//		UpdateButtons();
	//	}
	//}

	//void ExecuteRedoCommand()
	//{
	//	var target = Target();

	//	if (target is TextBox textBoxTarget)
	//	{
	//		textBoxTarget.Redo();
	//	}
	//	else if (target is RichEditBox richEditBoxTarget)
	//	{
	//		richEditBoxTarget.Document().Redo();
	//	}

	//	if (IsButtonInPrimaryCommands(TextControlButtons.Redo))
	//	{
	//		UpdateButtons();
	//	}
	//}

	//void ExecuteSelectAllCommand()
	//{
	//	var target = Target();

	//	if (target is TextBox textBoxTarget)
	//	{
	//		textBoxTarget.SelectAll();
	//	}
	//	else if (target is TextBlock textBlockTarget)
	//	{
	//		textBlockTarget.SelectAll();
	//	}
	//	else if (target is RichEditBox richEditBoxTarget)
	//	{
	//		var selection{ SharedHelpers.GetRichTextSelection(richEditBoxTarget) };

	//		if (selection)
	//		{
	//			selection.Expand(TextRangeUnit.Story);
	//		}
	//	}
	//	else if (target is RichTextBlock richTextBlockTarget)
	//	{
	//		richTextBlockTarget.SelectAll();
	//	}
	//	else if (target is RichTextBlockOverflow richTextBlockOverflowTarget)
	//	{
	//		if (var richTextBlockSource = richTextBlockOverflowTarget.ContentSource())
	//       {
	//			richTextBlockSource.SelectAll();
	//		}
	//	}
	//	else if (target is PasswordBox passwordBoxTarget)
	//	{
	//		passwordBoxTarget.SelectAll();
	//	}

	//	if (IsButtonInPrimaryCommands(TextControlButtons.SelectAll))
	//	{
	//		UpdateButtons();
	//	}
	//}

	//ICommandBarElement GetButton(TextControlButtons textControlButton)
	//{
	//	var foundButton = m_buttons.find(textControlButton);

	//	if (foundButton != m_buttons.end())
	//	{
	//		return foundButton.second;
	//	}
	//	else
	//	{
	//		switch (textControlButton)
	//		{
	//			case TextControlButtons.Cut:
	//				{
	//					AppBarButton button;
	//					var executeFunc = [this]() { ExecuteCutCommand(); };

	//					if (SharedHelpers.IsStandardUICommandAvailable())
	//					{
	//						InitializeButtonWithUICommand(button, StandardUICommand(StandardUICommandKind.Cut), executeFunc);
	//					}
	//					else
	//					{
	//						InitializeButtonWithProperties(
	//							button,
	//							SR_TextCommandLabelCut,
	//							Symbol.Cut,
	//							SR_TextCommandKeyboardAcceleratorKeyCut,
	//							SR_TextCommandDescriptionCut,
	//							executeFunc);
	//					}

	//					m_buttons[TextControlButtons.Cut] = button;
	//					return button;
	//				}
	//			case TextControlButtons.Copy:
	//				{
	//					AppBarButton button;
	//					var executeFunc = [this]() { ExecuteCopyCommand(); };

	//					if (SharedHelpers.IsStandardUICommandAvailable())
	//					{
	//						InitializeButtonWithUICommand(button, StandardUICommand(StandardUICommandKind.Copy), executeFunc);
	//					}
	//					else
	//					{
	//						InitializeButtonWithProperties(
	//							button,
	//							SR_TextCommandLabelCopy,
	//							Symbol.Copy,
	//							SR_TextCommandKeyboardAcceleratorKeyCopy,
	//							SR_TextCommandDescriptionCopy,
	//							executeFunc);
	//					}

	//					m_buttons[TextControlButtons.Copy] = button;
	//					return button;
	//				}
	//			case TextControlButtons.Paste:
	//				{
	//					AppBarButton button;
	//					var executeFunc = [this]() { ExecutePasteCommand(); };

	//					if (SharedHelpers.IsStandardUICommandAvailable())
	//					{
	//						InitializeButtonWithUICommand(button, StandardUICommand(StandardUICommandKind.Paste), executeFunc);
	//					}
	//					else
	//					{
	//						InitializeButtonWithProperties(
	//							button,
	//							SR_TextCommandLabelPaste,
	//							Symbol.Paste,
	//							SR_TextCommandKeyboardAcceleratorKeyPaste,
	//							SR_TextCommandDescriptionPaste,
	//							executeFunc);
	//					}

	//					m_buttons[TextControlButtons.Paste] = button;
	//					return button;
	//				}
	//			// Bold, Italic, and Underline don't have command library commands associated with them,
	//			// so we'll just unconditionally initialize them with properties.
	//			case TextControlButtons.Bold:
	//				{
	//					AppBarToggleButton button;
	//					InitializeButtonWithProperties(
	//						button,
	//						SR_TextCommandLabelBold,
	//						Symbol.Bold,
	//						SR_TextCommandKeyboardAcceleratorKeyBold,
	//						SR_TextCommandDescriptionBold,

	//						[this]() { ExecuteBoldCommand(); });

	//					m_buttons[TextControlButtons.Bold] = button;
	//					return button;
	//				}
	//			case TextControlButtons.Italic:
	//				{
	//					AppBarToggleButton button;
	//					InitializeButtonWithProperties(
	//						button,
	//						SR_TextCommandLabelItalic,
	//						Symbol.Italic,
	//						SR_TextCommandKeyboardAcceleratorKeyItalic,
	//						SR_TextCommandDescriptionItalic,

	//						[this]() { ExecuteItalicCommand(); });

	//					m_buttons[TextControlButtons.Italic] = button;
	//					return button;
	//				}
	//			case TextControlButtons.Underline:
	//				{
	//					AppBarToggleButton button;
	//					InitializeButtonWithProperties(
	//						button,
	//						SR_TextCommandLabelUnderline,
	//						Symbol.Underline,
	//						SR_TextCommandKeyboardAcceleratorKeyUnderline,
	//						SR_TextCommandDescriptionUnderline,

	//						[this]() { ExecuteUnderlineCommand(); });

	//					m_buttons[TextControlButtons.Underline] = button;
	//					return button;
	//				}
	//			case TextControlButtons.Undo:
	//				{
	//					AppBarButton button;
	//					var executeFunc = [this]() { ExecuteUndoCommand(); };

	//					if (SharedHelpers.IsStandardUICommandAvailable())
	//					{
	//						InitializeButtonWithUICommand(button, StandardUICommand(StandardUICommandKind.Undo), executeFunc);
	//					}
	//					else
	//					{
	//						InitializeButtonWithProperties(
	//							button,
	//							SR_TextCommandLabelUndo,
	//							Symbol.Undo,
	//							SR_TextCommandKeyboardAcceleratorKeyUndo,
	//							SR_TextCommandDescriptionUndo,
	//							executeFunc);
	//					}

	//					m_buttons[TextControlButtons.Undo] = button;
	//					return button;
	//				}
	//			case TextControlButtons.Redo:
	//				{
	//					AppBarButton button;
	//					var executeFunc = [this]() { ExecuteRedoCommand(); };

	//					if (SharedHelpers.IsStandardUICommandAvailable())
	//					{
	//						InitializeButtonWithUICommand(button, StandardUICommand(StandardUICommandKind.Redo), executeFunc);
	//					}
	//					else
	//					{
	//						InitializeButtonWithProperties(
	//							button,
	//							SR_TextCommandLabelRedo,
	//							Symbol.Redo,
	//							SR_TextCommandKeyboardAcceleratorKeyRedo,
	//							SR_TextCommandDescriptionRedo,
	//							executeFunc);
	//					}

	//					m_buttons[TextControlButtons.Redo] = button;
	//					return button;
	//				}
	//			case TextControlButtons.SelectAll:
	//				{
	//					AppBarButton button;
	//					var executeFunc = [this]() { ExecuteSelectAllCommand(); };

	//					if (SharedHelpers.IsStandardUICommandAvailable())
	//					{
	//						var command = StandardUICommand(StandardUICommandKind.SelectAll);
	//						command.IconSource(null);

	//						InitializeButtonWithUICommand(button, command, executeFunc);
	//					}
	//					else
	//					{
	//						InitializeButtonWithProperties(
	//							button,
	//							SR_TextCommandLabelSelectAll,
	//							SR_TextCommandKeyboardAcceleratorKeySelectAll,
	//							SR_TextCommandDescriptionSelectAll,
	//							executeFunc);
	//					}

	//					m_buttons[TextControlButtons.SelectAll] = button;
	//					return button;
	//				}
	//			default:
	//				MUX_MUX_ASSERT(false);
	//				return null;
	//		}
	//	}
	//}
}

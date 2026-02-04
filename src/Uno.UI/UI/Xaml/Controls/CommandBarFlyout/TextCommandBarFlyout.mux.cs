// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\CommandBarFlyout\TextCommandBarFlyout.cpp, tag winui3/release/1.7.3, commit 65718e2813a90fc900e8775d2ddc580b268fcc2f

using System;
using System.Collections.Generic;
using Uno.Disposables;
using Uno.UI.Helpers.WinUI;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.System;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using static Microsoft.UI.Xaml.Controls._Tracing;
using static Uno.UI.Helpers.WinUI.ResourceAccessor;

using Microsoft.UI.Text;

namespace Microsoft.UI.Xaml.Controls;

partial class TextCommandBarFlyout
{
	public TextCommandBarFlyout()
	{
		//__RP_Marker_ClassById(RuntimeProfiler.ProfId_TextCommandBarFlyout);
		m_dispatcherHelper = new DispatcherHelper(this);

		Opening += (s, e) => UpdateButtons();

		Opened += (s, e) =>
		{
			// If there aren't any primary commands and we aren't opening expanded,
			// or if there are just no commands at all, then we'll have literally no UI to show. 
			// We'll just close the flyout in that case - nothing should be opening us
			// in this state anyway, but this makes sure we don't have a light-dismiss layer
			// with nothing visible to light dismiss.

			if (PrimaryCommands.Count == 0 &&
				(SecondaryCommands.Count == 0 || (!m_commandBar.IsOpen && ShowMode != FlyoutShowMode.Standard)))
			{
				Hide();
			}
		};
	}

	private void InitializeButtonWithUICommand(
		ButtonBase button,
		XamlUICommand uiCommand,
		Action executeFunc)
	{
		void onExecuteRequested(object sender, object args) => executeFunc();
		uiCommand.ExecuteRequested += onExecuteRequested;
		m_buttonCommandRevokers.Add(Disposable.Create(() => uiCommand.ExecuteRequested -= onExecuteRequested));
		button.Command = uiCommand;
	}

	private void InitializeButtonWithProperties(
		ButtonBase button,
		string labelResourceId,
		string acceleratorKeyResourceId,
		string descriptionResourceId,
		Action executeFunc)
	{
		AppBarButton elementAsButton = button as AppBarButton;
		AppBarToggleButton elementAsToggleButton = button as AppBarToggleButton;

		MUX_ASSERT(elementAsButton is not null || elementAsToggleButton is not null);

		if (!ResourceAccessor.IsResourceIdNull(labelResourceId))
		{
			var label = ResourceAccessor.GetLocalizedStringResource(labelResourceId);

			if (elementAsButton is not null)
			{
				elementAsButton.Label = label;
			}
			else
			{
				elementAsToggleButton.Label = label;
			}
		}

		if (!ResourceAccessor.IsResourceIdNull(descriptionResourceId))
		{
			string description = ResourceAccessor.GetLocalizedStringResource(descriptionResourceId);

			AutomationProperties.SetHelpText(button, description);
			ToolTipService.SetToolTip(button, description);
		}

		void onAction(object s, object e) => executeFunc();
		if (elementAsToggleButton is not null)
		{
			elementAsToggleButton.Checked += onAction;
			elementAsToggleButton.Unchecked += onAction;
			m_toggleButtonCheckedRevokers.Add(Disposable.Create(() => elementAsToggleButton.Checked -= onAction));
			m_toggleButtonUncheckedRevokers.Add(Disposable.Create(() => elementAsToggleButton.Unchecked -= onAction));
		}
		else
		{
			button.Click += onAction;
			m_buttonClickRevokers.Add(Disposable.Create(() => button.Click -= onAction));
		}

		if (!ResourceAccessor.IsResourceIdNull(acceleratorKeyResourceId))
		{
			string acceleratorKeyString = ResourceAccessor.GetLocalizedStringResource(acceleratorKeyResourceId);

			if (acceleratorKeyString.Length > 0)
			{
				char acceleratorKeyChar = acceleratorKeyString[0];
				VirtualKey acceleratorKey = SharedHelpers.GetVirtualKeyFromChar(acceleratorKeyChar);

				if (acceleratorKey != VirtualKey.None)
				{
					KeyboardAccelerator keyboardAccelerator = new();
					keyboardAccelerator.Key = acceleratorKey;
					keyboardAccelerator.Modifiers = VirtualKeyModifiers.Control;

					button.KeyboardAccelerators.Add(keyboardAccelerator);
				}
			}
		}
	}

	private void InitializeButtonWithProperties(
		ButtonBase button,
		string labelResourceId,
		Symbol symbol,
		string acceleratorKeyResourceId,
		string descriptionResourceId,
		Action executeFunc)
	{
		InitializeButtonWithProperties(
			button,
			labelResourceId,
			acceleratorKeyResourceId,
			descriptionResourceId,
			executeFunc);

		AppBarButton elementAsButton = button as AppBarButton;
		AppBarToggleButton elementAsToggleButton = button as AppBarToggleButton;

		MUX_ASSERT(elementAsButton is not null || elementAsToggleButton is not null);

		SymbolIcon symbolIcon = new SymbolIcon(symbol);

		if (elementAsButton is not null)
		{
			elementAsButton.Icon = symbolIcon;
		}
		else
		{
			elementAsToggleButton.Icon = symbolIcon;
		}
	}

	private void UpdateButtons()
	{
		PrimaryCommands.Clear();
		SecondaryCommands.Clear();

		var buttonsToAdd = GetButtonsToAdd();

		void addButtonToCommandsIfPresent(TextControlButtons buttonType, IList<ICommandBarElement> commandsList)
		{
			if ((buttonsToAdd & buttonType) != TextControlButtons.None)
			{
				commandsList.Add(GetButton(buttonType));
			}
		}

		void addRichEditButtonToCommandsIfPresent(TextControlButtons buttonType, IList<ICommandBarElement> commandsList, Func<ITextSelection, bool> getIsChecked)
		{
			if ((buttonsToAdd & buttonType) != TextControlButtons.None)
			{
				var richEditBoxTarget = (object)Target as RichEditBox;
				var toggleButton = GetButton(buttonType) as AppBarToggleButton;
				var selection = SharedHelpers.GetRichTextSelection(richEditBoxTarget);

				if (selection is not null)
				{
					using var initializingButtons = Disposable.Create(() =>
					{
						m_isSettingToggleButtonState = false;
					});
					m_isSettingToggleButtonState = true;
					toggleButton.IsChecked = getIsChecked(selection);
				}

				commandsList.Add(toggleButton);
			}
		}

		FlyoutBase proofingFlyout = null;

		if (Target is TextBox textBoxTarget)
		{
			proofingFlyout = textBoxTarget.ProofingMenuFlyout;
		}
		else if ((object)Target is RichEditBox richEditBoxTarget)
		{
			proofingFlyout = richEditBoxTarget.ProofingMenuFlyout;
		}

		MenuFlyout proofingMenuFlyout = proofingFlyout as MenuFlyout;

		bool shouldIncludeProofingMenu =
			(bool)(proofingFlyout is not null) &&
			proofingMenuFlyout is null || proofingMenuFlyout.Items.Count > 0;

		if (shouldIncludeProofingMenu)
		{
			m_proofingButton = new AppBarButton();
			m_proofingButton.Label = ResourceAccessor.GetLocalizedStringResource(SR_ProofingMenuItemLabel);
			m_proofingButton.Flyout = proofingFlyout;

			void onProofingButtonLoaded(object sender, object eventArgs)
			{
				// If we have a proofing menu, we'll start with it open by invoking the button
				// as soon as the CommandBar opening animation completes.
				// We invoke the button instead of just showing the flyout to make the button
				// properly update its visual state as well.
				// If we have an open animation that we'll be executing, we'll postpone showing
				// the proofing menu until it's given a chance to get underway.
				// Otherwise, we'll just show the proofing menu immediately.
				if (m_commandBar is { } commandBar)
				{
					var openProofingMenuAction = () =>
					{
						// There isn't likely to be any way that the proofing button was deleted
						// between us scheduling this action and us actually executing it,
						// but it doesn't hurt to be resilient when we're scheduling an action
						// to occur in the future.
						if (m_proofingButton is not null)
						{
							var peer = m_proofingButton.OnCreateAutomationPeerInternal() as ButtonAutomationPeer;
							peer?.Invoke();
						}
					};

					if (commandBar.HasOpenAnimation() && commandBar.IsOpen)
					{
						// Allowing 100 ms to elapse before we open the proofing menu gives the proofing menu enough time
						// to be mostly open when the proofing menu opens (so the menu doesn't look detached from the CommandBarFlyout UI),
						// but doesn't wait long enough that there's a noticeable pause in between the user feeling like
						// the CommandBarFlyout is fully open and the proofing menu finally opening.
						// The exact number comes from design.
						SharedHelpers.ScheduleActionAfterWait(openProofingMenuAction, 100);
					}
					else
					{
						openProofingMenuAction();
					}
				}
			}

			m_proofingButton.Loaded += onProofingButtonLoaded;
			m_proofingButtonLoadedRevoker.Disposable = Disposable.Create(() => m_proofingButton.Loaded -= onProofingButtonLoaded);

			// We want interactions with any proofing menu element to close the entire flyout,
			// same as interactions with secondary commands, so we'll attach click event handlers
			// to the proofing menu items (if they exist) to handle that.
			if (proofingMenuFlyout is not null)
			{
				foreach (var revoker in m_proofingMenuItemClickRevokers)
				{
					revoker?.Dispose();
				}
				m_proofingMenuItemClickRevokers.Clear();

				foreach (var revoker in m_proofingMenuToggleItemClickRevokers)
				{
					revoker?.Dispose();
				}
				m_proofingMenuToggleItemClickRevokers.Clear();

				void closeFlyoutFunc(object sender, object args)
				{
					Hide();
				}

				// We might encounter MenuFlyoutSubItems, so we'll add them to this list
				// in order to ensure that we hook up handlers to their entries as well.
				Queue<IList<MenuFlyoutItemBase>> itemsList = new Queue<IList<MenuFlyoutItemBase>>();
				itemsList.Enqueue(proofingMenuFlyout.Items);

				while (itemsList.Count > 0)
				{
					var currentItems = itemsList.Dequeue();

					for (int i = 0; i < currentItems.Count; i++)
					{
						if (currentItems[i] is MenuFlyoutItem menuFlyoutItem)
						{
							menuFlyoutItem.Click += closeFlyoutFunc;
							m_proofingMenuItemClickRevokers.Add(Disposable.Create(() => menuFlyoutItem.Click -= closeFlyoutFunc));
						}
						else if (currentItems[i] is ToggleMenuFlyoutItem toggleMenuFlyoutItem)
						{
							toggleMenuFlyoutItem.Click += closeFlyoutFunc;
							m_proofingMenuToggleItemClickRevokers.Add(Disposable.Create(() => toggleMenuFlyoutItem.Click -= closeFlyoutFunc));
						}
						else if (currentItems[i] is MenuFlyoutSubItem menuFlyoutSubItem)
						{
							itemsList.Enqueue(menuFlyoutSubItem.Items);
						}
					}
				}
			}

			SecondaryCommands.Add(m_proofingButton);
		}
		else
		{
			m_proofingButton = null;
		}

		var commandListForCutCopyPaste = InputDevicePrefersPrimaryCommands ?
			PrimaryCommands : SecondaryCommands;

		addButtonToCommandsIfPresent(TextControlButtons.Cut, commandListForCutCopyPaste);
		addButtonToCommandsIfPresent(TextControlButtons.Copy, commandListForCutCopyPaste);
		addButtonToCommandsIfPresent(TextControlButtons.Paste, commandListForCutCopyPaste);

		addRichEditButtonToCommandsIfPresent(TextControlButtons.Bold, PrimaryCommands, (ITextSelection textSelection) => textSelection.CharacterFormat.Bold == FormatEffect.On);
		addRichEditButtonToCommandsIfPresent(TextControlButtons.Italic, PrimaryCommands, (ITextSelection textSelection) => textSelection.CharacterFormat.Italic == FormatEffect.On);
		addRichEditButtonToCommandsIfPresent(TextControlButtons.Underline, PrimaryCommands, (ITextSelection textSelection) =>
		{
			var underline = textSelection.CharacterFormat.Underline;
			return (underline != UnderlineType.None) && (underline != UnderlineType.Undefined);
		});

		addButtonToCommandsIfPresent(TextControlButtons.Undo, SecondaryCommands);
		addButtonToCommandsIfPresent(TextControlButtons.Redo, SecondaryCommands);
		addButtonToCommandsIfPresent(TextControlButtons.SelectAll, SecondaryCommands);
	}

	private TextControlButtons GetButtonsToAdd()
	{
		TextControlButtons buttonsToAdd = TextControlButtons.None;
		var target = Target;

		if (target is TextBox textBoxTarget)
		{
			buttonsToAdd = GetTextBoxButtonsToAdd(textBoxTarget);
		}
		else if (target is TextBlock textBlockTarget)
		{
			buttonsToAdd = GetTextBlockButtonsToAdd(textBlockTarget);
		}
		else if ((object)target is RichEditBox richEditBoxTarget)
		{
			buttonsToAdd = GetRichEditBoxButtonsToAdd(richEditBoxTarget);
		}
		else if (target is RichTextBlock richTextBlockTarget)
		{
			buttonsToAdd = GetRichTextBlockButtonsToAdd(richTextBlockTarget);
		}
		else if (target is RichTextBlockOverflow richTextBlockOverflowTarget)
		{
			if (richTextBlockOverflowTarget.ContentSource is { } richTextBlockSource)
			{
				buttonsToAdd = GetRichTextBlockButtonsToAdd(richTextBlockSource);
			}
		}
		else if (target is PasswordBox passwordBoxTarget)
		{
			buttonsToAdd = GetPasswordBoxButtonsToAdd(passwordBoxTarget);
		}

		return buttonsToAdd;
	}

	private TextControlButtons GetTextBoxButtonsToAdd(TextBox textBox)
	{
		TextControlButtons buttonsToAdd = TextControlButtons.None;

		if (!textBox.IsReadOnly)
		{
			if (textBox.SelectionLength > 0)
			{
				buttonsToAdd |= TextControlButtons.Cut;
			}

			if (textBox.CanPasteClipboardContent)
			{
				buttonsToAdd |= TextControlButtons.Paste;
			}

			if (textBox.CanUndo)
			{
				buttonsToAdd |= TextControlButtons.Undo;
			}

			if (textBox.CanRedo)
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
			var disabledFormattingAccelerators = richEditBox.DisabledFormattingAccelerators;

			if (!disabledFormattingAccelerators.HasFlag(DisabledFormattingAccelerators.Bold))
			{
				buttonsToAdd |= TextControlButtons.Bold;
			}

			if (!disabledFormattingAccelerators.HasFlag(DisabledFormattingAccelerators.Italic))
			{
				buttonsToAdd |= TextControlButtons.Italic;
			}

			if (!disabledFormattingAccelerators.HasFlag(DisabledFormattingAccelerators.Underline))
			{
				buttonsToAdd |= TextControlButtons.Underline;
			}

			if (document is not null && document.CanCopy() && selection is not null && selection.Length != 0)
			{
				buttonsToAdd |= TextControlButtons.Cut;
			}

			if (selection is not null && selection.CanPaste(0))
			{
				buttonsToAdd |= TextControlButtons.Paste;
			}

			if (document is not null && document.CanUndo())
			{
				buttonsToAdd |= TextControlButtons.Undo;
			}

			if (document is not null && document.CanRedo())
			{
				buttonsToAdd |= TextControlButtons.Redo;
			}
		}

		if (document is not null && document.CanCopy() && selection is not null && selection.Length != 0)
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

		if (richTextBlock.SelectedText.Length > 0)
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

		if (passwordBox.CanPasteClipboardContent)
		{
			buttonsToAdd |= TextControlButtons.Paste;
		}

		if (passwordBox.Password.Length > 0)
		{
			buttonsToAdd |= TextControlButtons.SelectAll;
		}

		return buttonsToAdd;
	}

	private bool IsButtonInPrimaryCommands(TextControlButtons button)
	{
		bool wasFound = PrimaryCommands.IndexOf(GetButton(button)) >= 0;
		return wasFound;
	}

	private void ExecuteCutCommand()
	{
		var target = Target;

		try
		{
			if (target is TextBox textBoxTarget)
			{
				textBoxTarget.CutSelectionToClipboard();
			}
			else if ((object)target is RichEditBox richEditBoxTarget)
			{
				var selection = SharedHelpers.GetRichTextSelection(richEditBoxTarget);

				if (selection is not null)
				{
					selection.Cut();
				}
			}
		}
		catch (Exception)
		{
			// If we got a clipboard error, we don't want to crash as a result of that - that can happen (e.g.)
			// if the app isn't the foreground window when we try to execute a clipboard operation.			
		}

		if (IsButtonInPrimaryCommands(TextControlButtons.Cut))
		{
			UpdateButtons();
		}
	}

	private void ExecuteCopyCommand()
	{
		var target = Target;

		try
		{
			var executeRichTextBlockCopyCommand = (RichTextBlock richTextBlockTarget) =>
			{
				richTextBlockTarget.CopySelectionToClipboard();
			};

			if (target is TextBox textBoxTarget)
			{
				textBoxTarget.CopySelectionToClipboard();
			}
			else if (target is TextBlock textBlockTarget)
			{
				textBlockTarget.CopySelectionToClipboard();
			}
			else if ((object)target is RichEditBox richEditBoxTarget)
			{
				var selection = SharedHelpers.GetRichTextSelection(richEditBoxTarget);

				if (selection is not null)
				{
					selection.Copy();
				}
			}
			else if (target is RichTextBlock richTextBlockTarget)
			{
				executeRichTextBlockCopyCommand(richTextBlockTarget);
			}
			else if (target is RichTextBlockOverflow richTextBlockOverflowTarget)
			{
				if (richTextBlockOverflowTarget.ContentSource is { } richTextBoxSource)
				{
					executeRichTextBlockCopyCommand(richTextBoxSource);
				}
			}
		}
		catch (Exception)
		{
			// If we got a clipboard error, we don't want to crash as a result of that - that can happen (e.g.)
			// if the app isn't the foreground window when we try to execute a clipboard operation.
		}

		if (IsButtonInPrimaryCommands(TextControlButtons.Copy))
		{
			UpdateButtons();
		}
	}

	private void ExecutePasteCommand()
	{
		var target = Target;

		try
		{
			if (target is TextBox textBoxTarget)
			{
				textBoxTarget.PasteFromClipboard();
			}
			else if ((object)target is RichEditBox richEditBoxTarget)
			{
				var selection = SharedHelpers.GetRichTextSelection(richEditBoxTarget);

				if (selection is not null)
				{
					selection.Paste(0);
				}
			}
			else if (target is PasswordBox passwordBoxTarget)
			{
				passwordBoxTarget.PasteFromClipboard();
			}
		}
		catch (Exception)
		{
			// If we got a clipboard error, we don't want to crash as a result of that - that can happen (e.g.)
			// if the app isn't the foreground window when we try to execute a clipboard operation.
		}

		if (IsButtonInPrimaryCommands(TextControlButtons.Paste))
		{
			UpdateButtons();
		}
	}

	private void ExecuteBoldCommand()
	{
		if (!m_isSettingToggleButtonState)
		{
			var target = Target;

			if ((object)target is RichEditBox richEditBoxTarget)
			{
				var selection = SharedHelpers.GetRichTextSelection(richEditBoxTarget);

				if (selection is not null)
				{
					var characterFormat = selection.CharacterFormat;
					if (characterFormat.Bold == FormatEffect.On)
					{
						characterFormat.Bold = FormatEffect.Off;
					}
					else
					{
						characterFormat.Bold = FormatEffect.On;
					}
				}
			}
		}
	}

	private void ExecuteItalicCommand()
	{
		if (!m_isSettingToggleButtonState)
		{
			var target = Target;

			if ((object)target is RichEditBox richEditBoxTarget)
			{
				var selection = SharedHelpers.GetRichTextSelection(richEditBoxTarget);

				if (selection is not null)
				{
					var characterFormat = selection.CharacterFormat;
					if (characterFormat.Italic == FormatEffect.On)
					{
						characterFormat.Italic = FormatEffect.Off;
					}
					else
					{
						characterFormat.Italic = FormatEffect.On;
					}
				}
			}
		}
	}

	private void ExecuteUnderlineCommand()
	{
		if (!m_isSettingToggleButtonState)
		{
			var target = Target;

			if ((object)target is RichEditBox richEditBoxTarget)
			{
				var selection = SharedHelpers.GetRichTextSelection(richEditBoxTarget);

				if (selection is not null)
				{
					var characterFormat = selection.CharacterFormat;
					if (characterFormat.Underline == UnderlineType.None || characterFormat.Underline == UnderlineType.Undefined)
					{
						characterFormat.Underline = UnderlineType.Single;
					}
					else
					{
						characterFormat.Underline = UnderlineType.None;
					}
				}
			}
		}
	}

	private void ExecuteUndoCommand()
	{
		var target = Target;

		if (target is TextBox textBoxTarget)
		{
			textBoxTarget.Undo();
		}
		else if ((object)target is RichEditBox richEditBoxTarget)
		{
			richEditBoxTarget.Document.Undo();
		}

		if (IsButtonInPrimaryCommands(TextControlButtons.Undo))
		{
			UpdateButtons();
		}
	}

	private void ExecuteRedoCommand()
	{
		var target = Target;

		if (target is TextBox textBoxTarget)
		{
			textBoxTarget.Redo();
		}
		else if ((object)target is RichEditBox richEditBoxTarget)
		{
			richEditBoxTarget.Document.Redo();
		}

		if (IsButtonInPrimaryCommands(TextControlButtons.Redo))
		{
			UpdateButtons();
		}
	}

	private void ExecuteSelectAllCommand()
	{
		var target = Target;

		if (target is TextBox textBoxTarget)
		{
			textBoxTarget.SelectAll();
		}
		else if (target is TextBlock textBlockTarget)
		{
			textBlockTarget.SelectAll();
		}
		else if ((object)target is RichEditBox richEditBoxTarget)
		{
			var selection = SharedHelpers.GetRichTextSelection(richEditBoxTarget);

			if (selection is not null)
			{
				selection.Expand(TextRangeUnit.Story);
			}
		}
		else if (target is RichTextBlock richTextBlockTarget)
		{
			richTextBlockTarget.SelectAll();
		}
		else if (target is RichTextBlockOverflow richTextBlockOverflowTarget)
		{
			if (richTextBlockOverflowTarget.ContentSource is { } richTextBlockSource)
			{
				richTextBlockSource.SelectAll();
			}
		}
		else if (target is PasswordBox passwordBoxTarget)
		{
			passwordBoxTarget.SelectAll();
		}

		if (IsButtonInPrimaryCommands(TextControlButtons.SelectAll))
		{
			UpdateButtons();
		}
	}

	private ICommandBarElement GetButton(TextControlButtons textControlButton)
	{
		if (m_buttons.TryGetValue(textControlButton, out var result))
		{
			return result;
		}
		else
		{
			switch (textControlButton)
			{
				case TextControlButtons.Cut:
					{
						AppBarButton button = new();
						var executeFunc = () => ExecuteCutCommand();

						InitializeButtonWithUICommand(button, new StandardUICommand(StandardUICommandKind.Cut), executeFunc);

						m_buttons[TextControlButtons.Cut] = button;
						return button;
					}
				case TextControlButtons.Copy:
					{
						AppBarButton button = new();
						var executeFunc = () => ExecuteCopyCommand();

						InitializeButtonWithUICommand(button, new StandardUICommand(StandardUICommandKind.Copy), executeFunc);

						m_buttons[TextControlButtons.Copy] = button;
						return button;
					}
				case TextControlButtons.Paste:
					{
						AppBarButton button = new();
						var executeFunc = () => ExecutePasteCommand();

						InitializeButtonWithUICommand(button, new StandardUICommand(StandardUICommandKind.Paste), executeFunc);

						m_buttons[TextControlButtons.Paste] = button;
						return button;
					}
				// Bold, Italic, and Underline don't have command library commands associated with them,
				// so we'll just unconditionally initialize them with properties.
				case TextControlButtons.Bold:
					{
						AppBarToggleButton button = new();
						InitializeButtonWithProperties(
							button,
							SR_TextCommandLabelBold,
							Symbol.Bold,
							SR_TextCommandKeyboardAcceleratorKeyBold,
							SR_TextCommandDescriptionBold,
							() => ExecuteBoldCommand());

						m_buttons[TextControlButtons.Bold] = button;
						return button;
					}
				case TextControlButtons.Italic:
					{
						AppBarToggleButton button = new();
						InitializeButtonWithProperties(
							button,
							SR_TextCommandLabelItalic,
							Symbol.Italic,
							SR_TextCommandKeyboardAcceleratorKeyItalic,
							SR_TextCommandDescriptionItalic,
							() => ExecuteItalicCommand());

						m_buttons[TextControlButtons.Italic] = button;
						return button;
					}
				case TextControlButtons.Underline:
					{
						AppBarToggleButton button = new();
						InitializeButtonWithProperties(
							button,
							SR_TextCommandLabelUnderline,
							Symbol.Underline,
							SR_TextCommandKeyboardAcceleratorKeyUnderline,
							SR_TextCommandDescriptionUnderline,
							() => ExecuteUnderlineCommand());

						m_buttons[TextControlButtons.Underline] = button;
						return button;
					}
				case TextControlButtons.Undo:
					{
						AppBarButton button = new();
						var executeFunc = () => ExecuteUndoCommand();

						InitializeButtonWithUICommand(button, new StandardUICommand(StandardUICommandKind.Undo), executeFunc);

						m_buttons[TextControlButtons.Undo] = button;
						return button;
					}
				case TextControlButtons.Redo:
					{
						AppBarButton button = new();
						var executeFunc = () => ExecuteRedoCommand();

						InitializeButtonWithUICommand(button, new StandardUICommand(StandardUICommandKind.Redo), executeFunc);

						m_buttons[TextControlButtons.Redo] = button;
						return button;
					}
				case TextControlButtons.SelectAll:
					{
						AppBarButton button = new();
						var executeFunc = () => ExecuteSelectAllCommand();

						var command = new StandardUICommand(StandardUICommandKind.SelectAll);
						command.IconSource = null;

						InitializeButtonWithUICommand(button, command, executeFunc);

						m_buttons[TextControlButtons.SelectAll] = button;
						return button;
					}
				default:
					MUX_ASSERT(false);
					return null;
			}
		}
	}
}

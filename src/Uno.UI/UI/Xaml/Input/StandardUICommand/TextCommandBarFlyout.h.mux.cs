using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.UI.Xaml.Controls;

partial class TextCommandBarFlyout
{
	//		enum class TextControlButtons
	//		{
	//			None = 0x0000,
	//    Cut = 0x0001,
	//    Copy = 0x0002,
	//    Paste = 0x0004,
	//    Bold = 0x0008,
	//    Italic = 0x0010,
	//    Underline = 0x0020,
	//    Undo = 0x0040,
	//    Redo = 0x0080,
	//    SelectAll = 0x0100,
	//};

	//		DECLARE_FLAG_ENUM_OPERATOR_OVERLOADS(TextControlButtons);

	//		class TextCommandBarFlyout :

	//	public implementation.TextCommandBarFlyoutT<TextCommandBarFlyout, CommandBarFlyout>
	//{
	//public:
	//    ForwardRefToBaseReferenceTracker(CommandBarFlyout)


	//	public TextCommandBarFlyout();

	//		private:
	//    void UpdateButtons();

	//		TextControlButtons GetButtonsToAdd();
	//		static TextControlButtons GetTextBoxButtonsToAdd(TextBox & textBox);
	//		static TextControlButtons GetTextBlockButtonsToAdd(TextBlock & textBlock);
	//		static TextControlButtons GetRichEditBoxButtonsToAdd(RichEditBox & richEditBox);
	//		static TextControlButtons GetRichTextBlockButtonsToAdd(RichTextBlock & richTextBlock);
	//		static TextControlButtons GetPasswordBoxButtonsToAdd(PasswordBox & passwordBox);

	//		bool IsButtonInPrimaryCommands(TextControlButtons button);

	//		void InitializeButtonWithUICommand(
	//			ButtonBase & button,
	//			XamlUICommand & uiCommand,
	//			std.function<void()> & executeFunc);

	//		void InitializeButtonWithProperties(
	//			ButtonBase & button,
	//			ResourceIdType labelResourceId,
	//			ResourceIdType acceleratorKeyResourceId,
	//			ResourceIdType descriptionResourceId,
	//			std.function<void()> & executeFunc);

	//		void InitializeButtonWithProperties(
	//			ButtonBase & button,
	//			ResourceIdType labelResourceId,
	//			Symbol & symbol,
	//			ResourceIdType acceleratorKeyResourceId,
	//			ResourceIdType descriptionResourceId,
	//			std.function<void()> & executeFunc);

	//		void ExecuteCutCommand();
	//		void ExecuteCopyCommand();
	//		void ExecutePasteCommand();
	//		void ExecuteBoldCommand();
	//		void ExecuteItalicCommand();
	//		void ExecuteUnderlineCommand();
	//		void ExecuteUndoCommand();
	//		void ExecuteRedoCommand();
	//		void ExecuteSelectAllCommand();

	//		ICommandBarElement GetButton(TextControlButtons button);

	//		std.map<TextControlButtons, ICommandBarElement> m_buttons;
	//		AppBarButton m_proofingButton { null };

	//		std.vector<XamlUICommand.ExecuteRequested_revoker> m_buttonCommandRevokers;
	//		std.vector<ButtonBase.Click_revoker> m_buttonClickRevokers;
	//		std.vector<ToggleButton.Checked_revoker> m_toggleButtonCheckedRevokers;
	//		std.vector<ToggleButton.Unchecked_revoker> m_toggleButtonUncheckedRevokers;

	//		FrameworkElement.Loaded_revoker m_proofingButtonLoadedRevoker { };

	//		std.vector<MenuFlyoutItem.Click_revoker> m_proofingMenuItemClickRevokers;
	//		std.vector<ToggleMenuFlyoutItem.Click_revoker> m_proofingMenuToggleItemClickRevokers;
	//		DispatcherHelper m_dispatcherHelper { this };

	//		bool m_isSettingToggleButtonState = false;
}

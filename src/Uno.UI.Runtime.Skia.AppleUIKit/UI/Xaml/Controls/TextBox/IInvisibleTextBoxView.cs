using Microsoft.UI.Xaml.Controls;
using UIKit;

namespace Uno.WinUI.Runtime.Skia.AppleUIKit.Controls;

//in iOS, we need to use two different controls to be able to accept return (UITextField vs UITextView)
//we use this interface to abstract properties that we need to modify in TextBox
internal interface IInvisibleTextBoxView
{
	string? Text { get; }

	bool IsCompatible(TextBox textBox);

	bool BecomeFirstResponder();

	bool ResignFirstResponder();

	bool IsFirstResponder { get; }

	void SetTextNative(string text);

	void Select(int start, int length);

	UITextAutocapitalizationType AutocapitalizationType { get; set; }

	UIKeyboardType KeyboardType { get; set; }

	UITextAutocorrectionType AutocorrectionType { get; set; }

	UITextSpellCheckingType SpellCheckingType { get; set; }

	bool SecureTextEntry { get; set; }

	UITextRange? SelectedTextRange { get; set; }

	UIReturnKeyType ReturnKeyType { get; set; }

	UIKeyboardAppearance KeyboardAppearance { get; set; }

	UITextPosition BeginningOfDocument { get; }

	nint GetOffsetFromPosition(UITextPosition fromPosition, UITextPosition toPosition);
}

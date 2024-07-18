using Microsoft.UI.Xaml.Controls;
using UIKit;
using ITextInput = UIKit.IUITextInput;

namespace Uno.WinUI.Runtime.Skia.AppleUIKit.Controls;

//in iOS, we need to use two different controls to be able to accept return (UITextField vs UITextView)
//we use this interface to abstract properties that we need to modify in TextBox
public interface IInvisibleTextBoxView : ITextInput
{
	string? Text { get; set; }

	bool IsCompatible(TextBox textBox);

	bool BecomeFirstResponder();

	bool ResignFirstResponder();

	bool IsFirstResponder { get; }

	void SetTextNative(string text);

	void Select(int start, int length);

	UITextAutocapitalizationType AutocapitalizationType { get; set; }

	UITextAutocorrectionType AutocorrectionType { get; set; }

	UIKeyboardType KeyboardType { get; set; }

	UIKeyboardAppearance KeyboardAppearance { get; set; }

	UIReturnKeyType ReturnKeyType { get; set; }

	bool EnablesReturnKeyAutomatically { get; set; }

	bool SecureTextEntry { get; set; }

	UITextSpellCheckingType SpellCheckingType { get; set; }
}

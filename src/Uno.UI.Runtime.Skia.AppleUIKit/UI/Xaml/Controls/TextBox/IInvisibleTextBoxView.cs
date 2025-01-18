using Microsoft.UI.Xaml.Controls;
using UIKit;
using ITextInput = UIKit.IUITextInput;

namespace Uno.WinUI.Runtime.Skia.AppleUIKit.Controls;

//in iOS, we need to use two different controls to be able to accept return (UITextField vs UITextView)
//we use this interface to abstract properties that we need to modify in TextBox
internal interface IInvisibleTextBoxView : ITextInput, IUITextInputTraits
{
	string? Text { get; }

	bool IsCompatible(TextBox textBox);

	bool BecomeFirstResponder();

	bool ResignFirstResponder();

	bool IsFirstResponder { get; }

	void SetTextNative(string text);

	void Select(int start, int length);
}

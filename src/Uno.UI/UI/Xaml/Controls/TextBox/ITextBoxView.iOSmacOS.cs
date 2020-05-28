using System;
using System.Collections.Generic;
using System.Text;

#if __IOS__
using UIKit;
using ITextInput = UIKit.IUITextInput;
#elif __MACOS__
using AppKit;
using ITextInput = AppKit.INSTextInput;
#endif

using Windows.UI.Xaml.Media;

namespace Windows.UI.Xaml.Controls
{
	//in iOS, we need to use two different controls to be able to accept return (UITextField vs UITextView)
	//we use this interface to abstract properties that we need to modify in TextBox
	public interface ITextBoxView : ITextInput
	{
		void UpdateFont();
		bool BecomeFirstResponder();
		bool ResignFirstResponder();

#if __IOS__
		bool IsFirstResponder { get; }
		void UpdateTextAlignment();
#endif

		Brush Foreground { get; set; }
		void SetTextNative(string text);
	}
}

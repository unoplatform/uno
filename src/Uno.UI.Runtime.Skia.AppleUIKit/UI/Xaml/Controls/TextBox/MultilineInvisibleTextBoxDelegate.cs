using System;
using System.Linq;
using Foundation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using UIKit;
using Uno.Extensions;

namespace Uno.WinUI.Runtime.Skia.AppleUIKit.Controls;

public partial class MultilineInvisibleTextBoxDelegate : UITextViewDelegate
{
	private readonly WeakReference<TextBoxView> _textBoxView;

	public MultilineInvisibleTextBoxDelegate(WeakReference<TextBoxView> textBoxView)
	{
		_textBoxView = textBoxView ?? throw new ArgumentNullException(nameof(textBoxView));
	}

	public override void Changed(UITextView textView)
	{
		var bindableTextView = textView as MultilineInvisibleTextBoxView;
		bindableTextView?.OnTextChanged();
	}

	/// <summary>
	/// Corresponds to a gain of focus
	/// </summary>
	public override void EditingStarted(UITextView textView)
	{
		if (_textBoxView.GetTarget() is TextBoxView textBoxView && textBoxView.TextBox?.FocusState == FocusState.Unfocused)
		{
			textBoxView.TextBox.Focus(FocusState.Pointer);
		}
	}

	/// <summary>
	/// Corresponds to a loss of focus
	/// </summary>
	public override void EditingEnded(UITextView textView)
	{
		var bindableTextView = textView as MultilineInvisibleTextBoxView;
		bindableTextView?.OnTextChanged();

		if (_textBoxView.GetTarget() is TextBoxView { TextBox.FocusState: not FocusState.Unfocused, TextBox.IsKeepingFocusOnEndEditing: false } textBox)
		{
			textBox.Unfocus();
		}
	}

	public override bool ShouldChangeText(UITextView textView, NSRange range, string text)
	{
		if (textView is MultilineInvisibleTextBoxView bindableTextView)
		{
			if (_textBoxView.GetTarget() is not TextBoxView textBoxView)
			{
				return false;
			}

			// Both IsReadOnly = true and IsTabStop = false can prevent editing
			if (textBoxView.TextBox.IsReadOnly || !textBoxView.TextBox.IsTabStop)
			{
				return false;
			}

			if (textBoxView.TextBox.OnKey(text.FirstOrDefault()))
			{
				return false;
			}

			if (textBoxView.TextBox.MaxLength > 0)
			{
				// When replacing text from pasting (multiple characters at once)
				// we should only allow it (return true) when the new text length
				// is lower or equal to the allowed length (TextBox.MaxLength)
				var newLength = bindableTextView.Text.Length + text.Length - range.Length;
				return newLength <= textBoxView.TextBox.MaxLength;
			}
		}

		return true;
	}

	public override bool ShouldEndEditing(UITextView textView)
	{
		return true;
	}
}

using System;
using System.Linq;
using Foundation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using UIKit;
using Uno.Extensions;

namespace Uno.WinUI.Runtime.Skia.AppleUIKit.Controls;

internal partial class MultilineInvisibleTextBoxDelegate : UITextViewDelegate
{
	private readonly WeakReference<InvisibleTextBoxViewExtension> _textBoxViewExtension;
	private string? _lastText;

	public MultilineInvisibleTextBoxDelegate(WeakReference<InvisibleTextBoxViewExtension> textBoxViewExtension)
	{
		_textBoxViewExtension = textBoxViewExtension ?? throw new ArgumentNullException(nameof(textBoxViewExtension));
	}

	public override void Changed(UITextView textView)
	{
		if (textView is MultilineInvisibleTextBoxView bindableTextView)
		{
			var currentText = textView.Text;
			if (_lastText != currentText)
			{
				_lastText = currentText;
				bindableTextView.OnTextChanged();
			}
		}
	}

	public override bool ShouldChangeText(UITextView textView, NSRange range, string replacementString)
	{
		if (textView is MultilineInvisibleTextBoxView textBoxView)
		{
			if (_textBoxViewExtension.GetTarget()?.Owner.TextBox is not TextBox textBox)
			{
				return false;
			}

			// Both IsReadOnly = true and IsTabStop = false can prevent editing
			if (textBox.IsReadOnly || !textBox.IsTabStop)
			{
				return false;
			}

			// TODO:MZ:
			//if (textBox.OnKey(text.FirstOrDefault()))
			//{
			//	return false;
			//}

			if (textBox.MaxLength > 0)
			{
				// When replacing text from pasting (multiple characters at once)
				// we should only allow it (return true) when the new text length
				// is lower or equal to the allowed length (TextBox.MaxLength)
				var newLength = (textBoxView.Text?.Length ?? 0) + replacementString.Length - range.Length;
				return newLength <= textBox.MaxLength;
			}
		}

		return true;
	}

	public override bool ShouldEndEditing(UITextView textView)
	{
		return true;
	}

	/// <summary>
	/// Corresponds to a gain of focus
	/// </summary>
	public override void EditingStarted(UITextView textView)
	{
		if (_textBoxViewExtension.GetTarget()?.Owner.TextBox is TextBox textBox && textBox.FocusState == FocusState.Unfocused)
		{
			textBox.Focus(FocusState.Pointer);
		}
	}

	/// <summary>
	/// Corresponds to a loss of focus
	/// </summary>
	public override void EditingEnded(UITextView textView)
	{
		var bindableTextView = textView as MultilineInvisibleTextBoxView;
		bindableTextView?.OnTextChanged();

		if (_textBoxViewExtension.GetTarget()?.Owner.TextBox is TextBox { FocusState: not FocusState.Unfocused } textBox)
		{
			textBox.Unfocus();
		}
	}
}

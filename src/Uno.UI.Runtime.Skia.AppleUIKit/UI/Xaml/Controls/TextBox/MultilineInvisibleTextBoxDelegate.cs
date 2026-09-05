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
			if (_textBoxViewExtension.GetTarget()?.Owner.Core is not { } core)
			{
				return false;
			}

			// Both IsReadOnly = true and IsTabStop = false can prevent editing
			if (core.IsReadOnly || !core.Owner.IsTabStop)
			{
				return false;
			}

			if (range.Length == 0 && string.IsNullOrEmpty(replacementString))
			{
				return false;
			}

			// During IME composition, allow text changes through without
			// MaxLength interference — the composition system manages length.
			if (textBoxView.IsComposing)
			{
				return true;
			}

			// Suppress the iOS autocorrect autospace fired when the caret leaves a word (see IsNoOpAutocorrectReplacement).
			if (InvisibleTextBoxAutocorrect.IsNoOpAutocorrectReplacement(textView.Text, range, replacementString))
			{
				return false;
			}

			// TODO:MZ:
			//if (textBox.OnKey(text.FirstOrDefault()))
			//{
			//	return false;
			//}

			if (core.MaxLength > 0)
			{
				// When replacing text from pasting (multiple characters at once)
				// we should only allow it (return true) when the new text length
				// is lower or equal to the allowed length (MaxLength)
				var newLength = (textBoxView.Text?.Length ?? 0) + replacementString.Length - range.Length;
				return newLength <= core.MaxLength;
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
		if (_textBoxViewExtension.GetTarget()?.Owner.Core is { Owner.FocusState: FocusState.Unfocused } core)
		{
			core.Owner.Focus(FocusState.Pointer);
		}
	}

	/// <summary>
	/// Corresponds to a loss of focus
	/// </summary>
	public override void EditingEnded(UITextView textView)
	{
		var bindableTextView = textView as MultilineInvisibleTextBoxView;
		bindableTextView?.OnTextChanged();

		if (_textBoxViewExtension.GetTarget()?.Owner.Core is { Owner.FocusState: not FocusState.Unfocused } core)
		{
			core.Owner.Unfocus();
		}
	}
}

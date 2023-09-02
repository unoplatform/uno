using Foundation;
using Uno.Extensions;
using System;
using System.Linq;
using UIKit;

namespace Windows.UI.Xaml.Controls
{
	public partial class MultilineTextBoxDelegate : UITextViewDelegate
	{
		private readonly WeakReference<TextBox> _textBox;

		public MultilineTextBoxDelegate(WeakReference<TextBox> textbox)
		{
			_textBox = textbox;
		}

		public override void Changed(UITextView textView)
		{
			var bindableTextView = textView as MultilineTextBoxView;
			bindableTextView?.OnTextChanged();
		}

		/// <summary>
		/// Corresponds to a gain of focus
		/// </summary>
		public override void EditingStarted(UITextView textView)
		{
			if (_textBox.GetTarget() is TextBox textBox && textBox.FocusState == FocusState.Unfocused)
			{
				textBox.Focus(FocusState.Pointer);
			}
		}

		/// <summary>
		/// Corresponds to a loss of focus
		/// </summary>
		public override void EditingEnded(UITextView textView)
		{
			var bindableTextView = textView as MultilineTextBoxView;
			bindableTextView?.OnTextChanged();

			if (_textBox.GetTarget() is TextBox { FocusState: not FocusState.Unfocused, IsKeepingFocusOnEndEditing: false } textBox)
			{
				textBox.Unfocus();
			}
		}

		public override bool ShouldChangeText(UITextView textView, NSRange range, string text)
		{
			if (textView is MultilineTextBoxView bindableTextView)
			{
				if (_textBox.GetTarget() is not TextBox textBox)
				{
					return false;
				}

				// Both IsReadOnly = true and IsTabStop = false can prevent editing
				if (textBox.IsReadOnly || !textBox.IsTabStop)
				{
					return false;
				}

				if (textBox.OnKey(text.FirstOrDefault()))
				{
					return false;
				}

				if (textBox.MaxLength > 0)
				{
					// When replacing text from pasting (multiple characters at once)
					// we should only allow it (return true) when the new text length
					// is lower or equal to the allowed length (TextBox.MaxLength)
					var newLength = bindableTextView.Text.Length + text.Length - range.Length;
					return newLength <= textBox.MaxLength;
				}
			}

			return true;
		}

		public override bool ShouldEndEditing(UITextView textView)
		{
			return true;
		}
	}
}

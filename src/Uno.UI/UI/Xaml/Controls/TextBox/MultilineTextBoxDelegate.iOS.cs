#nullable disable

using Foundation;
using Uno.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

			if (_textBox.GetTarget() is TextBox textBox && textBox.FocusState != FocusState.Unfocused)
			{
				textBox.Unfocus();
			}
		}

		public override bool ShouldChangeText(UITextView textView, NSRange range, string text)
		{
			var textBox = _textBox.GetTarget();
			if (textBox != null)
			{
				var bindableTextView = textView as MultilineTextBoxView;
				if (bindableTextView != null)
				{
					if (textBox.OnKey(text.FirstOrDefault()))
					{
						return false;

					}

					if (textBox.MaxLength > 0)
					{
						var newLength = bindableTextView.Text.Length + text.Length - range.Length;
						return newLength <= textBox.MaxLength;
					}
				}

				return true;
			}

			return false;
		}

		public override bool ShouldEndEditing(UITextView textView)
		{
			var bindableTextView = textView as MultilineTextBoxView;
			return true;
		}

		public override bool ShouldBeginEditing(UITextView textView)
		{
			if (_textBox.GetTarget() is not TextBox textBox)
			{
				return false;
			}

			// Both IsReadOnly = true and IsTabStop = false can prevent editing
			return !textBox.IsReadOnly && textBox.IsTabStop;
		}
	}
}

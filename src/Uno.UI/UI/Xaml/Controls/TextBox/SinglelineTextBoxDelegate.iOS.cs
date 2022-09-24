using Foundation;
using Uno.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UIKit;
using Windows.UI.Core;
using System.Threading.Tasks;

namespace Windows.UI.Xaml.Controls
{
	public partial class SinglelineTextBoxDelegate : UITextFieldDelegate
	{
		private WeakReference<TextBox> _textBox;

		public SinglelineTextBoxDelegate(WeakReference<TextBox> textbox)
		{
			_textBox = textbox;
		}

		public bool IsKeyboardHiddenOnEnter
		{
			get;
			set;
		}

		public override bool ShouldChangeCharacters(UITextField textField, NSRange range, string replacementString)
		{
			var textBoxView = textField as SinglelineTextBoxView;
			if (textBoxView != null)
			{
				if (_textBox.GetTarget()?.OnKey(replacementString.FirstOrDefault()) ?? false)
                {
                    return false;
                }

                if (_textBox.GetTarget()?.MaxLength > 0)
				{
					var newLength = textBoxView.Text.Length + replacementString.Length - range.Length;
					return newLength <= _textBox.GetTarget()?.MaxLength;
				};

				if (_textBox.GetTarget() is not TextBox textBox)
				{
					return false;
				}

				// Both IsReadOnly = true and IsTabStop = false can prevent editing
				return !textBox.IsReadOnly && textBox.IsTabStop;
			}

			return true;
		}

		public override bool ShouldReturn(UITextField textField)
		{
			if (IsKeyboardHiddenOnEnter)
			{
				_ = CoreDispatcher.Main.RunAsync(CoreDispatcherPriority.Normal,
					async () =>
					{
						// Delay losing focus to avoid concurrent interactions when transferring focus to another control. See 101152
						await Task.Delay(TimeSpan.FromMilliseconds(50));
						textField.ResignFirstResponder();
					});
			}

			var textBox = textField as SinglelineTextBoxView;
			if (textBox != null)
			{
                if (_textBox.GetTarget()?.OnKey('\n') ?? false)
                {
                    return false;
                }
			}

			return true;
		}

		/// <summary>
		/// Corresponds to a gain of focus
		/// </summary>
		public override void EditingStarted(UITextField textField)
		{
			if (_textBox.GetTarget() is TextBox textBox && textBox.FocusState == FocusState.Unfocused)
			{
				textBox.Focus(FocusState.Pointer);
			}
		}

		/// <summary>
		/// Corresponds to a loss of focus
		/// </summary>
		public override void EditingEnded(UITextField textField)
		{
			if (_textBox.GetTarget() is TextBox textBox && textBox.FocusState != FocusState.Unfocused)
			{
				textBox.Unfocus();
			}
		}
	}
}

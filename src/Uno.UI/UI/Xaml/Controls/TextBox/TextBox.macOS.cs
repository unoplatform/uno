using Windows.System;
using AppKit;
using CoreGraphics;
using Uno.UI.Extensions;
using Uno.Extensions;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Input;
using Uno.Foundation.Logging;

namespace Windows.UI.Xaml.Controls
{
	public partial class TextBox
	{
		private readonly bool _isPassword;
		private ITextBoxView _textBoxView;
		private TextBoxView _revealView;
		private bool _isSecured = true;

		protected TextBox(bool isPassword)
		{
			_isPassword = isPassword;
		}

		partial void InitializePropertiesPartial()
		{
			OnTextAlignmentChanged(TextAlignment);
		}

		partial void OnFocusStateChangedPartial(FocusState focusState)
		{
			if (_textBoxView != null)
			{
				if (focusState == FocusState.Unfocused)
				{
					_textBoxView.ResignFirstResponder();
				}
				else
				{
					_textBoxView.BecomeFirstResponder();
				}
			}
		}

		public override bool BecomeFirstResponder() => _textBoxView?.BecomeFirstResponder() ?? false;

		partial void OnAcceptsReturnChangedPartial(bool newValue)
		{
			UpdateTextBoxView();
		}

		partial void OnTextWrappingChangedPartial()
		{
			UpdateTextBoxView();
		}

		partial void OnTextAlignmentChangedPartial(TextAlignment newValue)
		{
			UpdateTextBoxView();
		}

		partial void SelectPartial(int start, int length)
		{
			_textBoxView?.Select(start, length);
		}

		partial void SelectAllPartial() => Select(0, Text.Length);

		private void UpdateTextBoxView()
		{
			if (_contentElement != null)
			{
				if (_textBoxView is TextBoxView || _textBoxView is SecureTextBoxView)
				{
					return;
				}

				if (_isPassword)
				{
					_textBoxView = new SecureTextBoxView(this) { UsesSingleLineMode = true, Alignment = TextAlignment.ToNativeTextAlignment() };
					_revealView = new TextBoxView(this) { UsesSingleLineMode = true, Alignment = TextAlignment.ToNativeTextAlignment() };
					_isSecured = true;
				}
				else
				{
					var textWrapping = TextWrapping;
					var usesSingleLineMode = !(AcceptsReturn || textWrapping != TextWrapping.NoWrap);
					_textBoxView = new TextBoxView(this)
					{
						UsesSingleLineMode = usesSingleLineMode,
						LineBreakMode = textWrapping == TextWrapping.WrapWholeWords ? NSLineBreakMode.ByWordWrapping : NSLineBreakMode.CharWrapping,
						Alignment = TextAlignment.ToNativeTextAlignment()
					};
				}

				_contentElement.Content = _textBoxView;
				_textBoxView.SetTextNative(Text);
				InitializeProperties();
			}
		}

		internal bool OnKey(char key)
		{
			// TODO: include modifier info
			var keyRoutedEventArgs = new KeyRoutedEventArgs(this, key.ToVirtualKey(), VirtualKeyModifiers.None)
			{
				CanBubbleNatively = true
			};

			var downHandled = RaiseEvent(KeyDownEvent, keyRoutedEventArgs);

			keyRoutedEventArgs.Handled = false; // reset to unhandled for Up
			var upHandled = RaiseEvent(KeyUpEvent, keyRoutedEventArgs);

			return downHandled || upHandled;
		}

		partial void UpdateFontPartial()
		{
			_textBoxView?.UpdateFont();
			_revealView?.UpdateFont();
		}

		partial void OnMaxLengthChangedPartial(int newValue)
		{
			//support by MultilineTextBoxDelegate and SinglelineTextBoxDelegate
		}

		partial void OnIsReadonlyChangedPartial()
		{
			//support by MultilineTextBoxDelegate and SinglelineTextBoxDelegate
		}

		partial void OnIsTextPredictionEnabledChangedPartial(bool newValue)
		{
			// There doesn't seem to be any way to disable/enable TextPrediction without disabling/enabling SpellCheck
			if (!IsTextPredictionEnabledErrorMessageShown)
			{
				this.Log().Warn("IsTextPredictionEnabled isn't supported on macOS.");
				IsTextPredictionEnabledErrorMessageShown = true;
			}
		}

		private static bool IsTextPredictionEnabledErrorMessageShown;

		public int SelectionStart
		{
			get
			{
				if (_textBoxView?.SelectedRange == null)
				{
					return 0;
				}

				return (int)_textBoxView?.SelectedRange.Location;
			}
			set
			{
				if (_textBoxView is TextBoxView sltbv)
				{
					sltbv.SelectWithFrame(sltbv.Frame, sltbv.CurrentEditor, null, value, _textBoxView.SelectedRange.Length);
				}
				else if (_textBoxView is SecureTextBoxView securedtv)
				{
					securedtv.SelectWithFrame(securedtv.Frame, securedtv.CurrentEditor, null, value, _textBoxView.SelectedRange.Length);
				}
			}
		}

		public int SelectionLength
		{
			get
			{
				if (_textBoxView?.SelectedRange == null)
				{
					return 0;
				}

				return (int)_textBoxView?.SelectedRange.Location;
			}
			set
			{
				if (_textBoxView is TextBoxView sltbv)
				{
					sltbv.SelectWithFrame(sltbv.Frame, sltbv.CurrentEditor, null, _textBoxView.SelectedRange.Location, value);
				}
				else if (_textBoxView is SecureTextBoxView securedtv)
				{
					securedtv.SelectWithFrame(securedtv.Frame, securedtv.CurrentEditor, null, _textBoxView.SelectedRange.Location, value);
				}
			}
		}

		partial void OnForegroundColorChangedPartial(Brush newValue)
		{
			if (_textBoxView != null)
			{
				_textBoxView.Foreground = newValue;
			}
			if (_revealView != null)
			{
				_revealView.Foreground = newValue;
			}
		}

		protected void SetSecureTextEntry(bool isSecure)
		{
			if (_textBoxView == null || _revealView == null)
			{
				return;
			}

			if (_isSecured == isSecure)
			{
				return;
			}

			if (isSecure)
			{
				_contentElement.Content = _textBoxView;
			}
			else
			{
				_revealView.SetTextNative(Text);
				_revealView.Frame = _contentElement.Frame;
				_contentElement.Content = _revealView;
			}

			_isSecured = isSecure;
		}
	}
}

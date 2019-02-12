using System;
using System.Collections.Generic;
using System.Text;
using Uno.UI;
using Windows.UI.Xaml.Data;
using UIKit;
using CoreGraphics;
using Uno.UI.Extensions;
using Uno.Extensions;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Input;
using Uno.Client;
using Foundation;
using Uno.Logging;

namespace Windows.UI.Xaml.Controls
{
	public partial class TextBox
	{
		private ITextBoxView _textBoxView;

		//Only implemented in TextBox in IOS. Key events are not passed to UIViews that dont implement UIKeyInput protocol
		//http://stackoverflow.com/questions/24106882/how-do-i-get-keyboard-events-without-a-textbox

		partial void InitializePropertiesPartial()
		{
			OnTextAlignmentChanged(CreateInitialValueChangerEventArgs(TextAlignmentProperty, null, TextAlignment));
			OnReturnKeyTypeChanged(ReturnKeyType);
			OnKeyboardAppearanceChanged(KeyboardAppearance);
		}

		public override CGSize SizeThatFits(CGSize size)
		{
			size = base.SizeThatFits(size);
			size = IFrameworkElementHelper.SizeThatFits(this, size);
			return size;
		}

		partial void OnFocusStateChangedPartial(FocusState focusState)
		{
			if (_textBoxView != null)
			{
				if (focusState == FocusState.Unfocused)
				{
					if (_textBoxView.IsFirstResponder)
					{
						_textBoxView.ResignFirstResponder();
					}
				}
				else
				{
					if (!_textBoxView.IsFirstResponder)
					{
						_textBoxView.BecomeFirstResponder();
					}
				}
			}
		}

		partial void OnForegroundColorChangedPartial(Brush newValue)
		{
		}

		public override bool BecomeFirstResponder()
		{
			return (_textBoxView?.BecomeFirstResponder())
				.GetValueOrDefault(false);
		}

		partial void OnAcceptsReturnChangedPartial(DependencyPropertyChangedEventArgs e)
		{
			UpdateTextBoxView();
		}

		partial void OnTextWrappingChangedPartial(DependencyPropertyChangedEventArgs e)
		{
			UpdateTextBoxView();
		}

		partial void OnTextAlignmentChangedPartial(DependencyPropertyChangedEventArgs e)
		{
			_textBoxView?.UpdateTextAlignment();
		}

		internal MultilineTextBoxView MultilineTextBox
		{
			get
			{
				return _textBoxView as MultilineTextBoxView;
			}
		}

		private void UpdateTextBoxView()
		{
			if (_contentElement != null)
			{
				if (AcceptsReturn || TextWrapping != TextWrapping.NoWrap)
				{
					if (_textBoxView is MultilineTextBoxView)
					{
						return;
					}

					_textBoxView = new MultilineTextBoxView(this)
					.Binding("Text", new Data.Binding()
					{
						Path = "Text",
						Source = this,
						Mode = BindingMode.TwoWay
					});

					_contentElement.Content = _textBoxView;
					InitializeProperties();
				}
				else
				{
					if (_textBoxView is SinglelineTextBoxView)
					{
						return;
					}

					_textBoxView = new SinglelineTextBoxView(this)
					.Binding("Text", new Data.Binding()
					{
						Path = "Text",
						Source = this,
						Mode = BindingMode.TwoWay
					});

					_contentElement.Content = _textBoxView;
					InitializeProperties();
				}
			}
		}

		internal bool OnKey(char key)
		{
			var keyRoutedEventArgs = new KeyRoutedEventArgs()
			{
				Key = key.ToVirtualKey(),
				CanBubbleNatively = true
			};

			var downHandled = RaiseEvent(KeyDownEvent, keyRoutedEventArgs);

			keyRoutedEventArgs.Handled = false; // reset to unhandled for Up
			var upHandled = RaiseEvent(KeyUpEvent, keyRoutedEventArgs);

			return downHandled || upHandled;
		}

		partial void OnInputScopeChangedPartial(DependencyPropertyChangedEventArgs e)
		{
			this.CoerceValue(ReturnKeyTypeProperty);

			if (_textBoxView != null)
			{
				_textBoxView.KeyboardType = InputScopeHelper.ConvertInputScopeToKeyboardType((InputScope)e.NewValue);

				//If SpellCheck is enabled we already set the Capitalization.
				if (!IsSpellCheckEnabled)
				{
					_textBoxView.AutocapitalizationType = InputScopeHelper.ConvertInputScopeToCapitalization((InputScope)e.NewValue);
				}
			}
		}

		partial void UpdateFontPartial(object sender)
		{
			var textBox = sender as TextBox;
			if (textBox != null && _textBoxView != null)
			{
				_textBoxView.UpdateFont();
			}
		}

		partial void OnMaxLengthChangedPartial(DependencyPropertyChangedEventArgs e)
		{
			//support by MultilineTextBoxDelegate and SinglelineTextBoxDelegate
		}

		partial void OnIsReadonlyChangedPartial(DependencyPropertyChangedEventArgs e)
		{
			//support by MultilineTextBoxDelegate and SinglelineTextBoxDelegate
		}

		partial void OnIsSpellCheckEnabledChangedPartial(DependencyPropertyChangedEventArgs e)
		{
			if (_textBoxView != null)
			{
				var isSpellCheckEnabled = (bool)e.NewValue;

				_textBoxView.SpellCheckingType = isSpellCheckEnabled
					? UITextSpellCheckingType.Yes
					: UITextSpellCheckingType.No;

				_textBoxView.AutocorrectionType = isSpellCheckEnabled
					? UITextAutocorrectionType.Yes
					: UITextAutocorrectionType.No;

				if (isSpellCheckEnabled)
				{
					_textBoxView.AutocapitalizationType = UITextAutocapitalizationType.Sentences;
				}
			}
		}

		partial void OnIsTextPredictionEnabledChangedPartial(DependencyPropertyChangedEventArgs e)
		{
			// There doesn't seem to be any way to disable/enable TextPrediction without disabling/enabling SpellCheck
			if (!IsTextPredictionEnabledErrorMessageShown)
			{
				this.Log().Warn("IsTextPredictionEnabled isn't supported on iOS. Use IsSpellCheckeEnabled instead.");
				IsTextPredictionEnabledErrorMessageShown = true;
			}
		}
		private static bool IsTextPredictionEnabledErrorMessageShown = false;

		public int SelectionStart
		{
			get
			{
				if (_textBoxView?.SelectedTextRange == null || _textBoxView?.BeginningOfDocument == null)
				{
					return 0;
				}

				return (int)_textBoxView.GetOffsetFromPosition(
					_textBoxView.BeginningOfDocument,
					_textBoxView.SelectedTextRange.Start
				);
			}
			set
			{
				if (_textBoxView != null)
				{
					_textBoxView.SelectedTextRange = _textBoxView.GetTextRange(value, value + this.SelectionLength);
				}
			}
		}

		public int SelectionLength
		{
			get
			{
				if (_textBoxView?.SelectedTextRange == null)
				{
					return 0;
				}

				return (int)_textBoxView.GetOffsetFromPosition(
					_textBoxView.SelectedTextRange.Start,
					_textBoxView.SelectedTextRange.End
				);
			}
			set
			{
				if (_textBoxView != null)
				{
					_textBoxView.SelectedTextRange = _textBoxView.GetTextRange(this.SelectionStart, this.SelectionStart + value);
				}
			}
		}

		protected void SetSecureTextEntry(bool isSecure)
		{
			if (_textBoxView != null)
			{
				_textBoxView.SecureTextEntry = isSecure;
			}
		}

		public override UIView HitTest(CGPoint point, UIEvent uievent)
		{
			var view = base.HitTest(point, uievent);

			if (view != null)
			{
				Uno.UI.Controls.Window.SetNeedsKeyboard(view, true);
			}

			return view;
		}

		#region ReturnKeyType DependencyProperty

		public UIReturnKeyType ReturnKeyType
		{
			get { return (UIReturnKeyType)GetValue(ReturnKeyTypeProperty); }
			set { SetValue(ReturnKeyTypeProperty, value); }
		}

		public static readonly DependencyProperty ReturnKeyTypeProperty =
			DependencyProperty.Register(
				"ReturnKeyType", 
				typeof(UIReturnKeyType), 
				typeof(TextBox), 
				new FrameworkPropertyMetadata(
					UIReturnKeyType.Default, 
					(s, e) => ((TextBox)s)?.OnReturnKeyTypeChanged((UIReturnKeyType)e.NewValue),
					coerceValueCallback: CoerceReturnKeyType
				)
			);


		private static object CoerceReturnKeyType(DependencyObject dependencyObject, object baseValue)
		{
			return dependencyObject is TextBox textBox && textBox.InputScope.GetFirstInputScopeNameValue() == InputScopeNameValue.Search
				? UIReturnKeyType.Search
				: baseValue;
		}

		private void OnReturnKeyTypeChanged(UIReturnKeyType newValue)
		{
			if (_textBoxView != null)
			{
				_textBoxView.ReturnKeyType = newValue;
			}
		}

		#endregion

		#region KeyboardAppearance DependendcyProperty
		public UIKeyboardAppearance KeyboardAppearance
		{
			get { return (UIKeyboardAppearance)GetValue(KeyboardAppearanceProperty); }
			set { SetValue(KeyboardAppearanceProperty, value); }
		}

		public static readonly DependencyProperty KeyboardAppearanceProperty =
			DependencyProperty.Register(
				"KeyboardAppearance",
				typeof(UIKeyboardAppearance),
				typeof(TextBox),
				new PropertyMetadata(
					UIKeyboardAppearance.Default,
					(s, e) => ((TextBox)s)?.OnKeyboardAppearanceChanged((UIKeyboardAppearance)e.NewValue)
				)
			);

		private void OnKeyboardAppearanceChanged(UIKeyboardAppearance newValue)
		{
			if(_textBoxView != null)
			{
				_textBoxView.KeyboardAppearance = newValue;
			}
		}

		#endregion
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Windows.System;
using Uno.UI;
using Windows.UI.Xaml.Data;
using UIKit;
using CoreGraphics;
using Uno.UI.Extensions;
using Uno.Extensions;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Input;
using Foundation;
using Uno.Foundation.Logging;

namespace Windows.UI.Xaml.Controls
{
	public partial class TextBox
	{
		private ITextBoxView _textBoxView;

		//Only implemented in TextBox in IOS. Key events are not passed to UIViews that don't implement UIKeyInput protocol
		//http://stackoverflow.com/questions/24106882/how-do-i-get-keyboard-events-without-a-textbox

		partial void InitializePropertiesPartial()
		{
			OnTextAlignmentChanged(TextAlignment);
			OnReturnKeyTypeChanged(ReturnKeyType);
			OnKeyboardAppearanceChanged(KeyboardAppearance);
			UpdateKeyboardThemePartial();
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

		public override bool BecomeFirstResponder()
		{
			return (_textBoxView?.BecomeFirstResponder())
				.GetValueOrDefault(false);
		}

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
			_textBoxView?.UpdateTextAlignment();
		}

		partial void SelectPartial(int start, int length)
		{
			if (_textBoxView != null)
			{
				_textBoxView.Select(start, length);
			}
		}

		partial void SelectAllPartial() => Select(0, Text.Length);

		internal MultilineTextBoxView MultilineTextBox
		{
			get
			{
				return _textBoxView as MultilineTextBoxView;
			}
		}

		/// <summary>
		/// Gets or sets whether the focus should be kept on the TextBox when the user taps outside of it.
		/// </summary>
		/// <remarks>
		/// In some cases, like when the TextBox is inside an <see cref="AutoSuggestBox"/>,
		/// the focus must be kept on the TextBox when making a selection.
		/// </remarks>
		/// Fix issue # https://github.com/unoplatform/uno/issues/11961
		internal bool IsKeepingFocusOnEndEditing { get; set; }

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

					_textBoxView = new MultilineTextBoxView(this);

					_contentElement.Content = _textBoxView;
					_textBoxView.SetTextNative(Text);
					InitializeProperties();
				}
				else
				{
					if (_textBoxView is SinglelineTextBoxView)
					{
						return;
					}

					_textBoxView = new SinglelineTextBoxView(this);

					_contentElement.Content = _textBoxView;
					_textBoxView.SetTextNative(Text);
					InitializeProperties();
				}
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

		partial void OnInputScopeChangedPartial(InputScope newValue)
		{
			this.CoerceValue(ReturnKeyTypeProperty);

			if (_textBoxView != null)
			{
				_textBoxView.KeyboardType = InputScopeHelper.ConvertInputScopeToKeyboardType(newValue);

				//If SpellCheck is enabled we already set the Capitalization.
				if (!IsSpellCheckEnabled)
				{
					_textBoxView.AutocapitalizationType = InputScopeHelper.ConvertInputScopeToCapitalization(newValue);
				}
			}
		}

		partial void UpdateFontPartial()
		{
			if (_textBoxView != null)
			{
				_textBoxView.UpdateFont();
			}
		}

		partial void OnMaxLengthChangedPartial(int newValue)
		{
			//support by MultilineTextBoxDelegate and SinglelineTextBoxDelegate
		}

		partial void OnIsReadonlyChangedPartial()
		{
			//support by MultilineTextBoxDelegate and SinglelineTextBoxDelegate
		}

		partial void OnSelectionHighlightColorChangedPartial(SolidColorBrush brush)
		{
			if (_textBoxView != null && brush != null)
			{
				_textBoxView.TintColor = brush.ColorWithOpacity;
			}
		}

		partial void OnIsSpellCheckEnabledChangedPartial(bool newValue)
		{
			if (_textBoxView != null)
			{
				_textBoxView.SpellCheckingType = newValue
					? UITextSpellCheckingType.Yes
					: UITextSpellCheckingType.No;

				_textBoxView.AutocorrectionType = newValue
					? UITextAutocorrectionType.Yes
					: UITextAutocorrectionType.No;

				if (newValue)
				{
					_textBoxView.AutocapitalizationType = UITextAutocapitalizationType.Sentences;
				}
			}
		}

		partial void OnIsTextPredictionEnabledChangedPartial(bool newValue)
		{
			// There doesn't seem to be any way to disable/enable TextPrediction without disabling/enabling SpellCheck
			if (!IsTextPredictionEnabledErrorMessageShown)
			{
				this.Log().Warn("IsTextPredictionEnabled isn't supported on iOS. Use IsSpellCheckeEnabled instead.");
				IsTextPredictionEnabledErrorMessageShown = true;
			}
		}
		private static bool IsTextPredictionEnabledErrorMessageShown;

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

		public static DependencyProperty ReturnKeyTypeProperty { get; } =
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


		private static object CoerceReturnKeyType(DependencyObject dependencyObject, object baseValue, DependencyPropertyValuePrecedences _)
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

		public static DependencyProperty KeyboardAppearanceProperty { get; } =
			DependencyProperty.Register(
				"KeyboardAppearance",
				typeof(UIKeyboardAppearance),
				typeof(TextBox),
				new FrameworkPropertyMetadata(
					UIKeyboardAppearance.Default,
					(s, e) => ((TextBox)s)?.OnKeyboardAppearanceChanged((UIKeyboardAppearance)e.NewValue)
				)
			);

		private void OnKeyboardAppearanceChanged(UIKeyboardAppearance newValue)
		{
			if (_textBoxView != null)
			{
				_textBoxView.KeyboardAppearance = newValue;
			}
		}

		#endregion

		partial void UpdateKeyboardThemePartial()
		{
			if (_textBoxView == null) return;

			// if KeyboardAppearance has been explicitly set, we leave it as is.
			if (KeyboardAppearance != UIKeyboardAppearance.Default) return;

			ElementTheme? GetExplicitlySetAppTheme() => Application.Current.IsThemeSetExplicitly
				? Application.Current.ActualElementTheme as ElementTheme?
				: null;

			// the appearance will be determined by the first parent/self that has a non-default RequestedTheme,
			// or the explicitly requested application theme.
			// note: the literal "Default" value is lost on the ActualTheme property, which is why we are not using it here.
			var theme = VisualTreeHelper.EnumerateAncestors(this).OfType<FrameworkElement>()
				.Prepend(this)
				.Select(x => x.RequestedTheme as ElementTheme?)
				.Append(GetExplicitlySetAppTheme())
				.FirstOrDefault(x => x != ElementTheme.Default);
			var appearance = theme switch
			{
				ElementTheme.Light => UIKeyboardAppearance.Light,
				ElementTheme.Dark => UIKeyboardAppearance.Dark,

				_ => UIKeyboardAppearance.Default
			};

			_textBoxView.KeyboardAppearance = appearance;
		}
	}
}

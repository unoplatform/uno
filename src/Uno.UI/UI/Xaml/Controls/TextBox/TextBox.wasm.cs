using Uno.Extensions;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;

namespace Windows.UI.Xaml.Controls
{
	public partial class TextBox : Control
	{
		private TextBoxView _textBoxView;
		
		protected override bool IsDelegatingFocusToTemplateChild() => true; // _textBoxView
		partial void OnTextClearedPartial() => FocusTextView();
		internal bool FocusTextView() => FocusManager.FocusNative(_textBoxView);

		private void UpdateTextBoxView()
		{
			if (_contentElement != null)
			{
				if (AcceptsReturn || TextWrapping != TextWrapping.NoWrap)
				{
					if (_textBoxView != null && _textBoxView.IsMultiline)
					{
						return;
					}

					_textBoxView = new TextBoxView(this, isMultiline: true);

					_contentElement.Content = _textBoxView;
					InitializeProperties();
				}
				else
				{
					if (_textBoxView != null && !_textBoxView.IsMultiline)
					{
						return;
					}

					_textBoxView = new TextBoxView(this, isMultiline: false);

					_contentElement.Content = _textBoxView;
					InitializeProperties();
				}
			}
		}

		partial void InitializePropertiesPartial()
		{
			if (_header != null)
			{
				AddHandler(PointerReleasedEvent, (PointerEventHandler)OnHeaderClick, true);
			}
		}

		private void OnHeaderClick(object sender, object args)
		{
			FocusTextView();
		}

		protected void SetIsPassword(bool isPassword)
		{
			if (_textBoxView != null)
			{
				_textBoxView.SetIsPassword(isPassword);
			}
		}

		partial void OnForegroundColorChangedPartial(Brush newValue)
		{
			_textBoxView?.SetForeground(newValue);
		}

		partial void UpdateFontPartial()
		{
			if(_textBoxView == null)
			{
				return;
			}

			_textBoxView.SetFontSize(FontSize);
			_textBoxView.SetFontStyle(FontStyle);
			_textBoxView.SetFontWeight(FontWeight);
			_textBoxView.SetFontFamily(FontFamily);
		}

		partial void OnTextWrappingChangedPartial(DependencyPropertyChangedEventArgs e)
		{
			_textBoxView?.SetTextWrappingAndTrimming(textWrapping: TextWrapping, textTrimming: null);
		}

		partial void OnTextAlignmentChangedPartial(DependencyPropertyChangedEventArgs e)
		{
			_textBoxView?.SetTextAlignment(TextAlignment);
		}

		partial void OnIsSpellCheckEnabledChangedPartial(DependencyPropertyChangedEventArgs e)
		{
			_textBoxView?.SetAttribute("spellcheck", IsSpellCheckEnabled.ToString());
		}

		private protected override void OnIsEnabledChanged(IsEnabledChangedEventArgs e)
		{
			base.OnIsEnabledChanged(e);

			ApplyEnabled(e.NewValue);
		}

		partial void OnIsReadonlyChangedPartial(DependencyPropertyChangedEventArgs e)
		{
			if (e.NewValue is bool isReadonly)
			{
				ApplyIsReadonly(isReadonly);
			}
		}

		private void ApplyEnabled(bool? isEnabled = null) => _textBoxView?.SetEnabled(isEnabled ?? IsEnabled);

		private void ApplyIsReadonly(bool? isReadOnly = null) => _textBoxView?.SetIsReadOnly(isReadOnly ?? IsReadOnly);

		public int SelectionStart
		{
			get => _textBoxView?.SelectionStart ?? 0;
			set => _textBoxView.Maybe(tbv => tbv.SelectionStart = value);
		}

		public int SelectionLength
		{
			get
			{
				if (_textBoxView == null)
				{
					return 0;
				}

				return _textBoxView.SelectionEnd - _textBoxView.SelectionStart;
			}
			set
			{
				if (_textBoxView == null)
				{
					return;
				}

				_textBoxView.SelectionEnd = _textBoxView.SelectionStart + value;
			}
		}
	}
}

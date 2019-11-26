using Uno.Extensions;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using Uno.Disposables;
using System.Text;
using Uno.UI;
using Windows.UI.Core;
using Windows.UI.Xaml.Media;
using System.Threading.Tasks;
using Windows.UI.Text;
using Uno.Logging;
using Uno.UI.UI.Xaml.Documents;

namespace Windows.UI.Xaml.Controls
{
	public partial class TextBox : Control
	{
		private TextBoxView _textBoxView;
		
		protected override bool IsDelegatingFocusToTemplateChild() => true; // _textBoxView
		protected override bool RequestFocus(FocusState state) => FocusTextView();
		partial void OnTextClearedPartial() => FocusTextView();
		protected virtual bool FocusTextView() => FocusManager.Focus(_textBoxView);




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
			ApplyEnabled();

			if(_header != null)
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

		partial void OnIsReadonlyChangedPartial(DependencyPropertyChangedEventArgs e)
		{
			if(e.NewValue is bool isReadonly)
			{
				if (isReadonly)
				{
					_textBoxView?.SetAttribute("readonly", "readonly");
				}
				else
				{
					_textBoxView?.RemoveAttribute("readonly");
				}
			}
		}

		partial void OnForegroundColorChangedPartial(Brush newValue)
		{
			_textBoxView?.SetForeground(newValue);
		}

		partial void UpdateFontPartial()
		{
			_textBoxView?.SetFontSize(FontSize);
			_textBoxView?.SetFontStyle(FontStyle);
			_textBoxView?.SetFontWeight(FontWeight);
			_textBoxView?.SetFontFamily(FontFamily);
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

		protected override void OnIsEnabledChanged(bool oldValue, bool newValue)
		{
			base.OnIsEnabledChanged(oldValue, newValue);

			ApplyEnabled();
		}

		private void ApplyEnabled() => _textBoxView?.SetEnabled(IsEnabled);

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
